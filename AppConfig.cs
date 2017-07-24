using System.Text;
using Microsoft.Extensions.Configuration;

namespace PaderbornUniversity.SILab.Hip.Webservice
{
    public class AppConfig
    {
        public string DbHost { get; set; }
        public string DbUsername { get; set; }
        public string DbPassword { get; set; }
        public string DbName { get; set; }
        public string CLIENT_ID { get; set; }
        public string Domain { get; set; }
        public string EmailService { get; set; }
        public string AllowHttp { get; set; }
        public string AdminEmail { get; set; }

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