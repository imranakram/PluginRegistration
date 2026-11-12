// Managed identity support is based on Plugin Identity Manager for XrmToolBox
// by David Rivard: https://github.com/drivardxrm/Driv.XTB.PluginIdentityManager

namespace Xrm.Sdk.PluginRegistration.Helpers
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Metadata;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Wrappers;
    using Models;

    /// <summary>
    /// Reads and writes managed identities and links them to plug-in assemblies and packages.
    /// Only Dataverse online has managed identities.
    /// </summary>
    public static class ManagedIdentityHelper
    {
        #region Public Methods

        /// <summary>
        /// Adds the managed identity lookup to an assembly or package query when the organization supports it.
        /// </summary>
        public static void AddLookupColumn(CrmOrganization org, ColumnSet columns)
        {
            if (org != null && org.ManagedIdentitySupported && columns != null && !columns.AllColumns
                && !columns.Columns.Contains(ManagedIdentity.LookupAttribute))
            {
                columns.AddColumn(ManagedIdentity.LookupAttribute);
            }
        }

        public static Guid Create(IOrganizationService service, ManagedIdentity identity, string solutionUniqueName)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            var entity = identity.ToEntity();
            entity[ManagedIdentity.CredentialSourceAttribute] = new OptionSetValue(ManagedIdentity.CredentialSourceIsManaged);

            var request = new CreateRequest { Target = entity };
            if (!string.IsNullOrEmpty(solutionUniqueName))
            {
                request["SolutionUniqueName"] = solutionUniqueName;
            }

            var id = ((CreateResponse)service.Execute(request)).id;
            identity.Id = id;
            return id;
        }

        public static void Delete(IOrganizationService service, Guid identityId)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            service.Delete(ManagedIdentity.EntityLogicalName, identityId);
        }

        /// <summary>
        /// Checks once per connection whether managed identities can be used. On-premises
        /// organizations never support them; online ones need the managedidentity table.
        /// </summary>
        public static bool DetectSupport(CrmOrganization org)
        {
            if (org?.ConnectionDetail == null || IsOnPremises(org.ConnectionDetail))
            {
                return false;
            }

            try
            {
                org.OrganizationService.Execute(new RetrieveEntityRequest
                {
                    LogicalName = ManagedIdentity.EntityLogicalName,
                    EntityFilters = EntityFilters.Entity
                });

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Sets or clears the managed identity of a plug-in assembly or package.
        /// </summary>
        /// <param name="entityLogicalName">pluginassembly or pluginpackage</param>
        /// <param name="identityId">The identity to link, or null to unlink.</param>
        public static void Link(IOrganizationService service, string entityLogicalName, Guid recordId, Guid? identityId)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            var record = new Entity(entityLogicalName, recordId);
            record[ManagedIdentity.LookupAttribute] = identityId.HasValue
                ? new EntityReference(ManagedIdentity.EntityLogicalName, identityId.Value)
                : null;

            service.Execute(new UpdateRequest { Target = record });
        }

        public static Guid? ReadLookup(Entity entity, out string name)
        {
            var reference = entity?.GetAttributeValue<EntityReference>(ManagedIdentity.LookupAttribute);
            name = reference?.Name;
            return reference?.Id;
        }

        /// <summary>
        /// Retrieves one managed identity whatever its credential source, or null when it doesn't exist.
        /// </summary>
        public static ManagedIdentity Retrieve(IOrganizationService service, Guid identityId)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            var query = new QueryExpression(ManagedIdentity.EntityLogicalName)
            {
                ColumnSet = IdentityColumns(),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(ManagedIdentity.PrimaryKey, ConditionOperator.Equal, identityId)
                    }
                }
            };

            var entity = service.RetrieveMultiple(query).Entities.FirstOrDefault();
            return entity != null ? ManagedIdentity.FromEntity(entity) : null;
        }

        /// <summary>
        /// Retrieves the managed identities that plug-ins can use (credential source IsManaged).
        /// </summary>
        public static List<ManagedIdentity> RetrieveAll(IOrganizationService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            var query = new QueryExpression(ManagedIdentity.EntityLogicalName)
            {
                ColumnSet = IdentityColumns(),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(ManagedIdentity.CredentialSourceAttribute, ConditionOperator.Equal, ManagedIdentity.CredentialSourceIsManaged)
                    }
                },
                Orders = { new OrderExpression(ManagedIdentity.NameAttribute, OrderType.Ascending) }
            };

            return service.RetrieveMultiple(query).Entities.Select(ManagedIdentity.FromEntity).ToList();
        }

        /// <summary>
        /// Brings the identity shown on loaded assemblies and packages in line with identities that were
        /// renamed or deleted, so the tree doesn't need a full reload.
        /// </summary>
        public static void SyncLoadedComponents(CrmOrganization org, IEnumerable<ManagedIdentity> identities, ICollection<Guid> deletedIds)
        {
            if (org == null)
            {
                return;
            }

            var names = (identities ?? Enumerable.Empty<ManagedIdentity>()).ToDictionary(i => i.Id, i => i.Name);
            var deleted = deletedIds ?? new Guid[0];

            void Sync(Guid? id, Action<Guid?, string> set, string currentName)
            {
                if (!id.HasValue)
                {
                    return;
                }

                if (deleted.Contains(id.Value))
                {
                    set(null, null);
                }
                else if (names.TryGetValue(id.Value, out var name) && name != currentName)
                {
                    set(id, name);
                }
            }

            foreach (var package in org.Packages)
            {
                Sync(package.ManagedIdentityId, package.SetManagedIdentity, package.ManagedIdentityName);
            }

            foreach (var assembly in org.Assemblies)
            {
                Sync(assembly.ManagedIdentityId, assembly.SetManagedIdentity, assembly.ManagedIdentityName);
            }
        }

        public static void Update(IOrganizationService service, ManagedIdentity identity)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (identity == null || identity.Id == Guid.Empty)
            {
                throw new ArgumentException("The managed identity must have an id.", nameof(identity));
            }

            service.Execute(new UpdateRequest { Target = identity.ToEntity() });
        }

        #endregion Public Methods

        #region Private Methods

        private static bool IsOnPremises(McTools.Xrm.Connection.ConnectionDetail detail)
        {
            try
            {
                return !detail.UseOnline;
            }
            catch (NullReferenceException)
            {
                // UseOnline reads OriginalUrl, which connections made from a connection string may not have.
                // Fall through to the metadata check: on-premises organizations don't have the table either.
                return false;
            }
        }

        private static ColumnSet IdentityColumns()
        {
            return new ColumnSet(
                ManagedIdentity.NameAttribute,
                ManagedIdentity.ApplicationIdAttribute,
                ManagedIdentity.TenantIdAttribute,
                ManagedIdentity.SubjectScopeAttribute,
                ManagedIdentity.VersionAttribute);
        }

        #endregion Private Methods
    }
}
