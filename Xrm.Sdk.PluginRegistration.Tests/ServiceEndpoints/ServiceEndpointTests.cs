using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms;
using Xrm.Sdk.PluginRegistration.Controls;
using Xrm.Sdk.PluginRegistration.Entities;
using Xrm.Sdk.PluginRegistration.Forms;
using Xrm.Sdk.PluginRegistration.Wrappers;

namespace Xrm.Sdk.PluginRegistration.Tests.ServiceEndpoints
{
    /// <summary>
    /// Registering and editing Azure service endpoints (Service Bus, relays, Event Hub, Event Grid).
    /// </summary>
    [TestFixture]
    public class ServiceEndpointTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string Namespace = "sb://contoso.servicebus.windows.net/";

        [Test]
        public void TheEnumsUseTheDataverseOptionValues()
        {
            // serviceendpoint option sets, as listed in the Dataverse table reference
            Assert.That((int)CrmServiceEndpointContract.OneWay, Is.EqualTo(1));
            Assert.That((int)CrmServiceEndpointContract.Queue, Is.EqualTo(2));
            Assert.That((int)CrmServiceEndpointContract.Rest, Is.EqualTo(3));
            Assert.That((int)CrmServiceEndpointContract.TwoWay, Is.EqualTo(4));
            Assert.That((int)CrmServiceEndpointContract.Topic, Is.EqualTo(5));
            Assert.That((int)CrmServiceEndpointContract.PersistentQueue, Is.EqualTo(6));
            Assert.That((int)CrmServiceEndpointContract.EventHub, Is.EqualTo(7));
            Assert.That((int)CrmServiceEndpointContract.Webhook, Is.EqualTo(8));
            Assert.That((int)CrmServiceEndpointContract.EventGrid, Is.EqualTo(9));

            Assert.That((int)CrmServiceEndpointAuthType.SASKey, Is.EqualTo(2));
            Assert.That((int)CrmServiceEndpointAuthType.SASToken, Is.EqualTo(3));
            Assert.That((int)CrmServiceEndpointAuthType.WebhookKey, Is.EqualTo(4));
            Assert.That((int)CrmServiceEndpointAuthType.HttpHeader, Is.EqualTo(5));
            Assert.That((int)CrmServiceEndpointAuthType.HttpQueryString, Is.EqualTo(6));
            Assert.That((int)CrmServiceEndpointAuthType.AccessKey, Is.EqualTo(8));

            Assert.That((int)CrmServiceEndpointMessageFormat.BinaryXml, Is.EqualTo(1));
            Assert.That((int)CrmServiceEndpointMessageFormat.Json, Is.EqualTo(2));
            Assert.That((int)CrmServiceEndpointMessageFormat.TextXml, Is.EqualTo(3));

            Assert.That((int)CrmServiceEndpointUserClaim.None, Is.EqualTo(1));
            Assert.That((int)CrmServiceEndpointUserClaim.UserId, Is.EqualTo(2));
            Assert.That((int)CrmServiceEndpointUserClaim.UserInfo, Is.EqualTo(3));
        }

        [Test]
        public void ANamespaceEndpointSendsItsAddressAndOnlyTheSecretsThatWereEntered()
        {
            var endpoint = new CrmServiceEndpoint(null)
            {
                Contract = CrmServiceEndpointContract.Queue,
                NamespaceAddress = Namespace,
                Path = "orders",
                AuthType = CrmServiceEndpointAuthType.SASKey,
                SasKeyName = "send",
                MessageFormat = CrmServiceEndpointMessageFormat.Json,
            };

            var kept = Entity(endpoint);
            Assert.That(kept.NamespaceAddress, Is.EqualTo(Namespace));
            Assert.That(kept.NamespaceFormat.Value, Is.EqualTo(2), "Namespace Address");
            Assert.That(kept.SASKeyName, Is.EqualTo("send"));
            Assert.That(kept.MessageFormat.Value, Is.EqualTo(2));
            Assert.That(kept.Attributes.ContainsKey("saskey"), Is.False, "Not sent, so Dataverse keeps it");
            Assert.That(kept.Attributes.ContainsKey("sastoken"), Is.False);
            Assert.That(kept.Attributes.ContainsKey("authvalue"), Is.False);

            endpoint.SasKey = "key";
            Assert.That(Entity(endpoint).SASKey, Is.EqualTo("key"));
        }

