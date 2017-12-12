namespace PaderbornUniversity.SILab.Hip.Webservice
{
    /// <summary>
    /// Configuration properties for a PostgreSQL database.
    /// </summary>
    public class PostgresDatabaseConfig
    {
        /// <summary>
        /// Name of the database to use.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Host name or IP of the database server.
        /// Default value: "localhost"
        /// </summary>
        public string Host { get; set; } = "localhost";

        /// <summary>
        /// User name. Default value: "postgres"
        /// </summary>
        public string Username { get; set; } = "postgres";

        /// <summary>
        /// Password. Default value: "postgres"
        /// </summary>
        public string Password { get; set; } = "postgres";

        /// <summary>
        /// Port. Default value: "5432"
        /// </summary>
        public string Port { get; set; } = "5432";

        public string ConnectionString =>
            $"Host={Host};Username={Username};Password={Password};Database={Name};Pooling=true;Port={Port};";
    }
}