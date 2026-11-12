using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Xrm.Sdk.PluginRegistration.Helpers;
using Xrm.Sdk.PluginRegistration.Models;
using Label = System.Windows.Forms.Label;

namespace Xrm.Sdk.PluginRegistration.Forms
{
    /// <summary>Edits one local child definition. The parent editor owns persistence.</summary>
    public sealed class CustomApiParameterForm : Form
    {
        private readonly CustomApiDefinition parent;
        private readonly CustomApiDefinition originalParent;
        private readonly CustomApiParameter initial;
        private readonly CustomApiParameter editingParameter;
        private readonly TextBox name = CustomApiFormLayout.Text(100);
        private readonly TextBox uniqueName = CustomApiFormLayout.Text(128);
        private readonly TextBox displayName = CustomApiFormLayout.Text(100);
        private readonly TextBox description;
        private readonly TextBox logicalName = CustomApiFormLayout.Text(100);
        private readonly Label tableHint = new Label { AutoSize = true, MaximumSize = new Size(420, 0) };
        private readonly ComboBox type = CustomApiFormLayout.Choices();
        private readonly CheckBox optional = new CheckBox { Text = "Optional", AutoSize = true };

        public CustomApiParameterForm(CustomApiDefinition parent, CustomApiDefinition originalParent,
            CustomApiParameter parameter, IEnumerable<string> tableNames = null)
        {
            this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
            this.originalParent = originalParent;
            editingParameter = parameter;
            initial = parameter?.Clone() ?? throw new ArgumentNullException(nameof(parameter));
            bool request = initial is CustomApiRequestParameter;
            description = CustomApiFormLayout.Text(request ? 300 : 100, true);
            Text = request ? "Request Parameter" : "Response Property";
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(610, 430);
            MinimumSize = new Size(520, 420);

            var fields = CustomApiFormLayout.CreateFields();
            CustomApiFormLayout.AddRow(fields, "Name *", name);
            CustomApiFormLayout.AddRow(fields, "Unique name *", uniqueName);
            CustomApiFormLayout.AddRow(fields, "Display name *", displayName);
            CustomApiFormLayout.AddRow(fields, "Description", description);
            CustomApiFormLayout.AddRow(fields, "Type *", type);
            CustomApiFormLayout.SuggestTables(logicalName, tableNames);
            CustomApiFormLayout.AddRow(fields, "Table logical name", logicalName);
            CustomApiFormLayout.AddRow(fields, string.Empty, tableHint);
            if (request) CustomApiFormLayout.AddRow(fields, "Is optional", optional);
            var content = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            content.Controls.Add(fields);
            Controls.Add(content);

            var accept = CustomApiFormLayout.Button("OK");
            var cancel = CustomApiFormLayout.Button("Cancel");
            cancel.DialogResult = DialogResult.Cancel;
            accept.Click += AcceptClicked;
            Controls.Add(CustomApiFormLayout.Buttons(cancel, accept));
            AcceptButton = accept;
            CancelButton = cancel;

            name.Text = initial.Name;
            uniqueName.Text = initial.UniqueName;
            displayName.Text = initial.DisplayName;
            description.Text = initial.Description;
            logicalName.Text = initial.LogicalEntityName;
            type.DataSource = Enum.GetValues(typeof(CustomApiParameterType));
            type.SelectedItem = initial.Type;
            optional.Checked = (initial as CustomApiRequestParameter)?.IsOptional == true;
            bool saved = initial.Id != Guid.Empty;
            if (!saved)
            {
                // Preserve names already supplied when reopening an unsaved child.
                CustomApiFormLayout.SuggestNames(name, displayName, uniqueName);
            }
            uniqueName.ReadOnly = saved;
            type.Enabled = !saved;
            optional.Enabled = !saved;
            bool protectedChild = initial.IsManaged && !initial.IsCustomizable;
            name.ReadOnly = displayName.ReadOnly = description.ReadOnly = protectedChild;
            type.SelectedIndexChanged += (sender, args) => UpdateTableState();
            UpdateTableState();
        }