        [Test]
        public void AUrlEndpointSendsNoNamespaceFields()
        {
            var endpoint = new CrmServiceEndpoint(null)
            {
                Contract = CrmServiceEndpointContract.EventGrid,
                Url = "https://topic.westeurope-1.eventgrid.azure.net/api/events",
                AuthType = CrmServiceEndpointAuthType.AccessKey,
                AuthValue = "key",
                NamespaceAddress = Namespace,
                SasKey = "ignored",
            };

            var entity = Entity(endpoint);
            Assert.That(entity.AuthValue, Is.EqualTo("key"));
            Assert.That(entity.Attributes.ContainsKey("namespaceaddress"), Is.False);
            Assert.That(entity.Attributes.ContainsKey("namespaceformat"), Is.False);
            Assert.That(entity.Attributes.ContainsKey("saskey"), Is.False);
        }

        [TestCase("contoso.servicebus.windows.net", Namespace)]
        [TestCase("  contoso.servicebus.windows.net/ ", Namespace)]
        [TestCase("sb://contoso.servicebus.windows.net", Namespace)]
        [TestCase("sb://contoso.servicebus.windows.net/", Namespace)]
        [TestCase("https://contoso.servicebus.windows.net:443/", Namespace)]
        [TestCase("", "")]
        public void TheNamespaceAddressIsCompletedToTheSbForm(string entered, string saved)
        {
            Assert.That(ServiceEndpointRegistrationForm.NormalizeNamespaceAddress(entered), Is.EqualTo(saved));
        }

        [Test]
        public void WebhooksAndServiceEndpointsHaveTheirOwnIcons()
        {
            var webhook = new CrmServiceEndpoint(null) { Contract = CrmServiceEndpointContract.Webhook };
            var queue = new CrmServiceEndpoint(null) { Contract = CrmServiceEndpointContract.PersistentQueue };

            Assert.That(webhook.NodeImageType, Is.EqualTo(Controls.CrmTreeNodeImageType.WebHook));
            Assert.That(queue.NodeImageType, Is.EqualTo(Controls.CrmTreeNodeImageType.ServiceEndpoint));

            var images = CrmResources.LoadImage(Controls.CrmTreeNodeImageType.WebHook, Controls.CrmTreeNodeImageType.ServiceEndpoint);
            Assert.That(images[Controls.CrmTreeNodeImageType.WebHook], Is.Not.Null);
            Assert.That(images[Controls.CrmTreeNodeImageType.ServiceEndpoint], Is.Not.Null);
        }

        [Test]
        public void ThePropertyGridShowsOnlyTheAddressFieldsOfTheEndpointsType()
        {
            var queue = new CrmServiceEndpoint(null)
            {
                Contract = CrmServiceEndpointContract.PersistentQueue,
                NamespaceAddress = Namespace,
                Path = "orders",
                SasKeyName = "send",
                SolutionNamespace = " ",
                MessageFormat = CrmServiceEndpointMessageFormat.Json,
            };
            Assert.That(GridProperties(queue), Does.Contain("NamespaceAddress").And.Contain("Path").And.Contain("SasKeyName").And.Contain("MessageFormat"));
            Assert.That(GridProperties(queue), Does.Not.Contain("Url").And.Not.Contain("SolutionNamespace"));

            var webhook = new CrmServiceEndpoint(null)
            {
                Contract = CrmServiceEndpointContract.Webhook,
                Url = "https://example.invalid/hook",
                Path = " ",
                MessageFormat = CrmServiceEndpointMessageFormat.Json,
            };
            Assert.That(GridProperties(webhook), Does.Contain("Url"));
            Assert.That(GridProperties(webhook), Does.Not.Contain("NamespaceAddress").And.Not.Contain("Path").And.Not.Contain("MessageFormat"));
        }

        [TestCase(CrmServiceEndpointContract.ManagedDataLake, true)]
        [TestCase(CrmServiceEndpointContract.ContainerStorage, true)]
        [TestCase(CrmServiceEndpointContract.PersistentQueue, false)]
        [TestCase(CrmServiceEndpointContract.Queue, false)]
        [TestCase(CrmServiceEndpointContract.EventGrid, false)]
        [TestCase(CrmServiceEndpointContract.Webhook, false)]
        public void EndpointsDataverseManagesItselfAreSystemItems(CrmServiceEndpointContract contract, bool system)
        {
            // System items can't be updated, unregistered or get new steps
            Assert.That(new CrmServiceEndpoint(null) { Contract = contract }.IsSystemCrmEntity, Is.EqualTo(system));
        }

