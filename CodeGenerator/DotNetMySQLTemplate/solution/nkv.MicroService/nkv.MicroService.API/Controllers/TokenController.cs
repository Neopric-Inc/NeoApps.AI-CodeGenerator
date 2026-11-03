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
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
                    var day = _appSettings.TokenValidityDay;
                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new Claim[]
                        {
                    new Claim(ClaimTypes.Name, usersModel.username.ToString()),
                    new Claim(ClaimTypes.Role, usersModel.role_id.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, usersModel.user_id.ToString()),
                    new Claim(ClaimTypes.Actor, owner_name),
                    // informational only: DataGuard resolves the tenant from user_tenants on every request ("" = platform, "unassigned")
                    new Claim(DataGuard.TenantClaim, DataGuard.TenantClaimValue(usersModel.user_id, usersModel.role_id)),
                    // token version: password/role/active changes and sign-out bump it, ending existing sessions
                    new Claim(AuthGuard.VersionClaim, AuthGuard.TokenVersion(usersModel.user_id).ToString())
                        }),
                        Expires = DateTime.UtcNow.AddMinutes(AuthGuard.TokenMinutes),
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                    };
                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    if (token != null)
                    {
                        TokenResponse tokenResponse = new TokenResponse()
                        {
                            AccessToken = tokenHandler.WriteToken(token),
                            ValidFrom = token.ValidFrom,
                            ValidTo = token.ValidTo,
                            user = usersModel
                        };

                        return Ok(new APIResponse(ResponseCode.SUCCESS, "Bearer Token Generated", tokenResponse));
                    }

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