using NUnit.Framework;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Xrm.Sdk.PluginRegistration.Controls;

namespace Xrm.Sdk.PluginRegistration.Tests.TreeViews
{
    /// <summary>
    /// #145: a double-click on a checkbox reaches the native tree control, which flips the box without
    /// raising AfterCheck, so an assembly and its plug-ins end up disagreeing. That needs real mouse input
    /// to reproduce, so these tests check the workaround's decision: double-clicks on a checkbox are
    /// dropped, all others still arrive.
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class CheckBoxSafeTreeViewTests
    {
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int MK_LBUTTON = 0x0001;

        [Test]
        public void ADoubleClickOnACheckBoxIsDropped()
        {
            Assert.That(DoubleClickReachesTheTree(onCheckBox: true), Is.False);
        }

        [Test]
        public void ADoubleClickOnTheLabelStillArrives()
        {
            // Expanding and collapsing nodes, and the tree's DoubleClick event, keep working
            Assert.That(DoubleClickReachesTheTree(onCheckBox: false), Is.True);
        }

        [Test]
        public void WithoutCheckBoxesNothingIsDropped()
        {
            Assert.That(DoubleClickReachesTheTree(onCheckBox: true, checkBoxes: false), Is.True);
        }

        private static bool DoubleClickReachesTheTree(bool onCheckBox, bool checkBoxes = true)
        {
            using (var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), Size = new Size(300, 200) })
            {
                var tree = new CheckBoxSafeTreeView { CheckBoxes = true, Dock = DockStyle.Fill };
                form.Controls.Add(tree);
                var node = tree.Nodes.Add("Assembly");
                node.Nodes.Add("Plugin");
                form.Show();
                Application.DoEvents();

                var y = node.Bounds.Top + node.Bounds.Height / 2;
                var point = onCheckBox ? CheckBoxPoint(tree, node, y) : new Point(node.Bounds.Left + 5, y);
                tree.CheckBoxes = checkBoxes;

                var reached = false;
                tree.MouseDown += (s, e) => reached |= e.Clicks == 2;
                SendMessage(tree.Handle, WM_LBUTTONDBLCLK, (IntPtr)MK_LBUTTON, (IntPtr)((point.Y << 16) | (point.X & 0xFFFF)));
                return reached;
            }
        }

        private static Point CheckBoxPoint(TreeView tree, TreeNode node, int y)
        {
            for (var x = 0; x < node.Bounds.Left; x++)
            {
                if (tree.HitTest(x, y).Location == TreeViewHitTestLocations.StateImage)
                {
                    return new Point(x + 3, y);
                }
            }
            Assert.Fail("No checkbox found next to the node");
            return Point.Empty;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}
