using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;

namespace nkv.MicroService.Utility
{
    public interface ICurrentUserService
    {
        string GetClaim(string token, string claimType);
    }
    public class CurrentUserService : ICurrentUserService
    {//        var accessToken = await HttpContext.GetTokenAsync("access_token");
        private IHttpContextAccessor HttpContextAccessor;
        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            HttpContextAccessor = httpContextAccessor;
        }
        public string GetClaim(string token, string claimType)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            var stringClaimValue = securityToken.Claims.First(claim => claim.Type == claimType).Value;
            return stringClaimValue;
        }
        
    }
}

