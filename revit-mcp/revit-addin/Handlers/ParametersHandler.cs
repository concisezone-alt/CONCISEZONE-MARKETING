using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RevitMCP.Addin.Handlers
{
    public class ParametersHandler
    {
        private readonly CommandQueue _queue;

        public ParametersHandler(CommandQueue queue) => _queue = queue;

        public object GetParameters(int elementId)
        {
            return _queue.Run(doc =>
            {
                var elem = doc.GetElement(new ElementId(elementId))
                    ?? throw new RevitApiException($"Element {elementId} not found.");

                return new
                {
                    id = elementId,
                    name = elem.Name,
                    category = elem.Category?.Name ?? "Unknown",
                    parameters = elem.Parameters
                        .Cast<Parameter>()
                        .Select(p => SerializeParameter(p))
                        .OrderBy(p => p.group)
                        .ThenBy(p => p.name)
                        .ToList()
                };
            });
        }

        public object SetParameter(int elementId, string body)
        {
            var req = JsonConvert.DeserializeObject<JObject>(body);
            var paramName = req["name"]?.ToString()
                ?? throw new RevitApiException("Missing 'name' field.");
            var value = req["value"]?.ToString()
                ?? throw new RevitApiException("Missing 'value' field.");

            return _queue.Run(doc =>
            {
                var elem = doc.GetElement(new ElementId(elementId))
                    ?? throw new RevitApiException($"Element {elementId} not found.");

                // Try by name first, then by GUID for shared params
                var param = FindParameter(elem, paramName);
                if (param == null)
                    throw new RevitApiException($"Parameter '{paramName}' not found on element {elementId}.");
                if (param.IsReadOnly)
                    throw new RevitApiException($"Parameter '{paramName}' is read-only.");

                using (var tx = new Transaction(doc, $"MCP: Set {paramName}"))
                {
                    tx.Start();
                    SetParameterValue(param, value);
                    tx.Commit();
                }

                return new { success = true, element_id = elementId, parameter = paramName, new_value = value };
            });
        }

        public object AddParameter(int elementId, string body)
        {
            var req = JsonConvert.DeserializeObject<JObject>(body);
            var paramName = req["name"]?.ToString()
                ?? throw new RevitApiException("Missing 'name' field.");
            var paramType = req["type"]?.ToString() ?? "Text";
            var group = req["group"]?.ToString() ?? "Identity Data";
            var value = req["value"]?.ToString();
            var sharedParamFilePath = req["shared_param_file"]?.ToString();

            return _queue.Run(doc =>
            {
                var elem = doc.GetElement(new ElementId(elementId))
                    ?? throw new RevitApiException($"Element {elementId} not found.");

                // Check if already exists
                if (FindParameter(elem, paramName) != null)
                {
                    // If it already exists and we have a value, just set it
                    if (value != null)
                    {
                        using (var tx = new Transaction(doc, $"MCP: Set existing {paramName}"))
                        {
                            tx.Start();
                            SetParameterValue(FindParameter(elem, paramName), value);
                            tx.Commit();
                        }
                    }
                    return new { success = true, action = "updated_existing", element_id = elementId, parameter = paramName };
                }

                // Add as project parameter via shared parameter file
                var spFile = sharedParamFilePath ?? GetOrCreateSharedParamFile();
                AddSharedParameter(doc, elem, paramName, paramType, group, spFile, value);

                return new { success = true, action = "created", element_id = elementId, parameter = paramName };
            });
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static Parameter FindParameter(Element elem, string name)
        {
            foreach (Parameter p in elem.Parameters)
                if (string.Equals(p.Definition.Name, name, StringComparison.OrdinalIgnoreCase))
                    return p;
            return null;
        }

        private static void SetParameterValue(Parameter p, string value)
        {
            switch (p.StorageType)
            {
                case StorageType.String:
                    p.Set(value);
                    break;
                case StorageType.Double:
                    if (!double.TryParse(value, out var d))
                        throw new RevitApiException($"Cannot convert '{value}' to number.");
                    // Revit stores lengths in feet internally — convert from the param's display unit
                    try
                    {
                        var unitTypeId = p.GetUnitTypeId();
                        if (unitTypeId != null && unitTypeId != UnitTypeId.Custom)
                            d = UnitUtils.ConvertToInternalUnits(d, unitTypeId);
                    }
                    catch { /* param has no unit (dimensionless) — use value as-is */ }
                    p.Set(d);
                    break;
                case StorageType.Integer:
                    if (int.TryParse(value, out var i)) p.Set(i);
                    else throw new RevitApiException($"Cannot convert '{value}' to integer.");
                    break;
                case StorageType.ElementId:
                    if (int.TryParse(value, out var eid)) p.Set(new ElementId(eid));
                    else throw new RevitApiException($"Cannot convert '{value}' to ElementId.");
                    break;
                default:
                    throw new RevitApiException($"Unsupported storage type: {p.StorageType}");
            }
        }

        private static void AddSharedParameter(
            Document doc, Element elem, string paramName,
            string paramType, string groupName, string spFilePath, string value)
        {
            var app = doc.Application;
            var origFile = app.SharedParametersFilename;
            app.SharedParametersFilename = spFilePath;

            var spFile = app.OpenSharedParameterFile()
                ?? throw new RevitApiException("Cannot open shared parameter file.");

            // Get or create group
            var group = spFile.Groups.get_Item(groupName)
                     ?? spFile.Groups.Create(groupName);

            // Get or create definition
            var def = group.Definitions.get_Item(paramName);
            if (def == null)
            {
                var specType = ResolveSpecType(paramType);
                var opts = new ExternalDefinitionCreationOptions(paramName, specType);
                def = group.Definitions.Create(opts);
            }

            // Bind to the element's category
            var catSet = new CategorySet();
            catSet.Insert(elem.Category);
            var binding = new InstanceBinding(catSet);

            var paramGroup = LabelUtils.GetLabelForGroup(BuiltInParameterGroup.PG_IDENTITY_DATA);
            doc.ParameterBindings.Insert(def, binding, BuiltInParameterGroup.PG_IDENTITY_DATA);

            app.SharedParametersFilename = origFile;

            // Set value if provided
            if (value != null)
            {
                var param = FindParameter(elem, paramName);
                if (param != null) SetParameterValue(param, value);
            }
        }

        private static ForgeTypeId ResolveSpecType(string typeName)
        {
            return typeName?.ToLower() switch
            {
                "text" => SpecTypeId.String.Text,
                "integer" or "int" => SpecTypeId.Int.Integer,
                "number" or "double" => SpecTypeId.Number,
                "length" => SpecTypeId.Length,
                "area" => SpecTypeId.Area,
                "volume" => SpecTypeId.Volume,
                "url" => SpecTypeId.String.Url,
                _ => SpecTypeId.String.Text
            };
        }

        private static string GetOrCreateSharedParamFile()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RevitMCP");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "RevitMCP_SharedParams.txt");
            if (!File.Exists(path))
                File.WriteAllText(path, "# This file is used by RevitMCP to store shared parameters\r\n");
            return path;
        }

        private static dynamic SerializeParameter(Parameter p)
        {
            string displayValue;
            try { displayValue = p.AsValueString() ?? p.AsString() ?? p.AsDouble().ToString("G6"); }
            catch { displayValue = "<error reading value>"; }

            return new
            {
                name = p.Definition.Name,
                group = p.Definition.GetGroupTypeId()?.TypeId ?? "unknown",
                storage_type = p.StorageType.ToString(),
                is_read_only = p.IsReadOnly,
                is_shared = p.IsShared,
                value = displayValue,
                guid = p.IsShared ? p.GUID.ToString() : null
            };
        }
    }
}
