using System;
using System.Drawing;
using System.Windows.Forms;

namespace Xrm.Sdk.PluginRegistration.Controls
{
    /// <summary>
    /// A TreeView whose checkboxes stay in step with AfterCheck.
    /// </summary>
    /// <remarks>
    /// When a checkbox is double-clicked, the Windows tree control toggles it a second time without
    /// raising AfterCheck. Anything kept in sync from AfterCheck, such as ticking an assembly's
    /// plug-ins, then disagrees with the box (#145). Double-clicks on a checkbox are ignored, so a
    /// fast second click does nothing instead of changing the box behind our back. Double-clicks
    /// elsewhere are passed on as usual.
    /// </remarks>
    public class CheckBoxSafeTreeView : TreeView
    {
        private const int WM_LBUTTONDBLCLK = 0x0203;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_LBUTTONDBLCLK && CheckBoxes && IsOnCheckBox(m.LParam))
            {
                m.Result = IntPtr.Zero;
                return;
            }

            base.WndProc(ref m);
        }

        private bool IsOnCheckBox(IntPtr lParam)
        {
            var value = lParam.ToInt64();
            var point = new Point((short)(value & 0xFFFF), (short)((value >> 16) & 0xFFFF));
            return HitTest(point).Location == TreeViewHitTestLocations.StateImage;
        }
    }
}
