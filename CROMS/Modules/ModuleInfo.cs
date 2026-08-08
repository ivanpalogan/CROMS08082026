using System;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// Describes one navigable module: its key, display title, sidebar group,
    /// and a factory that lazily builds the module's Form the first time it is shown.
    /// </summary>
    public class ModuleInfo
    {
        public string Key { get; }
        public string Title { get; }
        public string Group { get; }
        public Func<Form> Factory { get; }

        public ModuleInfo(string key, string title, string group, Func<Form> factory)
        {
            Key = key;
            Title = title;
            Group = group;
            Factory = factory;
        }
    }
}
