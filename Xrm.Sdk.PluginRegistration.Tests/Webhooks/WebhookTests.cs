using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using Xrm.Sdk.PluginRegistration.Entities;
using Xrm.Sdk.PluginRegistration.Forms;
using Xrm.Sdk.PluginRegistration.Helpers;
using Xrm.Sdk.PluginRegistration.Wrappers;

namespace Xrm.Sdk.PluginRegistration.Tests.Webhooks
{
    /// <summary>
    /// Issue #100: Dataverse never returns a webhook's authvalue, so editing must be able to keep
    /// it, and the header and query string values must be saved as well-formed XML.
    /// </summary>
    [TestFixture]
    public class WebhookTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void SettingsAreEscapedAndReadBackUnchanged()
        {
            var rows = new[]
            {
                Row(" x-prt-plain ", "hello"),
                Row("x-prt-special", "a & b 'quoted' \"double\" <x>"),
                Row(null, null),
                Row("", ""),
            };

            var xml = WebhookAuthValue.BuildSettings(rows);

            var settings = XElement.Parse(xml);
            Assert.That(settings.Name.LocalName, Is.EqualTo("settings"));
            Assert.That(settings.Elements("setting").Select(s => (string)s.Attribute("name") + "=" + (string)s.Attribute("value")),
                Is.EqualTo(new[] { "x-prt-plain=hello", "x-prt-special=a & b 'quoted' \"double\" <x>" }));
            Assert.That(xml, Does.StartWith("<settings><setting name=\"x-prt-plain\" value=\"hello\" />"));
        }

        [Test]
        public void NoRowsGiveNoSettings()
        {
            Assert.That(WebhookAuthValue.BuildSettings(new[] { Row(null, null), Row(" ", "") }), Is.Empty);
            Assert.That(WebhookAuthValue.BuildSettings(null), Is.Empty);
        }

        [Test]
        public void ValuesNeedAKeyAndKeysMustBeUnique()
        {
            Assert.That(WebhookAuthValue.Validate(new[] { Row("a", "1"), Row("b", "2") }), Is.Empty);
            Assert.That(WebhookAuthValue.Validate(new[] { Row(" ", "orphan") }), Is.EqualTo(new[] { "Every value needs a key." }));
            Assert.That(WebhookAuthValue.Validate(new[] { Row("X-Key", "1"), Row("x-key", "2") }),
                Is.EqualTo(new[] { "The key X-Key is used more than once." }));
        }

        [Test]
        public void AnUpdateWithoutAValueLeavesTheSavedValueAlone()
        {
            var webhook = new CrmServiceEndpoint(null) { ServiceEndpointId = Guid.NewGuid(), Name = "hook", Url = "https://example.invalid" };

            var kept = (ServiceEndpoint)webhook.GenerateCrmEntities()[ServiceEndpoint.EntityLogicalName];
            Assert.That(kept.Attributes.ContainsKey("authvalue"), Is.False, "Not sent, so Dataverse keeps it");

            webhook.AuthValue = "<settings />";
            var replaced = (ServiceEndpoint)webhook.GenerateCrmEntities()[ServiceEndpoint.EntityLogicalName];
            Assert.That(replaced.AuthValue, Is.EqualTo("<settings />"));
        }

