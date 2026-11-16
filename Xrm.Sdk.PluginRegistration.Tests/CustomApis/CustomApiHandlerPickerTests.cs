using Microsoft.Xrm.Sdk;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xrm.Sdk.PluginRegistration.Controls;
using Xrm.Sdk.PluginRegistration.Forms;
using Xrm.Sdk.PluginRegistration.Helpers;
using Xrm.Sdk.PluginRegistration.Models;

namespace Xrm.Sdk.PluginRegistration.Tests.CustomApis
{
    /// <summary>
    /// The Custom API editor's plug-in assembly and type pickers: searchable, always one of the
    /// listed records, and with the version shown, since Dataverse can hold the same assembly name
    /// in several major versions.
    /// </summary>
    [TestFixture, Apartment(ApartmentState.STA)]
    public class CustomApiHandlerPickerTests
    {
        private static readonly Guid VersionOne = Guid.NewGuid();
        private static readonly Guid VersionTwo = Guid.NewGuid();
        private static readonly Guid HandlerOne = Guid.NewGuid();
        private static readonly Guid HandlerTwo = Guid.NewGuid();
        private static readonly Guid OtherHandler = Guid.NewGuid();

        [Test]
        public void HandlersAreLoadedWithTheirAssemblysIdAndVersion()
        {
            var service = TwoVersionsService();

            var data = CustomApiEditorData.Load(service, null, includeSolutions: false);

            var query = service.Queries.Single();
            Assert.That(query.LinkEntities.Single().Columns.Columns, Is.EquivalentTo(new[] { "name", "version" }));
            var two = data.PluginTypes.Single(p => p.Id == HandlerTwo);
            Assert.That(two.AssemblyId, Is.EqualTo(VersionTwo));
            Assert.That(two.AssemblyVersion, Is.EqualTo("2.0.0.0"));
            Assert.That(two.AssemblyLabel, Is.EqualTo("Contoso.Plugins (2.0.0.0)"));
        }

        [Test]
        public void SameNamedAssembliesAreListedOncePerVersion()
        {
            using (var dialog = Editor(handler: null))
            {
                var assemblies = Field<FilteringComboBox>(dialog, "assembly");
                Assert.That(assemblies.Choices.Select(c => c.Value), Is.EqualTo(new[]
                {
                    "(All assemblies)", "Contoso.Other (1.0.0.0)", "Contoso.Plugins (1.0.0.0)", "Contoso.Plugins (2.0.0.0)"
                }));
                Assert.That(assemblies.Text, Is.EqualTo("(All assemblies)"));

                var handlers = Field<FilteringComboBox>(dialog, "pluginType");
                // The handler in both versions shows its version; the unique one doesn't need it.
                Assert.That(handlers.Choices.Select(c => c.Value), Is.EqualTo(new[]
                {
                    "(None)", "Contoso.Plugins.Handler (1.0.0.0)", "Contoso.Plugins.Handler (2.0.0.0)", "Contoso.Plugins.Other"
                }));
                Assert.That(handlers.Choices.Skip(1).Select(c => c.Detail), Is.EqualTo(new[]
                {
                    "Contoso.Plugins (1.0.0.0)", "Contoso.Plugins (2.0.0.0)", "Contoso.Other (1.0.0.0)"
                }));
            }
        }

        [Test]
        public void OpeningAnApiPicksTheVersionItsHandlerIsIn()
        {
            using (var dialog = Editor(handler: HandlerTwo))
            {
                Assert.That(Field<FilteringComboBox>(dialog, "assembly").Text, Is.EqualTo("Contoso.Plugins (2.0.0.0)"));
                var handlers = Field<FilteringComboBox>(dialog, "pluginType");
                Assert.That(handlers.Text, Is.EqualTo("Contoso.Plugins.Handler (2.0.0.0)"));
                Assert.That(handlers.Choices.Select(c => c.Key), Is.EqualTo(new object[] { Field<SearchChoice>(dialog, "noHandler").Key, HandlerTwo }));
                Assert.That(Invoke<CustomApiDefinition>(dialog, "CaptureDraft").PluginTypeId, Is.EqualTo(HandlerTwo));
            }
        }

        [Test]
        public void PickingTheOtherVersionsAssemblyAndHandlerChangesTheHandler()
        {
            using (var dialog = Editor(handler: HandlerTwo))
            {
                var assemblies = Field<FilteringComboBox>(dialog, "assembly");
                var handlers = Field<FilteringComboBox>(dialog, "pluginType");

                assemblies.SelectChoice(assemblies.Choices.Single(c => Equals(c.Key, VersionOne)));
                Assert.That(handlers.Text, Does.Contain("another assembly"), "Browsing keeps the current handler.");
                Assert.That(Invoke<CustomApiDefinition>(dialog, "CaptureDraft").PluginTypeId, Is.EqualTo(HandlerTwo));

                handlers.SelectChoice(handlers.Choices.Single(c => Equals(c.Key, HandlerOne)));
                Assert.That(handlers.Text, Is.EqualTo("Contoso.Plugins.Handler (1.0.0.0)"));
                Assert.That(Invoke<CustomApiDefinition>(dialog, "CaptureDraft").PluginTypeId, Is.EqualTo(HandlerOne));
            }
        }

