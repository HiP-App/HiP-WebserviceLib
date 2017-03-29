using System.Text;
using Microsoft.Extensions.Configuration;

namespace PaderbornUniversity.SILab.Hip.Webservice
{
    public class AppConfig
    {
        public string DB_HOST { get; set; }
        public string DB_USERNAME { get; set; }
        public string DB_PASSWORD { get; set; }
        public string DB_NAME { get; set; }
        public string CLIENT_ID { get; set; }
        public string DOMAIN { get; set; }
        public string EMAIL_SERVICE { get; set; }
        public string ALLOW_HTTP { get; set; }
        public string ADMIN_EMAIL { get; set; }

        public static string BuildConnectionString(IConfigurationRoot config)
        {
            var connectionString = new StringBuilder();

            connectionString.Append($"Host={config.GetValue<string>("DB_HOST")};");
            connectionString.Append($"Username={config.GetValue<string>("DB_USERNAME")};");
            connectionString.Append($"Password={config.GetValue<string>("DB_PASSWORD")};");
            connectionString.Append($"Database={config.GetValue<string>("DB_NAME")};");
            connectionString.Append($"Pooling=true;");

            return connectionString.ToString();
        }
    }
}