using Autodesk.Revit.UI;
using System;
using System.IO;

namespace RevitMCP.Addin
{
    public class App : IExternalApplication
    {
        internal static HttpServer Server;
        internal static CommandQueue Queue;
        private static readonly string LogPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RevitMCP", "revitmcp.log");

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                Log("RevitMCP starting…");

                Queue = new CommandQueue();
                application.ControlledApplication.ApplicationInitialized += (s, e) =>
                {
                    // ExternalEvent must be created after Revit fully starts
                    Queue.Initialize();
                };

                Server = new HttpServer(Queue, port: 7777);
                Server.Start();

                Log("RevitMCP HTTP server started on port 7777");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("RevitMCP Startup Error", ex.ToString());
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            try { Server?.Stop(); } catch { /* swallow on shutdown */ }
            Log("RevitMCP stopped");
            return Result.Succeeded;
        }

        internal static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch { /* never crash Revit due to logging */ }
        }
    }
}
