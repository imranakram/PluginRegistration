namespace Xrm.Sdk.PluginRegistration.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Builds the authvalue of a webhook that sends HTTP headers or query string parameters:
    /// &lt;settings&gt;&lt;setting name="key" value="value" /&gt;&lt;/settings&gt;
    /// </summary>
    public static class WebhookAuthValue
    {
        /// <summary>
        /// Builds the settings XML, escaped. Keys are trimmed and empty rows skipped; returns an
        /// empty string when no row is left.
        /// </summary>
        public static string BuildSettings(IEnumerable<KeyValuePair<string, string>> rows)
        {
            var settings = Rows(rows)
                .Select(row => new XElement("setting", new XAttribute("name", row.Key), new XAttribute("value", row.Value)))
                .ToList();
            return settings.Count == 0 ? string.Empty : new XElement("settings", settings).ToString(SaveOptions.DisableFormatting);
        }

        /// <summary>Problems that keep the rows from being saved; empty when they are fine.</summary>
        public static IEnumerable<string> Validate(IEnumerable<KeyValuePair<string, string>> rows)
        {
            var list = Rows(rows).ToList();
            if (list.Any(row => row.Key.Length == 0))
            {
                yield return "Every value needs a key.";
            }

            // HTTP header names are case-insensitive
            foreach (var key in list.Where(row => row.Key.Length != 0)
                .GroupBy(row => row.Key, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key))
            {
                yield return $"The key {key} is used more than once.";
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> Rows(IEnumerable<KeyValuePair<string, string>> rows)
        {
            return (rows ?? Enumerable.Empty<KeyValuePair<string, string>>())
                .Select(row => new KeyValuePair<string, string>(row.Key?.Trim() ?? string.Empty, row.Value ?? string.Empty))
                .Where(row => row.Key.Length != 0 || row.Value.Length != 0);
        }
    }
}
