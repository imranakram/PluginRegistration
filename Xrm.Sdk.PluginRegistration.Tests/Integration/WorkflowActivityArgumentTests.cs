namespace Xrm.Sdk.PluginRegistration.Tests.Integration
{
    using Microsoft.Xrm.Sdk.Query;
    using NUnit.Framework;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Xrm.Sdk.PluginRegistration.Entities;
    using Xrm.Sdk.PluginRegistration.Helpers;
    using Xrm.Sdk.PluginRegistration.Wrappers;

    /// <summary>
    /// Issue #16: after an assembly update that adds arguments to an existing custom workflow activity,
    /// the workflow designer kept showing the old ones. Builds of a small activity with the same name,
    /// version and key are compiled here, registered and updated with the tool's own code, and the
    /// activity's argument list, which Dataverse keeps in plugintype.customworkflowactivityinfo, is read
    /// back. Dataverse reads the arguments again only when the activity's type comes with the update: a
    /// plain update, even repeated, keeps the old ones. The same builds check the warning the tool gives
    /// before an update whose argument changes may break existing processes.
    /// </summary>
    [TestFixture]
    [Category(TestBench.Category)]
    [NonParallelizable]
    internal class WorkflowActivityArgumentTests
    {
        private const string AssemblyName = "PrtIt.WorkflowArguments";
        private const string TypesAssemblyName = "PrtIt.WorkflowArgumentTypes";
        private static readonly TimeSpan PropagationTimeout = TimeSpan.FromMinutes(1);

        private const string FirstBuild = @"
        [Input(""First input"")]
        public InArgument<string> FirstInput { get; set; }";

        // Adds an optional input and an output: safe for existing processes
        private const string SecondBuild = FirstBuild + @"

        [Input(""Second input"")]
        public InArgument<string> SecondInput { get; set; }

        [Output(""Result output"")]
        public OutArgument<string> ResultOutput { get; set; }";

        // Changes a type, removes an input and an output, and adds a required input: all risky
        private const string ThirdBuild = @"
        [Input(""First input"")]
        public InArgument<int> FirstInput { get; set; }

        [RequiredArgument]
        [Input(""Third input"")]
        public InArgument<string> ThirdInput { get; set; }";

        // One argument of each type the designer takes
        private const string AllTypesBuild = @"
        [Input(""Text"")] public InArgument<string> Text { get; set; }
        [Input(""Number"")] public InArgument<int> Number { get; set; }
        [Input(""Flag"")] public InArgument<bool> Flag { get; set; }
        [Input(""When"")] public InArgument<System.DateTime> When { get; set; }
        [Input(""Amount"")] public InArgument<decimal> Amount { get; set; }
        [Input(""Ratio"")] public InArgument<double> Ratio { get; set; }
        [Input(""Price"")] public InArgument<Microsoft.Xrm.Sdk.Money> Price { get; set; }
        [Input(""Account"")] [ReferenceTarget(""account"")] public InArgument<Microsoft.Xrm.Sdk.EntityReference> Account { get; set; }
        [Input(""Industry"")] [AttributeTarget(""account"", ""industrycode"")] public InArgument<Microsoft.Xrm.Sdk.OptionSetValue> Industry { get; set; }
        [RequiredArgument] [Input(""Must"")] public InArgument<string> Must { get; set; }
        [Input(""Both in"")] [Output(""Both out"")] public InOutArgument<string> Both { get; set; }
        [Output(""Answer"")] public OutArgument<int> Answer { get; set; }";

        private TestBench bench;
        private CrmOrganization org;
        private string workFolder;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            bench = TestBench.LoadOrIgnore();
            org = bench.LoadOrganization();
            // Under the test output: the compiler can't sign from paths with parentheses, as a user's temp folder may have
            workFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "prt-it-workflow-arguments-" + Guid.NewGuid().ToString("N"));
            RemoveLeftovers();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (bench?.Service != null) RemoveLeftovers();
            try { if (workFolder != null && Directory.Exists(workFolder)) Directory.Delete(workFolder, true); } catch (IOException) { }
        }

        [Test]
        public void UpdatingTheAssemblyPicksUpNewActivityArgumentsAndWarnsAboutRiskyOnes()
        {
            string first = Compile(AssemblyName, "v1", FirstBuild);
            string second = Compile(AssemblyName, "v2", SecondBuild);
            string third = Compile(AssemblyName, "v3", ThirdBuild);

            Guid assemblyId = Register(first, out Guid typeId);
            string before = ActivityInfo(typeId);
            TestContext.Progress.WriteLine("After registering: " + Arguments(before));
            Assert.That(before, Does.Contain("FirstInput").And.Not.Contain("SecondInput"));

            // Adding an optional input and an output is nothing to warn about
            var update = Read(second, AssemblyName);
            Assert.That(WorkflowActivityArguments.RiskyChanges(WorkflowActivityArguments.LoadRegistered(bench.Service, assemblyId), update.Plugins.Values), Is.Empty);

            // Apply the second build as the tool's Update does: same version, so the group name is
            // unchanged, but the activity's type still goes with the assembly. Without it, even a
            // repeated update leaves the old arguments.
            update.AssemblyId = assemblyId;
            org = bench.LoadOrganization();
            var registered = org[assemblyId];
            string group = RegistrationHelper.GenerateDefaultGroupName(registered.Name, new Version(registered.Version));
            var activityTypes = RegistrationHelper.WorkflowActivityTypesForUpdate(registered.Plugins, group, group);
            Assert.That(activityTypes.Select(t => t.Id), Is.EqualTo(new[] { typeId }));
            RegistrationHelper.UpdateAssembly(org, second, update, activityTypes);

            bool picked = WaitFor(() => ActivityInfo(typeId).Contains("SecondInput"));
            TestContext.Progress.WriteLine($"After Update ({(picked ? "picked up" : "not picked up")}): " + Arguments(ActivityInfo(typeId)));
            Assert.That(picked, Is.True, "The second build's arguments should reach the activity after Update.");
            Assert.That(ActivityInfo(typeId), Does.Contain("ResultOutput"));

            // The third build would break processes using the second's arguments, and the warning says how
            var risky = WorkflowActivityArguments.RiskyChanges(WorkflowActivityArguments.LoadRegistered(bench.Service, assemblyId),
                Read(third, AssemblyName).Plugins.Values);
            TestContext.Progress.WriteLine(WorkflowActivityArguments.Warning(risky));
            Assert.That(risky.Select(c => c.Description), Is.EquivalentTo(new[]
            {
                "Input \"First input\" (FirstInput) changes from string to int",
                "Input \"Second input\" (SecondInput) is removed",
                "Output \"Result output\" (ResultOutput) is removed",
                "Input \"Third input\" (ThirdInput) is new and required",
            }));
        }

        [Test]
        public void TheToolNamesEveryArgumentTypeAsDataverseDoes()
        {
            string path = Compile(TypesAssemblyName, "types", AllTypesBuild);
            var build = Read(path, TypesAssemblyName);
            Guid assemblyId = Register(path, out _, TypesAssemblyName);

            var registered = WorkflowActivityArguments.LoadRegistered(bench.Service, assemblyId).Values.Single();
            var read = build.Plugins.Values.Single().ActivityArguments;
            foreach (var argument in registered.OrderBy(a => a.Direction).ThenBy(a => a.PropertyName))
            {
                TestContext.Progress.WriteLine($"{argument.Direction} {argument.PropertyName}: {argument.TypeName}{(argument.Required ? ", required" : "")}");
            }

            Assert.That(read.Select(Key), Is.EquivalentTo(registered.Select(Key)),
                "The tool reads each argument's direction, type and required flag as Dataverse stores them");
            Assert.That(WorkflowActivityArguments.RiskyChanges(WorkflowActivityArguments.LoadRegistered(bench.Service, assemblyId), build.Plugins.Values), Is.Empty,
                "A build compared with itself has no changes");
        }

        private static string Key(ActivityArgument argument) =>
            $"{argument.Direction} {argument.PropertyName} {argument.TypeName} {(argument.Direction == ActivityArgumentDirection.Input && argument.Required)}";

        /// <summary>Registers the build as PluginRegistrationForm does for a new assembly.</summary>
        private Guid Register(string path, out Guid typeId, string assemblyName = AssemblyName)
        {
            var assembly = Read(path, assemblyName);
            Guid assemblyId = RegistrationHelper.RegisterAssembly(org, path, assembly);
            typeId = Guid.Empty;
            foreach (var plugin in assembly.Plugins.Values.ToList())
            {
                plugin.AssemblyId = assemblyId;
                typeId = RegistrationHelper.RegisterPlugin(org, plugin);
            }
            return assemblyId;
        }

        private string Compile(string assemblyName, string folder, string properties)
        {
            string directory = Path.Combine(workFolder, folder);
            Directory.CreateDirectory(directory);
            string output = Path.Combine(directory, assemblyName + ".dll");
            string sourceKey = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
                @"..\..\..\Xrm.Sdk.PluginRegistration.TestPlugins\TestPlugins.snk"));
            Assert.That(File.Exists(sourceKey), Is.True, "Signing key " + sourceKey);
            string keyFile = Path.Combine(directory, "key.snk");
            File.Copy(sourceKey, keyFile, true);

            string source = @"
