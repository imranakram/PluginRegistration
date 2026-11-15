using NUnit.Framework;
using Xrm.Sdk.PluginRegistration.Controls;

namespace Xrm.Sdk.PluginRegistration.Tests.ContextMenus
{
    [TestFixture]
    public class RegisterMenuRuleTests
    {
        [TestCase(null, true, true)]
        [TestCase(CrmTreeNodeType.Assembly, true, true)]
        [TestCase(CrmTreeNodeType.Plugin, true, true)]
        [TestCase(CrmTreeNodeType.WorkflowActivity, true, true)]
        [TestCase(CrmTreeNodeType.ServiceEndpoint, true, true)]
        [TestCase(CrmTreeNodeType.WebHook, true, true)]
        [TestCase(CrmTreeNodeType.Message, true, true)]
        [TestCase(CrmTreeNodeType.MessageEntity, true, true)]
        [TestCase(CrmTreeNodeType.CustomApi, true, true)]
        [TestCase(CrmTreeNodeType.Step, false, true)]
        [TestCase(CrmTreeNodeType.Image, false, false)]
        public void RegisterItemsAreOfferedWhereTheyMakeSense(CrmTreeNodeType? selected, bool step, bool image)
        {
            Assert.That(MainControl.CanRegisterStepFrom(selected), Is.EqualTo(step), "Register New Step");
            Assert.That(MainControl.CanRegisterImageFrom(selected), Is.EqualTo(image), "Register New Image");
        }
    }
}
