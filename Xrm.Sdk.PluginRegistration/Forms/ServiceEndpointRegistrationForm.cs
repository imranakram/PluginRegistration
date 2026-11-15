using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Xrm.Sdk.PluginRegistration.Helpers;
using Xrm.Sdk.PluginRegistration.Wrappers;
using XrmToolBox.Extensibility;
using Label = System.Windows.Forms.Label;

namespace Xrm.Sdk.PluginRegistration.Forms
{
    /// <summary>
    /// Registers or edits an Azure service endpoint: a Service Bus queue or topic, a relay, an
    /// Event Hub or an Event Grid topic. Webhooks have their own form.
    /// </summary>
    public sealed class ServiceEndpointRegistrationForm : Form
    {
        private sealed class Choice<T>
        {
            public Choice(T value, string label)
            {
                Value = value;
                Label = label;
            }

            public T Value { get; }
            public string Label { get; }
            public override string ToString() => Label;
        }

        // Dataverse no longer accepts the old Queue contract (2): "Use PersistentQueue (6) instead"
        private static readonly Choice<CrmServiceEndpointContract>[] Contracts =
        {
            new Choice<CrmServiceEndpointContract>(CrmServiceEndpointContract.PersistentQueue, "Queue"),
            new Choice<CrmServiceEndpointContract>(CrmServiceEndpointContract.Topic, "Topic"),
            new Choice<CrmServiceEndpointContract>(CrmServiceEndpointContract.EventHub, "Event Hub"),
            new Choice<CrmServiceEndpointContract>(CrmServiceEndpointContract.OneWay, "One-way relay"),
            new Choice<CrmServiceEndpointContract>(CrmServiceEndpointContract.TwoWay, "Two-way relay"),
            new Choice<CrmServiceEndpointContract>(CrmServiceEndpointContract.Rest, "REST relay"),
            new Choice<CrmServiceEndpointContract>(CrmServiceEndpointContract.EventGrid, "Event Grid"),
        };

        private static readonly Choice<CrmServiceEndpointAuthType>[] NamespaceAuthTypes =
        {
            new Choice<CrmServiceEndpointAuthType>(CrmServiceEndpointAuthType.SASKey, "SAS key"),
            new Choice<CrmServiceEndpointAuthType>(CrmServiceEndpointAuthType.SASToken, "SAS token"),
        };

        private static readonly Choice<CrmServiceEndpointAuthType>[] EventGridAuthTypes =
        {
            new Choice<CrmServiceEndpointAuthType>(CrmServiceEndpointAuthType.AccessKey, "Access key"),
        };

        private static readonly Choice<CrmServiceEndpointMessageFormat>[] MessageFormats =
        {
            new Choice<CrmServiceEndpointMessageFormat>(CrmServiceEndpointMessageFormat.BinaryXml, "Binary XML"),
            new Choice<CrmServiceEndpointMessageFormat>(CrmServiceEndpointMessageFormat.Json, "JSON"),
            new Choice<CrmServiceEndpointMessageFormat>(CrmServiceEndpointMessageFormat.TextXml, "Text XML"),
        };

        private static readonly Choice<CrmServiceEndpointUserClaim>[] UserClaims =
        {
            new Choice<CrmServiceEndpointUserClaim>(CrmServiceEndpointUserClaim.None, "None"),
            new Choice<CrmServiceEndpointUserClaim>(CrmServiceEndpointUserClaim.UserId, "User ID"),
            new Choice<CrmServiceEndpointUserClaim>(CrmServiceEndpointUserClaim.UserInfo, "User info"),
        };

        private readonly CrmOrganization m_org;
        private readonly MainControl m_orgControl;
        private readonly CrmServiceEndpoint m_endpoint;
        private readonly Dictionary<Control, Label> m_labels = new Dictionary<Control, Label>();

        private readonly TextBox txtName = CustomApiFormLayout.Text(256);
        private readonly TextBox txtDescription = CustomApiFormLayout.Text(2000, true);
        private readonly ComboBox cmbContract = CustomApiFormLayout.Choices();
        private readonly TextBox txtNamespace = CustomApiFormLayout.Text(255);
        private readonly TextBox txtPath = CustomApiFormLayout.Text(160);
        private readonly TextBox txtUrl = CustomApiFormLayout.Text(2000);
        private readonly ComboBox cmbAuth = CustomApiFormLayout.Choices();
        private readonly TextBox txtSasKeyName = CustomApiFormLayout.Text(256);
        private readonly TextBox txtSasKey = CustomApiFormLayout.Text(256);
        private readonly TextBox txtSasToken = CustomApiFormLayout.Text(256);
        private readonly TextBox txtAccessKey = CustomApiFormLayout.Text(2048);
        private readonly Label lblSecretHint = new Label { AutoSize = true, MaximumSize = new Size(440, 0) };
        private readonly Label lblNamespaceHint = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(440, 0),
            Text = "The Azure Service Bus or Event Hubs namespace, for example contoso.servicebus.windows.net",
        };
        private readonly ComboBox cmbMessageFormat = CustomApiFormLayout.Choices();
        private readonly ComboBox cmbUserClaim = CustomApiFormLayout.Choices();

