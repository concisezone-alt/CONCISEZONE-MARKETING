using Autodesk.Revit.DB;
using System;
using System.Linq;

namespace RevitMCP.Addin.Handlers
{
    public class ModelHandler
    {
        private readonly CommandQueue _queue;

        public ModelHandler(CommandQueue queue) => _queue = queue;

        public object GetInfo()
        {
            return _queue.Run(doc => new
            {
                title = doc.Title,
                path = doc.PathName,
                is_workshared = doc.IsWorkshared,
                project_number = GetProjectInfo(doc, BuiltInParameter.PROJECT_NUMBER),
                project_name = GetProjectInfo(doc, BuiltInParameter.PROJECT_NAME),
                project_status = GetProjectInfo(doc, BuiltInParameter.PROJECT_STATUS),
                client_name = GetProjectInfo(doc, BuiltInParameter.CLIENT_NAME),
                author = GetProjectInfo(doc, BuiltInParameter.PROJECT_AUTHOR),
                building_name = GetProjectInfo(doc, BuiltInParameter.PROJECT_BUILDING_NAME),
                revit_version = doc.Application.VersionNumber,
                element_count = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType().GetElementCount()
            });
        }

        public object GetLevels()
        {
            return _queue.Run(doc =>
            {
                return new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .Select(l => new
                    {
                        id = l.Id.IntegerValue,
                        name = l.Name,
                        elevation_m = Math.Round(l.Elevation * 0.3048, 3), // ft → m
                        elevation_ft = Math.Round(l.Elevation, 3)
                    }).ToList();
            });
        }

        public object GetWarnings()
        {
            return _queue.Run(doc =>
            {
                var warnings = doc.GetWarnings();
                return new
                {
                    total = warnings.Count,
                    warnings = warnings.Select(w => new
                    {
                        severity = w.GetSeverity().ToString(),
                        description = w.GetDescriptionText(),
                        failing_elements = w.GetFailingElements()
                            .Select(id => new
                            {
                                id = id.IntegerValue,
                                name = doc.GetElement(id)?.Name ?? "Unknown",
                                category = doc.GetElement(id)?.Category?.Name ?? "Unknown"
                            }).ToList()
                    })
                    .GroupBy(w => w.description)
                    .Select(g => new
                    {
                        count = g.Count(),
                        description = g.Key,
                        severity = g.First().severity,
                        samples = g.Take(3).SelectMany(w => w.failing_elements).ToList()
                    })
                    .OrderByDescending(w => w.count)
                    .ToList()
                };
            });
        }

        private static string GetProjectInfo(Document doc, BuiltInParameter bip)
        {
            try
            {
                return doc.ProjectInformation?.get_Parameter(bip)?.AsString() ?? string.Empty;
            }
            catch { return string.Empty; }
        }
    }
}