        [Test]
        public void TypingOnlySearchesAndLeavingKeepsAListedHandler()
        {
            using (var dialog = Editor(handler: HandlerTwo))
            {
                var assemblies = Field<FilteringComboBox>(dialog, "assembly");
                assemblies.SelectChoice(null);
                var handlers = Field<FilteringComboBox>(dialog, "pluginType");

                handlers.FilterTo("no such handler");
                Assert.That(handlers.Items.Cast<SearchChoice>().Select(c => c.Value), Is.EqualTo(new[] { "(None)" }), "(None) stays reachable.");
                handlers.ResolveTypedText();
                Assert.That(handlers.Text, Is.EqualTo("Contoso.Plugins.Handler (2.0.0.0)"), "Unknown text puts back the previous pick.");
                Assert.That(Invoke<CustomApiDefinition>(dialog, "CaptureDraft").PluginTypeId, Is.EqualTo(HandlerTwo));

                handlers.FilterTo("Other");
                handlers.ResolveTypedText();
                Assert.That(Invoke<CustomApiDefinition>(dialog, "CaptureDraft").PluginTypeId, Is.EqualTo(OtherHandler), "A single match is picked.");

                handlers.FilterTo("Handler");
                handlers.ResolveTypedText();
                Assert.That(Invoke<CustomApiDefinition>(dialog, "CaptureDraft").PluginTypeId, Is.EqualTo(OtherHandler), "Two matches pick neither.");

                handlers.FilterTo("1.0.0.0");
                Assert.That(handlers.Items.Cast<SearchChoice>().Select(c => c.Key),
                    Is.EquivalentTo(new object[] { Field<SearchChoice>(dialog, "noHandler").Key, HandlerOne, OtherHandler }), "Versions can be searched.");
                handlers.FilterTo("");
                handlers.ResolveTypedText();
                Assert.That(handlers.Text, Is.EqualTo("(None)"), "Clearing the box removes the handler.");
                Assert.That(Invoke<CustomApiDefinition>(dialog, "CaptureDraft").PluginTypeId, Is.Null);
            }
        }

        [Test]
        public void ChoicesSharingTheirTextAreToldApartByKey()
        {
            using (var form = new Form())
            using (var combo = new FilteringComboBox { RestrictToChoices = true })
            {
                form.Controls.Add(combo);
                var first = new SearchChoice("Same", "one", 1);
                var second = new SearchChoice("Same", "two", 2);
                combo.SetChoices(new[] { first, second });
                Assert.That(combo.Choices.Count, Is.EqualTo(2));

                int changes = 0;
                combo.SelectedChoiceChanged += (sender, args) => changes++;
                combo.SelectChoice(new SearchChoice("Same", key: 2));
                Assert.That(combo.SelectedChoice, Is.SameAs(second));
                Assert.That(combo.SelectedItem, Is.SameAs(second));
                Assert.That(changes, Is.EqualTo(1));

                combo.SetChoices(new[] { new SearchChoice("Same", "one", 1), new SearchChoice("Same", "two again", 2) });
                Assert.That(combo.SelectedChoice.Detail, Is.EqualTo("two again"), "A reload keeps the same record picked.");
            }
        }

        [TestCase("PrtTest.ManyPlugins.Plugin001")]
        [TestCase("account")]
        public void TheGreyDetailStartsAfterTheWholeValue(string value)
        {
            using (var font = new System.Drawing.Font("Segoe UI", 9))
            using (var bitmap = new System.Drawing.Bitmap(10, 10))
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                var row = new System.Drawing.Rectangle(2, 0, 700, 17);
                int valueWidth = TextRenderer.MeasureText(graphics, value, font).Width;

                var detail = FilteringComboBox.DetailBounds(graphics, value, font, row);

                // The detail was drawn over the value when it was measured with EndEllipsis (23 px for 170 px of text).
                Assert.That(detail.Left, Is.GreaterThanOrEqualTo(row.Left + valueWidth));
                Assert.That(detail.Right, Is.EqualTo(row.Right));
            }
        }

        private static CustomApiEditorForm Editor(Guid? handler)
        {
            var api = new CustomApiDefinition
            {
                Id = Guid.NewGuid(), UniqueName = "test_Example", Name = "Example", DisplayName = "Example API",
                Description = "Example", PluginTypeId = handler
            };
            var dialog = new CustomApiEditorForm(TwoVersionsService(), api);
            var task = Invoke<Task>(dialog, "LoadChoices");
            var timeout = Stopwatch.StartNew();
            while (!task.IsCompleted && timeout.Elapsed < TimeSpan.FromSeconds(10))
            {
                Application.DoEvents();
                Thread.Sleep(1);
            }
            task.GetAwaiter().GetResult();
            return dialog;
        }

        /// <summary>Contoso.Plugins in versions 1 and 2, both with the same handler, plus another assembly.</summary>
        private static RecordingOrganizationService TwoVersionsService()
        {
            return new RecordingOrganizationService
            {
                OnRetrieveMultiple = query =>
                {
                    Assert.That(query.EntityName, Is.EqualTo("plugintype"));
                    return new EntityCollection(new[]
                    {
                        PluginType(HandlerTwo, "Contoso.Plugins.Handler", VersionTwo, "Contoso.Plugins", "2.0.0.0"),
                        PluginType(HandlerOne, "Contoso.Plugins.Handler", VersionOne, "Contoso.Plugins", "1.0.0.0"),
                        PluginType(OtherHandler, "Contoso.Plugins.Other", Guid.NewGuid(), "Contoso.Other", "1.0.0.0")
                    });
                }
            };
        }

        private static Entity PluginType(Guid id, string name, Guid assemblyId, string assembly, string version)
        {
            return new Entity("plugintype", id)
            {
                ["typename"] = name,
                ["pluginassemblyid"] = new EntityReference("pluginassembly", assemblyId),
                ["assembly.name"] = new AliasedValue("pluginassembly", "name", assembly),
                ["assembly.version"] = new AliasedValue("pluginassembly", "version", version)
            };
        }

        private static T Field<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private static T Invoke<T>(object target, string name, params object[] arguments)
        {
            return (T)target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
        }
    }
}
