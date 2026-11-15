using NUnit.Framework;
using Xrm.Sdk.PluginRegistration.Controls;

namespace Xrm.Sdk.PluginRegistration.Tests.ContextMenus
{
    [TestFixture]
    public class RegisterMenuRuleTests
    {
        [TestCase(null, true, false)]
        [TestCase(CrmTreeNodeType.Assembly, true, false)]
        [TestCase(CrmTreeNodeType.Plugin, true, false)]
        [TestCase(CrmTreeNodeType.WorkflowActivity, true, false)]
        [TestCase(CrmTreeNodeType.ServiceEndpoint, true, false)]
        [TestCase(CrmTreeNodeType.WebHook, true, false)]
        [TestCase(CrmTreeNodeType.Message, true, false)]
        [TestCase(CrmTreeNodeType.MessageEntity, true, false)]
        [TestCase(CrmTreeNodeType.CustomApi, true, false)]
        [TestCase(CrmTreeNodeType.Step, false, true)]
        [TestCase(CrmTreeNodeType.Image, false, false)]
        public void RegisterItemsAreOfferedWhereTheyMakeSense(CrmTreeNodeType? selected, bool step, bool image)
        {
            Assert.That(MainControl.CanRegisterStepFrom(selected), Is.EqualTo(step), "Register New Step");
            Assert.That(MainControl.CanRegisterImageFrom(selected), Is.EqualTo(image), "Register New Image");
        }
    }
}
