namespace Xrm.Sdk.PluginRegistration.TestPlugins
{
    using Microsoft.Xrm.Sdk;
    using System;

    /// <summary>
    /// Writes what it received into the target's description, so a test can read back that the
    /// step fired and that its configuration and images were passed in. Register it on Create or
    /// Update of contact in PreOperation; a pre image named "pre" with lastname is optional.
    /// </summary>
    public class StampPlugin : IPlugin
    {
        public const string PreImageAlias = "pre";

        private readonly string unsecureConfig;
        private readonly string secureConfig;

        public StampPlugin(string unsecureConfig, string secureConfig)
        {
            this.unsecureConfig = unsecureConfig;
            this.secureConfig = secureConfig;
        }

        public static string Stamp(string message, string unsecureConfig, bool hasSecureConfig, string preImageLastName)
        {
            return $"stamp|{message}|{unsecureConfig}|secure={(hasSecureConfig ? "yes" : "no")}|pre={preImageLastName}";
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity target))
            {
                return;
            }

            string preImageLastName = null;
            if (context.PreEntityImages.Contains(PreImageAlias))
            {
                preImageLastName = context.PreEntityImages[PreImageAlias].GetAttributeValue<string>("lastname");
            }

            target["description"] = Stamp(context.MessageName, unsecureConfig, !string.IsNullOrEmpty(secureConfig), preImageLastName);
        }
    }
}
