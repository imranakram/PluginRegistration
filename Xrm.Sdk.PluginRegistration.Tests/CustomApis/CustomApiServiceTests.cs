namespace Xrm.Sdk.PluginRegistration.Tests.CustomApis
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Metadata;
    using Microsoft.Xrm.Sdk.Query;
    using NUnit.Framework;
    using System;
    using System.Linq;
    using System.ServiceModel;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Microsoft.Xrm.Sdk.WebServiceClient;
    using Xrm.Sdk.PluginRegistration.Helpers;
    using Xrm.Sdk.PluginRegistration.Models;

    [TestFixture]
    internal class CustomApiServiceTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void PublisherLookupFailureIsKnownToPrecedeAnyWrite(bool transportFailure)
        {
            var service = new RecordingOrganizationService
            {
                OnRetrieveMultiple = _ => transportFailure ? throw new TimeoutException("Read timed out") : new EntityCollection()
            };
            var error = Assert.Catch(() => new CustomApiService(service).Create(Draft(), "MissingSolution"));
            Assert.That(CustomApiService.IsSaveOutcomeUnknown(error), Is.False);
            Assert.That(service.Requests, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AddToSolutionSendsCompletePagedContractInOneTransaction(bool transactionFails)
        {
            var apiId = Guid.NewGuid();
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();
            var output = Guid.NewGuid();
            var fault = new FaultException<OrganizationServiceFault>(new ExecuteTransactionFault
            {
                FaultedRequestIndex = 2,
                Message = "A child could not be added to the solution."
            });
            var types = new System.Collections.Generic.Dictionary<string, int>
            {
                ["customapi"] = 10001, ["customapirequestparameter"] = 10002, ["customapiresponseproperty"] = 10003
            };
            var service = new RecordingOrganizationService
            {
                OnRetrieveMultiple = query =>
                {
                    if (query.EntityName == "solutioncomponentdefinition")
                        return new EntityCollection(query.Criteria.Conditions.Single().Values.Cast<string>()
                            .Select(table => new Entity(query.EntityName)
                            {
                                ["primaryentityname"] = table, ["solutioncomponenttype"] = types[table]
                            }).ToList());
                    Assert.That(query.Criteria.Conditions.Single().AttributeName, Is.EqualTo("customapiid"));
                    Assert.That(query.Criteria.Conditions.Single().Values.Single(), Is.EqualTo(apiId));
                    bool request = query.EntityName == "customapirequestparameter";
                    bool more = request && query.PageInfo.PageNumber == 1;
                    return new EntityCollection(new[] { new Entity(query.EntityName, request ? (more ? first : second) : output) })
                    {
                        MoreRecords = more, PagingCookie = more ? "next" : null
                    };
                },
                OnExecute = _ => transactionFails ? throw fault : new ExecuteTransactionResponse()
            };

            if (transactionFails)
                Assert.That(Assert.Throws<FaultException<OrganizationServiceFault>>(
                    () => new CustomApiService(service).AddToSolution(apiId, "TargetSolution")), Is.SameAs(fault));
            else
                new CustomApiService(service).AddToSolution(apiId, "TargetSolution");

            Assert.That(service.Requests, Has.Count.EqualTo(1));
            Assert.That(service.Requests.Single(), Is.TypeOf<ExecuteTransactionRequest>());
            var transaction = (ExecuteTransactionRequest)service.Requests.Single();
            Assert.That(transaction.ReturnResponses, Is.False);
            var adds = transaction.Requests.Cast<Microsoft.Crm.Sdk.Messages.AddSolutionComponentRequest>().ToList();
            Assert.That(adds.Select(add => add.ComponentId), Is.EqualTo(new[] { apiId, first, second, output }));
            Assert.That(adds.Select(add => add.ComponentType), Is.EqualTo(new[] { 10001, 10002, 10002, 10003 }));
            Assert.That(adds.All(add => add.SolutionUniqueName == "TargetSolution" && !add.AddRequiredComponents), Is.True);
        }

        [TestCase("Create")]
        [TestCase("ExecuteTransaction")]
        public void InstalledCrmClientThrowsTypedServerRejectionWithoutWrapping(string requestName)
        {
            // Exercise the real installed client's Execute and error handling. Substitute only
            // its service proxy so this test requires neither credentials nor a server write.
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var clientType = Type.GetType("Microsoft.Xrm.Tooling.Connector.CrmServiceClient, Microsoft.Xrm.Tooling.Connector", true);
            var client = (IOrganizationService)Activator.CreateInstance(clientType, true);
            var connectionField = clientType.GetField("CrmConnectionService", flags);
            var connection = FormatterServices.GetUninitializedObject(connectionField.FieldType);
            var authField = connection.GetType().GetField("_eAuthType", flags);
            authField.SetValue(connection, Enum.Parse(authField.FieldType, "ExternalTokenManagement"));
            var fault = SdkFault();
            using (var proxy = new RejectingWebProxy(fault))
            {
                connection.GetType().GetField("_externalWebClientProxy", flags).SetValue(connection, proxy);
                connectionField.SetValue(client, connection);
                var logField = clientType.GetField("logEntry", flags);
                logField.SetValue(client, Activator.CreateInstance(logField.FieldType, new object[] { "Client fault regression test" }));
                try
                {
                    var thrown = Assert.Throws<FaultException<OrganizationServiceFault>>(
                        () => client.Execute(new OrganizationRequest(requestName)));
                    Assert.That(thrown, Is.SameAs(fault));
                    Assert.That(proxy.Calls, Is.EqualTo(1));
                    Assert.That(CustomApiService.IsSaveOutcomeUnknown(thrown), Is.False);
                }
                finally
                {
                    connectionField.SetValue(client, null);
                    ((IDisposable)client).Dispose();
                }
            }
        }

        private sealed class RejectingWebProxy : OrganizationWebProxyClient, IOrganizationService
        {
            private readonly Exception error;
            public int Calls { get; private set; }

            public RejectingWebProxy(Exception error) : base(new Uri("http://localhost/unused"), false)
            {
                this.error = error;
            }

            OrganizationResponse IOrganizationService.Execute(OrganizationRequest request)
            {
                Calls++;
                throw error;
            }
        }

        [Test]
        public void SaveRejectionIsRecognizedThroughClientWrappers()
        {
            var fault = new FaultException<OrganizationServiceFault>(new OrganizationServiceFault());
            Assert.That(CustomApiService.IsSaveOutcomeUnknown(fault), Is.False);
            Assert.That(CustomApiService.IsSaveOutcomeUnknown(new InvalidOperationException("Client wrapper", fault)), Is.False);
            Assert.That(CustomApiService.IsSaveOutcomeUnknown(new AggregateException(fault)), Is.False);
            Assert.That(CustomApiService.IsSaveOutcomeUnknown(new ArgumentException("Invalid draft")), Is.False);
        }

        [Test]
        public void UnrecognizedOrMixedSaveFailuresStillRequireInspectionBeforeRetry()
        {
            var fault = new FaultException<OrganizationServiceFault>(new OrganizationServiceFault());
            foreach (var error in new Exception[]
            {
                new TimeoutException(), new CommunicationException(), new System.Net.WebException(),
                new InvalidOperationException("Unknown client failure"),
                new InvalidOperationException("Client wrapper", new TimeoutException()),
                new AggregateException(fault, new TimeoutException())
            })
                Assert.That(CustomApiService.IsSaveOutcomeUnknown(error), Is.True, error.ToString());
        }

        [Test]
        public void CreateSendsApiAndChildrenTogetherInTheSelectedSolution()
        {
            var organization = CreateOrganization();
            var newId = Guid.NewGuid();
            organization.OnExecute = request => new CreateResponse { Results = { ["id"] = newId } };
            var definition = Draft();
            definition.PluginTypeId = Guid.NewGuid();
            definition.RequestParameters.Add(RequestParameter());
            definition.ResponseProperties.Add(ResponseProperty());

            var result = new CustomApiService(organization).Create(definition, "ExampleSolution");

            Assert.That(result, Is.EqualTo(newId));
            Assert.That(organization.Requests, Has.Count.EqualTo(1));
            var create = (CreateRequest)organization.Requests.Single();
            Assert.That(create.Parameters["SolutionUniqueName"], Is.EqualTo("ExampleSolution"));
            Assert.That(create.Target.LogicalName, Is.EqualTo("customapi"));
            Assert.That(create.Target.GetAttributeValue<string>("uniquename"), Is.EqualTo(definition.UniqueName));
            Assert.That(create.Target.GetAttributeValue<EntityReference>("plugintypeid").Id, Is.EqualTo(definition.PluginTypeId));
            Assert.That(create.Target.RelatedEntities[new Relationship("customapi_customapirequestparameter")].Entities.Single().GetAttributeValue<string>("uniquename"), Is.EqualTo("Input"));
            Assert.That(create.Target.RelatedEntities[new Relationship("customapi_customapiresponseproperty")].Entities.Single().GetAttributeValue<string>("uniquename"), Is.EqualTo("Output"));
            Assert.That(definition.Id, Is.EqualTo(Guid.Empty));
            Assert.That(definition.RequestParameters.Single().Id, Is.EqualTo(Guid.Empty));
            Assert.That(definition.ResponseProperties.Single().Id, Is.EqualTo(Guid.Empty));

            var solutionQuery = organization.Queries.Single();
            Assert.That(solutionQuery.EntityName, Is.EqualTo("solution"));
            Assert.That(solutionQuery.Criteria.Conditions.Any(c => c.AttributeName == "uniquename" && Equals(c.Values.Single(), "ExampleSolution")), Is.True);
            Assert.That(solutionQuery.Criteria.Conditions.Any(c => c.AttributeName == "ismanaged" && Equals(c.Values.Single(), false)), Is.True);
        }

        [Test]
        public void CreateRejectsAPrefixThatDoesNotMatchTheSolutionPublisher()
        {
            var organization = CreateOrganization();
            var definition = Draft();
            definition.UniqueName = "other_Example";

            Assert.Throws<ArgumentException>(() => new CustomApiService(organization).Create(definition, "ExampleSolution"));
            Assert.That(organization.Requests, Is.Empty);
        }

        [Test]
        public void CreateRejectsAnExistingChildIdBeforeWriting()
        {
            var organization = CreateOrganization();
            var definition = Draft();
            definition.RequestParameters.Add(RequestParameter(Guid.NewGuid()));

            Assert.Throws<ArgumentException>(() => new CustomApiService(organization).Create(definition, "ExampleSolution"));
            Assert.That(organization.Requests, Is.Empty);
        }

        [Test]
        public void UpdateDoesNotSendAnEmptyTransaction()
        {
            var organization = new RecordingOrganizationService();
            var id = Guid.NewGuid();

            Assert.That(new CustomApiService(organization).Update(Draft(id), Draft(id)), Is.False);
            Assert.That(organization.Requests, Is.Empty);
        }

        [Test]
        public void UpdateTreatsMissingAndEmptyTextAsUnchanged()
        {
            var organization = new RecordingOrganizationService();
            var original = Draft(Guid.NewGuid());
            original.DisplayName = null;
            original.RequestParameters.Add(RequestParameter(Guid.NewGuid()));
            original.RequestParameters[0].Name = null;
            var definition = original.Clone();
            definition.DisplayName = string.Empty;
            definition.RequestParameters[0].Name = string.Empty;

            Assert.That(new CustomApiService(organization).Update(original, definition), Is.False);
            Assert.That(organization.Requests, Is.Empty);
        }

        [Test]
        public void UpdateWritesOnlyChangedMutableFieldsAndExplicitNullsForClearedValues()
        {
            var organization = TransactionOrganization();
            var original = Draft(Guid.NewGuid());
            original.ExecutePrivilegeName = "prvReadAccount";
            original.PluginTypeId = Guid.NewGuid();
            var definition = Draft(original.Id);
            definition.DisplayName = "Changed display name";
            definition.IsManaged = true;
            definition.IsCustomizable = false;
            definition.SdkMessageId = Guid.NewGuid();

            Assert.That(new CustomApiService(organization).Update(original, definition), Is.True);

            var transaction = (ExecuteTransactionRequest)organization.Requests.Single();
            var update = (UpdateRequest)transaction.Requests.Single();
            Assert.That(update.Target.Id, Is.EqualTo(original.Id));
            Assert.That(update.Target.Attributes.Keys, Is.EquivalentTo(new[] { "displayname", "executeprivilegename", "plugintypeid" }));
            Assert.That(update.Target["displayname"], Is.EqualTo(definition.DisplayName));
            Assert.That(update.Target["executeprivilegename"], Is.Null);
            Assert.That(update.Target["plugintypeid"], Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void CreateRejectsBlankApiDescriptionBeforeCallingDataverse(string description)
        {
            var organization = new RecordingOrganizationService();
            var definition = Draft();
            definition.Description = description;

            var error = Assert.Throws<ArgumentException>(() =>
                new CustomApiService(organization).Create(definition, "ExampleSolution"));

            Assert.That(error.Message, Does.Contain("Custom API description is required."));
            Assert.That(organization.Requests, Is.Empty);
            Assert.That(organization.Queries, Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void UpdateRejectsClearingApiDescriptionBeforeWriting(string description)
        {
            var organization = new RecordingOrganizationService();
            var original = Draft(Guid.NewGuid());
            var definition = Draft(original.Id);
            definition.Description = description;

            var error = Assert.Throws<ArgumentException>(() =>
                new CustomApiService(organization).Update(original, definition));

            Assert.That(error.Message, Does.Contain("Custom API description is required."));
            Assert.That(organization.Requests, Is.Empty);
        }

        [TestCase(null, "")]
        [TestCase("", null)]
        public void UpdatePreservesExistingBlankApiDescriptionDuringUnrelatedEdit(string saved, string draft)
        {
            var organization = TransactionOrganization();
            var original = Draft(Guid.NewGuid());
            original.Description = saved;
            var definition = Draft(original.Id);
            definition.Description = draft;
            definition.DisplayName = "Updated display name";

            new CustomApiService(organization).Update(original, definition);

            var transaction = (ExecuteTransactionRequest)organization.Requests.Single();
            var update = (UpdateRequest)transaction.Requests.Single();
            Assert.That(update.Target.Attributes.Keys, Is.EquivalentTo(new[] { "displayname" }));
        }

        [Test]
        public void UpdateCanStillClearRequestAndResponseDescriptions()
        {
            var organization = TransactionOrganization();
            var original = Draft(Guid.NewGuid());
            var input = RequestParameter(Guid.NewGuid());
            var output = ResponseProperty(Guid.NewGuid());
            input.Description = output.Description = "Original child description";
            original.RequestParameters.Add(input);
            original.ResponseProperties.Add(output);
            var definition = Draft(original.Id);
            definition.RequestParameters.Add(RequestParameter(input.Id));
            definition.ResponseProperties.Add(ResponseProperty(output.Id));

            new CustomApiService(organization).Update(original, definition);

            var transaction = (ExecuteTransactionRequest)organization.Requests.Single();
            Assert.That(transaction.Requests, Has.Count.EqualTo(2));
            foreach (var update in transaction.Requests.Cast<UpdateRequest>())
            {
                Assert.That(update.Target.Attributes.Keys, Is.EquivalentTo(new[] { "description" }));
                Assert.That(update.Target["description"], Is.Null);
            }
        }

        [Test]
        public void UpdateRejectsImmutableApiChangesBeforeWriting()
        {
            var organization = new RecordingOrganizationService();
            var original = Draft(Guid.NewGuid());
            var definition = Draft(original.Id);
            definition.UniqueName = "publisher_Renamed";

            Assert.Throws<ArgumentException>(() => new CustomApiService(organization).Update(original, definition));
            Assert.That(organization.Requests, Is.Empty);
        }

        [Test]
        public void UpdateRejectsAChildIdMissingFromTheOriginalApi()
        {
            var organization = new RecordingOrganizationService();
            var original = Draft(Guid.NewGuid());
            var definition = Draft(original.Id);
            definition.RequestParameters.Add(RequestParameter(Guid.NewGuid()));

            Assert.Throws<ArgumentException>(() => new CustomApiService(organization).Update(original, definition));
            Assert.That(organization.Requests, Is.Empty);
        }

        [Test]
        public void UpdateCombinesChildDeletionModificationAndCreationInOneTransaction()
        {
            var organization = TransactionOrganization();
            var original = Draft(Guid.NewGuid());
            var deleted = RequestParameter(Guid.NewGuid());
            var modified = ResponseProperty(Guid.NewGuid());
            original.RequestParameters.Add(deleted);
            original.ResponseProperties.Add(modified);
            var definition = Draft(original.Id);
            var replacement = RequestParameter();
            definition.RequestParameters.Add(replacement);
            var edited = ResponseProperty(modified.Id);
            edited.Description = "Updated response description";
            definition.ResponseProperties.Add(edited);

            new CustomApiService(organization).Update(original, definition);

            var transaction = (ExecuteTransactionRequest)organization.Requests.Single();
            Assert.That(transaction.Requests, Has.Count.EqualTo(3));
            var delete = (DeleteRequest)transaction.Requests.First();
            Assert.That(delete.Target.LogicalName, Is.EqualTo("customapirequestparameter"));
            Assert.That(delete.Target.Id, Is.EqualTo(deleted.Id));
            var update = transaction.Requests.OfType<UpdateRequest>().Single();
            Assert.That(update.Target.LogicalName, Is.EqualTo("customapiresponseproperty"));
            Assert.That(update.Target.Id, Is.EqualTo(modified.Id));
            Assert.That(update.Target.Attributes.Keys, Is.EquivalentTo(new[] { "description" }));
            var create = transaction.Requests.OfType<CreateRequest>().Single();
            Assert.That(create.Target.LogicalName, Is.EqualTo("customapirequestparameter"));
            Assert.That(create.Target.GetAttributeValue<EntityReference>("customapiid").Id, Is.EqualTo(original.Id));
            Assert.That(replacement.Id, Is.EqualTo(Guid.Empty));
        }

        [Test]
        public void CreatePropagatesAnSdkFaultWithoutRetryingOrChangingDraftIds()
        {
            var organization = CreateOrganization();
            var fault = SdkFault();
            organization.OnExecute = request => throw fault;
            var definition = Draft();
            definition.RequestParameters.Add(RequestParameter());

            Assert.That(Assert.Throws<FaultException<OrganizationServiceFault>>(() => new CustomApiService(organization).Create(definition, "ExampleSolution")), Is.SameAs(fault));
            Assert.That(organization.Requests, Has.Count.EqualTo(1));
            Assert.That(definition.Id, Is.EqualTo(Guid.Empty));
            Assert.That(definition.RequestParameters.Single().Id, Is.EqualTo(Guid.Empty));
        }

        [Test]
        public void UpdatePropagatesATransactionFaultWithoutRetrying()
        {
            var organization = new RecordingOrganizationService();
            var fault = SdkFault();
            organization.OnExecute = request => throw fault;
            var original = Draft(Guid.NewGuid());
            var definition = Draft(original.Id);
            definition.RequestParameters.Add(RequestParameter());

            Assert.That(Assert.Throws<FaultException<OrganizationServiceFault>>(() => new CustomApiService(organization).Update(original, definition)), Is.SameAs(fault));
            Assert.That(organization.Requests, Has.Count.EqualTo(1));
            Assert.That(definition.RequestParameters.Single().Id, Is.EqualTo(Guid.Empty));
        }

        [Test]
        public void ListIncludesPrivateApisAndFollowsPagingCookies()
        {
            var organization = new RecordingOrganizationService();
            var firstId = Guid.NewGuid();
            var secondId = Guid.NewGuid();
            var messageId = Guid.NewGuid();
            organization.OnRetrieveMultiple = query =>
            {
                Assert.That(query.EntityName, Is.EqualTo("customapi"));
                Assert.That(query.Criteria.Conditions.Any(c => c.AttributeName == "isprivate"), Is.False);
                if (query.PageInfo.PageNumber == 1)
                {
                    var first = ApiRow(firstId);
                    first["isprivate"] = true;
                    first["sdkmessageid"] = new EntityReference("sdkmessage", messageId);
                    return new EntityCollection(new[] { first }) { MoreRecords = true, PagingCookie = "next-page" };
                }

                Assert.That(query.PageInfo.PageNumber, Is.EqualTo(2));
                Assert.That(query.PageInfo.PagingCookie, Is.EqualTo("next-page"));
                return new EntityCollection(new[] { ApiRow(secondId) });
            };

            var results = new CustomApiService(organization).List();

            Assert.That(results.Select(api => api.Id), Is.EquivalentTo(new[] { firstId, secondId }));
            Assert.That(results.Single(api => api.Id == firstId).SdkMessageId, Is.EqualTo(messageId));
            Assert.That(organization.QueryPageNumbers, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void RetrieveScopesAndPagesChildQueriesAndReadsManagedProperties()
        {
            var organization = new RecordingOrganizationService();
            var id = Guid.NewGuid();
            var firstChildId = Guid.NewGuid();
            var secondChildId = Guid.NewGuid();
            organization.OnRetrieve = (name, rowId, columns) =>
            {
                Assert.That(name, Is.EqualTo("customapi"));
                Assert.That(rowId, Is.EqualTo(id));
                var row = ApiRow(id);
                row["iscustomizable"] = new BooleanManagedProperty(false);
                row["isprivate"] = true;
                return row;
            };
            organization.OnRetrieveMultiple = query =>
            {
                Assert.That(query.Criteria.Conditions.Any(c => c.AttributeName == "customapiid" && Equals(c.Values.Single(), id)), Is.True);
                if (query.EntityName == "customapiresponseproperty")
                {
                    return new EntityCollection();
                }

                Assert.That(query.EntityName, Is.EqualTo("customapirequestparameter"));
                var child = new Entity(query.EntityName, query.PageInfo.PageNumber == 1 ? firstChildId : secondChildId)
                {
                    ["name"] = "Input",
                    ["uniquename"] = "Input" + query.PageInfo.PageNumber,
                    ["displayname"] = "Input",
                    ["type"] = new OptionSetValue((int)CustomApiParameterType.String),
                    ["isoptional"] = true,
                    ["iscustomizable"] = new BooleanManagedProperty(false)
                };
                return new EntityCollection(new[] { child })
                {
                    MoreRecords = query.PageInfo.PageNumber == 1,
                    PagingCookie = query.PageInfo.PageNumber == 1 ? "child-page" : null
                };
            };

            var result = new CustomApiService(organization).Retrieve(id);

            Assert.That(result.IsPrivate, Is.True);
            Assert.That(result.IsCustomizable, Is.False);
            Assert.That(result.RequestParameters.Select(parameter => parameter.Id), Is.EquivalentTo(new[] { firstChildId, secondChildId }));
            Assert.That(result.RequestParameters.All(parameter => parameter.IsOptional && !parameter.IsCustomizable), Is.True);
            Assert.That(result.ResponseProperties, Is.Empty);
            Assert.That(organization.QueryPagingCookies, Does.Contain("child-page"));
        }

        [Test]
        public void DeleteTargetsOnlyTheApiAndLetsDataverseCascade()
        {
            var organization = new RecordingOrganizationService();
            var id = Guid.NewGuid();
            organization.OnRetrieve = (name, rowId, columns) => ApiRow(rowId);
            organization.OnDelete = (name, rowId) => { };

            new CustomApiService(organization).Delete(id);

            Assert.That(organization.DeletedRecords, Has.Count.EqualTo(1));
            Assert.That(organization.DeletedRecords.Single().LogicalName, Is.EqualTo("customapi"));
            Assert.That(organization.DeletedRecords.Single().Id, Is.EqualTo(id));
            Assert.That(organization.Queries, Is.Empty);
        }

        [Test]
        public void DeleteRejectsAManagedApiEvenWhenCustomizable()
        {
            var organization = new RecordingOrganizationService();
            organization.OnRetrieve = (name, id, columns) =>
            {
                var row = ApiRow(id);
                row["ismanaged"] = true;
                return row;
            };

            Assert.Throws<InvalidOperationException>(() => new CustomApiService(organization).Delete(Guid.NewGuid()));
            Assert.That(organization.DeletedRecords, Is.Empty);
        }

        [Test]
        public void DeletePropagatesAnSdkFaultWithoutRetrying()
        {
            var organization = new RecordingOrganizationService();
            var fault = SdkFault();
            organization.OnRetrieve = (name, id, columns) => ApiRow(id);
            organization.OnDelete = (name, id) => throw fault;

            Assert.That(Assert.Throws<FaultException<OrganizationServiceFault>>(() => new CustomApiService(organization).Delete(Guid.NewGuid())), Is.SameAs(fault));
            Assert.That(organization.DeletedRecords, Has.Count.EqualTo(1));
        }

        [Test]
        public void IsSupportedRequestsAttributesWhenQueryingAttributeMetadata()
        {
            var organization = new RecordingOrganizationService
            {
                OnExecute = request =>
                {
                    var query = ((RetrieveMetadataChangesRequest)request).Query;
                    Assert.That(query.AttributeQuery, Is.Not.Null);
                    Assert.That(query.Properties.PropertyNames, Does.Contain("Attributes"),
                        "Dataverse rejects an AttributeQuery unless the parent also requests Attributes.");
                    Assert.That(query.AttributeQuery.Properties.PropertyNames, Does.Contain("LogicalName"));
                    return new RetrieveMetadataChangesResponse { Results = { ["EntityMetadata"] = new EntityMetadataCollection() } };
                }
            };

            Assert.That(new CustomApiService(organization).IsSupported(), Is.False);
        }

        [Test]
        public void IsSupportedPropagatesMetadataAccessFaults()
        {
            var organization = new RecordingOrganizationService();
            var fault = SdkFault();
            organization.OnExecute = request => throw fault;

            Assert.That(Assert.Throws<FaultException<OrganizationServiceFault>>(() => new CustomApiService(organization).IsSupported()), Is.SameAs(fault));
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void IsSupportedRequiresEveryColumnUsedByTheEditor(bool omitParameterType, bool expected)
        {
            var metadata = new EntityMetadataCollection
            {
                TableMetadata("customapi", "customapiid", "name", "uniquename", "displayname", "description", "bindingtype",
                    "boundentitylogicalname", "allowedcustomprocessingsteptype", "plugintypeid", "sdkmessageid",
                    "executeprivilegename", "isfunction", "isprivate", "workflowsdkstepenabled", "ismanaged", "iscustomizable"),
                TableMetadata("customapirequestparameter", new[] { "customapirequestparameterid", "customapiid", "name", "uniquename",
                    "displayname", "description", "type", "logicalentityname", "isoptional", "ismanaged", "iscustomizable" }
                    .Where(column => !omitParameterType || column != "type").ToArray()),
                TableMetadata("customapiresponseproperty", "customapiresponsepropertyid", "customapiid", "name", "uniquename",
                    "displayname", "description", "type", "logicalentityname", "ismanaged", "iscustomizable")
            };
            var organization = new RecordingOrganizationService
            {
                OnExecute = request => new RetrieveMetadataChangesResponse { Results = { ["EntityMetadata"] = metadata } }
            };

            Assert.That(new CustomApiService(organization).IsSupported(), Is.EqualTo(expected));
        }

        [TestCase(0, CustomApiParameterType.Boolean)]
        [TestCase(1, CustomApiParameterType.DateTime)]
        [TestCase(2, CustomApiParameterType.Decimal)]
        [TestCase(3, CustomApiParameterType.Entity)]
        [TestCase(4, CustomApiParameterType.EntityCollection)]
        [TestCase(5, CustomApiParameterType.EntityReference)]
        [TestCase(6, CustomApiParameterType.Float)]
        [TestCase(7, CustomApiParameterType.Integer)]
        [TestCase(8, CustomApiParameterType.Money)]
        [TestCase(9, CustomApiParameterType.Picklist)]
        [TestCase(10, CustomApiParameterType.String)]
        [TestCase(11, CustomApiParameterType.StringArray)]
        [TestCase(12, CustomApiParameterType.Guid)]
        public void RetrieveMapsParameterTypeValuesForBothChildTables(int type, CustomApiParameterType expected)
        {
            var organization = new RecordingOrganizationService
            {
                OnRetrieve = (name, id, columns) => ApiRow(id),
                OnRetrieveMultiple = query => new EntityCollection(new[]
                {
                    new Entity(query.EntityName, Guid.NewGuid()) { ["type"] = new OptionSetValue(type) }
                })
            };

            var result = new CustomApiService(organization).Retrieve(Guid.NewGuid());

            Assert.That(result.RequestParameters.Single().Type, Is.EqualTo(expected));
            Assert.That(result.ResponseProperties.Single().Type, Is.EqualTo(expected));
        }

        [Test]
        public void RetrieveDoesNotTreatMissingChoiceValuesAsValidDefaults()
        {
            var organization = new RecordingOrganizationService
            {
                OnRetrieve = (name, id, columns) => new Entity(name, id),
                OnRetrieveMultiple = query => new EntityCollection(new[] { new Entity(query.EntityName, Guid.NewGuid()) })
            };

            var result = new CustomApiService(organization).Retrieve(Guid.NewGuid());

            Assert.That((int)result.BindingType, Is.EqualTo(-1));
            Assert.That((int)result.AllowedCustomProcessingStepType, Is.EqualTo(-1));
            Assert.That((int)result.RequestParameters.Single().Type, Is.EqualTo(-1));
            Assert.That((int)result.ResponseProperties.Single().Type, Is.EqualTo(-1));
        }

        private static EntityMetadata TableMetadata(string name, params string[] columns)
        {
            var metadata = new EntityMetadata { LogicalName = name };
            // The SDK exposes this response-only property with a non-public setter.
            typeof(EntityMetadata).GetProperty(nameof(EntityMetadata.Attributes)).SetValue(metadata,
                columns.Select(column => (AttributeMetadata)new StringAttributeMetadata { LogicalName = column }).ToArray());
            return metadata;
        }

        private static RecordingOrganizationService CreateOrganization()
        {
            return new RecordingOrganizationService
            {
                OnRetrieveMultiple = query => new EntityCollection(new[]
                {
                    new Entity("solution", Guid.NewGuid())
                    {
                        ["uniquename"] = "ExampleSolution",
                        ["ismanaged"] = false,
                        ["publisher.customizationprefix"] = new AliasedValue("publisher", "customizationprefix", "publisher")
                    }
                })
            };
        }

        private static RecordingOrganizationService TransactionOrganization()
        {
            return new RecordingOrganizationService { OnExecute = request => new ExecuteTransactionResponse() };
        }

        private static CustomApiDefinition Draft(Guid id = default(Guid))
        {
            return new CustomApiDefinition
            {
                Id = id,
                UniqueName = "publisher_Example",
                Name = "Example API",
                DisplayName = "Example API",
                Description = "Example API description"
            };
        }

        private static CustomApiRequestParameter RequestParameter(Guid id = default(Guid))
        {
            return new CustomApiRequestParameter
            {
                Id = id,
                UniqueName = "Input",
                Name = "Input",
                DisplayName = "Input",
                Type = CustomApiParameterType.String
            };
        }

        private static CustomApiResponseProperty ResponseProperty(Guid id = default(Guid))
        {
            return new CustomApiResponseProperty
            {
                Id = id,
                UniqueName = "Output",
                Name = "Output",
                DisplayName = "Output",
                Type = CustomApiParameterType.String
            };
        }

        private static Entity ApiRow(Guid id)
        {
            return new Entity("customapi", id)
            {
                ["uniquename"] = "publisher_Example",
                ["name"] = "Example API",
                ["displayname"] = "Example API",
                ["bindingtype"] = new OptionSetValue(0),
                ["allowedcustomprocessingsteptype"] = new OptionSetValue(0),
                ["ismanaged"] = false,
                ["iscustomizable"] = new BooleanManagedProperty(true)
            };
        }

        private static FaultException<OrganizationServiceFault> SdkFault()
        {
            return new FaultException<OrganizationServiceFault>(new OrganizationServiceFault { Message = "Server rejected the operation." });
        }
    }
}
