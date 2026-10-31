using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Xrm.Sdk.PluginRegistration.Forms;
using Xrm.Sdk.PluginRegistration.Helpers;
using Xrm.Sdk.PluginRegistration.Models;
using Xrm.Sdk.PluginRegistration.Wrappers;
using XrmToolBox.Extensibility;

namespace Xrm.Sdk.PluginRegistration
{
    public partial class MainControl
    {
        private CustomApiCatalog customApiCatalog = new CustomApiCatalog();
        private readonly Dictionary<Guid, CrmCustomApiNode> customApiMessageNodes = new Dictionary<Guid, CrmCustomApiNode>();
        private readonly List<ToolStripMenuItem> customApiRegisterItems = new List<ToolStripMenuItem>();
        private int? customApiErrorGeneration;
        private bool? customApiSupport;

        private void ResetCustomApiState()
        {
            customApiSupport = null;
            customApiMessageNodes.Clear();
            ApplyCustomApiCatalog(new CustomApiCatalog());
        }

        private void InitializeCustomApiMenus()
        {
            foreach (var items in new[] { toolRegister.DropDownItems, mnuContextNode.Items, mnuContextGeneral.Items })
            {
                var item = new ToolStripMenuItem("Register New Custom API", toolViewMessage.Image,
                    (sender, args) => OpenCustomApiEditor(null)) { Enabled = false };
                items.Add(item);
                customApiRegisterItems.Add(item);
            }
        }

        private static CustomApiCatalog ReadCustomApiCatalog(CrmOrganization organization, Action<string> reportProgress, bool? knownSupport = null)
        {
            // Optional capability faults must not prevent the existing registration tool loading.
            try
            {
                var apiService = new CustomApiService(organization.OrganizationService);
                if (!knownSupport.HasValue) reportProgress("Checking Custom API support...");
                bool supported = knownSupport ?? apiService.IsSupported();
                var catalog = new CustomApiCatalog { Supported = supported };
                if (supported)
                {
                    reportProgress("Loading Custom APIs...");
                    catalog.Definitions = apiService.List();
                    reportProgress("Loading Custom API step messages...");
                    OrganizationHelper.LoadCustomApiStepMessages(organization, catalog.Definitions);
                }
                return catalog;
            }
            catch (Exception error)
            {
                return new CustomApiCatalog { Error = error };
            }
        }

        private void ApplyCustomApiCatalog(CustomApiCatalog catalog)
        {
            customApiCatalog = catalog;
            // Only the current connection's UI callback applies results. Cache only a confirmed
            // schema: failures and "unsupported" are rechecked on refresh; reconnect clears it.
            customApiSupport = catalog.Error == null && catalog.Supported ? true : (bool?)null;
            foreach (var item in customApiRegisterItems) item.Enabled = catalog.Supported;
            if (catalog.Error != null && customApiErrorGeneration != connectionGeneration)
            {
                customApiErrorGeneration = connectionGeneration;
                ErrorMessageForm.ShowErrorMessageBox(this,
                    "Custom APIs could not be loaded. Other registration features remain available. Refresh to retry.",
                    "Custom API loading", catalog.Error);
            }
        }

        private List<CrmCustomApiNode> CreateCustomApiNodes()
        {
            customApiMessageNodes.Clear();
            var nodes = customApiCatalog.Definitions.Select(api => new CrmCustomApiNode(api)).ToList();
            foreach (var node in nodes)
            {
                if (node.SdkMessageId.HasValue && node.SdkMessageId.Value != Guid.Empty)
                    customApiMessageNodes[node.SdkMessageId.Value] = node;
            }
            return nodes;
        }

