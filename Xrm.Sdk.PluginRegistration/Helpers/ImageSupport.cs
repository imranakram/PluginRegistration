using System;
using System.Collections.Generic;
using System.Linq;
using Xrm.Sdk.PluginRegistration.Models;
using Xrm.Sdk.PluginRegistration.Wrappers;

namespace Xrm.Sdk.PluginRegistration.Helpers
{
    /// <summary>Which steps can have entity images, and how to say why one can't.</summary>
    public static class ImageSupport
    {
        /// <summary>Microsoft's list of the messages that support entity images.</summary>
        public const string DocsUrl =
            "https://learn.microsoft.com/power-apps/developer/data-platform/register-plug-in#messages-that-support-entity-images";

        /// <summary>Whether steps on the message can have images: it has a property that holds the record.</summary>
        public static bool Supports(CrmMessage message) =>
            message != null && message.ImageMessagePropertyNames != null && message.ImageMessagePropertyNames.Count > 0;

        /// <summary>The names of the messages whose steps can have images, sorted.</summary>
        public static List<string> SupportedMessages(IEnumerable<CrmMessage> messages) =>
            (messages ?? Enumerable.Empty<CrmMessage>())
                .Where(Supports)
                .Select(message => message.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <summary>
        /// Why steps on the message can't have images, or null when they can. A Custom API's message is
        /// named as one, since that is the case people run into.
        /// </summary>
        public static string Reason(CrmMessage message, IEnumerable<CustomApiSummary> apis, IEnumerable<CrmMessage> messages)
        {
            if (message == null || Supports(message)) return null;
            bool customApi = apis != null && apis.Any(api => api.SdkMessageId == message.MessageId);
            return $"Steps on {message.Name} can't have images{(customApi ? ": it's a Custom API message" : "")}. " +
                $"Dataverse supports images only on steps on {string.Join(", ", SupportedMessages(messages))}.";
        }
    }
}
