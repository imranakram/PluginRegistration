namespace Xrm.Sdk.PluginRegistration.Tests.ManagedIdentities
{
    using McTools.Xrm.Connection;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Query;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Xrm.Sdk.PluginRegistration.Entities;
    using Xrm.Sdk.PluginRegistration.Helpers;
    using Xrm.Sdk.PluginRegistration.Models;
    using Xrm.Sdk.PluginRegistration.Wrappers;

    [TestFixture]
    internal class ManagedIdentityTests
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void DetectSupport_OnPremises_IsFalseWithoutAskingTheServer()
        {
            var service = new RecordingOrganizationService();
            var org = Organization(service, online: false, supported: false);

            Assert.That(ManagedIdentityHelper.DetectSupport(org), Is.False);
            Assert.That(service.Requests, Is.Empty);
        }

        [Test]
        public void DetectSupport_Online_DependsOnTheManagedIdentityTable()
        {
            var withTable = new RecordingOrganizationService { OnExecute = request => new RetrieveEntityResponse() };
            Assert.That(ManagedIdentityHelper.DetectSupport(Organization(withTable, online: true, supported: false)), Is.True);
            var metadataRequest = (RetrieveEntityRequest)withTable.Requests.Single();
            Assert.That(metadataRequest.LogicalName, Is.EqualTo("managedidentity"));

            var withoutTable = new RecordingOrganizationService { OnExecute = r => throw new InvalidOperationException("Could not find entity") };
            Assert.That(ManagedIdentityHelper.DetectSupport(Organization(withoutTable, online: true, supported: false)), Is.False);
        }

        [Test]
        public void DetectSupport_ConnectionWithoutOriginalUrl_UsesTheMetadataCheck()
        {
            // ConnectionDetail.UseOnline throws when OriginalUrl is missing (connection-string connections).
            var service = new RecordingOrganizationService { OnExecute = request => new RetrieveEntityResponse() };
            var org = Organization(service, online: true, supported: false);
            org.ConnectionDetail.OriginalUrl = null;

            Assert.That(ManagedIdentityHelper.DetectSupport(org), Is.True);
        }

        [Test]
        public void AddLookupColumn_OnlyWhenSupported()
        {
            var unsupported = new ColumnSet("name");
            ManagedIdentityHelper.AddLookupColumn(Organization(new RecordingOrganizationService(), online: false, supported: false), unsupported);
            Assert.That(unsupported.Columns, Is.EquivalentTo(new[] { "name" }));

            var supported = new ColumnSet("name");
            var org = Organization(new RecordingOrganizationService(), online: true, supported: true);
            ManagedIdentityHelper.AddLookupColumn(org, supported);
            ManagedIdentityHelper.AddLookupColumn(org, supported);
            Assert.That(supported.Columns, Is.EquivalentTo(new[] { "name", "managedidentityid" }));
        }

        [Test]
        public void LoadAssemblies_AsksForTheLookupOnlineWithoutChangingTheSharedColumns()
        {
            var online = new RecordingOrganizationService { OnRetrieveMultiple = q => new EntityCollection() };
            OrganizationHelper.LoadAssemblies(Organization(online, online: true, supported: true, version: "9.2.0.0"), new Settings());

            var onPrem = new RecordingOrganizationService { OnRetrieveMultiple = q => new EntityCollection() };
            OrganizationHelper.LoadAssemblies(Organization(onPrem, online: false, supported: false, version: "9.1.0.0"), new Settings());

            Assert.That(online.Queries.Single().ColumnSet.Columns, Does.Contain("managedidentityid").And.Contain("packageid"));
            Assert.That(onPrem.Queries.Single().ColumnSet.Columns, Does.Not.Contain("managedidentityid").And.Not.Contain("packageid"));

            // The 9.1 load used to remove packageid from the cache shared by every connection.
            var again = new RecordingOrganizationService { OnRetrieveMultiple = q => new EntityCollection() };
            OrganizationHelper.LoadAssemblies(Organization(again, online: false, supported: false, version: "9.2.0.0"), new Settings());
            Assert.That(again.Queries.Single().ColumnSet.Columns, Does.Contain("packageid").And.Not.Contain("managedidentityid"));
        }

        [Test]
        public void Create_SetsManagedCredentialSourceVersionAndSolution()
        {
            var id = Guid.NewGuid();
            var service = new RecordingOrganizationService
            {
                OnExecute = r => new CreateResponse { Results = { ["id"] = id } }
            };
            var identity = new ManagedIdentity { Name = "Contoso Identity", ApplicationId = Guid.NewGuid(), TenantId = Guid.NewGuid() };

            var created = ManagedIdentityHelper.Create(service, identity, "contoso");

            Assert.That(created, Is.EqualTo(id));
            Assert.That(identity.Id, Is.EqualTo(id));
            var request = (CreateRequest)service.Requests.Single();
            Assert.That(request["SolutionUniqueName"], Is.EqualTo("contoso"));
            Assert.That(request.Target.LogicalName, Is.EqualTo("managedidentity"));
            Assert.That(request.Target.GetAttributeValue<OptionSetValue>("credentialsource").Value, Is.EqualTo(2));
            Assert.That(request.Target.GetAttributeValue<OptionSetValue>("subjectscope").Value, Is.EqualTo(1), "Environment scope by default");
            Assert.That(request.Target.GetAttributeValue<int?>("version"), Is.EqualTo(2), "Microsoft recommends version 2");
            Assert.That(request.Target.GetAttributeValue<Guid>("applicationid"), Is.EqualTo(identity.ApplicationId));
            Assert.That(request.Target.GetAttributeValue<Guid>("tenantid"), Is.EqualTo(identity.TenantId));
        }

        [Test]
        public void Link_SetsOrClearsTheLookup()
        {
            var service = new RecordingOrganizationService { OnExecute = r => new UpdateResponse() };
            var packageId = Guid.NewGuid();
            var identityId = Guid.NewGuid();

            ManagedIdentityHelper.Link(service, PluginPackage.EntityLogicalName, packageId, identityId);
            ManagedIdentityHelper.Link(service, PluginPackage.EntityLogicalName, packageId, null);

            var link = ((UpdateRequest)service.Requests[0]).Target;
            Assert.That(link.LogicalName, Is.EqualTo("pluginpackage"));
            Assert.That(link.Id, Is.EqualTo(packageId));
            Assert.That(link.GetAttributeValue<EntityReference>("managedidentityid").Id, Is.EqualTo(identityId));

            var unlink = ((UpdateRequest)service.Requests[1]).Target;
            Assert.That(unlink.Attributes.ContainsKey("managedidentityid"), Is.True);
            Assert.That(unlink["managedidentityid"], Is.Null);
        }

        [Test]
        public void RefreshFromPluginAssembly_ReadsTheLinkedIdentity()
        {
            var identityId = Guid.NewGuid();
            var entity = new PluginAssembly { PluginAssemblyId = Guid.NewGuid(), Name = "Contoso.Plugins" };
            entity["managedidentityid"] = new EntityReference("managedidentity", identityId) { Name = "Contoso Identity" };

            var online = new CrmPluginAssembly(Organization(new RecordingOrganizationService(), online: true, supported: true), entity);
            Assert.That(online.ManagedIdentityId, Is.EqualTo(identityId));
            Assert.That(online.ManagedIdentityName, Is.EqualTo("Contoso Identity"));
            Assert.That(online.Clone(false).ManagedIdentityId, Is.EqualTo(identityId));

            entity.Attributes.Remove("managedidentityid");
            online.RefreshFromPluginAssembly(entity);
            Assert.That(online.ManagedIdentityId, Is.Null, "Unlinking elsewhere clears it on refresh");
            Assert.That(online.ManagedIdentityName, Is.Null);
        }

        [Test]
        public void PropertyGridFilter_HidesManagedIdentityPropertiesOnPremises()
        {
            // The filter MainControl gives the properties grid for on-premises organizations.
            var onPremFilter = new Attribute[] { BrowsableAttribute.Yes, OnlineOnlyAttribute.No };
            var onlineFilter = new Attribute[] { BrowsableAttribute.Yes };

            foreach (var type in new[] { typeof(CrmPluginAssembly), typeof(CrmPluginPackage) })
            {
                var onPrem = TypeDescriptor.GetProperties(type, onPremFilter).Cast<PropertyDescriptor>().Select(p => p.Name).ToList();
                var online = TypeDescriptor.GetProperties(type, onlineFilter).Cast<PropertyDescriptor>().Select(p => p.Name).ToList();

                Assert.That(onPrem, Does.Contain("Name").And.Not.Contain("ManagedIdentityName").And.Not.Contain("ManagedIdentityId"), type.Name);
                Assert.That(online, Does.Contain("Name").And.Contain("ManagedIdentityName").And.Contain("ManagedIdentityId"), type.Name);
            }
        }

        [Test]
        public void SyncLoadedComponents_RenamesAndClearsOnlyChangedIdentities()
        {
            var org = Organization(new RecordingOrganizationService(), online: true, supported: true);
            var renamed = Guid.NewGuid();
            var deleted = Guid.NewGuid();
            var untouched = Guid.NewGuid();

            var package = new CrmPluginPackage(org) { PackageId = Guid.NewGuid(), Name = "contoso_Package" };
            package.SetManagedIdentity(renamed, "Old name");
            var assembly = new CrmPluginAssembly(org) { AssemblyId = Guid.NewGuid(), Name = "Contoso.Plugins" };
            assembly.SetManagedIdentity(deleted, "Deleted");
            var other = new CrmPluginAssembly(org) { AssemblyId = Guid.NewGuid(), Name = "Fabrikam.Plugins" };
            other.SetManagedIdentity(untouched, "Not in the list");

            Field<Dictionary<Guid, CrmPluginPackage>>(org, "m_packageList")[package.PackageId] = package;
            var assemblies = Field<Dictionary<Guid, CrmPluginAssembly>>(org, "m_assemblyList");
            assemblies[assembly.AssemblyId] = assembly;
            assemblies[other.AssemblyId] = other;

            ManagedIdentityHelper.SyncLoadedComponents(org,
                new[] { new ManagedIdentity { Id = renamed, Name = "New name" } },
                new[] { deleted });

            Assert.That(package.ManagedIdentityName, Is.EqualTo("New name"));
            Assert.That(assembly.ManagedIdentityId, Is.Null);
            Assert.That(other.ManagedIdentityId, Is.EqualTo(untouched), "Identities the dialog didn't list (other credential sources) stay linked");
            Assert.That(other.ManagedIdentityName, Is.EqualTo("Not in the list"));
        }

        private static T Field<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, InstanceFlags).GetValue(target);
        }

        private static CrmOrganization Organization(IOrganizationService service, bool online, bool supported, string version = "9.2.0.0")
        {
            // The public constructor loads the entire registration tree; these tests need only
            // the service, the connection and empty component lists.
            var org = (CrmOrganization)FormatterServices.GetUninitializedObject(typeof(CrmOrganization));
            var type = typeof(CrmOrganization);
            type.GetField("m_organizationService", InstanceFlags).SetValue(org, service);
            type.GetField("m_assemblyList", InstanceFlags).SetValue(org, new Dictionary<Guid, CrmPluginAssembly>());
            type.GetField("m_packageList", InstanceFlags).SetValue(org, new Dictionary<Guid, CrmPluginPackage>());
            type.GetField("m_pluginList", InstanceFlags).SetValue(org, new Dictionary<Guid, CrmPlugin>());
            type.GetField("m_stepList", InstanceFlags).SetValue(org, new Dictionary<Guid, CrmPluginStep>());
            type.GetField("m_imageList", InstanceFlags).SetValue(org, new Dictionary<Guid, CrmPluginImage>());
            // ConnectionDetail works out UseOnline from the URL, as it does for real connections.
            var url = online ? "https://contoso.crm4.dynamics.com" : "https://crm.contoso.local";
            org.ConnectionDetail = new ConnectionDetail(true) { OriginalUrl = url, WebApplicationUrl = url, OrganizationVersion = version };
            Assert.That(org.ConnectionDetail.UseOnline, Is.EqualTo(online));
            type.GetProperty(nameof(CrmOrganization.ManagedIdentitySupported)).SetValue(org, supported);
            return org;
        }
    }
}
