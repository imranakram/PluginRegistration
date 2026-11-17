using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Xrm.Sdk.PluginRegistration.Controls
{
    /// <summary>A value to pick in a <see cref="FilteringComboBox"/>, with an optional description such as a display name.</summary>
    public sealed class SearchChoice
    {
        public SearchChoice(string value, string detail = null, object key = null)
        {
            Value = value ?? string.Empty;
            Detail = detail;
            Key = key;
        }

        /// <summary>The text shown in the edit box and searched.</summary>
        public string Value { get; }

        /// <summary>Shown in grey after the value in the list, and searched too.</summary>
        public string Detail { get; }

        /// <summary>
        /// What the choice stands for, such as a record id. Choices with a key are told apart by it,
        /// so two can share a value; choices without one are told apart by their value.
        /// </summary>
        public object Key { get; }

        // The edit box shows the item text, so it holds the plain value.
        public override string ToString() => Value;

        public static IEnumerable<SearchChoice> FromValues(IEnumerable<string> values)
        {
            return (values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => new SearchChoice(value));
        }
    }

    /// <summary>
    /// An editable combo box that narrows its list to the choices containing the typed text, in the
    /// value or the description. By default any text can still be entered, so values missing from
    /// the list work. With <see cref="RestrictToChoices"/> the text only searches, and the value is
    /// always one of the choices: see <see cref="SelectedChoice"/>.
    /// </summary>
    public class FilteringComboBox : ComboBox
    {
        /// <summary>Most matches listed while typing, so thousands of choices stay responsive.</summary>
        public const int MaximumMatches = 200;

        private List<SearchChoice> choices = new List<SearchChoice>();
        private SearchChoice emptyChoice;
        private SearchChoice selectedChoice;
        private string typedFilter;

        public FilteringComboBox()
        {
            DropDownStyle = ComboBoxStyle.DropDown;
            DrawMode = DrawMode.OwnerDrawFixed;
            IntegralHeight = false;
            DropDownHeight = 240;
            AutoCompleteMode = AutoCompleteMode.None;
        }

        /// <summary>Raised when <see cref="SelectedChoice"/> changes, by the user or by <see cref="SelectChoice"/>.</summary>
        public event EventHandler SelectedChoiceChanged;

        /// <summary>When set, typed text only searches: leaving the box keeps a listed choice.</summary>
        public bool RestrictToChoices { get; set; }

        public IReadOnlyList<SearchChoice> Choices => choices;

        /// <summary>The picked choice when <see cref="RestrictToChoices"/> is set.</summary>
        public SearchChoice SelectedChoice => selectedChoice;

        /// <summary>
        /// Sets the choices. With <see cref="RestrictToChoices"/>, <paramref name="empty"/> is listed
        /// first, always, and is picked when the text is cleared, for example "(None)".
        /// </summary>
        public void SetChoices(IEnumerable<SearchChoice> values, SearchChoice empty = null)
        {
            var distinct = (values ?? Enumerable.Empty<SearchChoice>())
                .Where(choice => choice != null && choice.Value.Length > 0 && choice != empty)
                .GroupBy(choice => choice.Key ?? choice.Value.ToLowerInvariant())
                .Select(group => group.First())
                .OrderBy(choice => choice.Value, StringComparer.OrdinalIgnoreCase);
            emptyChoice = empty;
            choices = (empty == null ? distinct : new[] { empty }.Concat(distinct)).ToList();
            typedFilter = null;
            if (RestrictToChoices)
            {
                // Keep the pick when the same record is still listed, for example after a reload.
                var kept = selectedChoice == null ? null
                    : choices.FirstOrDefault(choice => SameChoice(choice, selectedChoice));
                selectedChoice = kept ?? emptyChoice;
                ShowItems(choices);
                ShowSelected();
            }
            else
            {
                ShowItems(choices);
            }
        }

        /// <summary>Picks a listed choice, matched by key or value; anything else picks the empty choice.</summary>
        public void SelectChoice(SearchChoice choice)
        {
            var listed = choice == null ? null : choices.FirstOrDefault(c => SameChoice(c, choice));
            Commit(listed ?? emptyChoice);
        }

        public static bool Matches(SearchChoice choice, string text)
        {
            if (choice == null) return false;
            string search = text?.Trim();
            if (string.IsNullOrEmpty(search)) return true;
            return choice.Value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (choice.Detail?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
        }

        /// <summary>
        /// The choices that match <paramref name="text"/>, those whose value starts with it first.
        /// Everything is returned for empty text; otherwise at most <see cref="MaximumMatches"/>.
        /// </summary>
        public static List<SearchChoice> Filter(IEnumerable<SearchChoice> choices, string text)
        {
            var all = choices ?? Enumerable.Empty<SearchChoice>();
            string search = text?.Trim();
            if (string.IsNullOrEmpty(search)) return all.ToList();
            return all.Where(choice => Matches(choice, search))
                .OrderBy(choice => choice.Value.StartsWith(search, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .Take(MaximumMatches)
                .ToList();
        }

        /// <summary>Lists the choices matching <paramref name="text"/> and keeps it as the typed value.</summary>
        public void FilterTo(string text)
        {
            int caret = Math.Min(SelectionStart, (text ?? string.Empty).Length);
            typedFilter = text;
            var matches = Filter(choices.Where(choice => choice != emptyChoice), text);
            if (RestrictToChoices && emptyChoice != null) matches.Insert(0, emptyChoice);
            ShowItems(matches);
            RestoreText(text, caret);
            if (!Focused) return;
            if (matches.Count > 0 && !DroppedDown)
            {
                DroppedDown = true;
                // Opening the list hides the mouse pointer and selects the closest item's text.
                Cursor.Current = Cursors.Default;
                RestoreText(text, caret);
            }
            else if (matches.Count == 0 && DroppedDown)
            {
                DroppedDown = false;
                RestoreText(text, caret);
            }
        }

        /// <summary>
        /// Ends a search in a restricted box: cleared text picks the empty choice, text naming exactly
        /// one choice picks it, and anything else puts back the previous pick.
        /// </summary>
        public void ResolveTypedText()
        {
            if (!RestrictToChoices || typedFilter == null) return;
            string text = Text.Trim();
            SearchChoice resolved;
            if (text.Length == 0)
            {
                resolved = emptyChoice ?? selectedChoice;
            }
            else
            {
                var exact = choices.Where(choice => string.Equals(choice.Value, text, StringComparison.OrdinalIgnoreCase)).ToList();
                var matches = Filter(choices.Where(choice => choice != emptyChoice), text);
                resolved = exact.Count == 1 ? exact[0] : matches.Count == 1 ? matches[0] : selectedChoice;
            }
            Commit(resolved);
        }

        protected override void OnTextUpdate(EventArgs e)
        {
            base.OnTextUpdate(e);
            FilterTo(Text);
        }

        protected override void OnDropDown(EventArgs e)
        {
            // Opened with the button: list what was typed, or everything after a pick.
            if (typedFilter == null && Items.Count != choices.Count)
            {
                string text = Text;
                ShowItems(choices);
                RestoreText(text, text.Length);
            }
            base.OnDropDown(e);
        }

        protected override void OnSelectionChangeCommitted(EventArgs e)
        {
            typedFilter = null;
            base.OnSelectionChangeCommitted(e);
            if (RestrictToChoices && SelectedItem is SearchChoice picked) Commit(picked);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (RestrictToChoices && !DroppedDown && (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape))
            {
                if (e.KeyCode == Keys.Escape && typedFilter != null)
                {
                    // Escape gives up the search and keeps the previous pick.
                    typedFilter = null;
                    Commit(selectedChoice);
                }
                else
                {
                    ResolveTypedText();
                }
            }
            base.OnKeyDown(e);
        }

        protected override void OnLeave(EventArgs e)
        {
            ResolveTypedText();
            base.OnLeave(e);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index >= 0 && e.Index < Items.Count)
            {
                var choice = Items[e.Index] as SearchChoice;
                string value = GetItemText(Items[e.Index]);
                bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                const TextFormatFlags flags = ItemTextFlags | TextFormatFlags.EndEllipsis;
                TextRenderer.DrawText(e.Graphics, value, e.Font, e.Bounds, selected ? SystemColors.HighlightText : ForeColor, flags);
                string detail = choice?.Detail;
                if (!string.IsNullOrEmpty(detail) && !string.Equals(detail, value, StringComparison.OrdinalIgnoreCase))
                {
                    var bounds = DetailBounds(e.Graphics, value, e.Font, e.Bounds);
                    TextRenderer.DrawText(e.Graphics, detail, e.Font, bounds, selected ? SystemColors.HighlightText : SystemColors.GrayText, flags);
                }
            }
            e.DrawFocusRectangle();
            base.OnDrawItem(e);
        }

        private const TextFormatFlags ItemTextFlags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;

        /// <summary>Where a choice's grey description goes in a list row: after its value, with a gap.</summary>
        public static Rectangle DetailBounds(IDeviceContext graphics, string value, Font font, Rectangle row)
        {
            // Measure without EndEllipsis: with it and no size limit, the text measures as if cut to an ellipsis.
            int width = TextRenderer.MeasureText(graphics, value ?? string.Empty, font,
                new Size(int.MaxValue, int.MaxValue), ItemTextFlags).Width;
            int left = row.X + width + font.Height / 2;
            return new Rectangle(left, row.Y, Math.Max(0, row.Right - left), row.Height);
        }

        private static bool SameChoice(SearchChoice a, SearchChoice b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a.Key != null || b.Key != null) return Equals(a.Key, b.Key);
            return string.Equals(a.Value, b.Value, StringComparison.OrdinalIgnoreCase);
        }

        private void Commit(SearchChoice choice)
        {
            typedFilter = null;
            bool changed = !ReferenceEquals(choice, selectedChoice);
            selectedChoice = choice;
            if (Items.Count != choices.Count) ShowItems(choices);
            ShowSelected();
            if (changed) SelectedChoiceChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ShowSelected()
        {
            // Select the object itself: two choices may share their text.
            int index = selectedChoice == null ? -1 : Items.IndexOf(selectedChoice);
            if (SelectedIndex != index) SelectedIndex = index;
            RestoreText(selectedChoice?.Value ?? string.Empty, (selectedChoice?.Value ?? string.Empty).Length);
        }

        private void ShowItems(IList<SearchChoice> items)
        {
            string text = Text;
            BeginUpdate();
            try
            {
                Items.Clear();
                Items.AddRange(items.Cast<object>().ToArray());
            }
            finally
            {
                EndUpdate();
            }
            RestoreText(text, text.Length);
        }

        /// <summary>
        /// The text, read safely. After its list is emptied and closed, the native box can still report a
        /// selected row that no longer exists, and the base getter then throws ArgumentOutOfRangeException.
        /// </summary>
        public override string Text
        {
            get
            {
                if (!IsHandleCreated || SelectedIndex < Items.Count) return base.Text;
                // What the edit box shows, without touching the stale selection.
                var text = new System.Text.StringBuilder(GetWindowTextLength(Handle) + 1);
                GetWindowText(Handle, text, text.Capacity);
                return text.ToString();
            }
            set => base.Text = value;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int maxCount);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        private void RestoreText(string text, int caret)
        {
            text = text ?? string.Empty;
            if (Text != text) Text = text;
            if (!IsHandleCreated) return;
            SelectionStart = Math.Min(caret, text.Length);
            SelectionLength = 0;
        }
    }
}
