using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace CustomClaimAPI
{
    public static class GetEmployeeDetails
    {
        // https://nimccfta-customclaims.azurewebsites.net/api/GetEmployeeDetails?code=MbX30aGOyJ7m1QwqLoHzRt1l7BRBUl5S1PwcEHEh_S7NAzFuW8o6Lw==
        [FunctionName("GetEmployeeDetails")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            ResponseContent responseContent = new ResponseContent();

            log.LogInformation("GetEmployeeDetails function processed a request.");

            // Validate the request credentials
            ClaimsPrincipal claimsPrincipal = Authorize(req, log);
            if (claimsPrincipal == null)
            {
                return new UnauthorizedResult();
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation("Request body: " + requestBody);

            dynamic r = JsonConvert.DeserializeObject(requestBody);

            if (r == null)
            {
                responseContent.data.actions[0].claims.jobTitle = string.Empty;
                responseContent.data.actions[0].claims.publisherId = string.Empty;
                responseContent.data.actions[0].claims.userRole = string.Empty;
            }
            else
            {
                // Retrieve employee details from the pubs database
                log.LogInformation($"User Display Name: {r.data.authenticationContext.user.displayName}");
                log.LogInformation($"User Given Name: {r.data.authenticationContext.user.givenName}");
                log.LogInformation($"User Surname: {r.data.authenticationContext.user.surname}");
                log.LogInformation($"User Id: {r.data.authenticationContext.user.id}");
                log.LogInformation($"User Principal Name: {r.data.authenticationContext.user.userPrincipalName}");

                string firstName = r.data.authenticationContext.user.givenName;
                string lastName = r.data.authenticationContext.user.surname;

                if (string.IsNullOrEmpty(firstName)
                    || string.IsNullOrEmpty(lastName))
                {
                    responseContent.data.actions[0].claims.jobTitle = string.Empty;
                    responseContent.data.actions[0].claims.publisherId = string.Empty;
                    responseContent.data.actions[0].claims.userRole = string.Empty;
                }
                else
                {
                    Employee employee = GetEmployee(firstName, lastName, log);
                    if (employee != null)
                    {
                        responseContent.data.actions[0].claims.jobTitle = employee.JobTitle;
                        responseContent.data.actions[0].claims.publisherId = employee.PublisherID;
                        responseContent.data.actions[0].claims.userRole = employee.UserRole;
                    }
                    else
                    {
                        responseContent.data.actions[0].claims.jobTitle = string.Empty;
                        responseContent.data.actions[0].claims.publisherId = string.Empty;
                        responseContent.data.actions[0].claims.userRole = string.Empty;
                    }
                }
            }

            return (ActionResult)new OkObjectResult(responseContent);
        }

        private static Employee GetEmployee(string firstName, string lastName, ILogger log)
        {
            Employee employee = null;
            const string sql = "SELECT e.[emp_id], e.[fname], e.[minit], e.[lname], e.[job_id], e.[job_lvl], e.[pub_id], e.[hire_date], j.[job_desc], p.[pub_name] FROM [dbo].[employee] e inner join [dbo].[jobs] j on j.[job_id] = e.[job_id] inner join [dbo].[publishers] p on p.[pub_id] = e.[pub_id] WHERE e.[fname] = @firstName AND e.[lname] = @lastName";
            List<SqlParameter> parameters = new List<SqlParameter>();
            SqlHelper sqlHelper = null;

            parameters.Add(new SqlParameter("@firstName", firstName));
            parameters.Add(new SqlParameter("@lastName", lastName));
            try
            {
                sqlHelper = new SqlHelper();
                SqlDataReader reader = sqlHelper.ExecuteDataReader(sql, CommandType.Text, ref parameters);
                while (reader.Read())
                {
                    employee = new Employee()
                    {
                        EmployeeID = reader["emp_id"].ToString(),
                        FirstName = reader["fname"].ToString(),
                        MiddleInitial = reader["minit"].ToString(),
                        LastName = reader["lname"].ToString(),
                        JobID = Convert.ToInt32(reader["job_id"]),
                        JobTitle = reader["job_desc"].ToString(),
                        JobLevel = Convert.ToInt32(reader["job_lvl"]),
                        PublisherID = reader["pub_id"].ToString(),
                        PublisherName = reader["pub_name"].ToString(),
                        HireDate = Convert.ToDateTime(reader["hire_date"])
                    };
                    Random random = new Random();
                    int roleNumber = random.Next(1, 4);
                    string userRole = string.Empty;
                    switch (roleNumber)
                    {
                        case 1:
                            userRole = "Global Administrator";
                            break;
                        case 2:
                            userRole = "User Administrator";
                            break;
                        case 3:
                            userRole = "Group Owner";
                            break;
                        default:
                            userRole = "User";
                            break;
                    }
                    employee.UserRole = userRole;
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Query of employee table failed");
            }
            finally
            {
                if (sqlHelper != null) sqlHelper.Close();
            }

            return employee;
        }
        private static ClaimsPrincipal Authorize(HttpRequest request, ILogger log)
        {
            ClaimsPrincipal claimsPrincipal = null;
            string audience = Environment.GetEnvironmentVariable("Audience", EnvironmentVariableTarget.Process);
            string clientId = Environment.GetEnvironmentVariable("ClientId", EnvironmentVariableTarget.Process);
            string tenantId = Environment.GetEnvironmentVariable("TenantId", EnvironmentVariableTarget.Process);
            string authority = Environment.GetEnvironmentVariable("Authority", EnvironmentVariableTarget.Process);
            ConfigurationManager<OpenIdConnectConfiguration> configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>($"{authority}/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever());
            ISecurityTokenValidator tokenValidator = new JwtSecurityTokenHandler();

            // For debugging/development purposes, one can enable additional detail in exceptions by setting IdentityModelEventSource.ShowPII to true.
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

            // check if there is a jwt in the authorization header, return 'Unauthorized' error if the token is null.
            if (request.Headers.ContainsKey("Authorization") && !string.IsNullOrEmpty(request.Headers["Authorization"]))
            {
                // Pull OIDC discovery document from Azure AD. For example, the tenant-independent version of the document is located
                // at https://login.microsoftonline.com/common/.well-known/openid-configuration.
                OpenIdConnectConfiguration config = null;
                try
                {
                    config = configurationManager.GetConfigurationAsync().Result;
                }
                catch (Exception ex)
                {
                    log.LogError("Retrieval of OpenId configuration failed with the following error: {0}", ex.Message);
                }

                if (config != null)
                {
                    // Support both v1 and v2 AAD issuer endpoints
                    IList<string> validissuers = new List<string>()
                    {
                        $"https://login.microsoftonline.com/{tenantId}",
                        $"https://login.microsoftonline.com/{tenantId}/v2.0",
                        $"https://login.windows.net/{tenantId}",
                        $"https://login.microsoft.com/{tenantId}",
                        $"https://sts.windows.net/{tenantId}"
                    };

                    // Initialize the token validation parameters
                    TokenValidationParameters validationParameters = new TokenValidationParameters
                    {
                        // Application ID URI and Client ID of this service application are both valid audiences
                        ValidAudiences = new[] { audience, clientId },
                        ValidIssuers = validissuers,
                        IssuerSigningKeys = config.SigningKeys
                    };

                    try
                    {
                        // Validate token.
                        SecurityToken securityToken;
                        string accessToken = request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                        claimsPrincipal = tokenValidator.ValidateToken(accessToken, validationParameters, out securityToken);

                        // This check is required to ensure that the Web API only accepts tokens from tenants where it has been consented to and provisioned.
                        //if (!claimsPrincipal.Claims.Any(x => x.Type == "http://schemas.microsoft.com/identity/claims/scope")
                        //    && !claimsPrincipal.Claims.Any(y => y.Type == "scp")
                        //    && !claimsPrincipal.Claims.Any(y => y.Type == "roles"))
                        //{
                        //    claimsPrincipal = null;
                        //}
                    }
                    catch (SecurityTokenValidationException stex)
                    {
                        log.LogError("Validation of security token failed with the following error: {0}", stex.Message);
                    }
                    catch (Exception ex)
                    {
                        log.LogError("Validation of security token failed with the following error: {0}", ex.Message);
                    }
                }
            }

            return claimsPrincipal;
        }
    }
}
