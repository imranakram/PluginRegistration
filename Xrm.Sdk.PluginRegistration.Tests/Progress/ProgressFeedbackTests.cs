using Microsoft.Xrm.Sdk;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms;
using Xrm.Sdk.PluginRegistration.Forms;
using Xrm.Sdk.PluginRegistration.Wrappers;
using XrmToolBox.Extensibility.Args;

namespace Xrm.Sdk.PluginRegistration.Tests.Progress
{
    /// <summary>
    /// Covers the feedback a user gets during slow operations: the progress sent to the XrmToolBox
    /// status bar, and the package dialog while it uploads.
    /// </summary>
    [TestFixture]
    public class ProgressFeedbackTests
    {
        [Test, Apartment(ApartmentState.STA)]
        public void StatusBarProgressAdvancesWithEachUnitAndKeepsItsText()
        {
            var sent = new List<StatusBarMessageEventArgs>();
            var progress = new ProgressIndicator(new Action<StatusBarMessageEventArgs>(sent.Add));

            // As RegistrationHelper.Unregister reports it
            progress.Initialize(4, "Unregistering Images");
            progress.Increment();
            progress.SetText("Unregistering Steps");
            progress.Increment();
            progress.Increment();
            progress.Increment();

            Assert.That(sent.Select(Describe), Is.EqualTo(new[]
            {
                "0|", "0|Unregistering Images", "25|Unregistering Images", "25|Unregistering Steps",
                "50|Unregistering Steps", "75|Unregistering Steps", "100|Unregistering Steps"
            }));

            // The run is over: the bar goes away with the text, and a late call does not bring it back
            sent.Clear();
            progress.Complete(true);
            progress.Increment();
            Assert.That(sent, Is.Not.Empty);
            Assert.That(sent.Select(message => message.Progress), Is.All.Null);
            Assert.That(sent.Last().Message, Is.Empty);
        }

        [Test, Apartment(ApartmentState.STA)]
        public void StatusBarProgressReachesTheEndForMoreThanAHundredUnits()
        {
            var sent = new List<StatusBarMessageEventArgs>();
            var progress = new ProgressIndicator(new Action<StatusBarMessageEventArgs>(sent.Add));

            progress.Initialize(250, "Unregistering Steps");
            for (int unit = 0; unit < 250; unit++) progress.Increment();
            // One call too many must not run past the end
            progress.Increment();

            var values = sent.Where(message => message.Progress.HasValue).Select(message => message.Progress.Value).ToList();
            Assert.That(values, Is.Ordered);
            Assert.That(values.First(), Is.EqualTo(0));
            Assert.That(values[values.Count / 2], Is.InRange(45, 55));
            Assert.That(values.Last(), Is.EqualTo(100));
        }

        [Test, Apartment(ApartmentState.STA)]
        public void StatusBarProgressIgnoresARunWithNothingToDo()
        {
            var sent = new List<StatusBarMessageEventArgs>();
            var progress = new ProgressIndicator(new Action<StatusBarMessageEventArgs>(sent.Add));

            Assert.DoesNotThrow(() => progress.Initialize(0, "Nothing"));
            progress.Increment();

            Assert.That(sent.Select(Describe), Is.EqualTo(new[] { "|Nothing" }));
        }

        [Test, Apartment(ApartmentState.STA)]
        public void PackageDialogIsLockedAndShowsAWaitCursorWhileItWorks()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var org = (CrmOrganization)FormatterServices.GetUninitializedObject(typeof(CrmOrganization));
            typeof(CrmOrganization).GetField("m_organizationService", flags).SetValue(org,
                new RecordingOrganizationService { OnRetrieveMultiple = _ => new EntityCollection() });
            using (var dialog = new PackageRegistrationForm(org, null, new Settings()))
            {
                var type = typeof(PackageRegistrationForm);
                var locked = new[] { "btnBrowse", "btnImport", "btnClose", "cbbSolution", "txtPluginPackageFile", "chkAddToSolution" }
                    .Select(name => (Control)type.GetField(name, flags).GetValue(dialog)).ToList();
                var setWorking = type.GetMethod("SetWorkingStatus", flags);
                var closing = type.GetMethod("OnFormClosing", flags);
                Func<bool> closeIsRefused = () =>
                {
                    var args = new FormClosingEventArgs(CloseReason.UserClosing, false);
                    closing.Invoke(dialog, new object[] { args });
                    return args.Cancel;
                };
                Assert.That(dialog.UseWaitCursor, Is.False);
                Assert.That(closeIsRefused(), Is.False);

                var waitPanel = type.GetField("waitPanel", flags);
                Assert.That(waitPanel.GetValue(dialog), Is.Null);
                // Shorter than the wait box, as the update dialog can be at a high DPI
                dialog.Height = 180;
                int heightBefore = dialog.Height;

                setWorking.Invoke(dialog, new object[] { true, false });

                var panel = (Panel)waitPanel.GetValue(dialog);
                Assert.That(panel, Is.Not.Null, "The wait box shows while it works");
                Assert.That(dialog.Controls.Contains(panel), Is.True);
                Assert.That(dialog.ClientRectangle.Contains(panel.Bounds), Is.True, "The wait box is not cut off");
                Assert.That(panel.Top, Is.EqualTo(dialog.ClientSize.Height - panel.Bottom).Within(1), "The wait box is centered");
                Assert.That(dialog.Controls.GetChildIndex(panel), Is.EqualTo(0), "The wait box is in front of the fields");
                Assert.That(dialog.UseWaitCursor, Is.True);
                Assert.That(locked.Select(control => control.Enabled), Is.All.False);
                Assert.That(closeIsRefused(), Is.True, "Closing must wait for the upload");
                var shutdown = new FormClosingEventArgs(CloseReason.WindowsShutDown, false);
                closing.Invoke(dialog, new object[] { shutdown });
                Assert.That(shutdown.Cancel, Is.False, "Only the user's own close is held back");

                setWorking.Invoke(dialog, new object[] { false, false });

                Assert.That(waitPanel.GetValue(dialog), Is.Null);
                Assert.That(dialog.Controls.Contains(panel), Is.False, "The wait box goes away when the work is done");
                Assert.That(panel.IsDisposed, Is.True);
                Assert.That(dialog.Height, Is.EqualTo(heightBefore), "The dialog gets its own height back");
                Assert.That(dialog.UseWaitCursor, Is.False);
                Assert.That(locked.Select(control => control.Enabled), Is.All.True);
                Assert.That(closeIsRefused(), Is.False);
            }
        }

        private static string Describe(StatusBarMessageEventArgs message)
        {
            return message.Progress + "|" + message.Message;
        }
    }
}
