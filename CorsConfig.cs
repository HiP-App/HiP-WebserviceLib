using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.Webservice
{
    /// <summary>
    /// Configuration properties for Cross-Origin Resource Sharing.
    /// </summary>
    public class CorsConfig
    {
        public Dictionary<string, CorsEnvironmentConfig> Cors { get; set; }
    }

    public class CorsEnvironmentConfig
    {
        public string[] Origins { get; set; }
        public string[] Headers { get; set; }
        public string[] Methods { get; set; }
        public string[] ExposedHeaders { get; set; }
    }
}
