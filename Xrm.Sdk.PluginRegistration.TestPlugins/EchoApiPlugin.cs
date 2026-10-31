namespace Xrm.Sdk.PluginRegistration.TestPlugins
{
    using Microsoft.Xrm.Sdk;
    using System;

    /// <summary>
    /// Implements the integration tests' Custom API: as its main operation it answers the Echo
    /// response property with the Text request parameter. Registered as a synchronous PostOperation
    /// step on the same API, it appends its unsecure configuration, so a test can read back that the
    /// step fired.
    /// </summary>
    public class EchoApiPlugin : IPlugin
    {
        public const string InputName = "Text";
        public const string OutputName = "Echo";
        private const int MainOperation = 30;

        private readonly string unsecureConfig;

        public EchoApiPlugin(string unsecureConfig, string secureConfig)
        {
            this.unsecureConfig = unsecureConfig;
        }

        public static string Echo(string text)
        {
            return "echo|" + text;
        }

        public static string AfterStep(string echo, string unsecureConfig)
        {
            return echo + "|step=" + unsecureConfig;
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.Stage == MainOperation)
            {
                context.OutputParameters[OutputName] = Echo(context.InputParameters[InputName] as string);
            }
            else if (context.OutputParameters.Contains(OutputName))
            {
                context.OutputParameters[OutputName] = AfterStep((string)context.OutputParameters[OutputName], unsecureConfig);
            }
        }
    }
}
