using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using NUnit.Framework;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using Xrm.Sdk.PluginRegistration.Helpers;
using Xrm.Sdk.PluginRegistration.TestPlugins;
using Xrm.Sdk.PluginRegistration.Wrappers;

namespace Xrm.Sdk.PluginRegistration.Tests.Assemblies
{
    /// <summary>
    /// Before an update, the tool compares each workflow activity's arguments in the build with those
    /// Dataverse has, and warns about changes existing processes may not survive. The integration test
    /// WorkflowActivityArgumentTests checks the same against a real environment, including the type names.
    /// </summary>
    [TestFixture]
    public class WorkflowActivityArgumentsTests
    {
        private const string Activity = "Contoso.Activities.Sample";

        [Test]
        public void ArgumentsAreReadFromTheActivitysProperties()
        {
            var arguments = WorkflowActivityArguments.Read(typeof(SampleActivity));

            Assert.That(arguments.Select(a => $"{a.Direction} {a.PropertyName} \"{a.Label}\" {a.TypeName} {a.Required}"), Is.EquivalentTo(new[]
            {
                "Input Name \"Name\" System.String False",
                "Input Count \"How many\" Microsoft.Crm.Sdk.CrmNumber True",
                "Input Account \"Account\" Microsoft.Crm.Sdk.Lookup False",
                "Input Both \"Both in\" System.String False",
                "Output Both \"Both out\" System.String False",
                "Output Result \"Result\" Microsoft.Crm.Sdk.CrmMoney False",
            }), "Properties without an Input or Output attribute, or that aren't arguments, are left out");
        }

        [Test]
        public void TheAssemblyReaderReadsActivitiesArguments()
        {
            // The reader runs in its own application domain, so the arguments have to survive the trip back
            var assembly = RegistrationHelper.RetrievePluginsFromAssembly(typeof(NoOpActivity).Assembly.Location);

            var activity = assembly.Plugins.Values.Single(p => p.TypeName == typeof(NoOpActivity).FullName);
            Assert.That(activity.ActivityArguments, Is.Not.Null.And.Empty);
            Assert.That(assembly.Plugins.Values.Where(p => p.PluginType == CrmPluginType.Plugin).Select(p => p.ActivityArguments), Is.All.Null);
        }

        [Test]
        public void DataversesArgumentListIsParsed()
        {
            var arguments = WorkflowActivityArguments.Parse(Info(new[]
                {
                    Parameter("Name", "Name", "System.String, mscorlib, Version=4.0.0.0", false),
                    Parameter("How many", "Count", "Microsoft.Crm.Sdk.CrmNumber, Microsoft.Crm.Sdk, Version=9.0.0.0", true)
                },
                Parameter("Result", "Result", "Microsoft.Crm.Sdk.CrmMoney, Microsoft.Crm.Sdk", false)));

            Assert.That(arguments.Select(a => $"{a.Direction} {a.PropertyName} \"{a.Label}\" {a.TypeName} {a.Required}"), Is.EqualTo(new[]
            {
                "Input Name \"Name\" System.String False",
                "Input Count \"How many\" Microsoft.Crm.Sdk.CrmNumber True",
                "Output Result \"Result\" Microsoft.Crm.Sdk.CrmMoney False",
            }));
            Assert.That(WorkflowActivityArguments.Parse(null), Is.Empty);
        }

        [Test]
        public void AddingOptionalArgumentsOrRelabellingIsNothingToWarnAbout()
        {
            var registered = Registered(Input("Name", "System.String"));
            var build = Build(Input("Name", "System.String", label: "Customer name"), Input("Extra", "System.String"), Output("Result", "Microsoft.Crm.Sdk.CrmMoney"));

            Assert.That(WorkflowActivityArguments.RiskyChanges(registered, build), Is.Empty);
        }

