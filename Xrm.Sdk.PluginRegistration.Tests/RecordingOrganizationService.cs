namespace Xrm.Sdk.PluginRegistration.Tests
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Records the SDK boundary without simulating Dataverse behavior. Tests supply responses
    /// explicitly so a new or unexpected service call cannot silently succeed.
    /// </summary>
    internal sealed class RecordingOrganizationService : IOrganizationService
    {
        public List<OrganizationRequest> Requests { get; } = new List<OrganizationRequest>();
        public List<QueryExpression> Queries { get; } = new List<QueryExpression>();
        public List<int> QueryPageNumbers { get; } = new List<int>();
        public List<string> QueryPagingCookies { get; } = new List<string>();
        public List<EntityReference> DeletedRecords { get; } = new List<EntityReference>();

        public Func<OrganizationRequest, OrganizationResponse> OnExecute { get; set; }
        public Func<QueryExpression, EntityCollection> OnRetrieveMultiple { get; set; }
        public Func<string, Guid, ColumnSet, Entity> OnRetrieve { get; set; }
        public Action<string, Guid> OnDelete { get; set; }

        public OrganizationResponse Execute(OrganizationRequest request)
        {
            Requests.Add(request);
            if (OnExecute == null)
            {
                throw new InvalidOperationException("Unexpected Execute: " + request.RequestName);
            }

            return OnExecute(request);
        }

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            var expression = query as QueryExpression;
            if (expression == null)
            {
                throw new InvalidOperationException("Expected a QueryExpression.");
            }

            Queries.Add(expression);
            // The service can reuse a query between pages; preserve values at call time.
            QueryPageNumbers.Add(expression.PageInfo.PageNumber);
            QueryPagingCookies.Add(expression.PageInfo.PagingCookie);
            if (OnRetrieveMultiple == null)
            {
                throw new InvalidOperationException("Unexpected RetrieveMultiple: " + expression.EntityName);
            }

            return OnRetrieveMultiple(expression);
        }

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            if (OnRetrieve == null)
            {
                throw new InvalidOperationException("Unexpected Retrieve: " + entityName);
            }

            return OnRetrieve(entityName, id, columnSet);
        }

        public void Delete(string entityName, Guid id)
        {
            DeletedRecords.Add(new EntityReference(entityName, id));
            if (OnDelete == null)
            {
                throw new InvalidOperationException("Unexpected Delete: " + entityName);
            }

            OnDelete(entityName, id);
        }

        public Guid Create(Entity entity)
        {
            throw new InvalidOperationException("Use a CreateRequest so tests can inspect solution membership.");
        }

        public void Update(Entity entity)
        {
            throw new InvalidOperationException("Updates must be part of the transaction.");
        }

        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            throw new NotSupportedException();
        }

        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            throw new NotSupportedException();
        }
    }
}