        public ServiceEndpointRegistrationForm(CrmOrganization org, MainControl orgControl, CrmServiceEndpoint endpoint)
        {
            m_org = org ?? throw new ArgumentNullException(nameof(org));
            m_orgControl = orgControl;
            m_endpoint = endpoint;

            Text = endpoint == null ? "Register New Service Endpoint" : "Update Service Endpoint";
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            ShowIcon = false;
            MinimizeBox = false;
            MaximizeBox = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(640, 560);
            MinimumSize = new Size(560, 480);

            txtSasKey.UseSystemPasswordChar = true;
            txtSasToken.UseSystemPasswordChar = true;
            txtAccessKey.UseSystemPasswordChar = true;

            var fields = CustomApiFormLayout.CreateFields();
            AddRow(fields, "Name *", txtName);
            AddRow(fields, "Description", txtDescription);
            AddRow(fields, "Type *", cmbContract);
            AddRow(fields, "Namespace address *", txtNamespace);
            AddRow(fields, string.Empty, lblNamespaceHint);
            AddRow(fields, "Path", txtPath);
            AddRow(fields, "Topic endpoint URL *", txtUrl);
            AddRow(fields, "Authentication *", cmbAuth);
            AddRow(fields, "SAS key name *", txtSasKeyName);
            AddRow(fields, "SAS key *", txtSasKey);
            AddRow(fields, "SAS token *", txtSasToken);
            AddRow(fields, "Access key *", txtAccessKey);
            AddRow(fields, string.Empty, lblSecretHint);
            AddRow(fields, "Message format", cmbMessageFormat);
            AddRow(fields, "User information sent", cmbUserClaim);

            var content = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            content.Controls.Add(fields);
            Controls.Add(content);

            var save = CustomApiFormLayout.Button("Save");
            var cancel = CustomApiFormLayout.Button("Cancel");
            cancel.DialogResult = DialogResult.Cancel;
            save.Click += SaveClicked;
            Controls.Add(CustomApiFormLayout.Buttons(cancel, save));
            AcceptButton = save;
            CancelButton = cancel;

            cmbContract.Items.AddRange(Contracts);
            cmbMessageFormat.Items.AddRange(MessageFormats);
            cmbUserClaim.Items.AddRange(UserClaims);
            new ToolTip().SetToolTip(txtNamespace, "The host name of the Service Bus or Event Hubs namespace, as shown in the Azure portal. The sb:// prefix is added for you.");

            if (endpoint != null)
            {
                txtName.Text = endpoint.Name;
                txtDescription.Text = endpoint.Description;
                // An endpoint on the old Queue contract can only be saved again as a persistent queue
                Select(cmbContract, endpoint.Contract == CrmServiceEndpointContract.Queue
                    ? CrmServiceEndpointContract.PersistentQueue
                    : endpoint.Contract);
                txtNamespace.Text = endpoint.NamespaceAddress;
                txtPath.Text = endpoint.Path;
                txtUrl.Text = endpoint.Url;
                txtSasKeyName.Text = endpoint.SasKeyName;
                Select(cmbUserClaim, endpoint.UserClaim);
            }

            if (cmbContract.SelectedItem == null)
            {
                Select(cmbContract, CrmServiceEndpointContract.PersistentQueue);
            }

            UpdateContractFields();

            if (endpoint != null)
            {
                Select(cmbAuth, endpoint.AuthType);
                if (endpoint.MessageFormat.HasValue)
                {
                    Select(cmbMessageFormat, endpoint.MessageFormat.Value);
                }
            }

            if (cmbUserClaim.SelectedItem == null)
            {
                Select(cmbUserClaim, CrmServiceEndpointUserClaim.None);
            }

            UpdateAuthFields();

            cmbContract.SelectedIndexChanged += (sender, args) => UpdateContractFields();
            cmbAuth.SelectedIndexChanged += (sender, args) => UpdateAuthFields();
        }

        private CrmServiceEndpointContract SelectedContract => Selected<CrmServiceEndpointContract>(cmbContract);

        private CrmServiceEndpointAuthType SelectedAuthType => Selected<CrmServiceEndpointAuthType>(cmbAuth);

