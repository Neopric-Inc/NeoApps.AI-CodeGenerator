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
        public ActionResult Post(AuthenticateModel model)
        {
            try
            {

                UsersModel usersModel = _usersManager.AuthenticateUser(model.Username, model.Password);

                //Note: Implement your own logic to get username/passoward and validate
                if (usersModel != null)
                {
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
                    new Claim(ClaimTypes.Actor, owner_name)
                        }),
                        Expires = DateTime.UtcNow.AddDays(day),
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
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, ex.Message));
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