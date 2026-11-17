namespace Xrm.Sdk.PluginRegistration.Tests.Integration
{
    using NUnit.Framework;
    using System.Diagnostics;
    using System.Linq;
    using Xrm.Sdk.PluginRegistration.Helpers;

    /// <summary>The Custom API editor's table and privilege pickers load from a real environment. Read-only.</summary>
    [TestFixture]
    [Category(TestBench.Category)]
    internal class CustomApiMetadataLoadTests
    {
        private TestBench bench;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            bench = TestBench.LoadOrIgnore();
            // Connects the bench's service.
            bench.LoadOrganization();
        }

        [Test]
        public void TablesAndPrivilegesLoad()
        {
            var timer = Stopwatch.StartNew();
            var metadata = CustomApiMetadata.Load(bench.Service);
            timer.Stop();

            Assert.That(metadata.TableError, Is.Null, metadata.TableError?.ToString());
            Assert.That(metadata.PrivilegeError, Is.Null, metadata.PrivilegeError?.ToString());
            var account = metadata.Tables.SingleOrDefault(t => t.Value == "account");
            Assert.That(account, Is.Not.Null);
            Assert.That(account.Detail, Is.Not.Empty, "Display names are loaded.");
            Assert.That(metadata.Privileges.Select(p => p.Value), Does.Contain("prvReadAccount"));

            TestContext.Progress.WriteLine($"{metadata.Tables.Count} tables and {metadata.Privileges.Count} privileges in {timer.ElapsedMilliseconds} ms");
        }

        [Test]
        public void HandlersComeWithTheirAssemblysIdAndVersion()
        {
            var data = CustomApiEditorData.Load(bench.Service, null, includeSolutions: false);

            if (data.PluginTypes.Count == 0) Assert.Inconclusive("The environment has no custom plug-in types.");
            Assert.That(data.PluginTypes.Where(p => p.AssemblyId == null || string.IsNullOrEmpty(p.AssemblyVersion)).Select(p => p.Name), Is.Empty);
            TestContext.Progress.WriteLine($"{data.PluginTypes.Count} handlers in {data.PluginTypes.Select(p => p.AssemblyId).Distinct().Count()} assemblies");
        }
    }
}