        /// <summary>
        /// Dataverse never returns the SAS key, SAS token or access key. When editing with the same
        /// authentication type, leaving them empty keeps the saved values.
        /// </summary>
        private bool CanKeepSavedSecret => m_endpoint != null
            && m_endpoint.AuthType == SelectedAuthType
            && m_endpoint.UsesNamespace == CrmServiceEndpoint.IsNamespaceContract(SelectedContract);

        private void AddRow(TableLayoutPanel fields, string caption, Control control)
        {
            CustomApiFormLayout.AddRow(fields, caption, control);
            m_labels[control] = (Label)fields.GetControlFromPosition(0, fields.RowCount - 1);
        }

        private void ShowRow(Control control, bool visible)
        {
            control.Visible = visible;
            m_labels[control].Visible = visible;
        }

        private void UpdateContractFields()
        {
            var contract = SelectedContract;
            var usesNamespace = CrmServiceEndpoint.IsNamespaceContract(contract);

            ShowRow(txtNamespace, usesNamespace);
            ShowRow(lblNamespaceHint, usesNamespace);
            ShowRow(txtPath, usesNamespace);
            ShowRow(txtUrl, !usesNamespace);
            ShowRow(cmbMessageFormat, usesNamespace);
            m_labels[txtPath].Text = PathCaption(contract);

            var authTypes = usesNamespace ? NamespaceAuthTypes : EventGridAuthTypes;
            var current = cmbAuth.SelectedItem as Choice<CrmServiceEndpointAuthType>;
            if (!cmbAuth.Items.Cast<object>().SequenceEqual(authTypes))
            {
                cmbAuth.Items.Clear();
                cmbAuth.Items.AddRange(authTypes);
                cmbAuth.SelectedItem = authTypes.FirstOrDefault(choice => choice.Value == current?.Value) ?? authTypes[0];
            }

            if (cmbMessageFormat.SelectedItem == null || m_endpoint == null)
            {
                // The relays hand the listener the binary RemoteExecutionContext; the rest is usually read as JSON
                var relay = contract == CrmServiceEndpointContract.OneWay || contract == CrmServiceEndpointContract.TwoWay
                    || contract == CrmServiceEndpointContract.Rest;
                Select(cmbMessageFormat, relay ? CrmServiceEndpointMessageFormat.BinaryXml : CrmServiceEndpointMessageFormat.Json);
            }

            UpdateAuthFields();
        }

        private void UpdateAuthFields()
        {
            var usesNamespace = CrmServiceEndpoint.IsNamespaceContract(SelectedContract);
            var authType = cmbAuth.SelectedItem == null ? (CrmServiceEndpointAuthType?)null : SelectedAuthType;

            ShowRow(txtSasKeyName, usesNamespace && authType == CrmServiceEndpointAuthType.SASKey);
            ShowRow(txtSasKey, usesNamespace && authType == CrmServiceEndpointAuthType.SASKey);
            ShowRow(txtSasToken, usesNamespace && authType == CrmServiceEndpointAuthType.SASToken);
            ShowRow(txtAccessKey, !usesNamespace);

            var keep = cmbAuth.SelectedItem != null && CanKeepSavedSecret;
            ShowRow(lblSecretHint, m_endpoint != null);
            lblSecretHint.Text = keep
                ? "Saved keys aren't shown, because Dataverse doesn't return them. Leave empty to keep them."
                : "Enter the key for the new authentication type.";
        }

        private static string PathCaption(CrmServiceEndpointContract contract)
        {
            switch (contract)
            {
                case CrmServiceEndpointContract.Queue:
                case CrmServiceEndpointContract.PersistentQueue:
                    return "Queue name *";
                case CrmServiceEndpointContract.Topic:
                    return "Topic name *";
                case CrmServiceEndpointContract.EventHub:
                    return "Event Hub name *";
                default:
                    return "Path";
            }
        }

        private static bool PathRequired(CrmServiceEndpointContract contract) => PathCaption(contract).EndsWith("*");

