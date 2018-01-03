namespace Xrm.Sdk.PluginRegistration.Models
{
    using CsvHelper.Configuration.Attributes;

    public class CsvModel
    {
        [Name("Module")]
        public string Module { get; set; }
        [Name("Description")]
        public string Description { get; set; }
        [Name("Primary Entity")]
        public string PrimaryEntity { get; set; }
        [Name("Secondary Entity")]
        public string SecondaryEntity { get; set; }
        [Name("Message")]
        public string Message { get; set; }
        [Name("Execution Mode")]
        public string ExecutionMode { get; set; }
        [Name("Stage")]
        public string Stage { get; set; }
        [Name("Execution Order")]
        public string ExecutionOrder { get; set; }
        [Name("Deployment")]
        /// <summary>
        /// ServerOnly, Offline, Both
        /// </summary>
        public string Deployment { get; set; }
        //public string Rank { get; set; }
        [Name("Filtering Attributes ")]
        public string FilteringAttributes { get; set; }
        
    }
}