using System.Activities;
using System.Reflection;
using Microsoft.Xrm.Sdk.Workflow;

[assembly: AssemblyVersion(""1.0.0.0"")]

namespace " + assemblyName + @"
{
    public class ArgumentsActivity : CodeActivity
    {" + properties + @"

        protected override void Execute(CodeActivityContext context)
        {
        }
    }
}";

            string sourceFile = Path.Combine(directory, "ArgumentsActivity.cs");
            File.WriteAllText(sourceFile, source);

            // Visual Studio's Roslyn compiler signs as the solution's build does; the framework's own
            // compiler, which CodeDom uses, fails to sign on some machines
            string compiler = FindRoslynCompiler();
            if (compiler == null) Assert.Inconclusive("Visual Studio's Roslyn compiler (MSBuild\\Current\\Bin\\Roslyn\\csc.exe) was not found.");

            string testDirectory = TestContext.CurrentContext.TestDirectory;
            string framework = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            string arguments = string.Join(" ", new[]
            {
                "/nologo", "/target:library", "/noconfig", "/nostdlib+",
                Quoted("/out:", output), Quoted("/keyfile:", keyFile),
                Quoted("/r:", Path.Combine(framework, "mscorlib.dll")),
                Quoted("/r:", Path.Combine(framework, "System.dll")),
                Quoted("/r:", Path.Combine(framework, "System.Core.dll")),
                Quoted("/r:", Path.Combine(framework, "System.Runtime.Serialization.dll")),
                Quoted("/r:", Path.Combine(framework, "System.Activities.dll")),
                Quoted("/r:", Path.Combine(testDirectory, "Microsoft.Xrm.Sdk.dll")),
                Quoted("/r:", Path.Combine(testDirectory, "Microsoft.Xrm.Sdk.Workflow.dll")),
                Quoted("", sourceFile)
            });
            var start = new ProcessStartInfo(compiler, arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using (var process = Process.Start(start))
            {
                string log = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                process.WaitForExit();
                Assert.That(process.ExitCode, Is.EqualTo(0), log);
            }

            return output;
        }

        private static string Quoted(string option, string path) => option + "\"" + path + "\"";

        private static string FindRoslynCompiler()
        {
            foreach (var root in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) })
            {
                string studio = Path.Combine(root, "Microsoft Visual Studio");
                if (!Directory.Exists(studio)) continue;
                var found = Directory.EnumerateDirectories(studio)
                    .SelectMany(Directory.EnumerateDirectories)
                    .Select(edition => Path.Combine(edition, @"MSBuild\Current\Bin\Roslyn\csc.exe"))
                    .Where(File.Exists)
                    .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (found != null) return found;
            }
            return null;
        }

        private static CrmPluginAssembly Read(string path, string assemblyName)
        {
            var assembly = RegistrationHelper.RetrievePluginsFromAssembly(path);
            assembly.SourceType = CrmAssemblySourceType.Database;
            assembly.IsolationMode = CrmAssemblyIsolationMode.Sandbox;
            assembly.ServerFileName = null;
            Assert.That(assembly.Plugins.Values.Select(p => p.TypeName), Is.EqualTo(new[] { assemblyName + ".ArgumentsActivity" }));
            return assembly;
        }

        private string ActivityInfo(Guid typeId) =>
            bench.Service.Retrieve(PluginType.EntityLogicalName, typeId, new ColumnSet("customworkflowactivityinfo"))
                .GetAttributeValue<string>("customworkflowactivityinfo") ?? string.Empty;

        /// <summary>The argument names in the activity info, for the test output.</summary>
        private static string Arguments(string info) =>
            string.Join(", ", new[] { "FirstInput", "SecondInput", "ResultOutput" }.Where(info.Contains));

        private static bool WaitFor(Func<bool> condition)
        {
            var watch = Stopwatch.StartNew();
            while (!condition())
            {
                if (watch.Elapsed > PropagationTimeout) return false;
                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
            return true;
        }

        private void RemoveLeftovers()
        {
            org = bench.LoadOrganization();
            var leftovers = org.Assemblies.Values.Where(a => a.Name == AssemblyName || a.Name == TypesAssemblyName).Cast<ICrmEntity>().ToArray();
            if (leftovers.Length > 0)
            {
                TestContext.Progress.WriteLine($"Removing {leftovers.Length} leftover assembly registration(s).");
                RegistrationHelper.Unregister(org, null, leftovers);
                org = bench.LoadOrganization();
            }
        }
    }
}
