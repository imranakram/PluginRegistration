namespace Xrm.Sdk.PluginRegistration.Tests.Integration
{
    using McTools.Xrm.Connection;
    using Microsoft.Xrm.Tooling.Connector;
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Text.Json;
    using Xrm.Sdk.PluginRegistration.Wrappers;

    /// <summary>
    /// The Dataverse environment the integration tests run against. Configure it with either
    /// - a testbench.local.json next to testbench.example.json (never commit it: it holds a secret),
    /// - PRT_TESTBENCH: the path to such a file, or
    /// - PRT_TESTBENCH_CONNECTION: the connection string itself (for CI secrets).
    /// Without any of these the integration tests are skipped.
    /// </summary>
    internal sealed class TestBench
    {
        public const string Category = "Integration";

        private const string ConnectionVariable = "PRT_TESTBENCH_CONNECTION";
        private const string FileVariable = "PRT_TESTBENCH";
        private const string FileName = "testbench.local.json";

        private readonly string connectionString;
        private CrmServiceClient client;
        private ConnectionDetail detail;

        private TestBench(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public CrmServiceClient Service => client;

        /// <summary>
        /// Loads the configuration, or calls Assert.Ignore when there is none.
        /// </summary>
        public static TestBench LoadOrIgnore()
        {
            var connection = Environment.GetEnvironmentVariable(ConnectionVariable);
            if (string.IsNullOrWhiteSpace(connection))
            {
                var path = Environment.GetEnvironmentVariable(FileVariable);
                if (string.IsNullOrWhiteSpace(path))
                {
                    path = FindFile(TestContext.CurrentContext.TestDirectory);
                }

                if (path == null || !File.Exists(path))
                {
                    Assert.Ignore($"No test bench configured: add {FileName} (see testbench.example.json) or set {ConnectionVariable}.");
                }

                using (var json = JsonDocument.Parse(File.ReadAllText(path)))
                {
                    connection = json.RootElement.GetProperty("connection").GetString();
                }
            }

            return new TestBench(connection);
        }

        /// <summary>
        /// Loads the organization the way the tool does after connecting or refreshing.
        /// </summary>
        public CrmOrganization LoadOrganization()
        {
            if (client == null)
            {
                Connect();
            }

            return new CrmOrganization(detail, new Settings(), null, client);
        }

        private void Connect()
        {
            client = new CrmServiceClient(connectionString);
            if (!client.IsReady)
            {
                // LastCrmError never contains the secret, unlike the connection string
                throw new InvalidOperationException("Unable to connect to the test bench: " + client.LastCrmError, client.LastCrmException);
            }

            // What XrmToolBox's connection control fills in when it connects
            detail = new ConnectionDetail(true)
            {
                ServiceClient = client,
                Organization = client.ConnectedOrgUniqueName,
                OrganizationFriendlyName = client.ConnectedOrgFriendlyName,
                OrganizationVersion = client.ConnectedOrgVersion.ToString(),
                OrganizationServiceUrl = client.CrmConnectOrgUriActual?.ToString(),
                WebApplicationUrl = client.ConnectedOrgPublishedEndpoints[Microsoft.Xrm.Sdk.Discovery.EndpointType.WebApplication],
            };
        }

        private static string FindFile(string directory)
        {
            for (var dir = new DirectoryInfo(directory); dir != null; dir = dir.Parent)
            {
                foreach (var candidate in new[] { Path.Combine(dir.FullName, FileName), Path.Combine(dir.FullName, "Xrm.Sdk.PluginRegistration.Tests", FileName) })
                {
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }
    }
}
