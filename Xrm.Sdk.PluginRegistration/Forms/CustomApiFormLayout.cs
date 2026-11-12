using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Xrm.Sdk.PluginRegistration.Forms
{
    /// <summary>Consistent, DPI-aware rows for the Custom API dialogs.</summary>
    internal static class CustomApiFormLayout
    {
        public static void SuggestNames(TextBox name, TextBox displayName, TextBox uniqueName)
        {
            bool suggesting = false;
            bool displayEdited = !string.IsNullOrEmpty(displayName.Text);
            bool uniqueEdited = !string.IsNullOrEmpty(uniqueName.Text);
            displayName.TextChanged += (sender, args) => { if (!suggesting) displayEdited = true; };
            uniqueName.TextChanged += (sender, args) => { if (!suggesting) uniqueEdited = true; };
            name.TextChanged += (sender, args) =>
            {
                suggesting = true;
                try
                {
                    if (!displayEdited) displayName.Text = name.Text;
                    if (!uniqueEdited)
                        uniqueName.Text = new string(name.Text.ToLowerInvariant()
                            .Where(character => (character >= 'a' && character <= 'z') ||
                                (character >= '0' && character <= '9') || character == '_')
                            // Unprefixed child names must start with a letter or underscore.
                            .SkipWhile(character => character >= '0' && character <= '9').ToArray());
                }
                finally { suggesting = false; }
            };
        }

        /// <summary>Suggests table names as the user types, matching the step registration form.</summary>
        public static void SuggestTables(TextBox table, IEnumerable<string> tableNames)
        {
            var suggestions = new AutoCompleteStringCollection();
            suggestions.AddRange((tableNames ?? Enumerable.Empty<string>()).ToArray());
            table.AutoCompleteCustomSource = suggestions;
            table.AutoCompleteSource = AutoCompleteSource.CustomSource;
            table.AutoCompleteMode = AutoCompleteMode.Suggest;
        }

        public static TableLayoutPanel CreateFields()
        {
            var fields = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                ColumnCount = 2,
                Padding = new Padding(12)
            };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return fields;
        }

        public static void AddRow(TableLayoutPanel fields, string caption, Control control)
        {
            int row = fields.RowCount++;
            fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var label = new Label
            {
                Text = caption,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 7, 14, 7)
            };
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(0, 4, 0, 4);
            fields.Controls.Add(label, 0, row);
            fields.Controls.Add(control, 1, row);
            control.AccessibleName = caption.TrimEnd(' ', '*');
        }

        public static TextBox Text(int maximum, bool multiline = false)
        {
            return new TextBox
            {
                MaxLength = maximum,
                Multiline = multiline,
                Height = multiline ? 65 : 23,
                ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
            };
        }

        public static ComboBox Choices()
        {
            return new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, IntegralHeight = false, DropDownHeight = 240 };
        }

        public static FlowLayoutPanel Buttons(params Control[] controls)
        {
            var panel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Bottom,
                AutoSize = true,
                Padding = new Padding(8),
                WrapContents = false
            };
            panel.Controls.AddRange(controls);
            return panel;
        }

        public static Button Button(string text)
        {
            return new Button { Text = text, AutoSize = true, MinimumSize = new Size(85, 28) };
        }
    }
}
