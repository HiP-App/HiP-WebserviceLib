namespace PaderbornUniversity.SILab.Hip.Webservice
{
    /// <summary>
    /// Configuration properties for authentication/authorization.
    /// </summary>
    public class AuthConfig
    {
        /// <summary>
        /// Audience of all HiP APIs.
        /// Default value: "https://hip.cs.upb.de/API"
        /// </summary>
        public virtual string Audience { get; set; } = "https://hip.cs.upb.de/API";

        /// <summary>
        /// Authority.
        /// Default value: "https://hip.eu.auth0.com/"
        /// </summary>
        public virtual string Authority { get; set; } = "https://hip.eu.auth0.com/";
    }
}