        public CustomApiParameter Parameter { get; private set; }

        private void UpdateTableState()
        {
            var selected = type.SelectedItem as CustomApiParameterType?;
            bool allowsTable = selected == CustomApiParameterType.Entity || selected == CustomApiParameterType.EntityReference ||
                (initial is CustomApiRequestParameter && selected == CustomApiParameterType.EntityCollection);
            logicalName.ReadOnly = initial.Id != Guid.Empty || !allowsTable;
            logicalName.Enabled = allowsTable;
            tableHint.Text = initial.Id != Guid.Empty
                ? "Type and table logical name are fixed after creation."
                : !allowsTable ? "This type does not support a table logical name."
                : "Enter a table logical name; matching tables are suggested as you type. Leave blank for any table.";
            if (initial.Id == Guid.Empty && !allowsTable) logicalName.Clear();
        }

        private void AcceptClicked(object sender, EventArgs args)
        {
            var candidate = initial.Clone();
            candidate.Name = name.Text;
            candidate.UniqueName = uniqueName.Text;
            candidate.DisplayName = displayName.Text;
            candidate.Description = description.Text;
            candidate.LogicalEntityName = logicalName.Text;
            candidate.Type = type.SelectedItem is CustomApiParameterType selected ? selected : (CustomApiParameterType)(-1);
            if (candidate is CustomApiRequestParameter request) request.IsOptional = optional.Checked;

            var errors = ValidateCandidate(candidate);
            if (errors.Count > 0)
            {
                MessageBox.Show(this, string.Join(Environment.NewLine, errors), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Parameter = candidate;
            DialogResult = DialogResult.OK;
            Close();
        }

        private IReadOnlyList<string> ValidateCandidate(CustomApiParameter candidate)
        {
            // Validate this child even when the user has not finished the parent's required fields.
            // A second, complete validation at Save checks sibling names and the full API contract.
            var validationDraft = ValidationParent();
            AddParameter(validationDraft, candidate);
            CustomApiDefinition validationOriginal = null;
            if (originalParent != null)
            {
                validationOriginal = ValidationParent();
                var saved = originalParent.RequestParameters.Cast<CustomApiParameter>()
                    .Concat(originalParent.ResponseProperties).FirstOrDefault(p => p.Id == candidate.Id && p.Id != Guid.Empty);
                if (saved != null) AddParameter(validationOriginal, saved.Clone());
            }
            var errors = CustomApiValidator.Validate(validationDraft, validationOriginal).ToList();
            var siblings = candidate is CustomApiRequestParameter
                ? parent.RequestParameters.Cast<CustomApiParameter>() : parent.ResponseProperties.Cast<CustomApiParameter>();
            // Unsaved rows have no ID, so the caller passes the original row object to identify it.
            if (siblings.Any(p => !ReferenceEquals(p, editingParameter) &&
                string.Equals(p.UniqueName, candidate.UniqueName, StringComparison.OrdinalIgnoreCase)))
                errors.Add("A parameter with this unique name already exists in this list.");
            return errors;
        }

        private CustomApiDefinition ValidationParent()
        {
            return new CustomApiDefinition
            {
                Id = originalParent?.Id ?? Guid.Empty,
                Name = "Validation",
                UniqueName = "Validation",
                DisplayName = "Validation",
                Description = "Validation",
                BindingType = parent.BindingType,
                BoundEntityLogicalName = parent.BindingType == CustomApiBindingType.Global ? null : "validation",
                WorkflowSdkStepEnabled = parent.WorkflowSdkStepEnabled,
                IsManaged = originalParent?.IsManaged == true,
                IsCustomizable = originalParent?.IsCustomizable ?? true
            };
        }

        private static void AddParameter(CustomApiDefinition definition, CustomApiParameter parameter)
        {
            if (parameter is CustomApiRequestParameter request) definition.RequestParameters.Add(request);
            else definition.ResponseProperties.Add((CustomApiResponseProperty)parameter);
        }
    }
}
