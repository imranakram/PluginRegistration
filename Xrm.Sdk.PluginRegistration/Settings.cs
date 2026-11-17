using System.Collections.Generic;

namespace Xrm.Sdk.PluginRegistration
{
    public class Settings
    {
        #region Public Properties

        public string ExcludedAssemblies { get; set; }
        public bool ExcludeManagedAssemblies { get; set; }

        /// <summary>List managed Custom APIs in Display by Custom API. Off, the default, hides them.</summary>
        public bool ShowManagedCustomApis { get; set; }

        public List<PluginAssemblyFileMapping> Mappings { get; set; } = new List<PluginAssemblyFileMapping>();

        #endregion Public Properties
    }
}