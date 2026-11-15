namespace Xrm.Sdk.PluginRegistration.Tests.Integration
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Metadata;
    using NUnit.Framework;
    using System;
    using System.Linq;
    using System.ServiceModel;

    /// <summary>
    /// Checks the serviceendpoint values the tool sends against a real environment: the enums
    /// against the option sets, and each endpoint type the form offers against Create.
    /// </summary>
    [TestFixture]
    [Category(TestBench.Category)]
    internal class ServiceEndpointMetadataTests
    {
        private TestBench bench;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            bench = TestBench.LoadOrIgnore();
            bench.LoadOrganization();
        }

        [TestCase("contract", typeof(CrmServiceEndpointContract))]
        [TestCase("authtype", typeof(CrmServiceEndpointAuthType))]
        [TestCase("messageformat", typeof(CrmServiceEndpointMessageFormat))]
        [TestCase("userclaim", typeof(CrmServiceEndpointUserClaim))]
        [TestCase("connectionmode", typeof(CrmServiceEndpointConnectionMode))]
        public void EveryEnumValueIsAnOption(string attribute, Type enumType)
        {
            var response = (RetrieveAttributeResponse)bench.Service.Execute(new RetrieveAttributeRequest
            {
                EntityLogicalName = "serviceendpoint",
                LogicalName = attribute,
            });
            var options = ((EnumAttributeMetadata)response.AttributeMetadata).OptionSet.Options
                .ToDictionary(o => o.Value.Value, o => o.Label?.UserLocalizedLabel?.Label);

            foreach (var value in Enum.GetValues(enumType))
            {
                Assert.That(options.ContainsKey((int)value), Is.True, $"{attribute} has no option {(int)value} for {value}");
            }

            // Labels are localized, so they're only logged, to help when a value is wrong
            TestContext.Progress.WriteLine($"{attribute}: " + string.Join(", ", options.OrderBy(o => o.Key).Select(o => $"{o.Key}={o.Value}")));
        }

        /// <summary>
        /// Every type ServiceEndpointRegistrationForm offers can be created as the form builds it.
        /// Dataverse doesn't contact the namespace or topic when saving, so the addresses are fake.
        /// </summary>
        [TestCase(CrmServiceEndpointContract.OneWay)]
        [TestCase(CrmServiceEndpointContract.Rest)]
        [TestCase(CrmServiceEndpointContract.TwoWay)]
        [TestCase(CrmServiceEndpointContract.Topic)]
        [TestCase(CrmServiceEndpointContract.PersistentQueue)]
        [TestCase(CrmServiceEndpointContract.EventHub)]
        [TestCase(CrmServiceEndpointContract.EventGrid)]
        public void EveryOfferedTypeCanBeCreated(CrmServiceEndpointContract contract)
        {
            var id = bench.Service.Create(NewEndpoint(contract));
            bench.Service.Delete("serviceendpoint", id);
        }

        /// <summary>
        /// Why the form offers "Queue" as the persistent queue contract.
        /// </summary>
        [Test]
        public void TheOldQueueContractIsRejected()
        {
            var fault = Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
            {
                var id = bench.Service.Create(NewEndpoint(CrmServiceEndpointContract.Queue));
                bench.Service.Delete("serviceendpoint", id);
            });
            Assert.That(fault.Message, Does.Contain("PersistentQueue"));
        }

        private static PluginRegistration.Entities.ServiceEndpoint NewEndpoint(CrmServiceEndpointContract contract)
        {
            var usesNamespace = CrmServiceEndpoint.IsNamespaceContract(contract);
            var endpoint = new CrmServiceEndpoint(null)
            {
                Name = "PRT-IT type check " + contract,
                Contract = contract,
                ConnectionMode = CrmServiceEndpointConnectionMode.Normal,
                UserClaim = CrmServiceEndpointUserClaim.None,
                AuthType = usesNamespace ? CrmServiceEndpointAuthType.SASKey : CrmServiceEndpointAuthType.AccessKey,
                NamespaceAddress = usesNamespace ? "sb://prt-integration-test.servicebus.windows.net/" : null,
                Path = usesNamespace ? "probe" : null,
                SasKeyName = usesNamespace ? "RootManageSharedAccessKey" : null,
                SasKey = usesNamespace ? "cHJ0LWludGVncmF0aW9uLXRlc3Qta2V5LW5vdC1yZWFsLTEyMzQ1Njc4OTA=" : null,
                MessageFormat = usesNamespace ? CrmServiceEndpointMessageFormat.Json : (CrmServiceEndpointMessageFormat?)null,
                Url = usesNamespace ? null : "https://prt-integration-test.westeurope-1.eventgrid.azure.net/api/events",
                AuthValue = usesNamespace ? null : "cHJ0LWludGVncmF0aW9uLXRlc3Qta2V5LW5vdC1yZWFsLTEyMzQ1Njc4OTA=",
            };
            return (PluginRegistration.Entities.ServiceEndpoint)endpoint.GenerateCrmEntities()["serviceendpoint"];
        }
    }
}