        private void OpenCustomApiEditor(Guid? id)
        {
            if (!CanRunRegistrationCommand || !customApiCatalog.Supported) return;
            var organization = m_org;
            int generation = connectionGeneration;
            var service = organization.OrganizationService;
            var tableNames = CustomApiTableNames(organization);
            // Block repeated opens while the definition loads, as RefreshFullTreeView does.
            SetRegistrationControlsEnabled(false);
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading Custom API...",
                Work = (worker, args) => args.Result = id.HasValue ? new CustomApiService(service).Retrieve(id.Value) : null,
                PostWorkCallBack = args =>
                {
                    if (generation != connectionGeneration || IsDisposed) return;
                    SetRegistrationControlsEnabled(connectionReady);
                    if (args.Error != null)
                    {
                        ErrorMessageForm.ShowErrorMessageBox(this, "Unable to load the Custom API.", "Custom API", args.Error);
                        return;
                    }
                    using (var editor = new CustomApiEditorForm(service, (CustomApiDefinition)args.Result, tableNames))
                    {
                        editor.ShowDialog(ParentForm);
                        if ((editor.Saved || editor.SaveOutcomeUnknown) && generation == connectionGeneration)
                        {
                            // Regenerate both SDK messages and the independent API roots after a save.
                            RefreshFullTreeView(editor.SavedApiId, CrmViewType.Message);
                        }
                    }
                }
            });
        }

        private void AddCustomApiToSolution(CrmCustomApiNode api)
        {
            if (!CanRunRegistrationCommand || !customApiCatalog.Supported) return;
            var service = m_org.OrganizationService;
            int generation = connectionGeneration;
            using (var dialog = new SolutionPicker(service))
            {
                if (dialog.ShowDialog(ParentForm) != DialogResult.OK || generation != connectionGeneration) return;
                string solutionName = dialog.SelectedSolution.GetAttributeValue<string>("uniquename");
                string solutionLabel = dialog.SelectedSolution.GetAttributeValue<string>("friendlyname");
                WorkAsync(new WorkAsyncInfo
                {
                    Message = $"Adding Custom API to solution {solutionLabel}",
                    Work = (worker, args) => new CustomApiService(service).AddToSolution(api.Id, solutionName),
                    PostWorkCallBack = args =>
                    {
                        if (generation != connectionGeneration || IsDisposed) return;
                        if (args.Error != null)
                            ErrorMessageForm.ShowErrorMessageBox(this, "Unable to add the Custom API to the solution.",
                                "Add to Solution", args.Error);
                    }
                });
            }
        }

        /// <summary>
        /// Table names for autocomplete, taken from the already-loaded message filters as the step form does.
        /// </summary>
        private static List<string> CustomApiTableNames(CrmOrganization organization)
        {
            return organization.MessageEntities
                .Select(filter => filter.PrimaryEntity)
                .Where(table => !string.IsNullOrEmpty(table) && !string.Equals(table, "none", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(table => table, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void DeleteCustomApi(CrmCustomApiNode api)
        {
            if (!CanRunRegistrationCommand || !customApiCatalog.Supported) return;
            if (api.IsManaged)
            {
                MessageBox.Show(this, "Managed Custom APIs cannot be deleted here.", "Delete Custom API",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show(this,
                "Delete Custom API '" + api.UniqueName + "' and its request parameters and response properties? " +
                "Registered steps and other dependencies may prevent deletion.", "Delete Custom API",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

            var service = m_org.OrganizationService;
            int generation = connectionGeneration;
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Deleting Custom API...",
                Work = (worker, args) => new CustomApiService(service).Delete(api.Id),
                PostWorkCallBack = args =>
                {
                    if (generation != connectionGeneration || IsDisposed) return;
                    if (args.Error != null)
                        ErrorMessageForm.ShowErrorMessageBox(this, "Unable to delete the Custom API.", "Custom API", args.Error);
                    // A failed response can still follow a successful server delete; refresh without retrying.
                    RefreshFullTreeView();
                }
            });
        }

        private sealed class CustomApiCatalog
        {
            public bool Supported { get; set; }
            public IReadOnlyList<CustomApiSummary> Definitions { get; set; } = new CustomApiSummary[0];
            public Exception Error { get; set; }
        }

        private sealed class OrganizationLoadResult
        {
            public CrmOrganization Organization { get; set; }
            public CustomApiCatalog CustomApis { get; set; }
        }
    }
}
