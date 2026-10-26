namespace Xrm.Sdk.PluginRegistration.TestPlugins
{
    using Microsoft.Xrm.Sdk;
    using System;

    /// <summary>
    /// A second plug-in type, so registering the assembly covers more than one type.
    /// </summary>
    public class NoOpPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
        }
    }
}