        private void SaveClicked(object sender, EventArgs e)
        {
            var endpoint = BuildServiceEndpoint(out var errors);
            if (endpoint == null)
            {
                MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Invalid configuration",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Upsert(endpoint);
        }

        /// <summary>
        /// Validates the dialog and builds the endpoint to save, or returns null with the errors.
        /// The secrets are null when the saved ones are kept.
        /// </summary>
        private CrmServiceEndpoint BuildServiceEndpoint(out List<string> errors)
        {
            errors = new List<string>();
            var contract = SelectedContract;
            var usesNamespace = CrmServiceEndpoint.IsNamespaceContract(contract);
            var authType = SelectedAuthType;

            var endpoint = new CrmServiceEndpoint(m_org)
            {
                ServiceEndpointId = m_endpoint?.ServiceEndpointId ?? Guid.Empty,
                Name = txtName.Text.Trim(),
                Description = txtDescription.Text.Trim(),
                Contract = contract,
                AuthType = authType,
                ConnectionMode = CrmServiceEndpointConnectionMode.Normal,
                UserClaim = Selected<CrmServiceEndpointUserClaim>(cmbUserClaim),
            };

            if (endpoint.Name.Length == 0)
            {
                errors.Add("Name is required.");
            }

            if (usesNamespace)
            {
                endpoint.NamespaceAddress = NormalizeNamespaceAddress(txtNamespace.Text);
                endpoint.Path = txtPath.Text.Trim();
                endpoint.MessageFormat = Selected<CrmServiceEndpointMessageFormat>(cmbMessageFormat);

                if (endpoint.NamespaceAddress.Length == 0)
                {
                    errors.Add("Namespace address is required.");
                }
                else if (!Uri.TryCreate(endpoint.NamespaceAddress, UriKind.Absolute, out var address) || address.Scheme != "sb"
                    || !address.Host.Contains("."))
                {
                    errors.Add("Namespace address must be the namespace's host name, such as contoso.servicebus.windows.net.");
                }

                if (endpoint.Path.Length == 0 && PathRequired(contract))
                {
                    errors.Add($"{PathCaption(contract).TrimEnd(' ', '*')} is required.");
                }

                if (authType == CrmServiceEndpointAuthType.SASKey)
                {
                    endpoint.SasKeyName = txtSasKeyName.Text.Trim();
                    if (endpoint.SasKeyName.Length == 0)
                    {
                        errors.Add("SAS key name is required.");
                    }
                    endpoint.SasKey = Secret(txtSasKey, "SAS key", errors);
                }
                else
                {
                    endpoint.SasToken = Secret(txtSasToken, "SAS token", errors);
                }
            }
            else
            {
                endpoint.Url = txtUrl.Text.Trim();
                if (endpoint.Url.Length == 0)
                {
                    errors.Add("Topic endpoint URL is required.");
                }
                else if (!Uri.TryCreate(endpoint.Url, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps)
                {
                    errors.Add("Topic endpoint URL must be a full https:// address.");
                }

                endpoint.AuthValue = Secret(txtAccessKey, "Access key", errors);
            }

            return errors.Count == 0 ? endpoint : null;
        }

        /// <summary>
        /// Dataverse wants the namespace as sb://host/. Accepts that, the plain host name, or the
        /// https:// address the Azure portal shows, and returns the sb:// form.
        /// </summary>
        public static string NormalizeNamespaceAddress(string value)
        {
            value = (value ?? string.Empty).Trim();
            if (value.Length == 0)
            {
                return value;
            }

            if (!value.Contains("://"))
            {
                value = "sb://" + value;
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                return value;
            }

            if (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            {
                return "sb://" + uri.Host + "/";
            }

            return uri.Scheme == "sb" && uri.AbsolutePath == "/" ? "sb://" + uri.Host + "/" : value;
        }

        /// <summary>
        /// Returns the entered secret, or null to keep the saved one (an error when there is none to keep).
        /// </summary>
        private string Secret(TextBox box, string caption, List<string> errors)
        {
            var value = box.Text.Trim();
            if (value.Length > 0)
            {
                return value;
            }

            if (!CanKeepSavedSecret)
            {
                errors.Add($"{caption} is required.");
            }

            return null;
        }

        private void Upsert(CrmServiceEndpoint endpoint)
        {
            DialogResult = DialogResult.OK;
            Close();

            var isNew = endpoint.ServiceEndpointId == Guid.Empty;
            m_orgControl.WorkAsync(new WorkAsyncInfo($"{(isNew ? "Registering" : "Updating")} service endpoint...",
                (args) =>
                {
                    if (isNew)
                    {
                        RegistrationHelper.RegisterServiceEndpoint(m_org, endpoint);
                    }
                    else
                    {
                        RegistrationHelper.UpdateServiceEndpoint(m_org, endpoint);
                    }
                })
            {
                PostWorkCallBack = (completed) =>
                {
                    if (completed.Error == null)
                    {
                        m_orgControl.RefreshFullTreeView();
                    }
                    else
                    {
                        MessageBox.Show(completed.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            });
        }

        private static T Selected<T>(ComboBox box) => ((Choice<T>)box.SelectedItem).Value;

        /// <summary>
        /// Selects the choice for the value. Values the dialog doesn't offer (for example an old
        /// endpoint's ACS authentication) leave the current choice in place.
        /// </summary>
        private static void Select<T>(ComboBox box, T value)
        {
            var match = box.Items.Cast<Choice<T>>().FirstOrDefault(choice => EqualityComparer<T>.Default.Equals(choice.Value, value));
            if (match != null)
            {
                box.SelectedItem = match;
            }
        }
    }
}
