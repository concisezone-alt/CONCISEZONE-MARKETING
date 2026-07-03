using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace RevitMCP.Addin
{
    /// <summary>
    /// Thread-safe bridge between the HTTP server thread and the Revit API thread.
    /// All Revit API calls must execute on the main Revit thread via ExternalEvent.
    /// </summary>
    public class CommandQueue : IExternalEventHandler
    {
        private readonly ConcurrentQueue<IRevitCommand> _pending = new ConcurrentQueue<IRevitCommand>();
        private ExternalEvent _externalEvent;

        public void Initialize()
        {
            _externalEvent = ExternalEvent.Create(this);
        }

        // Called from HTTP server background thread — enqueues work, waits for result
        public T Run<T>(Func<Document, T> action)
        {
            if (_externalEvent == null)
                throw new InvalidOperationException("CommandQueue not initialized — Revit not ready.");

            var cmd = new RevitCommand<T>(action);
            _pending.Enqueue(cmd);
            _externalEvent.Raise();

            if (!cmd.Done.Wait(TimeSpan.FromSeconds(30)))
                throw new TimeoutException("Revit did not respond within 30 seconds.");

            if (cmd.Error != null) throw cmd.Error;
            return cmd.Result;
        }

        // Called on Revit main thread
        public void Execute(UIApplication app)
        {
            var doc = app.ActiveUIDocument?.Document;
            if (doc == null) return;

            while (_pending.TryDequeue(out var cmd))
            {
                try { cmd.Execute(doc); }
                catch (Exception ex) { cmd.Fail(ex); }
            }
        }

        public string GetName() => "RevitMCP";
    }

    public interface IRevitCommand
    {
        void Execute(Document doc);
        void Fail(Exception ex);
    }

    public class RevitCommand<T> : IRevitCommand
    {
        private readonly Func<Document, T> _action;
        public T Result { get; private set; }
        public Exception Error { get; private set; }
        public ManualResetEventSlim Done { get; } = new ManualResetEventSlim(false);

        public RevitCommand(Func<Document, T> action) => _action = action;

        public void Execute(Document doc)
        {
            try { Result = _action(doc); }
            finally { Done.Set(); }
        }

        public void Fail(Exception ex)
        {
            Error = ex;
            Done.Set();
        }
    }
}
