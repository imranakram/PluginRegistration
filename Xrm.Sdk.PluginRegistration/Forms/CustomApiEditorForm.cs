using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xrm.Sdk.PluginRegistration.Helpers;
using Xrm.Sdk.PluginRegistration.Models;
using Label = System.Windows.Forms.Label;

namespace Xrm.Sdk.PluginRegistration.Forms
{
    /// <summary>
    /// Edits a complete local draft and saves it atomically. The snapshot supplied by the caller
    /// remains unchanged so canceling this dialog cannot change the tree's cached definition.
    /// </summary>
    public sealed class CustomApiEditorForm : Form
    {
        private readonly IOrganizationService service;
        private readonly IReadOnlyList<string> tableNames;
        private readonly CustomApiDefinition original;
        private readonly CustomApiDefinition draft;
        private readonly TabControl tabs = new TabControl { Dock = DockStyle.Fill };
        private readonly TextBox name = CustomApiFormLayout.Text(100);
        private readonly TextBox uniqueName = CustomApiFormLayout.Text(128);
        private readonly Label uniqueNamePrefix = new Label
        {
            AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 4, 0),
            AccessibleName = "Solution publisher prefix"
        };
        private readonly TextBox displayName = CustomApiFormLayout.Text(100);
        private readonly TextBox description = CustomApiFormLayout.Text(300, true);
        private readonly TextBox boundTable = CustomApiFormLayout.Text(100);
        private readonly Label boundTableHint = new Label { AutoSize = true, MaximumSize = new Size(540, 0) };
        private readonly TextBox executePrivilege = CustomApiFormLayout.Text(100);
        private readonly ComboBox binding = CustomApiFormLayout.Choices();
        private readonly ComboBox processing = CustomApiFormLayout.Choices();
        private readonly ComboBox solution = CustomApiFormLayout.Choices();
        private readonly ComboBox assembly = CustomApiFormLayout.Choices();
        private readonly ComboBox pluginType = CustomApiFormLayout.Choices();
        private readonly CheckBox function = new CheckBox { Text = "Function (GET)", AutoSize = true };
        private readonly CheckBox isPrivate = new CheckBox { Text = "Private", AutoSize = true };
        private readonly Button createSolution = CustomApiFormLayout.Button("New solution...");
        private readonly Button save = CustomApiFormLayout.Button("Save");
        private readonly Button cancel = CustomApiFormLayout.Button("Cancel");
        private readonly Button reload = CustomApiFormLayout.Button("Reload choices");
        private readonly Label status = new Label { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(12, 6, 12, 6) };
        private readonly DataGridView requests = CreateGrid(true);
        private readonly DataGridView responses = CreateGrid(false);
        private readonly List<CustomApiPluginTypeOption> pluginTypes = new List<CustomApiPluginTypeOption>();
        private bool busy;
        private bool saving;
        private bool loaded;
        private bool uncertainSave;
        private bool settingChoices;