        [Test]
        public void AStepOnAServiceEndpointHasTheEndpointAsItsHandler()
        {
            var endpointId = Guid.NewGuid();
            // As loaded: the endpoint stands in for the assembly, and Dataverse fills in the Service Bus plug-in
            var step = new CrmPluginStep(null)
            {
                AssemblyId = endpointId,
                PluginId = new CrmServiceEndpoint(null).ServiceBusPlugin,
                ServiceBusConfigurationId = endpointId,
            };

            var entity = (SdkMessageProcessingStep)step.GenerateCrmEntities()[SdkMessageProcessingStep.EntityLogicalName];

            Assert.That(entity.EventHandler.LogicalName, Is.EqualTo(ServiceEndpoint.EntityLogicalName));
            Assert.That(entity.EventHandler.Id, Is.EqualTo(endpointId));
        }

        [Test]
        public void TheRelaysAndTheServiceBusEntitiesUseANamespace()
        {
            var namespaced = Enum.GetValues(typeof(CrmServiceEndpointContract)).Cast<CrmServiceEndpointContract>()
                .Where(CrmServiceEndpoint.IsNamespaceContract);
            Assert.That(namespaced, Is.EquivalentTo(new[]
            {
                CrmServiceEndpointContract.OneWay, CrmServiceEndpointContract.Queue, CrmServiceEndpointContract.Rest,
                CrmServiceEndpointContract.TwoWay, CrmServiceEndpointContract.Topic, CrmServiceEndpointContract.PersistentQueue,
                CrmServiceEndpointContract.EventHub,
            }));
        }

