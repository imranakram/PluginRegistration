// Managed identity support is based on Plugin Identity Manager for XrmToolBox
// by David Rivard: https://github.com/drivardxrm/Driv.XTB.PluginIdentityManager

namespace Xrm.Sdk.PluginRegistration.Models
{
    using Microsoft.Xrm.Sdk;
    using System;

    /// <summary>
    /// A Dataverse managedidentity record that a plug-in assembly or package can use
    /// to get tokens through IManagedIdentityService.
    /// </summary>
    public sealed class ManagedIdentity
    {
        #region Public Fields

        public const string EntityLogicalName = "managedidentity";

        /// <summary>The lookup column on pluginassembly and pluginpackage.</summary>
        public const string LookupAttribute = "managedidentityid";

        public const string PrimaryKey = "managedidentityid";
        public const string NameAttribute = "name";
        public const string ApplicationIdAttribute = "applicationid";
        public const string TenantIdAttribute = "tenantid";
        public const string CredentialSourceAttribute = "credentialsource";
        public const string SubjectScopeAttribute = "subjectscope";
        public const string VersionAttribute = "version";

        /// <summary>credentialsource value for identities used by plug-ins.</summary>
        public const int CredentialSourceIsManaged = 2;

        /// <summary>Version 2 builds the federated credential subject from the certificate DN hashes and is the one Microsoft recommends.</summary>
        public const int RecommendedVersion = 2;

        #endregion Public Fields

        #region Public Enums

        public enum Scope
        {
            Global = 0,
            Environment = 1,
            DevOnly = 2
        }

        #endregion Public Enums

        #region Public Properties

        public Guid Id { get; set; }

        public string Name { get; set; }

        public Guid ApplicationId { get; set; }

        public Guid TenantId { get; set; }

        public Scope SubjectScope { get; set; } = Scope.Environment;

        public int? Version { get; set; } = RecommendedVersion;

        #endregion Public Properties

        #region Public Methods

        public static ManagedIdentity FromEntity(Entity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var scope = entity.GetAttributeValue<OptionSetValue>(SubjectScopeAttribute);
            return new ManagedIdentity
            {
                Id = entity.Id,
                Name = entity.GetAttributeValue<string>(NameAttribute),
                ApplicationId = entity.GetAttributeValue<Guid>(ApplicationIdAttribute),
                TenantId = entity.GetAttributeValue<Guid>(TenantIdAttribute),
                SubjectScope = scope != null ? (Scope)scope.Value : Scope.Environment,
                Version = entity.GetAttributeValue<int?>(VersionAttribute)
            };
        }

        public Entity ToEntity()
        {
            var entity = new Entity(EntityLogicalName);
            if (Id != Guid.Empty)
            {
                entity.Id = Id;
            }

            entity[NameAttribute] = Name;
            entity[ApplicationIdAttribute] = ApplicationId;
            entity[TenantIdAttribute] = TenantId;
            entity[SubjectScopeAttribute] = new OptionSetValue((int)SubjectScope);
            entity[VersionAttribute] = Version;
            return entity;
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? ApplicationId.ToString() : Name;
        }

        #endregion Public Methods
    }
}
