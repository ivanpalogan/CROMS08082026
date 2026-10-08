using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// Starts constructing the module screens while the operator is still choosing a service window,
    /// so less is left to do once the main window opens. Only screens that do not read the chosen
    /// window while they are built are handled here; Queue Management and the Dashboard read it, so
    /// they are built afterwards by MainForm. Forms are constructed but not shown; MainForm adopts
    /// them in <c>EnsureModule</c>. Discarded when the user changes (log out).
    /// </summary>
    public static class ModulePrebuilder
    {
        // Built with the window / user already known.
        private static readonly HashSet<string> SessionDependent = new HashSet<string> { "queue", "dashboard" };

        private static readonly Dictionary<string, Form> _built = new Dictionary<string, Form>();
        private static readonly Queue<ModuleInfo> _pending = new Queue<ModuleInfo>();
        private static Timer _timer;

        /// <summary>Begins building in small steps on the UI thread (runs while a modal dialog is open).</summary>
        public static void Start(HashSet<string> allowedKeys)
        {
            Clear();
            foreach (var m in ModuleRegistry.All)
            {
                if (SessionDependent.Contains(m.Key)) continue;
                if (allowedKeys != null && !allowedKeys.Contains(m.Key)) continue;
                _pending.Enqueue(m);
            }
            _timer = new Timer { Interval = 40 };
            _timer.Tick += (s, e) => Step();
            _timer.Start();
        }

        private static void Step()
        {
            if (_pending.Count == 0) { Stop(); return; }
            var m = _pending.Dequeue();
            try { _built[m.Key] = m.Factory(); }
            catch (Exception ex) { CROMS.Data.ErrorLog.Write("Prebuild " + m.Key, ex); }
        }

        private static void Stop()
        {
            if (_timer == null) return;
            _timer.Stop(); _timer.Dispose(); _timer = null;
        }

        /// <summary>Builds whatever is still waiting, now. Called when the main window needs them.</summary>
        public static void Finish()
        {
            Stop();
            while (_pending.Count > 0) Step();
        }

        /// <summary>Hands over a prebuilt form (once), or null when it was not built.</summary>
        public static Form Take(string key)
        {
            if (_built.TryGetValue(key, out var f)) { _built.Remove(key); return f; }
            return null;
        }

        /// <summary>Stops building and drops anything built for a previous user.</summary>
        public static void Clear()
        {
            Stop();
            _pending.Clear();
            foreach (var f in _built.Values) { try { f.Dispose(); } catch { } }
            _built.Clear();
        }
    }
}
