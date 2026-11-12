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

                setWorking.Invoke(dialog, new object[] { true, false });

                Assert.That(dialog.UseWaitCursor, Is.True);
                Assert.That(locked.Select(control => control.Enabled), Is.All.False);
                Assert.That(closeIsRefused(), Is.True, "Closing must wait for the upload");
                var shutdown = new FormClosingEventArgs(CloseReason.WindowsShutDown, false);
                closing.Invoke(dialog, new object[] { shutdown });
                Assert.That(shutdown.Cancel, Is.False, "Only the user's own close is held back");

                setWorking.Invoke(dialog, new object[] { false, false });

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
