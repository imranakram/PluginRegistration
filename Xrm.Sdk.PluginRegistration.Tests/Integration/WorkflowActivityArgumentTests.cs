namespace Xrm.Sdk.PluginRegistration.Tests.Integration
{
    using Microsoft.Xrm.Sdk;
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
    /// the workflow designer kept showing the old ones. Two builds of a small activity, with the same name,
    /// version and key, are compiled here: the first with one input, the second with another input and an
    /// output. The first is registered and the second applied with the tool's Update, and the activity's
    /// argument list, which Dataverse keeps in plugintype.customworkflowactivityinfo, is read back.
    /// </summary>
    [TestFixture]
    [Category(TestBench.Category)]
    [NonParallelizable]
    internal class WorkflowActivityArgumentTests
    {
        private const string AssemblyName = "PrtIt.WorkflowArguments";
        private const string ActivityTypeName = "PrtIt.WorkflowArguments.ArgumentsActivity";
        private static readonly TimeSpan PropagationTimeout = TimeSpan.FromMinutes(1);

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
        public void UpdatingTheAssemblyPicksUpNewActivityArguments()
        {
            string first = Compile("v1", secondBuild: false);
            string second = Compile("v2", secondBuild: true);

            // Register the first build, as PluginRegistrationForm does for a new assembly
            var assembly = Read(first);
            Guid assemblyId = RegistrationHelper.RegisterAssembly(org, first, assembly);
            Guid typeId = Guid.Empty;
            foreach (var plugin in assembly.Plugins.Values.ToList())
            {
                plugin.AssemblyId = assemblyId;
                typeId = RegistrationHelper.RegisterPlugin(org, plugin);
            }

            string before = ActivityInfo(typeId);
            TestContext.Progress.WriteLine("After registering: " + Arguments(before));
            Assert.That(before, Does.Contain("FirstInput").And.Not.Contain("SecondInput"));

            // Apply the second build as the tool's Update does: same version, so the group name is
            // unchanged, but the activity's type still goes with the assembly. Without it, even a
            // repeated update leaves the old arguments.
            var update = Read(second);
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
        }

        private string Compile(string folder, bool secondBuild)
        {
            string directory = Path.Combine(workFolder, folder);
            Directory.CreateDirectory(directory);
            string output = Path.Combine(directory, AssemblyName + ".dll");
            string sourceKey = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
                @"..\..\..\Xrm.Sdk.PluginRegistration.TestPlugins\TestPlugins.snk"));
            Assert.That(File.Exists(sourceKey), Is.True, "Signing key " + sourceKey);
            string keyFile = Path.Combine(directory, "key.snk");
            File.Copy(sourceKey, keyFile, true);

            string extra = secondBuild
                ? @"
        [Input(""Second input"")]
        public InArgument<string> SecondInput { get; set; }

        [Output(""Result output"")]
        public OutArgument<string> ResultOutput { get; set; }"
                : string.Empty;
            string source = @"
using System.Activities;
using System.Reflection;
using Microsoft.Xrm.Sdk.Workflow;

[assembly: AssemblyVersion(""1.0.0.0"")]

namespace PrtIt.WorkflowArguments
{
    public class ArgumentsActivity : CodeActivity
    {
        [Input(""First input"")]
        public InArgument<string> FirstInput { get; set; }
" + extra + @"

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

        private static CrmPluginAssembly Read(string path)
        {
            var assembly = RegistrationHelper.RetrievePluginsFromAssembly(path);
            assembly.SourceType = CrmAssemblySourceType.Database;
            assembly.IsolationMode = CrmAssemblyIsolationMode.Sandbox;
            assembly.ServerFileName = null;
            Assert.That(assembly.Plugins.Values.Select(p => p.TypeName), Is.EqualTo(new[] { ActivityTypeName }));
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
            var leftovers = org.Assemblies.Values.Where(a => a.Name == AssemblyName).Cast<ICrmEntity>().ToArray();
            if (leftovers.Length > 0)
            {
                TestContext.Progress.WriteLine($"Removing {leftovers.Length} leftover assembly registration(s).");
                RegistrationHelper.Unregister(org, null, leftovers);
                org = bench.LoadOrganization();
            }
        }
    }
}