        public CustomApiEditorForm(IOrganizationService service, CustomApiDefinition original = null,
            IEnumerable<string> tableNames = null)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.tableNames = (tableNames ?? Enumerable.Empty<string>()).ToList();
            this.original = original?.Clone();
            draft = original?.Clone() ?? new CustomApiDefinition();
            Text = original == null ? "Create Custom API" : "Custom API: " + original.UniqueName;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(850, 740);
            MinimumSize = new Size(720, 560);
            Controls.Add(tabs);
            Controls.Add(status);
            Controls.Add(CustomApiFormLayout.Buttons(cancel, save, reload));
            BuildDetails();
            BuildParameters("Request parameters", requests, true);
            BuildParameters("Response properties", responses, false);
            PopulateFields();
            if (original == null)
            {
                CustomApiFormLayout.SuggestNames(name, displayName, uniqueName);
                ApplyPublisherPrefix();
            }
            RefreshParameters();
            cancel.DialogResult = DialogResult.Cancel;
            CancelButton = cancel;
            save.Click += SaveClicked;
            reload.Click += async (sender, args) => await LoadChoices();
            createSolution.Click += CreateSolutionClicked;
            binding.SelectedIndexChanged += (sender, args) => UpdateBindingState();
            solution.SelectedIndexChanged += (sender, args) => ApplyPublisherPrefix();
            assembly.SelectedIndexChanged += (sender, args) => FilterPluginTypes();
            Shown += async (sender, args) => await LoadChoices();
            FormClosing += (sender, args) => { if (saving) args.Cancel = true; };
            save.Enabled = false;
            UpdateBindingState();
        }

        public Guid? SavedApiId { get; private set; }
        public bool Saved { get; private set; }
        public bool SaveOutcomeUnknown => uncertainSave;

        private void BuildDetails()
        {
            var details = new TabPage("API") { AutoScroll = true };
            var fields = CustomApiFormLayout.CreateFields();
            details.Controls.Add(fields);
            tabs.TabPages.Add(details);
            if (original == null)
            {
                var solutionRow = new TableLayoutPanel { ColumnCount = 2, AutoSize = true };
                solutionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                solutionRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                solution.Dock = DockStyle.Fill;
                solutionRow.Controls.Add(solution, 0, 0);
                solutionRow.Controls.Add(createSolution, 1, 0);
                CustomApiFormLayout.AddRow(fields, "Solution *", solutionRow);
                solution.AccessibleName = "Solution";
            }
            CustomApiFormLayout.AddRow(fields, "Name *", name);
            CustomApiFormLayout.AddRow(fields, "Display name *", displayName);
            if (original == null)
            {
                // Keep the publisher prefix outside the editable suffix so selecting or pasting
                // over the text cannot remove the solution's namespace.
                var uniqueNameRow = new TableLayoutPanel { ColumnCount = 2, AutoSize = true };
                uniqueNameRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                uniqueNameRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                uniqueName.Dock = DockStyle.Fill;
                uniqueName.Margin = Padding.Empty;
                uniqueNameRow.Controls.Add(uniqueNamePrefix, 0, 0);
                uniqueNameRow.Controls.Add(uniqueName, 1, 0);
                CustomApiFormLayout.AddRow(fields, "Unique name *", uniqueNameRow);
                uniqueName.AccessibleName = "Unique name suffix";
            }
            else
            {
                CustomApiFormLayout.AddRow(fields, "Unique name *", uniqueName);
            }
            CustomApiFormLayout.AddRow(fields, "Description *", description);
            CustomApiFormLayout.AddRow(fields, "Binding type *", binding);
            CustomApiFormLayout.SuggestTables(boundTable, tableNames);
            CustomApiFormLayout.AddRow(fields, "Bound table logical name", boundTable);
            CustomApiFormLayout.AddRow(fields, string.Empty, boundTableHint);
            CustomApiFormLayout.AddRow(fields, "Allowed processing steps *", processing);
            CustomApiFormLayout.AddRow(fields, "Message type", function);
            CustomApiFormLayout.AddRow(fields, "Visibility", isPrivate);
            CustomApiFormLayout.AddRow(fields, "Execute privilege name", executePrivilege);
            CustomApiFormLayout.AddRow(fields, "Plug-in assembly", assembly);
            CustomApiFormLayout.AddRow(fields, "Plug-in type", pluginType);
            CustomApiFormLayout.AddRow(fields, string.Empty, new Label
            {
                AutoSize = true,
                MaximumSize = new Size(460, 0),
                Text = "* Required. Contract fields are fixed after creation. Child edits are saved with the API."
            });
            if (draft.WorkflowSdkStepEnabled)
                CustomApiFormLayout.AddRow(fields, string.Empty, new Label { Text = "Enabled for workflow", AutoSize = true });
            if (original?.IsManaged == true)
                CustomApiFormLayout.AddRow(fields, string.Empty, new Label
                {
                    Text = original.IsCustomizable ? "Managed API" : "Managed API: existing API fields cannot be customized.",
                    AutoSize = true
                });
        }

        private void BuildParameters(string title, DataGridView grid, bool request)
        {
            var page = new TabPage(title);
            page.Controls.Add(grid);
            var add = CustomApiFormLayout.Button("Add...");
            var edit = CustomApiFormLayout.Button("Edit...");
            var remove = CustomApiFormLayout.Button("Remove");
            page.Controls.Add(CustomApiFormLayout.Buttons(remove, edit, add));
            tabs.TabPages.Add(page);
            add.Click += (sender, args) => EditParameter(grid, request, true);
            edit.Click += (sender, args) => EditParameter(grid, request, false);
            remove.Click += (sender, args) => RemoveParameter(grid, request);
            grid.CellDoubleClick += (sender, args) => { if (args.RowIndex >= 0) EditParameter(grid, request, false); };
            grid.SelectionChanged += (sender, args) =>
            {
                var selected = SelectedParameter(grid);
                edit.Enabled = selected != null;
                remove.Enabled = selected != null && !selected.IsManaged;
            };
            edit.Enabled = remove.Enabled = false;
        }

        private void PopulateFields()
        {
            name.Text = draft.Name;
            uniqueName.Text = draft.UniqueName;
            displayName.Text = draft.DisplayName;
            description.Text = draft.Description;
            boundTable.Text = draft.BoundEntityLogicalName;
            executePrivilege.Text = draft.ExecutePrivilegeName;
            binding.DataSource = Enum.GetValues(typeof(CustomApiBindingType));
            binding.SelectedItem = draft.BindingType;
            processing.DataSource = Enum.GetValues(typeof(CustomApiProcessingStepType));
            processing.SelectedItem = draft.AllowedCustomProcessingStepType;
            function.Checked = draft.IsFunction;
            isPrivate.Checked = draft.IsPrivate;
            uniqueName.ReadOnly = original != null;
            binding.Enabled = processing.Enabled = function.Enabled = original == null;
            bool protectedApi = original?.IsManaged == true && !original.IsCustomizable;
            name.ReadOnly = displayName.ReadOnly = description.ReadOnly = executePrivilege.ReadOnly = protectedApi;
            isPrivate.Enabled = assembly.Enabled = pluginType.Enabled = !protectedApi;
        }

        private async Task LoadChoices()
        {
            if (busy) return;
            var currentType = loaded ? (pluginType.SelectedItem as HandlerChoice)?.Id : draft.PluginTypeId;
            string currentSolution = (solution.SelectedItem as CustomApiSolutionOption)?.UniqueName;
            SetBusy(true, original == null ? "Loading solutions and plug-in types..." : "Loading plug-in types...");
            try
            {
                var choices = await Task.Run(() => CustomApiEditorData.Load(service, currentType, includeSolutions: original == null));
                if (IsDisposed) return;
                settingChoices = true;
                pluginTypes.Clear();
                pluginTypes.AddRange(choices.PluginTypes);
                solution.DisplayMember = "Name";
                solution.DataSource = choices.Solutions.ToList();
                solution.SelectedIndex = -1;
                if (currentSolution != null)
                    solution.SelectedItem = choices.Solutions.FirstOrDefault(s => s.UniqueName == currentSolution);
                var assemblies = new List<string> { "(All assemblies)" };
                assemblies.AddRange(pluginTypes.Select(p => p.AssemblyName ?? string.Empty).Distinct().OrderBy(n => n));
                assembly.DataSource = assemblies;
                assembly.SelectedIndex = 0;
                settingChoices = false;
                PopulatePluginTypes(currentType);
                ApplyPublisherPrefix();
                loaded = true;
                status.Text = original != null ? "Edit the definition, then Save to apply all changes."
                    : choices.Solutions.Count == 0 ? "Create an unmanaged solution to continue."
                    : "Choose a solution and enter the API definition.";
            }
            catch (Exception error)
            {
                if (IsDisposed) return;
                // Keep previously loaded choices (and the current selection) usable after a failed reload.
                status.Text = "Choices could not be loaded. Use Reload choices to retry.";
                ShowError(error);
            }
            finally
            {
                settingChoices = false;
                if (!IsDisposed) SetBusy(false);
            }
        }

        private void ApplyPublisherPrefix()
        {
            if (settingChoices || original != null) return;
            string prefix = (solution.SelectedItem as CustomApiSolutionOption)?.PublisherPrefix;
            uniqueNamePrefix.Text = string.IsNullOrEmpty(prefix) ? string.Empty : prefix + "_";
            uniqueName.Enabled = !string.IsNullOrEmpty(prefix);
            uniqueName.MaxLength = Math.Max(1, 128 - uniqueNamePrefix.Text.Length);
        }

        private void FilterPluginTypes()
        {
            if (settingChoices) return;
            PopulatePluginTypes((pluginType.SelectedItem as HandlerChoice)?.Id);
        }

        private void PopulatePluginTypes(Guid? selectedId)
        {
            var types = pluginTypes.AsEnumerable();
            if (assembly.SelectedIndex > 0)
                types = types.Where(p => (p.AssemblyName ?? string.Empty) == (string)assembly.SelectedItem);
            var choices = new List<HandlerChoice> { new HandlerChoice { Name = "(None)" } };
            choices.AddRange(types.Select(p => new HandlerChoice { Id = p.Id, Name = p.Name }));
            if (selectedId.HasValue && choices.All(p => p.Id != selectedId))
            {
                // Browsing assemblies must not silently remove the current handler from the draft.
                var retained = pluginTypes.FirstOrDefault(p => p.Id == selectedId.Value);
                if (retained != null)
                    choices.Add(new HandlerChoice { Id = retained.Id, Name = retained.Name + " (selected; another assembly)" });
            }
            pluginType.DisplayMember = "Name";
            pluginType.DataSource = choices;
            pluginType.SelectedItem = choices.FirstOrDefault(p => p.Id == selectedId) ?? choices[0];
        }

        private void UpdateBindingState()
        {
            bool global = binding.SelectedItem is CustomApiBindingType selected && selected == CustomApiBindingType.Global;
            boundTable.ReadOnly = original != null || global;
            boundTable.Enabled = !global;
            boundTableHint.Text = original != null
                ? "Binding type and bound table are fixed after the API is created."
                : global ? "Global APIs have no bound table. Choose Entity or EntityCollection to enter a table."
                : "Enter a table logical name; matching tables are suggested as you type. A bound table is required.";
            if (original == null && global) boundTable.Clear();
        }

        private CustomApiDefinition CaptureDraft()
        {
            var candidate = draft.Clone();
            candidate.Name = name.Text;
            var selectedSolution = solution.SelectedItem as CustomApiSolutionOption;
            candidate.UniqueName = original == null && selectedSolution != null
                ? selectedSolution.PublisherPrefix + "_" + uniqueName.Text
                : uniqueName.Text;
            candidate.DisplayName = displayName.Text;
            candidate.Description = description.Text;
            candidate.BoundEntityLogicalName = boundTable.Text;
            candidate.ExecutePrivilegeName = executePrivilege.Text;
            candidate.BindingType = binding.SelectedItem is CustomApiBindingType selectedBinding ? selectedBinding : (CustomApiBindingType)(-1);
            candidate.AllowedCustomProcessingStepType = processing.SelectedItem is CustomApiProcessingStepType selectedProcessing
                ? selectedProcessing : (CustomApiProcessingStepType)(-1);
            candidate.IsFunction = function.Checked;
            candidate.IsPrivate = isPrivate.Checked;
            candidate.PluginTypeId = (pluginType.SelectedItem as HandlerChoice)?.Id;
            return candidate;
        }

        private async void SaveClicked(object sender, EventArgs args)
        {
            if (busy || !loaded || uncertainSave) return;
            var candidate = CaptureDraft();
            var selectedSolution = solution.SelectedItem as CustomApiSolutionOption;
            var errors = CustomApiValidator.Validate(candidate, original, original == null ? selectedSolution?.PublisherPrefix : null).ToList();
            if (original == null && selectedSolution == null) errors.Add("Select an unmanaged solution.");
            if (errors.Count > 0)
            {
                MessageBox.Show(this, string.Join(Environment.NewLine, errors), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            saving = true;
            SetBusy(true, "Saving Custom API...");
            try
            {
                // Capture all UI values before background work. A successful write is recorded before
                // the caller refreshes its tree, so a refresh failure never repeats the write.
                Guid? id = await Task.Run(() =>
                {
                    var apis = new CustomApiService(service);
                    if (original == null) return (Guid?)apis.Create(candidate, selectedSolution.UniqueName);
                    return apis.Update(original, candidate) ? (Guid?)original.Id : null;
                });
                SavedApiId = id;
                Saved = id.HasValue;
                saving = false;
                SetBusy(false);
                DialogResult = Saved ? DialogResult.OK : DialogResult.Cancel;
                Close();
            }
            catch (Exception error)
            {
                uncertainSave = CustomApiService.IsSaveOutcomeUnknown(error);
                status.Text = uncertainSave
                    ? "Save outcome is unknown. Close, refresh, and inspect '" + candidate.UniqueName + "' before saving again."
                    : "Changes were not saved. Correct the reported error and try again.";
                MessageBox.Show(this, status.Text + Environment.NewLine + Environment.NewLine + error.Message,
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                saving = false;
                if (!IsDisposed) SetBusy(false);
            }
        }

        private void CreateSolutionClicked(object sender, EventArgs args)
        {
            using (var dialog = new CustomApiSolutionForm(service))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                var solutions = solution.Items.Cast<CustomApiSolutionOption>().ToList();
                solutions.Add(dialog.CreatedSolution);
                solution.DataSource = solutions.OrderBy(s => s.Name).ToList();
                solution.SelectedItem = dialog.CreatedSolution;
                status.Text = "Solution created. Enter the API definition and Save.";
            }
        }

        private void EditParameter(DataGridView grid, bool request, bool add)
        {
            var existing = add ? null : SelectedParameter(grid);
            if (!add && existing == null) return;
            CustomApiParameter parameter = existing ?? (request
                ? (CustomApiParameter)new CustomApiRequestParameter { Type = CustomApiParameterType.String }
                : new CustomApiResponseProperty { Type = CustomApiParameterType.String });
            // Pass the draft lists themselves for duplicate checks, and copy current binding fields.
            draft.BindingType = binding.SelectedItem is CustomApiBindingType selected ? selected : CustomApiBindingType.Global;
            using (var dialog = new CustomApiParameterForm(draft, original, parameter, tableNames))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                if (request)
                {
                    int index = draft.RequestParameters.IndexOf(existing as CustomApiRequestParameter);
                    if (index < 0) draft.RequestParameters.Add((CustomApiRequestParameter)dialog.Parameter);
                    else draft.RequestParameters[index] = (CustomApiRequestParameter)dialog.Parameter;
                }
                else
                {
                    int index = draft.ResponseProperties.IndexOf(existing as CustomApiResponseProperty);
                    if (index < 0) draft.ResponseProperties.Add((CustomApiResponseProperty)dialog.Parameter);
                    else draft.ResponseProperties[index] = (CustomApiResponseProperty)dialog.Parameter;
                }
                RefreshParameters();
            }
        }

        private void RemoveParameter(DataGridView grid, bool request)
        {
            var selected = SelectedParameter(grid);
            if (selected == null || selected.IsManaged) return;
            if (MessageBox.Show(this, "Remove '" + selected.UniqueName + "' from the draft? The server is updated only when you Save.",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (request) draft.RequestParameters.Remove((CustomApiRequestParameter)selected);
            else draft.ResponseProperties.Remove((CustomApiResponseProperty)selected);
            RefreshParameters();
        }

        private void RefreshParameters()
        {
            requests.DataSource = null;
            responses.DataSource = null;
            requests.DataSource = draft.RequestParameters.ToList();
            responses.DataSource = draft.ResponseProperties.ToList();
            tabs.TabPages[1].Text = "Request parameters (" + draft.RequestParameters.Count + ")";
            tabs.TabPages[2].Text = "Response properties (" + draft.ResponseProperties.Count + ")";
        }

        private static CustomApiParameter SelectedParameter(DataGridView grid)
        {
            return grid.CurrentRow?.DataBoundItem as CustomApiParameter;
        }

        private static DataGridView CreateGrid(bool request)
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SystemColors.Window
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "UniqueName", HeaderText = "Unique name", FillWeight = 35 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DisplayName", HeaderText = "Display name", FillWeight = 30 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Type", HeaderText = "Type", FillWeight = 20 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "LogicalEntityName", HeaderText = "Table", FillWeight = 20 });
            if (request) grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsOptional", HeaderText = "Optional", FillWeight = 15 });
            grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsManaged", HeaderText = "Managed", FillWeight = 15 });
            return grid;
        }

        private void SetBusy(bool value, string message = null)
        {
            busy = value;
            tabs.Enabled = reload.Enabled = !value;
            cancel.Enabled = !saving;
            save.Enabled = !value && loaded && !uncertainSave;
            UseWaitCursor = value;
            if (message != null) status.Text = message;
        }

        private void ShowError(Exception error)
        {
            MessageBox.Show(this, error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private sealed class HandlerChoice
        {
            public Guid? Id { get; set; }
            public string Name { get; set; }
        }
    }
}
