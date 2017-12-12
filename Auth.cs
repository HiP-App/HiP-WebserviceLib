using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace PaderbornUniversity.SILab.Hip.Webservice
{
    public static class Auth
    {
        /// <summary>
        /// Retrieves the user ID from an <see cref="IIdentity"/>.
        /// </summary>
        /// <returns>The user ID string or null if no valid identity was provided</returns>
        public static string GetUserIdentity(this IIdentity identity)
        {
            return (identity as ClaimsIdentity)?.Claims
                .FirstOrDefault(c => c.Type == "https://hip.cs.upb.de/sub")?
                .Value;
        }

        public static IReadOnlyList<Claim> GetUserRoles(this IIdentity identity)
        {
            return (identity as ClaimsIdentity)?
                .FindAll(c => c.Type == "https://hip.cs.upb.de/roles").ToList() ?? new List<Claim>();
        }
    }
}
