using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCP.Addin.Handlers
{
    public class ElementsHandler
    {
        private readonly CommandQueue _queue;

        public ElementsHandler(CommandQueue queue) => _queue = queue;

        public object GetElements(string category, string levelName)
        {
            return _queue.Run(doc =>
            {
                var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();

                // Filter by BuiltInCategory if provided
                if (!string.IsNullOrEmpty(category))
                {
                    if (TryParseCategory(category, out var bic))
                        collector = collector.OfCategory(bic);
                    else
                        throw new RevitApiException($"Unknown category: {category}. Use e.g. Walls, Doors, Rooms, Floors, Windows, Columns, Roofs, Stairs, Ceilings.");
                }

                var elements = collector.ToElements();

                // Filter by level if provided
                if (!string.IsNullOrEmpty(levelName))
                {
                    var levelId = GetLevelId(doc, levelName);
                    elements = elements.Where(e =>
                    {
                        var lvlParam = e.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)
                                    ?? e.get_Parameter(BuiltInParameter.ROOM_LEVEL_ID);
                        return lvlParam != null && lvlParam.AsElementId() == levelId;
                    }).ToList();
                }

                return elements.Select(e => new
                {
                    id = e.Id.IntegerValue,
                    name = e.Name,
                    category = e.Category?.Name ?? "Unknown",
                    level = GetLevelName(doc, e),
                    type_id = e.GetTypeId()?.IntegerValue,
                    type_name = doc.GetElement(e.GetTypeId())?.Name ?? string.Empty
                }).ToList();
            });
        }

        private static bool TryParseCategory(string name, out BuiltInCategory result)
        {
            var map = new Dictionary<string, BuiltInCategory>(StringComparer.OrdinalIgnoreCase)
            {
                ["Walls"] = BuiltInCategory.OST_Walls,
                ["Doors"] = BuiltInCategory.OST_Doors,
                ["Windows"] = BuiltInCategory.OST_Windows,
                ["Rooms"] = BuiltInCategory.OST_Rooms,
                ["Floors"] = BuiltInCategory.OST_Floors,
                ["Roofs"] = BuiltInCategory.OST_Roofs,
                ["Ceilings"] = BuiltInCategory.OST_Ceilings,
                ["Columns"] = BuiltInCategory.OST_Columns,
                ["StructuralColumns"] = BuiltInCategory.OST_StructuralColumns,
                ["Stairs"] = BuiltInCategory.OST_Stairs,
                ["Railings"] = BuiltInCategory.OST_StairsRailing,
                ["Furniture"] = BuiltInCategory.OST_Furniture,
                ["MechanicalEquipment"] = BuiltInCategory.OST_MechanicalEquipment,
                ["ElectricalEquipment"] = BuiltInCategory.OST_ElectricalEquipment,
                ["Plumbing"] = BuiltInCategory.OST_PlumbingFixtures,
                ["GenericModels"] = BuiltInCategory.OST_GenericModel,
                ["Spaces"] = BuiltInCategory.OST_MEPSpaces,
            };
            return map.TryGetValue(name, out result);
        }

        private static ElementId GetLevelId(Document doc, string name)
        {
            var level = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (level == null) throw new RevitApiException($"Level not found: {name}");
            return level.Id;
        }

        private static string GetLevelName(Document doc, Element e)
        {
            var param = e.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)
                     ?? e.get_Parameter(BuiltInParameter.ROOM_LEVEL_ID);
            if (param == null) return string.Empty;
            var lvl = doc.GetElement(param.AsElementId()) as Level;
            return lvl?.Name ?? string.Empty;
        }
    }
}
