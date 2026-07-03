using System.Collections.Specialized;

namespace RevitMCP.Addin.Handlers
{
    public class Router
    {
        private readonly ElementsHandler _elements;
        private readonly ParametersHandler _parameters;
        private readonly RoomsHandler _rooms;
        private readonly ModelHandler _model;

        public Router(CommandQueue queue)
        {
            _elements = new ElementsHandler(queue);
            _parameters = new ParametersHandler(queue);
            _rooms = new RoomsHandler(queue);
            _model = new ModelHandler(queue);
        }

        public object Dispatch(string method, string path, NameValueCollection qs, string body)
        {
            // GET /elements?category=Walls&level=Ground+Floor
            if (method == "GET" && path == "/elements")
                return _elements.GetElements(qs["category"], qs["level"]);

            // GET /element/{id}/parameters
            if (method == "GET" && path.StartsWith("/element/") && path.EndsWith("/parameters"))
            {
                var id = ParseId(path, "/element/", "/parameters");
                return _parameters.GetParameters(id);
            }

            // POST /element/{id}/set-parameter
            if (method == "POST" && path.StartsWith("/element/") && path.EndsWith("/set-parameter"))
            {
                var id = ParseId(path, "/element/", "/set-parameter");
                return _parameters.SetParameter(id, body);
            }

            // POST /element/{id}/add-parameter
            if (method == "POST" && path.StartsWith("/element/") && path.EndsWith("/add-parameter"))
            {
                var id = ParseId(path, "/element/", "/add-parameter");
                return _parameters.AddParameter(id, body);
            }

            // GET /rooms
            if (method == "GET" && path == "/rooms")
                return _rooms.GetRooms(qs["level"]);

            // GET /room/{id}/parameters
            if (method == "GET" && path.StartsWith("/room/") && path.EndsWith("/parameters"))
            {
                var id = ParseId(path, "/room/", "/parameters");
                return _rooms.GetRoomParameters(id);
            }

            // POST /room/{id}/set-parameter
            if (method == "POST" && path.StartsWith("/room/") && path.EndsWith("/set-parameter"))
            {
                var id = ParseId(path, "/room/", "/set-parameter");
                return _parameters.SetParameter(id, body);
            }

            // GET /model/warnings
            if (method == "GET" && path == "/model/warnings")
                return _model.GetWarnings();

            // GET /model/info
            if (method == "GET" && path == "/model/info")
                return _model.GetInfo();

            // GET /model/levels
            if (method == "GET" && path == "/model/levels")
                return _model.GetLevels();

            // GET /health
            if (method == "GET" && path == "/health")
                return new { status = "ok", version = "1.0.0" };

            throw new RevitApiException($"Unknown route: {method} {path}");
        }

        private static int ParseId(string path, string prefix, string suffix)
        {
            var s = path.Substring(prefix.Length, path.Length - prefix.Length - suffix.Length);
            if (int.TryParse(s, out var id)) return id;
            throw new RevitApiException($"Invalid element id in path: {path}");
        }
    }
}
