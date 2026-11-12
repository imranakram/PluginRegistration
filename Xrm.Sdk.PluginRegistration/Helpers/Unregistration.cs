namespace Xrm.Sdk.PluginRegistration.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The records one unregister removes. Built by RegistrationHelper.PlanUnregister; the same
    /// shape also lists what was actually deleted.
    /// </summary>
    public sealed class UnregisterPlan
    {
        public List<Guid> Images { get; } = new List<Guid>();
        public List<Guid> Steps { get; } = new List<Guid>();
        public List<Guid> SecureConfigs { get; } = new List<Guid>();
        public List<Guid> Plugins { get; } = new List<Guid>();
        public List<Guid> Assemblies { get; } = new List<Guid>();
        public List<Guid> ServiceEndpoints { get; } = new List<Guid>();
        public List<Guid> Packages { get; } = new List<Guid>();

        public int Total =>
            Images.Count + Steps.Count + SecureConfigs.Count + Plugins.Count + Assemblies.Count + ServiceEndpoints.Count + Packages.Count;

        /// <summary>
        /// Counts per kind of record, for the summary shown to the user. Secure configurations are
        /// part of their steps, so they are not listed.
        /// </summary>
        public Dictionary<string, int> Statistics()
        {
            var stats = new Dictionary<string, int>();
            Add(stats, Packages, "Package", "Packages");
            Add(stats, ServiceEndpoints, "ServiceEndpoint", "ServiceEndpoints");
            Add(stats, Assemblies, "Assembly", "Assemblies");
            Add(stats, Plugins, "Plugin", "Plugins");
            Add(stats, Steps, "Step", "Steps");
            Add(stats, Images, "Image", "Images");
            return stats;
        }

        /// <summary>One line per kind of record, such as "150 Plugins Unregistered Successfully".</summary>
        public string Summary()
        {
            return string.Join(Environment.NewLine, Statistics().Select(stat => $"{stat.Value} {stat.Key} Unregistered Successfully"));
        }

        private static void Add(Dictionary<string, int> stats, List<Guid> ids, string singular, string plural)
        {
            if (ids.Count != 0)
            {
                stats.Add(ids.Count == 1 ? singular : plural, ids.Count);
            }
        }
    }

    /// <summary>
    /// The outcome of RegistrationHelper.ExecuteUnregister. When a delete fails, the run stops:
    /// Error is set and Deleted lists what was removed before it.
    /// </summary>
    public sealed class UnregisterResult
    {
        public UnregisterResult(UnregisterPlan plan)
        {
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        }

        public UnregisterPlan Plan { get; }

        public UnregisterPlan Deleted { get; } = new UnregisterPlan();

        public Exception Error { get; internal set; }
    }

    /// <summary>Progress of an unregister run: the current kind of record and how many records are done.</summary>
    public sealed class UnregisterProgress
    {
        public UnregisterProgress(string text, int done, int total)
        {
            Text = text;
            Done = done;
            Total = total;
        }

        public string Text { get; }

        public int Done { get; }

        public int Total { get; }

        public int Percent => Total <= 0 ? 0 : Math.Min(100, Done * 100 / Total);

        public override string ToString()
        {
            return $"{Text} ({Done} of {Total})";
        }
    }

    /// <summary>
    /// Unregister was refused before anything was deleted. The message explains why and what to do instead.
    /// </summary>
    public sealed class UnregisterRefusedException : InvalidOperationException
    {
        public UnregisterRefusedException(string message)
            : base(message)
        {
        }
    }
}