        [Test, Apartment(ApartmentState.STA)]
        public void EditingKeepsTheSavedValuesWhenLeftEmpty()
        {
            var existing = new CrmServiceEndpoint(null)
            {
                ServiceEndpointId = Guid.NewGuid(),
                Name = "PRT test webhook",
                Url = "https://example.invalid/hook",
                AuthType = CrmServiceEndpointAuthType.HttpHeader,
            };
            using (var form = new WebHookRegistrationForm(NewOrganization(), null, existing))
            {
                var hint = Field<Label>(form, "lblAuthHint");
                Assert.That(IsShown(hint), Is.True);
                Assert.That(hint.Text, Does.Contain("Leave empty to keep them"));

                Field<TextBox>(form, "txtName").Text = "Renamed";
                var webhook = Build(form, out var errors);

                Assert.That(errors, Is.Empty);
                Assert.That(webhook.ServiceEndpointId, Is.EqualTo(existing.ServiceEndpointId));
                Assert.That(webhook.Name, Is.EqualTo("Renamed"));
                Assert.That(webhook.AuthValue, Is.Null, "Empty keeps the saved values");

                // Entered values replace them
                Field<DataGridView>(form, "dgvKeyValue").Rows.Add("x-key", "a & b");
                Assert.That(Build(form, out _).AuthValue, Is.EqualTo("<settings><setting name=\"x-key\" value=\"a &amp; b\" /></settings>"));
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void ChangingTheAuthenticationTypeNeedsNewValues()
        {
            var existing = new CrmServiceEndpoint(null)
            {
                ServiceEndpointId = Guid.NewGuid(),
                Name = "PRT test webhook",
                Url = "https://example.invalid/hook",
                AuthType = CrmServiceEndpointAuthType.HttpHeader,
            };
            using (var form = new WebHookRegistrationForm(NewOrganization(), null, existing))
            {
                Field<ComboBox>(form, "cmbAuth").SelectedItem = CrmServiceEndpointAuthType.HttpQueryString.ToString();

                Assert.That(Field<Label>(form, "lblAuthHint").Text, Is.EqualTo("Enter the values for the new authentication type."));
                Assert.That(Build(form, out var errors), Is.Null);
                Assert.That(errors, Is.EqualTo(new[] { "Auth Value is required." }));

                Field<DataGridView>(form, "dgvKeyValue").Rows.Add("code", "123");
                var webhook = Build(form, out errors);
                Assert.That(errors, Is.Empty);
                Assert.That(webhook.AuthType, Is.EqualTo(CrmServiceEndpointAuthType.HttpQueryString));
                Assert.That(webhook.AuthValue, Is.EqualTo("<settings><setting name=\"code\" value=\"123\" /></settings>"));
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void ANewWebhookNeedsValues()
        {
            using (var form = new WebHookRegistrationForm(NewOrganization(), null, null))
            {
                Assert.That(IsShown(Field<Label>(form, "lblAuthHint")), Is.False);
                Field<TextBox>(form, "txtName").Text = "PRT test webhook";
                Field<TextBox>(form, "txtEndpointUrl").Text = "https://example.invalid/hook";

                Assert.That(Build(form, out var errors), Is.Null);
                Assert.That(errors, Is.EqualTo(new[] { "Auth Value is required." }));

                Field<DataGridView>(form, "dgvKeyValue").Rows.Add(null, "orphan");
                Build(form, out errors);
                Assert.That(errors, Is.EqualTo(new[] { "Every value needs a key." }));
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void AWebhookKeyIsTrimmed()
        {
            using (var form = new WebHookRegistrationForm(NewOrganization(), null, null))
            {
                Field<TextBox>(form, "txtName").Text = "PRT test webhook";
                Field<TextBox>(form, "txtEndpointUrl").Text = "https://example.invalid/hook";
                Field<ComboBox>(form, "cmbAuth").SelectedItem = CrmServiceEndpointAuthType.WebhookKey.ToString();
                Field<TextBox>(form, "txtValue").Text = "  secret  ";

                var webhook = Build(form, out var errors);

                Assert.That(errors, Is.Empty);
                Assert.That(webhook.AuthType, Is.EqualTo(CrmServiceEndpointAuthType.WebhookKey));
                Assert.That(webhook.AuthValue, Is.EqualTo("secret"));
            }
        }

        private static KeyValuePair<string, string> Row(string key, string value) => new KeyValuePair<string, string>(key, value);

        private static CrmServiceEndpoint Build(WebHookRegistrationForm form, out List<string> errors)
        {
            var args = new object[] { null };
            var webhook = (CrmServiceEndpoint)typeof(WebHookRegistrationForm).GetMethod("BuildWebhook", Flags).Invoke(form, args);
            errors = (List<string>)args[0];
            return webhook;
        }

        private static T Field<T>(WebHookRegistrationForm form, string name) => (T)typeof(WebHookRegistrationForm).GetField(name, Flags).GetValue(form);

        /// <summary>The control's own Visible setting; Visible reads false while the form isn't shown.</summary>
        private static bool IsShown(Control control) => (bool)typeof(Control).GetMethod("GetState", Flags).Invoke(control, new object[] { 0x2 });

        private static CrmOrganization NewOrganization() => (CrmOrganization)FormatterServices.GetUninitializedObject(typeof(CrmOrganization));
    }
}
