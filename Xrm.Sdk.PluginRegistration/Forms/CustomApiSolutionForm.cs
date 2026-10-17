using Microsoft.Xrm.Sdk;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xrm.Sdk.PluginRegistration.Helpers;
using Label = System.Windows.Forms.Label;

namespace Xrm.Sdk.PluginRegistration.Forms
{
    public sealed class CustomApiSolutionForm : Form
    {
        private readonly IOrganizationService service;
        private readonly TableLayoutPanel fields = CustomApiFormLayout.CreateFields();
        private readonly TextBox uniqueName = CustomApiFormLayout.Text(65);
        private readonly TextBox displayName = CustomApiFormLayout.Text(256);
        private readonly TextBox version = CustomApiFormLayout.Text(50);
        private readonly ComboBox publisher = CustomApiFormLayout.Choices();
        private readonly Button create = CustomApiFormLayout.Button("Create");
        private readonly Button cancel = CustomApiFormLayout.Button("Cancel");
        private readonly Label status = new Label { Height = 48, Dock = DockStyle.Bottom, Padding = new Padding(12, 6, 12, 6) };
        private bool busy;
        private bool saving;
        private bool loaded;
        private bool uncertainSave;

        public CustomApiSolutionForm(IOrganizationService service)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            Text = "Create Unmanaged Solution";
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(600, 330);
            MinimumSize = new Size(520, 330);
            CustomApiFormLayout.AddRow(fields, "Unique name *", uniqueName);
            CustomApiFormLayout.AddRow(fields, "Display name *", displayName);
            CustomApiFormLayout.AddRow(fields, "Version *", version);
            CustomApiFormLayout.AddRow(fields, "Publisher *", publisher);
            CustomApiFormLayout.AddRow(fields, string.Empty, new Label
            {
                AutoSize = true,
                MaximumSize = new Size(360, 0),
                Text = "The solution is created immediately and remains if you cancel the API editor."
            });
            version.Text = "1.0.0.0";
            var content = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            content.Controls.Add(fields);
            Controls.Add(content);
            Controls.Add(status);
            Controls.Add(CustomApiFormLayout.Buttons(cancel, create));
            cancel.DialogResult = DialogResult.Cancel;
            AcceptButton = create;
            CancelButton = cancel;
            create.Click += CreateClicked;
            Shown += async (sender, args) => await LoadPublishers();
            FormClosing += (sender, args) => { if (saving) args.Cancel = true; };
            create.Enabled = false;
        }

        public CustomApiSolutionOption CreatedSolution { get; private set; }

        private async Task LoadPublishers()
        {
            SetBusy(true, "Loading publishers...");
            try
            {
                var publishers = await Task.Run(() => CustomApiEditorData.LoadPublishers(service));
                if (IsDisposed) return;
                publisher.DisplayMember = "Name";
                publisher.DataSource = publishers;
                loaded = true;
                status.Text = publishers.Count == 0 ? "No publishers are available. Create a publisher in Power Apps first." : "Select the publisher for this solution.";
            }
            catch (Exception error)
            {
                if (IsDisposed) return;
                status.Text = "Publishers could not be loaded. Close and reopen this dialog to retry.";
                MessageBox.Show(this, error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { if (!IsDisposed) SetBusy(false); }
        }

        private async void CreateClicked(object sender, EventArgs args)
        {
            if (busy || uncertainSave) return;
            var selectedPublisher = publisher.SelectedItem as CustomApiPublisherOption;
            if (selectedPublisher == null || string.IsNullOrWhiteSpace(uniqueName.Text) || string.IsNullOrWhiteSpace(displayName.Text))
            {
                MessageBox.Show(this, "Enter the solution names and select a publisher.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string solutionName = uniqueName.Text;
            string solutionDisplayName = displayName.Text;
            string solutionVersion = version.Text;
            saving = true;
            SetBusy(true, "Creating solution...");
            try
            {
                CreatedSolution = await Task.Run(() => CustomApiEditorData.CreateSolution(service,
                    solutionName, solutionDisplayName, solutionVersion, selectedPublisher));
                saving = false;
                SetBusy(false);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception error)
            {
                // A transport failure does not prove the create was rolled back. Never submit it twice.
                uncertainSave = CustomApiService.IsSaveOutcomeUnknown(error);
                status.Text = uncertainSave
                    ? "Save outcome is unknown. Check for solution '" + solutionName + "' before creating it again."
                    : "The solution was not created. Correct the error and try again.";
                MessageBox.Show(this, status.Text + Environment.NewLine + Environment.NewLine + error.Message,
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                saving = false;
                if (!IsDisposed) SetBusy(false);
            }
        }

        private void SetBusy(bool value, string message = null)
        {
            busy = value;
            fields.Enabled = !value;
            cancel.Enabled = !saving;
            create.Enabled = !value && loaded && publisher.Items.Count > 0 && !uncertainSave;
            UseWaitCursor = value;
            if (message != null) status.Text = message;
        }
    }
}
