using Newtonsoft.Json;
using RevitMCP.Addin.Handlers;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RevitMCP.Addin
{
    public class HttpServer
    {
        private readonly HttpListener _listener;
        private readonly CommandQueue _queue;
        private readonly Router _router;
        private CancellationTokenSource _cts;

        private readonly string _authToken =
            Environment.GetEnvironmentVariable("REVITMCP_TOKEN") ?? "changeme";

        public HttpServer(CommandQueue queue, int port)
        {
            _queue = queue;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _router = new Router(queue);
        }

        public void Start()
        {
            _listener.Start();
            _cts = new CancellationTokenSource();
            Task.Run(() => Listen(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
        }

        private async Task Listen(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try { ctx = await _listener.GetContextAsync(); }
                catch { break; }

                _ = Task.Run(() => Handle(ctx), ct);
            }
        }

        private void Handle(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            var res = ctx.Response;

            try
            {
                // Auth check
                var auth = req.Headers["Authorization"] ?? string.Empty;
                if (!auth.Equals($"Bearer {_authToken}", StringComparison.Ordinal))
                {
                    SendJson(res, 401, new { error = "Unauthorized" });
                    return;
                }

                // Read body
                string body = string.Empty;
                if (req.HasEntityBody)
                    using (var sr = new StreamReader(req.InputStream, req.ContentEncoding))
                        body = sr.ReadToEnd();

                var result = _router.Dispatch(req.HttpMethod, req.Url.AbsolutePath, req.QueryString, body);
                SendJson(res, 200, result);
            }
            catch (RevitApiException ex)
            {
                App.Log($"Revit API error: {ex}");
                SendJson(res, 422, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                App.Log($"Unhandled error: {ex}");
                SendJson(res, 500, new { error = ex.Message });
            }
        }

        private static void SendJson(HttpListenerResponse res, int status, object data)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(json);
            res.StatusCode = status;
            res.ContentType = "application/json; charset=utf-8";
            res.ContentLength64 = bytes.Length;
            res.OutputStream.Write(bytes, 0, bytes.Length);
            res.OutputStream.Close();
        }
    }

    public class RevitApiException : Exception
    {
        public RevitApiException(string msg) : base(msg) { }
    }
}
