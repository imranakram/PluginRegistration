namespace Xrm.Sdk.PluginRegistration.TestPlugins
{
    using System.Activities;

    /// <summary>
    /// A custom workflow activity, so registering the assembly covers that plug-in type too.
    /// </summary>
    public class NoOpActivity : CodeActivity
    {
        protected override void Execute(CodeActivityContext context)
        {
        }
    }
}
