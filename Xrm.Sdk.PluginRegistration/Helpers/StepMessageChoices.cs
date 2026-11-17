using System;
using System.Collections.Generic;
using System.Linq;
using Xrm.Sdk.PluginRegistration.Controls;
using Xrm.Sdk.PluginRegistration.Models;
using Xrm.Sdk.PluginRegistration.Wrappers;

namespace Xrm.Sdk.PluginRegistration.Helpers
{
    /// <summary>A Custom API's limit on the steps registered on its message.</summary>
    public sealed class CustomApiStepLimit
    {
        public CustomApiStepLimit(string apiName, string hint)
        {
            ApiName = apiName;
            Hint = hint;
        }

        public string ApiName { get; }

        /// <summary>Explains the limit in the step form.</summary>
        public string Hint { get; }
    }

    /// <summary>The Message and Primary Entity choices of the step form, and the Custom API rules for them.</summary>
    public static class StepMessageChoices
    {
        public const string CustomApiDetail = "Custom API";
        public const string NoTableDetail = "no table";
        public const string AllTablesDetail = "all tables";

        /// <summary>The messages, each keyed by its id. A Custom API's message says so in grey, and can be searched for by it.</summary>
        public static List<SearchChoice> Messages(IEnumerable<CrmMessage> messages, IEnumerable<CustomApiSummary> apis)
        {
            var apiMessages = new HashSet<Guid>((apis ?? Enumerable.Empty<CustomApiSummary>())
                .Where(api => api.SdkMessageId.HasValue).Select(api => api.SdkMessageId.Value));
            return (messages ?? Enumerable.Empty<CrmMessage>())
                .Where(message => message != null && !string.IsNullOrEmpty(message.Name))
                .Select(message => new SearchChoice(message.Name, apiMessages.Contains(message.MessageId) ? CustomApiDetail : null, message.MessageId))
                .ToList();
        }

        /// <summary>
        /// The tables a message's steps can be registered on. "none" says what it means: every table for a
        /// message that has tables, such as Update, and no table for a message without any.
        /// </summary>
        public static List<SearchChoice> PrimaryEntities(CrmMessage message)
        {
            if (message == null) return new List<SearchChoice>();
            var names = message.FindMessageEntities(null, null)
                .Select(entity => entity.PrimaryEntity)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            string noneDetail = names.Any(name => !IsNone(name)) ? AllTablesDetail : NoTableDetail;
            return names.Select(name => new SearchChoice(name, IsNone(name) ? noneDetail : null)).ToList();
        }

        private static bool IsNone(string name) => string.Equals(name, "none", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// The limit set by the Custom API that owns the message, or null when there is none. Only
        /// Async Only needs one here: Dataverse leaves the messages of APIs allowing no steps out of the
        /// step filters, so they are never offered.
        /// </summary>
        public static CustomApiStepLimit LimitFor(Guid? messageId, IEnumerable<CustomApiSummary> apis)
        {
            if (!messageId.HasValue || apis == null) return null;
            var api = apis.FirstOrDefault(a => a.SdkMessageId == messageId.Value);
            if (api?.AllowedCustomProcessingStepType != CustomApiProcessingStepType.AsyncOnly) return null;
            return new CustomApiStepLimit(api.UniqueName,
                $"Custom API {api.UniqueName} allows only asynchronous steps (Allowed Custom Processing Step Type: Async Only), so this step runs asynchronously in Post-operation.");
        }
    }
}