        [Test]
        public void RemovedRetypedAndNewlyRequiredArgumentsAreReported()
        {
            var registered = Registered(
                Input("Name", "System.String"), Input("Count", "Microsoft.Crm.Sdk.CrmNumber"),
                Input("Note", "System.String"), Output("Result", "Microsoft.Crm.Sdk.CrmMoney"));
            var build = Build(
                Input("Name", "System.String", required: true), Input("Count", "System.String"),
                Input("Owner", "Microsoft.Crm.Sdk.Lookup", required: true), Input("Extra", "System.String"));

            var changes = WorkflowActivityArguments.RiskyChanges(registered, build);

            Assert.That(changes.Select(c => c.Activity), Is.All.EqualTo(Activity));
            Assert.That(changes.Select(c => c.Description), Is.EquivalentTo(new[]
            {
                "Input Name is now required",
                "Input Count changes from int to string",
                "Input Note is removed",
                "Output Result is removed",
                "Input Owner is new and required",
            }));
            string warning = WorkflowActivityArguments.Warning(changes);
            Assert.That(warning, Does.Contain(Activity).And.Contain("- Input Note is removed").And.EndWith("Update anyway?"));
        }

        [Test]
        public void NewActivitiesAndUnknownTypesAreLeftAlone()
        {
            var registered = Registered(Input("Value", "Microsoft.Crm.Sdk.SomethingNew"));
            Assert.That(WorkflowActivityArguments.RiskyChanges(registered, Build(Input("Value", null))), Is.Empty,
                "A type the tool can't name isn't reported as changed");

            var other = new Dictionary<string, List<ActivityArgument>> { ["Contoso.Activities.Other"] = new List<ActivityArgument> { Input("Gone", "System.String") } };
            Assert.That(WorkflowActivityArguments.RiskyChanges(other, Build(Input("Required", "System.String", required: true))), Is.Empty,
                "An activity that isn't registered yet has nothing to break");
            Assert.That(WorkflowActivityArguments.RiskyChanges(null, Build()), Is.Empty);
        }

        private static ActivityArgument Input(string name, string type, bool required = false, string label = null) =>
            new ActivityArgument { PropertyName = name, Label = label ?? name, Direction = ActivityArgumentDirection.Input, TypeName = type, Required = required };

        private static ActivityArgument Output(string name, string type) =>
            new ActivityArgument { PropertyName = name, Label = name, Direction = ActivityArgumentDirection.Output, TypeName = type };

        private static Dictionary<string, List<ActivityArgument>> Registered(params ActivityArgument[] arguments) =>
            new Dictionary<string, List<ActivityArgument>> { [Activity] = arguments.ToList() };

        private static CrmPlugin[] Build(params ActivityArgument[] arguments) => new[]
        {
            new CrmPlugin(null) { TypeName = Activity, PluginType = CrmPluginType.WorkflowActivity, ActivityArguments = arguments.ToList() }
        };

        private static string Info(string[] inputs, string outputs) =>
            "<?xml version=\"1.0\" encoding=\"utf-16\"?><SandboxCustomActivityInfo><CustomActivityInfo><Name>" + Activity + "</Name></CustomActivityInfo>" +
            "<Inputs>" + string.Concat(inputs) + "</Inputs><Outputs>" + outputs + "</Outputs></SandboxCustomActivityInfo>";

        private static string Parameter(string label, string property, string type, bool required) =>
            $"<CustomActivityParameterInfo><Name>{label}</Name><TypeName>{type}</TypeName><IsInOutArgument>false</IsInOutArgument>" +
            $"<Required>{required.ToString().ToLowerInvariant()}</Required><DependencyPropertyName>{property}</DependencyPropertyName></CustomActivityParameterInfo>";

        /// <summary>An activity with one argument of several kinds, and properties that aren't arguments.</summary>
        public class SampleActivity : CodeActivity
        {
            [Input("Name")]
            public InArgument<string> Name { get; set; }

            [RequiredArgument]
            [Input("How many")]
            public InArgument<int> Count { get; set; }

            [Input("Account")]
            [ReferenceTarget("account")]
            public InArgument<EntityReference> Account { get; set; }

            [Input("Both in")]
            [Output("Both out")]
            public InOutArgument<string> Both { get; set; }

            [Output("Result")]
            public OutArgument<Money> Result { get; set; }

            // Neither is an argument the designer shows
            public InArgument<string> Undecorated { get; set; }

            public string Plain { get; set; }

            protected override void Execute(CodeActivityContext context)
            {
            }
        }
    }
}
