namespace Xrm.Sdk.PluginRegistration.Forms
{
    using Helpers;
    using Microsoft.Xrm.Sdk;
    using Models;
    using System;
    using System.Windows.Forms;

    /// <summary>
    /// Creates a managed identity, or edits an existing one, and saves it to Dataverse.
    /// </summary>
    public partial class ManagedIdentityEditForm : Form
    {
        #region Private Fields

        private readonly ManagedIdentity existing;
        private readonly IOrganizationService service;
        private string solutionUniqueName;

        #endregion Private Fields

        #region Public Constructors

        /// <param name="existing">The identity to edit, or null to create one.</param>
        /// <param name="defaultName">Suggested name for a new identity.</param>
        public ManagedIdentityEditForm(IOrganizationService service, ManagedIdentity existing, string defaultName)
        {
            InitializeComponent();

            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.existing = existing;

            foreach (var scope in Enum.GetValues(typeof(ManagedIdentity.Scope)))
            {
                cboScope.Items.Add(scope);
            }

            if (existing == null)
            {
                Text = "New Managed Identity";
                txtName.Text = defaultName;
                cboScope.SelectedItem = ManagedIdentity.Scope.Environment;
                // New identities always use the version Microsoft recommends.
                numVersion.Minimum = numVersion.Maximum = ManagedIdentity.RecommendedVersion;
                numVersion.Value = ManagedIdentity.RecommendedVersion;
                numVersion.Enabled = false;
            }
            else
            {
                Text = "Edit Managed Identity";
                txtName.Text = existing.Name;
                txtApplicationId.Text = existing.ApplicationId.ToString();
                txtTenantId.Text = existing.TenantId.ToString();
                cboScope.SelectedItem = existing.SubjectScope;
                // An empty version means the original (deprecated) format. Keep the current version until the
                // user upgrades on purpose, and only allow upgrades: version 0 is deprecated, 2 is recommended.
                var current = Math.Max(0, Math.Min(ManagedIdentity.RecommendedVersion, existing.Version ?? 0));
                numVersion.Minimum = current;
                numVersion.Maximum = ManagedIdentity.RecommendedVersion;
                numVersion.Value = current;
                numVersion.Enabled = current < ManagedIdentity.RecommendedVersion;

                // A solution only applies when the record is created.
                lblSolution.Visible = txtSolution.Visible = btnSolution.Visible = false;
            }
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>The saved identity, with its id.</summary>
        public ManagedIdentity Identity { get; private set; }

        #endregion Public Properties

        #region Private Methods

        private void btnOk_Click(object sender, EventArgs e)
        {
            var identity = ReadIdentity(out var error);
            if (identity == null)
            {
                MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                if (existing == null)
                {
                    ManagedIdentityHelper.Create(service, identity, solutionUniqueName);
                }
                else
                {
                    ManagedIdentityHelper.Update(service, identity);
                }

                Identity = identity;
            }
            catch (Exception ex)
            {
                ErrorMessageForm.ShowErrorMessageBox(this, "Unable to save the managed identity.", Text, ex);
                DialogResult = DialogResult.None;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void btnSolution_Click(object sender, EventArgs e)
        {
            using (var picker = new SolutionPicker(service))
            {
                if (picker.ShowDialog(this) == DialogResult.OK && picker.SelectedSolution != null)
                {
                    solutionUniqueName = picker.SelectedSolution.GetAttributeValue<string>("uniquename");
                    txtSolution.Text = picker.SelectedSolution.GetAttributeValue<string>("friendlyname");
                }
                else
                {
                    solutionUniqueName = null;
                    txtSolution.Text = string.Empty;
                }
            }
        }

        private ManagedIdentity ReadIdentity(out string error)
        {
            error = null;
            var name = txtName.Text.Trim();
            if (name.Length == 0)
            {
                error = "Enter a name.";
                return null;
            }

            if (!Guid.TryParse(txtApplicationId.Text.Trim(), out var applicationId) || applicationId == Guid.Empty)
            {
                error = "The application (client) ID must be a GUID.";
                return null;
            }

            if (!Guid.TryParse(txtTenantId.Text.Trim(), out var tenantId) || tenantId == Guid.Empty)
            {
                error = "The tenant ID must be a GUID.";
                return null;
            }

            return new ManagedIdentity
            {
                Id = existing?.Id ?? Guid.Empty,
                Name = name,
                ApplicationId = applicationId,
                TenantId = tenantId,
                SubjectScope = (ManagedIdentity.Scope)cboScope.SelectedItem,
                Version = existing != null && !existing.Version.HasValue && numVersion.Value == 0
                    ? (int?)null
                    : (int)numVersion.Value
            };
        }

        #endregion Private Methods
    }
}
