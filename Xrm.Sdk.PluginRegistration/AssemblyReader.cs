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

namespace Xrm.Sdk.PluginRegistration
{
    using Helpers;
    using Microsoft.Xrm.Sdk;
    using System;
    using System.Activities;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Wrappers;

    public sealed class AssemblyReader : MarshalByRefObject
    {
        #region Public Constructors

        public AssemblyReader()
        {
            //Attach the resolver so that assemblies can be resolved correctly
            AssemblyResolver.AttachResolver(AppDomain.CurrentDomain);
        }

        #endregion Public Constructors

        #region Public Methods

        public CrmPluginAssembly RetrieveAssemblyProperties(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            else if (!File.Exists(path))
            {
                throw new ArgumentException("Path does not point to an existing file");
            }

            return RetrieveAssemblyProperties(LoadAssembly(path), path);
        }

        public CrmPluginAssembly RetrievePluginsFromAssembly(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            else if (!File.Exists(path))
            {
                throw new ArgumentException("Path does not point to an existing file");
            }

            //Load the assembly
            var assembly = LoadAssembly(path);
            var assemblyName = assembly.GetName();
            var defaultGroupName = RegistrationHelper.GenerateDefaultGroupName(assemblyName.Name, assemblyName.Version);

            //Retrieve the assembly properties
            var pluginAssembly = RetrieveAssemblyProperties(assembly, path);

            //Loop through each type and process it
            var errorList = new List<string>();
            foreach (var t in assembly.GetExportedTypes())
            {
                //Plugins and Workflow Activities must be non-abstract classes
                if (t.IsAbstract || !t.IsClass)
                {
                    continue;
                }

                // Plugins must implement the IPlugin interface
                string errorMessage = null;
                CrmPluginType type;
                CrmPluginIsolatable isolatable;

                //Retrieve the two interface types
                Type xrmPlugin = t.GetInterface(typeof(IPlugin).FullName);
                var v4Plugin = t.GetInterface("Microsoft.Crm.Sdk.IPlugin");

                var workflowGroupName = defaultGroupName;
                var pluginName = t.FullName;

                Version sdkVersion = null;
                if (null != xrmPlugin)
                {
                    type = CrmPluginType.Plugin;
                    isolatable = CrmPluginIsolatable.Yes;
                    sdkVersion = xrmPlugin.Assembly.GetName().Version;
                }
                else if (null != v4Plugin)
                {
                    type = CrmPluginType.Plugin;
                    isolatable = CrmPluginIsolatable.No;
                }
                else if (t.IsSubclassOf(typeof(CodeActivity)) || t.IsSubclassOf(typeof(Activity)))
                {
                    type = CrmPluginType.WorkflowActivity;
                    isolatable = CrmPluginIsolatable.Yes;
                }
                else
                {
                    continue;
                }

                if (errorMessage != null)
                {
                    errorList.Add(errorMessage);
                }
                else
                {
                    var plugin = new CrmPlugin(null)
                    {
                        TypeName = t.FullName,
                        Name = pluginName,
                        PluginType = type,
                        PluginId = Guid.NewGuid(),
                        AssemblyId = pluginAssembly.AssemblyId,
                        AssemblyName = pluginAssembly.Name,
                        Isolatable = isolatable
                    };

                    if (type == CrmPluginType.WorkflowActivity && !string.IsNullOrWhiteSpace(workflowGroupName))
                    {
                        plugin.WorkflowActivityGroupName = workflowGroupName;
                    }

                    if (type == CrmPluginType.WorkflowActivity)
                    {
                        //Compared with the registered arguments before an update
                        plugin.ActivityArguments = WorkflowActivityArguments.Read(t);
                    }

                    pluginAssembly.AddPlugin(plugin);
                }
            }

            return pluginAssembly;
        }

        #endregion Public Methods

        #region Private Methods

        private Assembly LoadAssembly(string path)
        {
            return Assembly.LoadFrom(path);
        }

        private CrmPluginAssembly RetrieveAssemblyProperties(Assembly assembly, string path)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            var pluginAssembly = new CrmPluginAssembly(null)
            {
                AssemblyId = Guid.NewGuid(),
                SourceType = CrmAssemblySourceType.Disk
            };

            var fileInfo = new FileInfo(path);
            pluginAssembly.ServerFileName = fileInfo.Name;

            var name = assembly.GetName();
            string cultureLabel;
            if (name.CultureInfo.LCID == System.Globalization.CultureInfo.InvariantCulture.LCID)
            {
                cultureLabel = "neutral";
            }
            else
            {
                cultureLabel = name.CultureInfo.Name;
            }

            pluginAssembly.Name = name.Name;
            pluginAssembly.Version = name.Version.ToString();
            pluginAssembly.Culture = cultureLabel;

            byte[] tokenBytes = name.GetPublicKeyToken();
            if (null == tokenBytes || 0 == tokenBytes.Length)
            {
                pluginAssembly.PublicKeyToken = null;
            }
            else
            {
                pluginAssembly.PublicKeyToken = string.Join(string.Empty, tokenBytes.Select(b => b.ToString("X2")));
            }

            return pluginAssembly;
        }

        #endregion Private Methods
    }
}