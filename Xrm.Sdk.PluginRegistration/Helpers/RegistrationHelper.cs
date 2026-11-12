// =====================================================================
//
//  This file is part of the Microsoft Dynamics CRM SDK code samples.
//
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  This source code is intended only as a supplement to Microsoft
//  Development Tools and/or on-line documentation.  See these other
//  materials for detailed information regarding Microsoft code samples.
//
//  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//  PARTICULAR PURPOSE.
//
// =====================================================================

namespace Xrm.Sdk.PluginRegistration.Helpers
{
    using Entities;
    using Forms;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.ExceptionServices;
    using System.Text;
    using Wrappers;
    using Xrm.Sdk.PluginRegistration.Controls;

    public static class RegistrationHelper
    {
        #region Public Methods

        /// <summary>
        /// Generate the default group name for an activity
        /// </summary>
        public static string GenerateDefaultGroupName(string assemblyName, Version version)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                throw new ArgumentNullException("assemblyName");
            }
            else if (null == version)
            {
                throw new ArgumentNullException("version");
            }

            return string.Format(CultureInfo.InvariantCulture, "{0} ({1})", assemblyName, version);
        }

        /// <summary>
        /// Generates a step description for the given message, primary and secondary entities, for a Step
        /// </summary>
        /// <param name="typeName">Name of the plug-in type</param>
        /// <param name="messageName">Message Name</param>
        /// <param name="primaryEntity">Primary Entity</param>
        /// <param name="secondaryEntity">Secondary Entity</param>
        /// <returns>Description that the Step should use</returns>
        public static string GenerateStepDescription(string typeName, string messageName, string primaryEntity, string secondaryEntity)
        {
            var descriptionBuilder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(typeName))
            {
                descriptionBuilder.AppendFormat("{0}: ", typeName);
            }

            if (string.IsNullOrEmpty(messageName))
            {
                descriptionBuilder.Append("Not Specified of ");
            }
            else
            {
                descriptionBuilder.AppendFormat("{0} of ", messageName);
            }

            bool hasPrimaryEntity = false;
            if (!string.IsNullOrEmpty(primaryEntity) &&
                !string.Equals(primaryEntity, "none", StringComparison.InvariantCultureIgnoreCase))
            {
                hasPrimaryEntity = true;
                descriptionBuilder.Append(primaryEntity);
            }

            if (!string.IsNullOrEmpty(secondaryEntity) &&
                !string.Equals(secondaryEntity, "none", StringComparison.InvariantCultureIgnoreCase))
            {
                string format;
                if (hasPrimaryEntity)
                {
                    format = "and {0}";
                }
                else
                {
                    format = "{0}";
                }

                descriptionBuilder.AppendFormat(format, secondaryEntity);
            }
            else if (!hasPrimaryEntity)
            {
                descriptionBuilder.Append("any Entity");
            }

            return descriptionBuilder.ToString();
        }

        /// <summary>
        /// Determines if the given TypeName is in use for the given assembly id
        /// </summary>
        /// <param name="org">Organization to check</param>
        /// <param name="typeName">Type Name to check</param>
        /// <param name="pluginAssemblyId">Assembly that should contain the Type</param>
        /// <param name="pluginTypeId">pluginTypeId that is using the type name</param>
        /// <returns>True if the type name is found</returns>
        public static bool PluginExists(CrmOrganization org, string typeName, Guid pluginAssemblyId,
            out Guid pluginTypeId)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (typeName == null)
            {
                throw new ArgumentNullException("typeName");
            }

            QueryByAttribute query = new QueryByAttribute(PluginType.EntityLogicalName)
            {
                ColumnSet = new ColumnSet()
            };
            query.AddAttributeValue("typename", typeName);
            if (pluginAssemblyId != Guid.Empty)
            {
                query.AddAttributeValue("pluginassemblyid", pluginAssemblyId);
            }

            var results = org.OrganizationService.RetrieveMultipleAllPages(query);
            if (results.Entities != null && results.Entities.Count > 0)
            {
                pluginTypeId = results.Entities[0].Id;
                return true;
            }
            else
            {
                pluginTypeId = Guid.Empty;
                return false;
            }
        }

        public static Guid RegisterAssembly(CrmOrganization org, string pathToAssembly, CrmPluginAssembly assembly)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }
            else if (assembly.SourceType == CrmAssemblySourceType.Database && pathToAssembly == null)
            {
                throw new ArgumentNullException("pathToAssembly", "Cannot be null when SourceType is Database");
            }

            PluginAssembly ptl = (PluginAssembly)assembly.GenerateCrmEntities()[PluginAssembly.EntityLogicalName];
            if (assembly.SourceType == CrmAssemblySourceType.Database)
            {
                ptl.Content = Convert.ToBase64String(File.ReadAllBytes(pathToAssembly));
            }

            return org.OrganizationService.Create(ptl);
        }

        public static Guid RegisterImage(CrmOrganization org, CrmPluginImage image)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            if (org.Webhooks.TryGetValue(image.AssemblyId, out CrmServiceEndpoint pluginAssembly))
            {
                return RegisterImage(org, image, pluginAssembly.Steps[image.StepId]);
            }
            return RegisterImage(org, image, org[image.AssemblyId][image.PluginId][image.StepId]);
        }

        public static Guid RegisterImage(CrmOrganization org, CrmPluginImage image, CrmPluginStep step)
        {
            if (null == org)
            {
                throw new ArgumentNullException("org");
            }
            else if (null == image)
            {
                throw new ArgumentNullException("image");
            }
            else if (null == step)
            {
                throw new ArgumentNullException("step");
            }

            //Retrieve the SDK entity equivalent of the given image
            Dictionary<string, object> entityList = image.GenerateCrmEntities(step.MessageId, step.MessageEntityId);
            SdkMessageProcessingStepImage sdkImage = (SdkMessageProcessingStepImage)entityList[SdkMessageProcessingStepImage.EntityLogicalName];

            //If the step that owns this image is a profiled step, the step will be the original step (the step that is being profiled),
            //not the profiler step. The Profiler step is what should be set on the server, since that is the step that is actually enabled.
            if (step.IsProfiled && null != sdkImage.SdkMessageProcessingStepId)
            {
                sdkImage.SdkMessageProcessingStepId.Id = step.ProfilerStepId.GetValueOrDefault();
            }

            return org.OrganizationService.Create(sdkImage);
        }

        public static Guid RegisterPlugin(CrmOrganization org, CrmPlugin plugin)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (plugin == null)
            {
                throw new ArgumentNullException("plugin");
            }

            PluginType pt = (PluginType)plugin.GenerateCrmEntities()[PluginType.EntityLogicalName];

            return org.OrganizationService.Create(pt);
        }

        public static Guid RegisterServiceEndpoint(CrmOrganization org, CrmServiceEndpoint serviceEndpoint)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (serviceEndpoint == null)
            {
                throw new ArgumentNullException("serviceEndpoint");
            }
            ServiceEndpoint sep = serviceEndpoint.GenerateCrmEntities()[ServiceEndpoint.EntityLogicalName] as ServiceEndpoint;

            return org.OrganizationService.Create(sep);
        }

        public static Guid RegisterStep(CrmOrganization org, CrmPluginStep step)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (step == null)
            {
                throw new ArgumentNullException("step");
            }

            Dictionary<string, object> entityList = step.GenerateCrmEntities();
            SdkMessageProcessingStep sdkStep = (SdkMessageProcessingStep)entityList[SdkMessageProcessingStep.EntityLogicalName];

            //This is a sanity check. The UI won't allow a user to set the secure configuration.
            if (org.SecureConfigurationPermissionDenied)
            {
                sdkStep.Attributes.Remove("sdkmessageprocessingstepsecureconfigid");
                sdkStep.RelatedEntities.Clear();
            }
            else if (entityList.ContainsKey(Entities.SdkMessageProcessingStepSecureConfig.EntityLogicalName))
            {
                Guid secureConfigId = Guid.NewGuid();

                //Create the related secure config in the related entities
                SdkMessageProcessingStepSecureConfig sdkSecureConfig =
                    (SdkMessageProcessingStepSecureConfig)entityList[SdkMessageProcessingStepSecureConfig.EntityLogicalName];
                sdkSecureConfig.Id = secureConfigId;
                sdkStep.RelatedEntities[new Relationship(CrmPluginStep.RelationshipStepToSecureConfig)] =
                    new EntityCollection(new Entity[] { sdkSecureConfig }) { EntityName = sdkSecureConfig.LogicalName };
                step.SecureConfigurationId = secureConfigId;
            }

            return org.OrganizationService.Create(sdkStep);
        }

        public static Guid RegisterWebHook(CrmOrganization org, CrmServiceEndpoint webhook)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (webhook == null)
            {
                throw new ArgumentNullException("webhook");
            }

            ServiceEndpoint wh = webhook.GenerateCrmEntities()[ServiceEndpoint.EntityLogicalName] as ServiceEndpoint;

            return org.OrganizationService.Create(wh);
        }

        /// <summary>
        /// Extracts the properties for the plugin assembly
        /// </summary>
        /// <param name="pathToAssembly">Path to the DLL file that is needed to gather information</param>
        /// <returns>Properties Assembly</returns>
        public static CrmPluginAssembly RetrieveAssemblyProperties(string pathToAssembly)
        {
            using (var context = new AppDomainContext<AssemblyReader>())
            {
                return context.Proxy.RetrieveAssemblyProperties(pathToAssembly);
            }
        }

        /// <summary>
        /// Extracts all Workflow Activities and Plugins from the Assembly
        /// </summary>
        /// <param name="pathToAssembly">Path to the DLL file that is needed to gather information</param>
        /// <returns>List of Plugins in the Assembly</returns>
        public static CrmPluginAssembly RetrievePluginsFromAssembly(string pathToAssembly)
        {
            using (var context = new AppDomainContext<AssemblyReader>())
            {
                return context.Proxy.RetrievePluginsFromAssembly(pathToAssembly);
            }
        }

        public static List<Guid> RetrieveSecureConfigIdsForStepId(CrmOrganization org, IList<Guid> stepIds)
        {
            return RetrieveReferenceAttributeIds(org, SdkMessageProcessingStep.EntityLogicalName,
                "sdkmessageprocessingstepsecureconfigid", "sdkmessageprocessingstepid", stepIds);
        }

        /// <summary>
        /// Unregister entities and cascade the operation. Does not change the organization's cached
        /// registrations; use PlanUnregister, ExecuteUnregister and ApplyUnregister for that.
        /// </summary>
        /// <param name="org">Organization to be used</param>
        /// <param name="prog">ProgressIndicator to indicate success</param>
        /// <param name="crmEntity">Entities to be unregistered</param>
        /// <returns>Statistics of what was unregistered</returns>
        public static Dictionary<string, int> Unregister(CrmOrganization org, ProgressIndicator prog, params ICrmEntity[] crmEntity)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (crmEntity == null || crmEntity.Length == 0)
            {
                throw new ArgumentNullException("crmEntity");
            }

            var plan = PlanUnregister(org, crmEntity);
            UnregisterResult result;
            try
            {
                result = ExecuteUnregister(org, plan, ReportTo(prog));
            }
            finally
            {
                prog?.Complete(true);
            }

            if (result.Error != null)
            {
                ExceptionDispatchInfo.Capture(result.Error).Throw();
            }

            return plan.Statistics();
        }

        /// <summary>
        /// Lists everything that unregistering the given entities removes: plug-ins of assemblies,
        /// steps of plug-ins and service endpoints, profiler steps, secure configurations and images.
        /// Reads Dataverse and the cached steps only, so it can run on a background thread.
        /// </summary>
        public static UnregisterPlan PlanUnregister(CrmOrganization org, params ICrmEntity[] crmEntity)
        {
            if (org == null)
            {
                throw new ArgumentNullException(nameof(org));
            }
            else if (crmEntity == null || crmEntity.Length == 0)
            {
                throw new ArgumentNullException(nameof(crmEntity));
            }

            var plan = new UnregisterPlan();
            foreach (var entity in crmEntity)
            {
                switch (entity.EntityType)
                {
                    case PluginPackage.EntityLogicalName:
                        AddMissing(plan.Packages, entity.EntityId);
                        break;

                    case ServiceEndpoint.EntityLogicalName:
                        AddMissing(plan.ServiceEndpoints, entity.EntityId);
                        break;

                    case PluginAssembly.EntityLogicalName:
                        AddMissing(plan.Assemblies, entity.EntityId);
                        break;

                    case PluginType.EntityLogicalName:
                        AddMissing(plan.Plugins, entity.EntityId);
                        break;

                    case SdkMessageProcessingStep.EntityLogicalName:
                        AddMissing(plan.Steps, entity.EntityId);
                        break;

                    case SdkMessageProcessingStepImage.EntityLogicalName:
                        AddMissing(plan.Images, entity.EntityId);
                        break;

                    default:
                        throw new NotImplementedException($"Type = {entity.EntityType}");
                }
            }

            // Dataverse deletes the plug-in types of an assembly in a package, and only then refuses
            // to delete the assembly itself. Refuse before anything is deleted.
            EnsureNotPartOfPackage(org, plan.Assemblies, plan.Plugins);

            // Retrieve the up-to-date children, so records the tree doesn't show are removed as well
            AddMissing(plan.Steps, RetrieveStepIdsForServiceEndpoint(org, plan.ServiceEndpoints));
            AddMissing(plan.Plugins, RetrievePluginIdsForAssembly(org, plan.Assemblies));
            AddMissing(plan.Steps, RetrieveStepIdsForPlugins(org, plan.Plugins));

            // Profiler steps go with the steps they profile
            for (int i = plan.Steps.Count - 1; i >= 0; i--)
            {
                if (org.Steps.TryGetValue(plan.Steps[i], out CrmPluginStep step))
                {
                    var profilerStepId = step.ProfilerStepId.GetValueOrDefault();
                    if (Guid.Empty != profilerStepId && profilerStepId != step.StepId)
                    {
                        AddMissing(plan.Steps, profilerStepId);
                    }
                }
            }

            AddMissing(plan.SecureConfigs, RetrieveSecureConfigIdsForStepId(org, plan.Steps));
            AddMissing(plan.Images, RetrieveImageIdsForStepId(org, plan.Steps));
            return plan;
        }

        /// <summary>
        /// Deletes the records of the plan one at a time, children first. Stops at the first failure
        /// and returns it in the result together with what was deleted before it. Does not touch the
        /// tree or the cached registrations, so it can run on a background thread.
        /// </summary>
        /// <param name="report">Called before each kind of record and after each delete; may be null</param>
        public static UnregisterResult ExecuteUnregister(CrmOrganization org, UnregisterPlan plan, Action<UnregisterProgress> report)
        {
            if (org == null)
            {
                throw new ArgumentNullException(nameof(org));
            }

            var result = new UnregisterResult(plan);
            var deleted = result.Deleted;
            var phases = new[]
            {
                (Entity: SdkMessageProcessingStepImage.EntityLogicalName, Text: "Unregistering images", Ids: plan.Images, Deleted: deleted.Images),
                (Entity: SdkMessageProcessingStep.EntityLogicalName, Text: "Unregistering steps", Ids: plan.Steps, Deleted: deleted.Steps),
                (Entity: SdkMessageProcessingStepSecureConfig.EntityLogicalName, Text: "Unregistering secure configurations", Ids: plan.SecureConfigs, Deleted: deleted.SecureConfigs),
                (Entity: PluginType.EntityLogicalName, Text: "Unregistering plug-ins", Ids: plan.Plugins, Deleted: deleted.Plugins),
                (Entity: PluginAssembly.EntityLogicalName, Text: "Unregistering assemblies", Ids: plan.Assemblies, Deleted: deleted.Assemblies),
                (Entity: ServiceEndpoint.EntityLogicalName, Text: "Unregistering service endpoints", Ids: plan.ServiceEndpoints, Deleted: deleted.ServiceEndpoints),
                (Entity: PluginPackage.EntityLogicalName, Text: "Unregistering packages", Ids: plan.Packages, Deleted: deleted.Packages),
            };

            int total = plan.Total;
            int done = 0;
            try
            {
                foreach (var phase in phases.Where(phase => phase.Ids.Count != 0))
                {
                    report?.Invoke(new UnregisterProgress(phase.Text, done, total));
                    foreach (var id in phase.Ids)
                    {
                        org.OrganizationService.Delete(phase.Entity, id);
                        phase.Deleted.Add(id);
                        done++;
                        report?.Invoke(new UnregisterProgress(phase.Text, done, total));
                    }
                }
            }
            catch (Exception ex)
            {
                result.Error = ex;
            }

            return result;
        }

        /// <summary>
        /// Removes deleted records from the organization's cached registrations. Run it on the UI
        /// thread, because the tree reads the same lists.
        /// </summary>
        public static void ApplyUnregister(CrmOrganization org, UnregisterPlan deleted)
        {
            if (org == null)
            {
                throw new ArgumentNullException(nameof(org));
            }
            else if (deleted == null)
            {
                throw new ArgumentNullException(nameof(deleted));
            }

            deleted.Images.ForEach(org.Images.Remove);
            deleted.Steps.ForEach(org.Steps.Remove);
            deleted.Plugins.ForEach(org.Plugins.Remove);
            foreach (var assemblyId in deleted.Assemblies)
            {
                if (!org.Assemblies.TryGetValue(assemblyId, out CrmPluginAssembly assembly))
                {
                    continue;
                }

                org.Assemblies.Remove(assemblyId);

                // With one version left, the plug-ins no longer need the version in their names
                var remaining = org.Assemblies.Values.Where(a => a.Name == assembly.Name).ToList();
                if (remaining.Count == 1)
                {
                    remaining[0].MultipleVersions = false;
                    foreach (var plugin in remaining[0].Plugins.Values)
                    {
                        plugin.AssemblyVersion = null;
                    }
                }
            }

            deleted.ServiceEndpoints.ForEach(org.ServiceEndpoints.Remove);
            deleted.Packages.ForEach(org.Packages.Remove);
        }

        /// <summary>
        /// Assembly is Uploaded if it is database.
        /// We dont do Smart updatesqui
        /// </summary>
        /// <param name="org"></param>
        /// <param name="pathToAssembly"></param>
        /// <param name="assembly"></param>
        public static void UpdateAssembly(CrmOrganization org, string pathToAssembly, CrmPluginAssembly assembly, params PluginType[] type)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            var ptl = (PluginAssembly)assembly.GenerateCrmEntities()[PluginAssembly.EntityLogicalName];

            //If the assembly path is not set, then the content does not need to be updated
            if (!string.IsNullOrEmpty(pathToAssembly) &&
                assembly.SourceType == CrmAssemblySourceType.Database)
            {
                ptl.Content = Convert.ToBase64String(File.ReadAllBytes(pathToAssembly));
            }

            if (null != type && 0 != type.Length)
            {
                ptl.pluginassembly_plugintype = type;
            }

            org.OrganizationService.Update(ptl);
            OrganizationHelper.RefreshAssembly(org, assembly);
        }

        public static void UpdateAssembly(CrmOrganization org, string description, Guid assemblyId)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (assemblyId == Guid.Empty)
            {
                throw new ArgumentNullException("assemblyId");
            }

            // Work around as updating only description is failing with publickeytoken not null
            var pt1 = org.OrganizationService.Retrieve(PluginAssembly.EntityLogicalName, assemblyId, new ColumnSet(true)).ToEntity<PluginAssembly>();
            //PluginAssembly pt1 = new PluginAssembly();
            pt1.Description = description;
            org.OrganizationService.Update(pt1);
        }

        public static void UpdateImage(CrmOrganization org, CrmPluginImage image)
        {
            if (null == org)
            {
                throw new ArgumentNullException("org");
            }
            else if (null == image)
            {
                throw new ArgumentNullException("image");
            }

            UpdateImage(org, image, org[image.AssemblyId][image.PluginId][image.StepId]);
        }

        public static void UpdateImage(CrmOrganization org, CrmPluginImage image, CrmPluginStep step)
        {
            if (null == org)
            {
                throw new ArgumentNullException("org");
            }
            else if (null == image)
            {
                throw new ArgumentNullException("image");
            }
            else if (null == step)
            {
                throw new ArgumentNullException("step");
            }

            //Retrieve the SDK entity equivalent of the given image
            var entityList = image.GenerateCrmEntities(step.MessageId, step.MessageEntityId);
            var sdkImage = (SdkMessageProcessingStepImage)entityList[SdkMessageProcessingStepImage.EntityLogicalName];

            //If the step that owns this image is a profiled step, the step will be the original step (the step that is being profiled),
            //not the profiler step. The Profiler step is what should be set on the server, since that is the step that is actually enabled.
            if (step.IsProfiled && null != sdkImage.SdkMessageProcessingStepId)
            {
                sdkImage.SdkMessageProcessingStepId.Id = step.ProfilerStepId.GetValueOrDefault();
            }

            org.OrganizationService.Update(sdkImage);
            OrganizationHelper.RefreshImage(org, image, step);
        }

        public static void UpdatePlugin(CrmOrganization org, CrmPlugin plugin)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (plugin == null)
            {
                throw new ArgumentNullException("plugin");
            }

            var ptl = (PluginType)plugin.GenerateCrmEntities()[PluginType.EntityLogicalName];

            org.OrganizationService.Update(ptl);
            OrganizationHelper.RefreshPlugin(org, plugin);
        }

        public static void UpdatePlugin(CrmOrganization org, Guid pluginId, string typeName, string friendlyName)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            if (pluginId == Guid.Empty)
            {
                throw new ArgumentException("Invalid Guid", "pluginId");
            }
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException("Invalid typeName", "typeName");
            }

            if (friendlyName == null)
            {
                //No updates will occur if friendly name is null. Don't need to do anything
                return;
            }

            PluginType updatePlugin = new PluginType
            {
                PluginTypeId = new Guid?()
            };
            updatePlugin["plugintypeid"] = pluginId;

            updatePlugin.FriendlyName = friendlyName;
            updatePlugin.TypeName = typeName;
            org.OrganizationService.Update(updatePlugin);
            return;
        }

        public static void UpdatePluginFriendlyName(CrmOrganization org, Guid pluginId, string friendlyName)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (pluginId == Guid.Empty)
            {
                throw new ArgumentException("Invalid Guid", "pluginId");
            }
            else if (friendlyName == null)
            {
                //No updates will occur if friendly name is null. Don't need to do anything
                return;
            }

            PluginType updatePlugin = new PluginType
            {
                PluginTypeId = new Guid?()
            };
            updatePlugin["plugintypeid"] = pluginId;

            updatePlugin.FriendlyName = friendlyName;

            org.OrganizationService.Update(updatePlugin);
            return;
        }

        public static void UpdateServiceEndpoint(CrmOrganization org, CrmServiceEndpoint serviceEndpoint)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (serviceEndpoint == null)
            {
                throw new ArgumentNullException("serviceEndpoint");
            }
            var sep = serviceEndpoint.GenerateCrmEntities()[ServiceEndpoint.EntityLogicalName] as ServiceEndpoint;

            org.OrganizationService.Update(sep);
            OrganizationHelper.RefreshServiceEndpoint(org, serviceEndpoint);
        }

        public static bool UpdateStep(CrmOrganization org, CrmPluginStep step, Guid? origSecureConfigId, IList<CrmPluginImage> updateImages)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (step == null)
            {
                throw new ArgumentNullException("step");
            }

            var entityList = step.GenerateCrmEntities();
            var sdkStep = (SdkMessageProcessingStep)entityList[SdkMessageProcessingStep.EntityLogicalName];

            // Loop through each image and set the new message property
            List<SdkMessageProcessingStepImage> sdkImages = null;
            if (null != updateImages)
            {
                // Ensure that the given message supports images
                var message = org.Messages[step.MessageId];
                if (0 == message.ImageMessagePropertyNames.Count && step.Images.Count > 0)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                        "The step has images registered, but the \"{0}\" message doesn't support images.{1}In order to change the message to \"{0}\", delete the existing images.",
                        message.Name, Environment.NewLine));
                }

                // Loop through the existing images and update their message property name values
                sdkImages = new List<SdkMessageProcessingStepImage>(updateImages.Count);
                foreach (CrmPluginImage image in updateImages)
                {
                    // Set the message property name for each of the images
                    string propertyName = MessagePropertyNameForm.SelectMessagePropertyName(message);
                    if (string.IsNullOrWhiteSpace(propertyName))
                    {
                        return false;
                    }
                    else if (string.Equals(image.MessagePropertyName, propertyName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    // Create the entity to update the value
                    var sdkImage = new SdkMessageProcessingStepImage
                    {
                        Id = image.ImageId,
                        MessagePropertyName = propertyName,
                        EntityState = EntityState.Changed
                    };

                    sdkImages.Add(sdkImage);
                }
            }

            //This is a sanity check. The UI won't allow a user to set the secure configuration.
            if (org.SecureConfigurationPermissionDenied)
            {
                sdkStep.Attributes.Remove("sdkmessageprocessingstepsecureconfigid");
                sdkStep.RelatedEntities.Clear();
                origSecureConfigId = null;
            }
            else if (entityList.ContainsKey(Entities.SdkMessageProcessingStepSecureConfig.EntityLogicalName))
            {
                if (null == origSecureConfigId)
                {
                    entityList.Remove(Entities.SdkMessageProcessingStepSecureConfig.EntityLogicalName);
                }
                else
                {
                    SdkMessageProcessingStepSecureConfig sdkSecureConfig =
                        (SdkMessageProcessingStepSecureConfig)entityList[SdkMessageProcessingStepSecureConfig.EntityLogicalName];

                    Guid secureConfigId;
                    EntityState secureConfigState;
                    if (step.SecureConfigurationId == origSecureConfigId && origSecureConfigId.GetValueOrDefault() != Guid.Empty)
                    {
                        //Set the ID of the secure configuration to be the
                        secureConfigId = origSecureConfigId.GetValueOrDefault();
                        secureConfigState = EntityState.Changed;

                        //Set the original secure config id so that the current id is not deleted
                        sdkStep.SdkMessageProcessingStepSecureConfigId =
                            new EntityReference(SdkMessageProcessingStepSecureConfig.EntityLogicalName, secureConfigId);
                    }
                    else
                    {
                        secureConfigId = Guid.NewGuid();
                        secureConfigState = EntityState.Created;
                    }

                    //Set the configuration id for the step
                    step.SecureConfigurationId = secureConfigId;

                    //Populate the secure configuration object and add it to the related entities
                    sdkSecureConfig.SdkMessageProcessingStepSecureConfigId = secureConfigId;
                    sdkSecureConfig.EntityState = secureConfigState;

                    //Update the related entities
                    sdkStep.sdkmessageprocessingstepsecureconfigid_sdkmessageprocessingstep = sdkSecureConfig;

                    //Reset the original secure configuration
                    origSecureConfigId = null;
                }
            }
            else if (null != origSecureConfigId)
            {
                // To null out if it was set before
                sdkStep.SdkMessageProcessingStepSecureConfigId = null;
                step.SecureConfigurationId = Guid.Empty;

                if (Guid.Empty == origSecureConfigId)
                {
                    origSecureConfigId = null;
                }
            }

            // If the images need to be updated, there are two possible scenarios:
            // 1) The step is profiled -- The parent of the image is not the step that is currently being updated
            //    but rather the profiler step itself. In order to avoid changing this, there will have to be two SDK
            //    operations (update the step and update the images). Otherwise, the next time the profiled step executes,
            //    the images won't be present.
            // 2) The step is not profiled -- The image can be added to the related entities because the parent step is the
            //    current step being updated.
            if (null != sdkImages && sdkImages.Count > 0)
            {
                if (!step.IsProfiled || step.ProfilerStepId == step.StepId)
                {
                    sdkStep.sdkmessageprocessingstepid_sdkmessageprocessingstepimage = sdkImages;
                }
            }

            //Update the step
            org.OrganizationService.Update(sdkStep);

            if (step.IsProfiled && null != sdkImages && sdkImages.Count > 0)
            {
                // Update the Profiler step with the new property values as a single transaction to minimize the
                // possibility of data corruption.
                SdkMessageProcessingStep profilerStep = new SdkMessageProcessingStep
                {
                    Id = step.ProfilerStepId.GetValueOrDefault(),
                    sdkmessageprocessingstepid_sdkmessageprocessingstepimage = sdkImages
                };
                org.OrganizationService.Update(profilerStep);
            }

            // Refresh the objects so that the caller has up-to-date data
            OrganizationHelper.RefreshStep(org, step);
            if (null != updateImages)
            {
                foreach (CrmPluginImage image in updateImages)
                {
                    OrganizationHelper.RefreshImage(org, image, step);
                }
            }

            // Delete the orphaned Secure config when nulling out the value on the step
            if (null != origSecureConfigId)
            {
                org.OrganizationService.Delete(SdkMessageProcessingStepSecureConfig.EntityLogicalName, origSecureConfigId.GetValueOrDefault());
            }

            return true;
        }

        public static void UpdateStepDescription(CrmOrganization org, Guid stepId, string description)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (stepId == Guid.Empty)
            {
                throw new ArgumentException("Invalid Guid", "stepId");
            }
            else if (description == null)
            {
                //No updates will occur if description is null. Don't need to do anything
                return;
            }

            SdkMessageProcessingStep updateStep = new SdkMessageProcessingStep
            {
                SdkMessageProcessingStepId = new Guid?()
            };
            updateStep["sdkmessageprocessingstepid"] = stepId;

            updateStep.Description = description;

            org.OrganizationService.Update(updateStep);
            return;
        }

        public static void UpdateStepStatus(CrmOrganization org, Guid stepId, bool isEnable)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (stepId == Guid.Empty)
            {
                throw new ArgumentException("Invalid Guid", "stepId");
            }

            Microsoft.Crm.Sdk.Messages.SetStateRequest request = new Microsoft.Crm.Sdk.Messages.SetStateRequest
            {
                EntityMoniker = new EntityReference(SdkMessageProcessingStep.EntityLogicalName, stepId)
            };
            if (isEnable)
            {
                request.State = new OptionSetValue((int)SdkMessageProcessingStepState.Enabled);
            }
            else
            {
                request.State = new OptionSetValue((int)SdkMessageProcessingStepState.Disabled);
            }
            request.Status = new OptionSetValue(-1);
            org.OrganizationService.Execute(request);

            return;
        }

        public static void UpdatewebHook(CrmOrganization org, CrmServiceEndpoint webhook)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (webhook.ServiceEndpointId == Guid.Empty)
            {
                throw new ArgumentNullException("webhookId");
            }

            var wh = (ServiceEndpoint)webhook.GenerateCrmEntities()[ServiceEndpoint.EntityLogicalName];

            // Work around as updating only description is failing with publickeytoken not null
            org.OrganizationService.Update(wh);
            OrganizationHelper.RefreshServiceEndpoint(org, webhook);
        }

        #endregion Public Methods

        #region Private Methods

        private static object[] ConvertIdArrayToObjectArray(IList<Guid> idList)
        {
            if (idList == null)
            {
                return null;
            }

            List<object> newIdList = new List<object>();
            if (idList != null && idList.Count != 0)
            {
                foreach (Guid id in idList)
                {
                    if (id != Guid.Empty)
                    {
                        newIdList.Add(id);
                    }
                }
            }

            return newIdList.ToArray();
        }

        /// <summary>
        /// Throws when one of the assemblies, or the assembly of one of the plug-ins, is part of a
        /// plug-in package. Those come and go with the package, so they are unregistered by
        /// unregistering or updating the package.
        /// </summary>
        private static void EnsureNotPartOfPackage(CrmOrganization org, List<Guid> assemblyIds, List<Guid> pluginIds)
        {
            if (assemblyIds.Count != 0)
            {
                var query = new QueryExpression(PluginAssembly.EntityLogicalName) { ColumnSet = new ColumnSet("name", "packageid") };
                query.Criteria.AddCondition("pluginassemblyid", ConditionOperator.In, assemblyIds.Cast<object>().ToArray());
                query.Criteria.AddCondition("packageid", ConditionOperator.NotNull);
                var assembly = org.OrganizationService.RetrieveMultiple(query).Entities.FirstOrDefault();
                if (assembly != null)
                {
                    throw PartOfPackage($"The assembly {assembly.GetAttributeValue<string>("name")}",
                        assembly.GetAttributeValue<EntityReference>("packageid"));
                }
            }

            if (pluginIds.Count != 0)
            {
                var query = new QueryExpression(PluginType.EntityLogicalName) { ColumnSet = new ColumnSet("typename") };
                query.Criteria.AddCondition("plugintypeid", ConditionOperator.In, pluginIds.Cast<object>().ToArray());
                var link = query.AddLink(PluginAssembly.EntityLogicalName, "pluginassemblyid", "pluginassemblyid");
                link.EntityAlias = "assembly";
                link.Columns = new ColumnSet("packageid");
                link.LinkCriteria.AddCondition("packageid", ConditionOperator.NotNull);
                var plugin = org.OrganizationService.RetrieveMultiple(query).Entities.FirstOrDefault();
                if (plugin != null)
                {
                    throw PartOfPackage($"The plug-in {plugin.GetAttributeValue<string>("typename")}",
                        plugin.GetAttributeValue<AliasedValue>("assembly.packageid")?.Value as EntityReference);
                }
            }
        }

        private static UnregisterRefusedException PartOfPackage(string what, EntityReference package)
        {
            var name = string.IsNullOrEmpty(package?.Name) ? "a plug-in package" : $"the plug-in package {package.Name}";
            return new UnregisterRefusedException($"{what} is part of {name}, so it can't be unregistered on its own. " +
                "Unregister the package, or update it with a package that no longer contains it. Nothing was unregistered.");
        }

        private static void AddMissing(List<Guid> list, Guid id)
        {
            if (!list.Contains(id))
            {
                list.Add(id);
            }
        }

        private static void AddMissing(List<Guid> list, IEnumerable<Guid> ids)
        {
            foreach (var id in ids)
            {
                AddMissing(list, id);
            }
        }

        /// <summary>
        /// Passes unregister progress on to a ProgressIndicator, which counts one unit per delete.
        /// </summary>
        private static Action<UnregisterProgress> ReportTo(ProgressIndicator prog)
        {
            if (prog == null)
            {
                return null;
            }

            string text = null;
            int done = -1;
            return progress =>
            {
                if (done < 0)
                {
                    prog.Initialize(progress.Total, progress.Text);
                }
                else if (progress.Text != text)
                {
                    prog.SetText(progress.Text);
                }

                if (done >= 0 && progress.Done > done)
                {
                    prog.Increment();
                }

                text = progress.Text;
                done = progress.Done;
            };
        }

        private static List<Guid> RetrieveImageIdsForStepId(CrmOrganization org, IList<Guid> stepIds)
        {
            return RetrieveReferenceAttributeIds(org, SdkMessageProcessingStepImage.EntityLogicalName, "sdkmessageprocessingstepimageid", "sdkmessageprocessingstepid", stepIds);
        }

        private static List<Guid> RetrievePluginIdsForAssembly(CrmOrganization org, IList<Guid> assemblyIds)
        {
            return RetrieveReferenceAttributeIds(org, PluginType.EntityLogicalName, "plugintypeid", "pluginassemblyid", assemblyIds);
        }

        /// <summary>
        /// Retrieve the Id for each entityName that has a filterAttribute value in the filterIdList.
        /// </summary>
        private static List<Guid> RetrieveReferenceAttributeIds(CrmOrganization org, string entityName, string retrieveAttribute, string filterAttribute, IList<Guid> filterIdList)
        {
            #region Argument Validation

            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (entityName == null)
            {
                throw new ArgumentNullException("entityName");
            }
            else if (retrieveAttribute == null)
            {
                throw new ArgumentNullException("retrieveAttribute");
            }
            else if (filterAttribute == null)
            {
                throw new ArgumentNullException("filterAttribute");
            }
            else if (retrieveAttribute == filterAttribute)
            {
                throw new ArgumentException("Attributes must be different");
            }

            #endregion Argument Validation

            if (filterIdList.Count == 0)
            {
                return new List<Guid>();
            }

            //Generate the query
            var cols = new ColumnSet();
            cols.AddColumns(retrieveAttribute);

            var idCondition = new ConditionExpression();
            idCondition.AttributeName = filterAttribute;
            idCondition.Operator = ConditionOperator.In;
            idCondition.Values.Clear();
            idCondition.Values.AddRange(ConvertIdArrayToObjectArray(filterIdList));

            var query = new QueryExpression();
            query.ColumnSet = cols;
            query.Criteria.AddCondition(idCondition);
            query.EntityName = entityName;

            //Loop through the results
            var resultList = new List<Guid>();
            foreach (var entity in org.OrganizationService.RetrieveMultipleAllPages(query).Entities)
            {
                foreach (KeyValuePair<string, object> prop in entity.Attributes)
                {
                    if (prop.Key == retrieveAttribute)
                    {
                        var propType = prop.Value.GetType();
                        if (propType == typeof(Guid))
                        {
                            resultList.Add((Guid)prop.Value);
                        }
                        else if (propType == typeof(EntityReference))
                        {
                            resultList.Add(((EntityReference)prop.Value).Id);
                        }
                        else
                        {
                            throw new ArgumentException("Unknown property returned " + prop.GetType().FullName);
                        }
                    }
                }
            }

            return resultList;
        }

        private static List<Guid> RetrieveStepIdsForPlugins(CrmOrganization org, IList<Guid> pluginIds)
        {
            return RetrieveReferenceAttributeIds(org, SdkMessageProcessingStep.EntityLogicalName, "sdkmessageprocessingstepid", "plugintypeid", pluginIds);
        }

        private static List<Guid> RetrieveStepIdsForServiceEndpoint(CrmOrganization org, IList<Guid> serviceEndpointIds)
        {
            return RetrieveReferenceAttributeIds(org, SdkMessageProcessingStep.EntityLogicalName, "sdkmessageprocessingstepid", "eventhandler", serviceEndpointIds);
        }

        #endregion Private Methods
    }
}