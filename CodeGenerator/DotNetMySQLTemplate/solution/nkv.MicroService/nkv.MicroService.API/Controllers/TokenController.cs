using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using nkv.MicroService.Utility;
using nkv.MicroService.Manager.Interface;
using nkv.MicroService.Model;

namespace nkv.MicroService.API.Controllers
{
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        IUsersManager _usersManager;
        IPermissionmatrixManager permissionmatrixManager;
        public TokenController(IOptions<AppSettings> appSettings, IUsersManager usersManager, IPermissionmatrixManager permissionmatrixManager)
        {
            _appSettings = appSettings.Value;
            _usersManager = usersManager;
            this.permissionmatrixManager = permissionmatrixManager;
        }
        /// <summary>
        /// Signs a person in: a JWT with the usual claims (name, role, id, workspace owner, tenant, token version), set as the
        /// HttpOnly session cookie. Used by password sign-in, email codes and email invites.
        /// </summary>
        public static (string jwt, DateTime validFrom, DateTime validTo) IssueSession(ControllerBase c, string secret, int userId, string username, int roleId, string owner)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, roleId.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Actor, owner ?? username),
                    // informational only: DataGuard resolves the tenant from user_tenants on every request ("" = platform, "unassigned")
                    new Claim(DataGuard.TenantClaim, DataGuard.TenantClaimValue(userId, roleId)),
                    // token version: password/role/active changes and sign-out bump it, ending existing sessions
                    new Claim(AuthGuard.VersionClaim, AuthGuard.TokenVersion(userId).ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(AuthGuard.TokenMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)), SecurityAlgorithms.HmacSha256Signature)
            });
            var jwt = tokenHandler.WriteToken(token);
            SessionCookie.Set(c.Response, jwt, token.ValidTo);
            return (jwt, token.ValidFrom, token.ValidTo);
        }

        [HttpPost]
        [Route(APIEndpoint.DefaultRoute)]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
        public ActionResult Post(AuthenticateModel model)
        {
            try
            {
                // lockout after repeated failures (same answer for unknown usernames, so accounts can't be discovered)
                var locked = AuthGuard.LockedMinutes(model?.Username);
                if (locked > 0)
                    return StatusCode(429, new APIResponse(ResponseCode.ERROR, $"Too many failed sign-ins. Try again in {locked} minute(s)."));

                UsersModel usersModel = _usersManager.AuthenticateUser(model?.Username, model?.Password);
                if (usersModel == null) AuthGuard.RecordFailure(model?.Username);
                else AuthGuard.RecordSuccess(model.Username);

                if (usersModel != null)
                {
                    usersModel.password_hash = null; // never echo credentials back to the client
                    string owner_name = permissionmatrixManager.GetPermissionmatrixOwnerIDByUserID(usersModel.user_id);
                    var (jwt, validFrom, validTo) = IssueSession(this, _appSettings.Secret, usersModel.user_id, usersModel.username, usersModel.role_id, owner_name);
                    return Ok(new APIResponse(ResponseCode.SUCCESS, "Bearer Token Generated", new TokenResponse
                    {
                        // the web UI signs in with the HttpOnly cookie only; API clients get the bearer token as before
                        AccessToken = SessionCookie.IsBrowserClient(Request) ? null : jwt,
                        ValidFrom = validFrom,
                        ValidTo = validTo,
                        user = usersModel
                    }));
                }

                return StatusCode(401, new APIResponse(ResponseCode.ERROR, "Invalid request"));

            }
            catch (Exception ex)
            {
                return ApiHardening.Error(this, ex);
            }
        }
    }


    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public UsersModel user { get; set; }
    }
}