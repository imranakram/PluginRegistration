namespace Xrm.Sdk.PluginRegistration.Forms
{
    using Helpers;
    using Microsoft.Xrm.Sdk;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows.Forms;

    /// <summary>
    /// Picks the managed identity of a plug-in assembly or package, and creates, edits or deletes identities.
    /// The link itself is saved by the caller from <see cref="SelectedIdentity"/>.
    /// </summary>
    public partial class ManagedIdentityForm : Form
    {
        #region Private Fields

        private const string DocsUrl = "https://learn.microsoft.com/power-platform/admin/set-up-managed-identity";
        private const string NoneItem = "(None)";

        private readonly Guid? currentIdentityId;
        private readonly string defaultIdentityName;
        private readonly IOrganizationService service;
        private List<ManagedIdentity> identities = new List<ManagedIdentity>();

        #endregion Private Fields

        #region Public Constructors

        /// <param name="targetDescription">Shown at the top, e.g. "Package: contoso_MyPackage".</param>
        /// <param name="defaultIdentityName">Suggested name for a new identity.</param>
        /// <param name="currentIdentityId">The identity linked today, if any.</param>
        public ManagedIdentityForm(IOrganizationService service, string targetDescription, string defaultIdentityName, Guid? currentIdentityId)
        {
            InitializeComponent();

            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.defaultIdentityName = defaultIdentityName;
            this.currentIdentityId = currentIdentityId;
            lblTarget.Text = targetDescription;
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>The identities as they are when the dialog closes, including ones created, edited or deleted here.</summary>
        public IReadOnlyList<ManagedIdentity> Identities => identities;

        /// <summary>Identities deleted in this dialog.</summary>
        public ICollection<Guid> DeletedIdentityIds { get; } = new HashSet<Guid>();

        /// <summary>True when an identity was created, edited or deleted in this dialog.</summary>
        public bool IdentitiesChanged { get; private set; }

        /// <summary>The identity to link, or null to unlink.</summary>
        public ManagedIdentity SelectedIdentity => cboIdentities.SelectedItem as ManagedIdentity;

        #endregion Public Properties

        #region Private Methods

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var identity = SelectedIdentity;
            if (identity == null)
            {
                return;
            }

            var answer = MessageBox.Show(this,
                $"Delete the managed identity '{identity}'?{Environment.NewLine}{Environment.NewLine}Plug-ins that use it will no longer be able to get tokens.",
                "Delete Managed Identity", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes)
            {
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                ManagedIdentityHelper.Delete(service, identity.Id);
                identities.Remove(identity);
                DeletedIdentityIds.Add(identity.Id);
                IdentitiesChanged = true;
                BindIdentities(null);
            }
            catch (Exception ex)
            {
                ErrorMessageForm.ShowErrorMessageBox(this, "Unable to delete the managed identity.", "Managed Identity", ex);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            var identity = SelectedIdentity;
            if (identity == null)
            {
                return;
            }

            using (var dialog = new ManagedIdentityEditForm(service, identity, null))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    identities[identities.IndexOf(identity)] = dialog.Identity;
                    IdentitiesChanged = true;
                    BindIdentities(dialog.Identity.Id);
                }
            }
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            using (var dialog = new ManagedIdentityEditForm(service, null, defaultIdentityName))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    identities.Add(dialog.Identity);
                    identities = identities.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList();
                    IdentitiesChanged = true;
                    BindIdentities(dialog.Identity.Id);
                }
            }
        }

        private void BindIdentities(Guid? selectId)
        {
            cboIdentities.BeginUpdate();
            cboIdentities.Items.Clear();
            cboIdentities.Items.Add(NoneItem);
            foreach (var identity in identities)
            {
                cboIdentities.Items.Add(identity);
            }

            var selected = selectId.HasValue ? identities.FirstOrDefault(i => i.Id == selectId.Value) : null;
            cboIdentities.SelectedItem = (object)selected ?? NoneItem;
            cboIdentities.EndUpdate();
            UpdateDetails();
        }

        private void cboIdentities_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDetails();
        }

        private void lnkDocs_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(DocsUrl);
        }

        private void ManagedIdentityForm_Load(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                identities = ManagedIdentityHelper.RetrieveAll(service);

                // The list only has identities with credential source "managed". Keep a linked identity
                // of another kind in the list, so saving without changes doesn't unlink it.
                if (currentIdentityId.HasValue && identities.All(i => i.Id != currentIdentityId.Value))
                {
                    var current = ManagedIdentityHelper.Retrieve(service, currentIdentityId.Value);
                    if (current != null)
                    {
                        identities.Insert(0, current);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageForm.ShowErrorMessageBox(this, "Unable to load the managed identities.", "Managed Identity", ex);
                btnOk.Enabled = false;
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            BindIdentities(currentIdentityId);
        }

        private void UpdateDetails()
        {
            var identity = SelectedIdentity;
            btnEdit.Enabled = identity != null;
            btnDelete.Enabled = identity != null;

            if (identity == null)
            {
                lblDetails.Text = "No managed identity. Save to unlink the current one, if any.";
                return;
            }

            lblDetails.Text = string.Join(Environment.NewLine,
                $"Application (client) ID: {identity.ApplicationId}",
                $"Tenant ID: {identity.TenantId}",
                $"Subject scope: {identity.SubjectScope}",
                $"Version: {(identity.Version.HasValue ? identity.Version.ToString() : "(not set)")}");
        }

        #endregion Private Methods
    }
}
