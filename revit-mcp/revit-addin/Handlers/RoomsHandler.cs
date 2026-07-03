using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCP.Addin.Handlers
{
    public class RoomsHandler
    {
        private readonly CommandQueue _queue;

        public RoomsHandler(CommandQueue queue) => _queue = queue;

        public object GetRooms(string levelName)
        {
            return _queue.Run(doc =>
            {
                var rooms = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .Cast<Room>()
                    .Where(r => r.Area > 0); // placed rooms only

                if (!string.IsNullOrEmpty(levelName))
                    rooms = rooms.Where(r =>
                        (doc.GetElement(r.LevelId) as Level)?.Name
                            .Equals(levelName, StringComparison.OrdinalIgnoreCase) == true);

                return rooms.Select(r => new
                {
                    id = r.Id.IntegerValue,
                    number = r.Number,
                    name = r.Name,
                    level = (doc.GetElement(r.LevelId) as Level)?.Name ?? string.Empty,
                    area_sqm = Math.Round(r.Area * 0.092903, 3), // sq ft → sq m
                    area_sqft = Math.Round(r.Area, 3),
                    perimeter = Math.Round(r.Perimeter, 3),
                    department = GetParamStr(r, BuiltInParameter.ROOM_DEPARTMENT),
                    phase = (doc.GetElement(r.phaseId) as Phase)?.Name ?? string.Empty,
                    is_redundant = r.Area < 0.01,
                    // IFC-relevant fields
                    ifc_export_element = GetParamStr(r, BuiltInParameter.IFC_EXPORT_ELEMENT_AS),
                    occupancy = GetParamStr(r, BuiltInParameter.ROOM_OCCUPANCY),
                    comments = GetParamStr(r, BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)
                }).OrderBy(r => r.level).ThenBy(r => r.number).ToList();
            });
        }

        public object GetRoomParameters(int roomId)
        {
            return _queue.Run(doc =>
            {
                var room = doc.GetElement(new ElementId(roomId)) as Room
                    ?? throw new RevitApiException($"Room {roomId} not found or is not a Room element.");

                var parameters = room.Parameters
                    .Cast<Parameter>()
                    .Select(p =>
                    {
                        string val;
                        try { val = p.AsValueString() ?? p.AsString(); }
                        catch { val = null; }

                        return new
                        {
                            name = p.Definition.Name,
                            group = p.Definition.GetGroupTypeId()?.TypeId ?? "unknown",
                            storage_type = p.StorageType.ToString(),
                            is_read_only = p.IsReadOnly,
                            is_shared = p.IsShared,
                            value = val,
                            guid = p.IsShared ? p.GUID.ToString() : null
                        };
                    })
                    .OrderBy(p => p.group).ThenBy(p => p.name)
                    .ToList();

                return new
                {
                    id = roomId,
                    number = room.Number,
                    name = room.Name,
                    level = (doc.GetElement(room.LevelId) as Level)?.Name,
                    area_sqm = Math.Round(room.Area * 0.092903, 3),
                    parameter_count = parameters.Count,
                    parameters
                };
            });
        }

        private static string GetParamStr(Element e, BuiltInParameter bip)
        {
            try { return e.get_Parameter(bip)?.AsString() ?? string.Empty; }
            catch { return string.Empty; }
        }
    }
}
