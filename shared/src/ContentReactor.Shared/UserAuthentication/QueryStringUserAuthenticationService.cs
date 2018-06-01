using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContentReactor.Shared.UserAuthentication
{
    public class QueryStringUserAuthenticationService : IUserAuthenticationService
    {
        // Note: This implementation of the UserAuthentication class uses the query string to obtain the user ID.
        // This assumes that the APIs are called by trusted clients that have performed user authentication.
        // An extension to this sample would be to pass the user's identity in using a bearer token through the
        // Authorization header, and to validate the token and obtain the user ID from a claim.

        public Task<bool> GetUserIdAsync(HttpRequest req, out string userId, out IActionResult responseResult)
        {
            // retrieve the user ID parameter from the query string
            var userIdParameter = req.Query
                .SingleOrDefault(q => string.Compare(q.Key, "userId", StringComparison.OrdinalIgnoreCase) == 0)
                .Value;
            
            if (string.IsNullOrEmpty(userIdParameter) || userIdParameter.Count == 0)
            {
                responseResult = new BadRequestObjectResult(new { error = "Missing mandatory 'userId' parameter in query string." });
                userId = null;
                return Task.FromResult(false);
            }

            if (userIdParameter.Count > 1)
            {
                responseResult = new BadRequestObjectResult(new { error = "Please only specify one 'userId' parameter in query string." });
                userId = null;
                return Task.FromResult(false);
            }

            // we have a valid user ID that we can return
            responseResult = null;
            userId = userIdParameter.Single();
            return Task.FromResult(true);
        }
    }
}
