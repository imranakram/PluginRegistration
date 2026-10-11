using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xrm.Sdk.PluginRegistration.Controls;
using Xrm.Sdk.PluginRegistration.Models;

namespace Xrm.Sdk.PluginRegistration.Wrappers
{
    /// <summary>
    /// A definition has its own identity even when its generated message has no steps.
    /// Only summaries are exposed here; editing always retrieves the complete definition.
    /// </summary>
    public sealed class CrmCustomApiNode : ICrmTreeNode
    {
        private readonly Dictionary<Guid, CrmPluginStep> steps = new Dictionary<Guid, CrmPluginStep>();

        public CrmCustomApiNode(CustomApiSummary summary)
        {
            if (summary == null) throw new ArgumentNullException(nameof(summary));
            Id = summary.Id;
            Name = summary.Name;
            UniqueName = summary.UniqueName;
            SdkMessageId = summary.SdkMessageId;
            IsManaged = summary.IsManaged;
        }

        [ReadOnly(true)] public Guid Id { get; }
        [ReadOnly(true)] public string Name { get; }
        [ReadOnly(true)] public string UniqueName { get; }
        [ReadOnly(true)] public Guid? SdkMessageId { get; }
        [ReadOnly(true)] public bool IsManaged { get; }

        [Browsable(false)] public Guid NodeId => Id;
        [Browsable(false)] public string NodeText => "(Custom API) " + UniqueName;
        [Browsable(false)] public string NodeTypeLabel => "Custom API";
        [Browsable(false)] public CrmTreeNodeType NodeType => CrmTreeNodeType.CustomApi;
        [Browsable(false)] public CrmTreeNodeImageType NodeImageType => CrmTreeNodeImageType.Message;
        [Browsable(false)] public CrmTreeNodeImageType NodeSelectedImageType => CrmTreeNodeImageType.MessageSelected;
        [Browsable(false)] public List<ICrmTreeNode> NodeChildren => steps.Values.Cast<ICrmTreeNode>().ToList();

        public void AddStep(CrmPluginStep step)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));
            if (!SdkMessageId.HasValue || step.MessageId != SdkMessageId.Value)
                throw new ArgumentException("The step does not belong to this API's generated message.", nameof(step));
            steps[step.StepId] = step;
        }

        public void RemoveStep(Guid id) => steps.Remove(id);
    }
}
