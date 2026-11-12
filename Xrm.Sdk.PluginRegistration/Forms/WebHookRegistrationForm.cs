using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;
using Newtonsoft.Json;
using Xrm.Sdk.PluginRegistration.Helpers;
using Xrm.Sdk.PluginRegistration.Models;
using Xrm.Sdk.PluginRegistration.Wrappers;
using XrmToolBox.Extensibility;

namespace Xrm.Sdk.PluginRegistration.Forms
{
    public partial class WebHookRegistrationForm : Form
    {
        private CrmOrganization m_org;
        private CrmServiceEndpoint m_webhook;
        private MainControl m_orgControl;

        public WebHookRegistrationForm(CrmOrganization org, MainControl orgControl, CrmServiceEndpoint webhook)
        {
            m_org = org ?? throw new ArgumentNullException("org");
            m_webhook = webhook;
            m_orgControl = orgControl;

            InitializeComponent();

            if (m_webhook != null)
            {
                txtName.Text = m_webhook.Name;
                txtEndpointUrl.Text = m_webhook.Url;
                cmbAuth.SelectedItem = m_webhook.AuthType.ToString();
            }
            else
            {
                cmbAuth.SelectedItem = CrmServiceEndpointAuthType.HttpHeader.ToString();
            }

            UpdateAuthHint();
        }

        private CrmServiceEndpointAuthType SelectedAuthType =>
            (CrmServiceEndpointAuthType)Enum.Parse(typeof(CrmServiceEndpointAuthType), cmbAuth.SelectedItem.ToString(), true);

        /// <summary>
        /// Dataverse never returns the saved values, so the dialog can't show them. When editing
        /// with the same authentication type, empty values keep the saved ones.
        /// </summary>
        private bool CanKeepSavedValues => m_webhook != null && SelectedAuthType == m_webhook.AuthType;

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void cmbAuth_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedAuthMethod = (string)cmbAuth.SelectedItem;

            if (selectedAuthMethod == "WebhookKey")
            {
                lblValue.Visible = true;
                txtValue.Visible = true;
                dgvKeyValue.Visible = false;

                dgvKeyValue.Rows.Clear();
            }
            else
            {
                lblValue.Visible = false;
                txtValue.Visible = false;
                dgvKeyValue.Visible = true;

                txtValue.Text = string.Empty;
            }

            UpdateAuthHint();
        }

        private void UpdateAuthHint()
        {
            lblAuthHint.Visible = m_webhook != null;
            lblAuthHint.Text = CanKeepSavedValues
                ? "Saved values aren't shown, because Dataverse doesn't return them. Leave empty to keep them, or enter all values to replace them."
                : "Enter the values for the new authentication type.";
            toolTip1.SetToolTip(lblAuthHint, lblAuthHint.Text);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var webhook = BuildWebhook(out var errorMsgs);
            if (webhook != null)
            {
                UpsertWebhook(webhook);
            }
            else
            {
                MessageBox.Show(string.Join(Environment.NewLine, errorMsgs),
                    "Invalid configuration",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Validates the dialog and builds the webhook to save, or returns null with the errors.
        /// AuthValue is null when the saved values are kept.
        /// </summary>
        private CrmServiceEndpoint BuildWebhook(out List<string> errorMsgs)
        {
            errorMsgs = new List<string>();

            if (txtName.Text == string.Empty)
            {
                errorMsgs.Add("Endpoint Name is required");
            }

            if (txtEndpointUrl.Text == string.Empty)
            {
                errorMsgs.Add("Endpoint URL is required");
            }
            else
            {
                var isValid = Uri.TryCreate(txtEndpointUrl.Text, UriKind.Absolute, out _);
                if (!isValid)
                {
                    errorMsgs.Add("Endpoint URL should be valid.");
                }
            }

            string authValue;
            if (SelectedAuthType == CrmServiceEndpointAuthType.WebhookKey)
            {
                authValue = txtValue.Text.Trim();
            }
            else
            {
                var rows = KeyValueRows();
                errorMsgs.AddRange(WebhookAuthValue.Validate(rows));
                authValue = WebhookAuthValue.BuildSettings(rows);
            }

            if (authValue.Length == 0)
            {
                authValue = null;
                if (errorMsgs.Count == 0 && !CanKeepSavedValues)
                {
                    errorMsgs.Add("Auth Value is required.");
                }
            }

            if (errorMsgs.Count != 0)
            {
                return null;
            }

            return new CrmServiceEndpoint(m_org)
            {
                Url = txtEndpointUrl.Text,
                Name = txtName.Text,
                AuthType = SelectedAuthType,
                ServiceEndpointId = m_webhook?.ServiceEndpointId ?? Guid.Empty,
                AuthValue = authValue,
                ConnectionMode = CrmServiceEndpointConnectionMode.Normal,
                Contract = CrmServiceEndpointContract.Webhook,
                UserClaim = CrmServiceEndpointUserClaim.None
            };
        }

        private List<KeyValuePair<string, string>> KeyValueRows()
        {
            return dgvKeyValue.Rows.Cast<DataGridViewRow>()
                .Where(row => !row.IsNewRow)
                .Select(row => new KeyValuePair<string, string>(row.Cells[0].Value?.ToString(), row.Cells[1].Value?.ToString()))
                .ToList();
        }

        private void UpsertWebhook(CrmServiceEndpoint webhook)
        {
            Close();

            var isNew = webhook.ServiceEndpointId == Guid.Empty;

            m_orgControl.WorkAsync(new WorkAsyncInfo($"{(isNew ? "Creating" : "Updating")} webhook...",
                (eventargs) =>
                {
                    if (isNew)
                    {
                        RegistrationHelper.RegisterWebHook(m_org, webhook);
                    }
                    else
                    {
                        RegistrationHelper.UpdatewebHook(m_org, webhook);
                    }
                })
            {
                PostWorkCallBack = (completedargs) =>
                {
                    if (completedargs.Error == null)
                    {
                        m_orgControl.RefreshFullTreeView();
                    }
                    else
                    {
                        MessageBox.Show(completedargs.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            });
        }

        private void btnGenerateWebhookSite_Click(object sender, EventArgs e)
        {
            var baseUrl = "https://webhook.site";

            var client = new HttpClient();
            var response = client.PostAsync($@"{baseUrl}/token", null).Result;
            var responseJson = response.Content.ReadAsStringAsync().Result;

            var webhookSiteResponse = JsonConvert.DeserializeObject<WebhookSiteResponse>(responseJson);
            var webhookId = webhookSiteResponse?.uuid;

            var webhookMonitorUrl = $"{baseUrl}/#!/{webhookId}";
            Clipboard.SetText(webhookMonitorUrl);

            txtEndpointUrl.Text = $"{baseUrl}/{webhookId}";
            cmbAuth.SelectedItem = CrmServiceEndpointAuthType.WebhookKey.ToString();
            txtValue.Text = "123";
            txtName.Text = string.IsNullOrEmpty(txtName.Text) 
                ? $"Webhook.site: {webhookMonitorUrl}"
                : txtName.Text;

            var dialogResponse = MessageBox.Show(
                $"Would you like to open the Webhook.site monitor in your browser?\n\nPlease note that the URL for the Webhook monitor has been copied to your clipboard.",
                "Question",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1
            );

            if (dialogResponse == DialogResult.No) { return; }

            Process.Start(webhookMonitorUrl);
        }
    }
}
