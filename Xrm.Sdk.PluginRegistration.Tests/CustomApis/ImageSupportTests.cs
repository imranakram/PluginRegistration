using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms;
using Xrm.Sdk.PluginRegistration.Controls;
using Xrm.Sdk.PluginRegistration.Forms;
using Xrm.Sdk.PluginRegistration.Helpers;
using Xrm.Sdk.PluginRegistration.Models;
using Xrm.Sdk.PluginRegistration.Wrappers;

namespace Xrm.Sdk.PluginRegistration.Tests.CustomApis
{
    /// <summary>
    /// Only some messages support entity images, and Custom API messages never do. Register New Image
    /// is offered only where it can work, and the image dialog says why a step can't have one.
    /// </summary>
    [TestFixture, Apartment(ApartmentState.STA)]
    public class ImageSupportTests
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void TheReasonNamesTheMessageACustomApiAndTheMessagesThatCan()
        {
            var update = Message("Update", imageProperty: "Target");
            var create = Message("Create", imageProperty: "Id");
            var api = Message("cr418_apid");
            var retrieve = Message("Retrieve");
            var messages = new[] { update, create, api, retrieve };
            var apis = new[] { new CustomApiSummary { UniqueName = "cr418_apid", SdkMessageId = api.MessageId } };

            Assert.That(ImageSupport.SupportedMessages(messages), Is.EqualTo(new[] { "Create", "Update" }));
            Assert.That(ImageSupport.Reason(update, apis, messages), Is.Null);
            Assert.That(ImageSupport.Reason(api, apis, messages), Is.EqualTo(
                "Steps on cr418_apid can't have images: it's a Custom API message. Dataverse supports images only on steps on Create, Update."));
            Assert.That(ImageSupport.Reason(retrieve, apis, messages), Does.StartWith("Steps on Retrieve can't have images. "));
            Assert.That(ImageSupport.Reason(null, apis, messages), Is.Null);
            Assert.That(ImageSupport.DocsUrl, Does.EndWith("#messages-that-support-entity-images"));
        }

        [Test]
        public void RegisterNewImageIsOfferedOnlyOnStepsWhoseMessageCanHaveImages()
        {
            using (var control = new MainControl())
            {
                var update = Message("Update", imageProperty: "Target");
                var api = Message("cr418_apid");
                Connect(control, update, api);
                ApplyCatalog(control, new CustomApiSummary { UniqueName = "cr418_apid", SdkMessageId = api.MessageId });
                var commands = new[] { "toolImageRegister", "mnuContextNodeImageRegister", "mnuContextGeneralImageRegister" }
                    .Select(name => (ToolStripItem)typeof(MainControl).GetField(name, Instance).GetValue(control)).ToList();

                Invoke(control, "SelectItem", Step(api));
                Assert.That(commands.Select(item => item.Enabled), Is.All.False);
                Assert.That(commands.Select(item => item.ToolTipText), Is.All.Contains("it's a Custom API message"));

                Invoke(control, "SelectItem", Step(update));
                Assert.That(commands.Select(item => item.Enabled), Is.All.True);
                Assert.That(commands.Select(item => item.ToolTipText), Is.All.Null);
            }
        }

        [Test]
        public void TheImageDialogLinksToMicrosoftsListAndSaysWhyAStepCantHaveOne()
        {
            using (var control = new MainControl())
            {
                var update = Message("Update", imageProperty: "Target");
                var api = Message("cr418_apid");
                var org = Connect(control, update, api);
                ApplyCatalog(control, new CustomApiSummary { UniqueName = "cr418_apid", SdkMessageId = api.MessageId });
                var onApi = Step(api);
                var onUpdate = Step(update);

                using (var form = new ImageRegistrationForm(org, control, new List<ICrmTreeNode> { onApi, onUpdate }, null, onApi.StepId))
                {
                    var link = (LinkLabel)typeof(ImageRegistrationForm).GetField("lnkImageSupport", Instance).GetValue(form);
                    var register = (Button)typeof(ImageRegistrationForm).GetField("btnRegister", Instance).GetValue(form);

                    Assert.That(register.Enabled, Is.False);
                    Assert.That(link.Text, Is.EqualTo("Steps on cr418_apid can't have images: it's a Custom API message. " + ImageRegistrationForm.LearnMore));
                    Assert.That(link.Text.Substring(link.LinkArea.Start, link.LinkArea.Length), Is.EqualTo(ImageRegistrationForm.LearnMore));

                    var select = typeof(ImageRegistrationForm).GetMethod("trvPlugins_SelectionChanged", Instance);
                    select.Invoke(form, new object[] { null, new CrmTreeNodeTreeEventArgs(onUpdate, TreeViewAction.Unknown) });

                    Assert.That(register.Enabled, Is.True);
                    Assert.That(link.Text, Is.EqualTo(ImageRegistrationForm.ImageSupportLink));
                    Assert.That(link.LinkArea.Length, Is.EqualTo(ImageRegistrationForm.ImageSupportLink.Length));
                }
            }
        }

        private static CrmMessage Message(string name, string imageProperty = null)
        {
            var properties = new List<ImageMessagePropertyName>();
            if (imageProperty != null) properties.Add(new ImageMessagePropertyName(imageProperty, "Record"));
            return new CrmMessage(null, Guid.NewGuid(), name, false, 1, null, null, properties);
        }

        private static CrmPluginStep Step(CrmMessage message) => new CrmPluginStep(null)
        {
            StepId = Guid.NewGuid(), MessageId = message.MessageId, Name = "Step on " + message.Name
        };

        private static CrmOrganization Connect(MainControl control, params CrmMessage[] messages)
        {
            var org = (CrmOrganization)FormatterServices.GetUninitializedObject(typeof(CrmOrganization));
            typeof(CrmOrganization).GetField("m_messageList", Instance)
                .SetValue(org, messages.ToDictionary(message => message.MessageId));
            typeof(CrmOrganization).GetField("m_messageEntityList", Instance)
                .SetValue(org, new Dictionary<Guid, CrmMessageEntity>());
            typeof(MainControl).GetField("m_org", Instance).SetValue(control, org);
            // The menus and toolbar are disabled as a whole until there is a connection.
            typeof(MainControl).GetField("connectionReady", Instance).SetValue(control, true);
            Invoke(control, "SetRegistrationControlsEnabled", true);
            return org;
        }

        private static void ApplyCatalog(MainControl control, params CustomApiSummary[] apis)
        {
            var catalogType = typeof(MainControl).GetNestedType("CustomApiCatalog", BindingFlags.NonPublic);
            var catalog = Activator.CreateInstance(catalogType, true);
            catalogType.GetProperty("Supported").SetValue(catalog, true);
            catalogType.GetProperty("Definitions").SetValue(catalog, apis);
            Invoke(control, "ApplyCustomApiCatalog", catalog);
        }

        private static void Invoke(MainControl control, string name, params object[] arguments)
        {
            try
            {
                typeof(MainControl).GetMethod(name, Instance).Invoke(control, arguments);
            }
            catch (TargetInvocationException error)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            }
        }
    }
}