        [Test, Apartment(ApartmentState.STA)]
        public void ANewQueueNeedsANamespaceAQueueNameAndAKey()
        {
            using (var form = NewForm(null))
            {
                Assert.That(IsShown(Field<Label>(form, "lblSecretHint")), Is.False);
                Assert.That(IsShown(Field<TextBox>(form, "txtNamespace")), Is.True);
                Assert.That(IsShown(Field<TextBox>(form, "txtUrl")), Is.False);

                Field<TextBox>(form, "txtName").Text = "Orders";
                Assert.That(Build(form, out var errors), Is.Null);
                Assert.That(errors, Is.EqualTo(new[]
                {
                    "Namespace address is required.",
                    "Queue name is required.",
                    "SAS key name is required.",
                    "SAS key is required.",
                }));

                Field<TextBox>(form, "txtNamespace").Text = "contoso";
                Field<TextBox>(form, "txtPath").Text = " orders ";
                Field<TextBox>(form, "txtSasKeyName").Text = "send";
                Field<TextBox>(form, "txtSasKey").Text = " key ";
                Build(form, out errors);
                Assert.That(errors, Is.EqualTo(new[] { "Namespace address must be the namespace's host name, such as contoso.servicebus.windows.net." }));

                // The plain host name is enough: the sb:// form Dataverse wants is filled in
                Field<TextBox>(form, "txtNamespace").Text = "contoso.servicebus.windows.net";
                var endpoint = Build(form, out errors);
                Assert.That(errors, Is.Empty);
                Assert.That(endpoint.NamespaceAddress, Is.EqualTo(Namespace));
                Assert.That(endpoint.ServiceEndpointId, Is.EqualTo(Guid.Empty));
                Assert.That(endpoint.Contract, Is.EqualTo(CrmServiceEndpointContract.PersistentQueue), "Dataverse rejects the old Queue contract");
                Assert.That(endpoint.Path, Is.EqualTo("orders"));
                Assert.That(endpoint.AuthType, Is.EqualTo(CrmServiceEndpointAuthType.SASKey));
                Assert.That(endpoint.SasKey, Is.EqualTo("key"));
                Assert.That(endpoint.MessageFormat, Is.EqualTo(CrmServiceEndpointMessageFormat.Json));
                Assert.That(endpoint.UserClaim, Is.EqualTo(CrmServiceEndpointUserClaim.None));
                Assert.That(endpoint.ConnectionMode, Is.EqualTo(CrmServiceEndpointConnectionMode.Normal));
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void ASasTokenReplacesTheKeyFields()
        {
            using (var form = NewForm(null))
            {
                Choose(form, "cmbContract", "Event Hub");
                Choose(form, "cmbAuth", "SAS token");

                Assert.That(IsShown(Field<TextBox>(form, "txtSasKey")), Is.False);
                Assert.That(IsShown(Field<TextBox>(form, "txtSasToken")), Is.True);

                Field<TextBox>(form, "txtName").Text = "Telemetry";
                Field<TextBox>(form, "txtNamespace").Text = Namespace;
                Build(form, out var errors);
                Assert.That(errors, Is.EqualTo(new[] { "Event Hub name is required.", "SAS token is required." }));

                Field<TextBox>(form, "txtPath").Text = "hub";
                Field<TextBox>(form, "txtSasToken").Text = "SharedAccessSignature sr=x&sig=y";
                var endpoint = Build(form, out errors);
                Assert.That(errors, Is.Empty);
                Assert.That(endpoint.Contract, Is.EqualTo(CrmServiceEndpointContract.EventHub));
                Assert.That(endpoint.AuthType, Is.EqualTo(CrmServiceEndpointAuthType.SASToken));
                Assert.That(endpoint.SasToken, Is.EqualTo("SharedAccessSignature sr=x&sig=y"));
                Assert.That(endpoint.SasKeyName, Is.Null);
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void ARelayDefaultsToBinaryXmlAndNeedsNoPath()
        {
            using (var form = NewForm(null))
            {
                Choose(form, "cmbContract", "Two-way relay");
                Assert.That(Field<ComboBox>(form, "cmbMessageFormat").SelectedItem.ToString(), Is.EqualTo("Binary XML"));

                Field<TextBox>(form, "txtName").Text = "Relay";
                Field<TextBox>(form, "txtNamespace").Text = Namespace;
                Field<TextBox>(form, "txtSasKeyName").Text = "listen";
                Field<TextBox>(form, "txtSasKey").Text = "key";
                Assert.That(Build(form, out var errors), Is.Not.Null);
                Assert.That(errors, Is.Empty);
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void AnEventGridTopicNeedsAnHttpsUrlAndAnAccessKey()
        {
            using (var form = NewForm(null))
            {
                Choose(form, "cmbContract", "Event Grid");
                Assert.That(IsShown(Field<TextBox>(form, "txtNamespace")), Is.False);
                Assert.That(IsShown(Field<TextBox>(form, "txtUrl")), Is.True);
                Assert.That(IsShown(Field<TextBox>(form, "txtAccessKey")), Is.True);
                Assert.That(Field<ComboBox>(form, "cmbAuth").SelectedItem.ToString(), Is.EqualTo("Access key"));

                Field<TextBox>(form, "txtName").Text = "Grid";
                Field<TextBox>(form, "txtUrl").Text = "http://topic.example.invalid/api/events";
                Build(form, out var errors);
                Assert.That(errors, Is.EqualTo(new[] { "Topic endpoint URL must be a full https:// address.", "Access key is required." }));

                Field<TextBox>(form, "txtUrl").Text = "https://topic.example.invalid/api/events";
                Field<TextBox>(form, "txtAccessKey").Text = "key";
                var endpoint = Build(form, out errors);
                Assert.That(errors, Is.Empty);
                Assert.That(endpoint.Contract, Is.EqualTo(CrmServiceEndpointContract.EventGrid));
                Assert.That(endpoint.AuthType, Is.EqualTo(CrmServiceEndpointAuthType.AccessKey));
                Assert.That(endpoint.AuthValue, Is.EqualTo("key"));
                Assert.That(endpoint.NamespaceAddress, Is.Null);
                Assert.That(endpoint.MessageFormat, Is.Null);
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void EditingFillsInTheSavedValuesAndKeepsTheKeyWhenLeftEmpty()
        {
            var existing = SavedTopic();
            using (var form = NewForm(existing))
            {
                Assert.That(Field<TextBox>(form, "txtName").Text, Is.EqualTo("Orders"));
                Assert.That(Field<ComboBox>(form, "cmbContract").SelectedItem.ToString(), Is.EqualTo("Topic"));
                Assert.That(Field<TextBox>(form, "txtNamespace").Text, Is.EqualTo(Namespace));
                Assert.That(Field<TextBox>(form, "txtPath").Text, Is.EqualTo("orders"));
                Assert.That(Field<TextBox>(form, "txtSasKeyName").Text, Is.EqualTo("send"));
                Assert.That(Field<ComboBox>(form, "cmbMessageFormat").SelectedItem.ToString(), Is.EqualTo("Text XML"));
                Assert.That(Field<ComboBox>(form, "cmbUserClaim").SelectedItem.ToString(), Is.EqualTo("User info"));
                Assert.That(Field<Label>(form, "lblSecretHint").Text, Does.Contain("Leave empty to keep them"));

                Field<TextBox>(form, "txtPath").Text = "orders-v2";
                var endpoint = Build(form, out var errors);
                Assert.That(errors, Is.Empty);
                Assert.That(endpoint.ServiceEndpointId, Is.EqualTo(existing.ServiceEndpointId));
                Assert.That(endpoint.Path, Is.EqualTo("orders-v2"));
                Assert.That(endpoint.SasKey, Is.Null, "Empty keeps the saved key");
                Assert.That(endpoint.MessageFormat, Is.EqualTo(CrmServiceEndpointMessageFormat.TextXml));
                Assert.That(Entity(endpoint).Attributes.ContainsKey("saskey"), Is.False);

                // Moving to another Service Bus entity keeps the key as well
                Choose(form, "cmbContract", "Queue");
                Assert.That(Build(form, out errors).SasKey, Is.Null);
                Assert.That(errors, Is.Empty);
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void ChangingTheAuthenticationTypeNeedsTheNewSecret()
        {
            using (var form = NewForm(SavedTopic()))
            {
                Choose(form, "cmbAuth", "SAS token");
                Assert.That(Field<Label>(form, "lblSecretHint").Text, Is.EqualTo("Enter the key for the new authentication type."));
                Assert.That(Build(form, out var errors), Is.Null);
                Assert.That(errors, Is.EqualTo(new[] { "SAS token is required." }));
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void AnEndpointWithAnAuthenticationTheDialogDoesntOfferCanStillBeOpened()
        {
            // For example an old ACS endpoint (authtype 1): the dialog falls back to SAS key and asks for one
            var existing = SavedTopic();
            existing.AuthType = (CrmServiceEndpointAuthType)1;
            using (var form = NewForm(existing))
            {
                Assert.That(Field<ComboBox>(form, "cmbAuth").SelectedItem.ToString(), Is.EqualTo("SAS key"));
                Build(form, out var errors);
                Assert.That(errors, Is.EqualTo(new[] { "SAS key is required." }));
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void AnEndpointOnTheOldQueueContractIsSavedAsAPersistentQueue()
        {
            var existing = SavedTopic();
            existing.Contract = CrmServiceEndpointContract.Queue;
            using (var form = NewForm(existing))
            {
                Assert.That(Field<ComboBox>(form, "cmbContract").SelectedItem.ToString(), Is.EqualTo("Queue"));
                var endpoint = Build(form, out var errors);
                Assert.That(errors, Is.Empty);
                Assert.That(endpoint.Contract, Is.EqualTo(CrmServiceEndpointContract.PersistentQueue));
                Assert.That(endpoint.SasKey, Is.Null, "Still a namespace endpoint with the same authentication, so the key is kept");
            }
        }

        [Test]
        public void TheImageDialogPreselectsTheOnlyStepUnderTheClickedNode()
        {
            var step = new CrmPluginStep(null) { StepId = Guid.NewGuid() };
            var endpoint = new Node(CrmTreeNodeType.ServiceEndpoint, step);
            var assembly = new Node(CrmTreeNodeType.Assembly, new Node(CrmTreeNodeType.Plugin), new Node(CrmTreeNodeType.Plugin, step));

            Assert.That(ImageRegistrationForm.FindOnlyStep(endpoint), Is.SameAs(step));
            Assert.That(ImageRegistrationForm.FindOnlyStep(assembly), Is.SameAs(step), "Steps are found at any depth");
            Assert.That(ImageRegistrationForm.FindOnlyStep(step), Is.SameAs(step), "Opened from the step itself");
        }

        [Test]
        public void TheImageDialogLeavesTheChoiceWhenThereIsNoSingleStep()
        {
            var two = new Node(CrmTreeNodeType.ServiceEndpoint,
                new CrmPluginStep(null) { StepId = Guid.NewGuid() },
                new CrmPluginStep(null) { StepId = Guid.NewGuid() });

            Assert.That(ImageRegistrationForm.FindOnlyStep(two), Is.Null, "Two steps: the user picks");
            Assert.That(ImageRegistrationForm.FindOnlyStep(new Node(CrmTreeNodeType.ServiceEndpoint)), Is.Null, "No steps");
            Assert.That(ImageRegistrationForm.FindOnlyStep(null), Is.Null);
        }

        [TestCase("Create", true)]
        [TestCase("create", true)]
        [TestCase("Update", false)]
        [TestCase("Delete", false)]
        public void OnCreateThePostImageIsTheOnlyChoice(string message, bool onlyPost)
        {
            Assert.That(ImageRegistrationForm.OnlyPostImageAllowed(message), Is.EqualTo(onlyPost));
        }

        private sealed class Node : ICrmTreeNode
        {
            public Node(CrmTreeNodeType type, params ICrmTreeNode[] children)
            {
                NodeType = type;
                NodeChildren = children.ToList();
            }

            public List<ICrmTreeNode> NodeChildren { get; }
            public Guid NodeId { get; } = Guid.NewGuid();
            public CrmTreeNodeImageType NodeImageType => CrmTreeNodeImageType.Assembly;
            public CrmTreeNodeImageType NodeSelectedImageType => CrmTreeNodeImageType.Assembly;
            public string NodeText => NodeType.ToString();
            public CrmTreeNodeType NodeType { get; }
            public string NodeTypeLabel => NodeType.ToString();
        }

        private static CrmServiceEndpoint SavedTopic() => new CrmServiceEndpoint(null)
        {
            ServiceEndpointId = Guid.NewGuid(),
            Name = "Orders",
            Contract = CrmServiceEndpointContract.Topic,
            NamespaceAddress = Namespace,
            Path = "orders",
            AuthType = CrmServiceEndpointAuthType.SASKey,
            SasKeyName = "send",
            MessageFormat = CrmServiceEndpointMessageFormat.TextXml,
            UserClaim = CrmServiceEndpointUserClaim.UserInfo,
        };

        private static string[] GridProperties(CrmServiceEndpoint endpoint) =>
            TypeDescriptor.GetProperties(endpoint, new Attribute[] { BrowsableAttribute.Yes }).Cast<PropertyDescriptor>().Select(p => p.Name).ToArray();

        private static ServiceEndpoint Entity(CrmServiceEndpoint endpoint) =>
            (ServiceEndpoint)endpoint.GenerateCrmEntities()[ServiceEndpoint.EntityLogicalName];

        private static ServiceEndpointRegistrationForm NewForm(CrmServiceEndpoint existing) =>
            new ServiceEndpointRegistrationForm((CrmOrganization)FormatterServices.GetUninitializedObject(typeof(CrmOrganization)), null, existing);

        private static CrmServiceEndpoint Build(ServiceEndpointRegistrationForm form, out List<string> errors)
        {
            var args = new object[] { null };
            var endpoint = (CrmServiceEndpoint)typeof(ServiceEndpointRegistrationForm).GetMethod("BuildServiceEndpoint", Flags).Invoke(form, args);
            errors = (List<string>)args[0];
            return endpoint;
        }

        private static void Choose(ServiceEndpointRegistrationForm form, string comboBox, string label)
        {
            var box = Field<ComboBox>(form, comboBox);
            box.SelectedItem = box.Items.Cast<object>().Single(item => item.ToString() == label);
        }

        private static T Field<T>(ServiceEndpointRegistrationForm form, string name) =>
            (T)typeof(ServiceEndpointRegistrationForm).GetField(name, Flags).GetValue(form);

        /// <summary>The control's own Visible setting; Visible reads false while the form isn't shown.</summary>
        private static bool IsShown(Control control) => (bool)typeof(Control).GetMethod("GetState", Flags).Invoke(control, new object[] { 0x2 });
    }
}
