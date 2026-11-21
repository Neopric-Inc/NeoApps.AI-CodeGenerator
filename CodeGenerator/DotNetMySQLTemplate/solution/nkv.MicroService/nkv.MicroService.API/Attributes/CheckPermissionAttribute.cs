using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using nkv.MicroService.Manager.Interface;
using nkv.MicroService.Model;
using System.Security.Claims;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace nkv.MicroService.API.Attributes
{
    public class CheckPermissionAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string _entityName;
        private readonly string _actionType;
        private IDatabase _db;

        public CheckPermissionAttribute(string entityName, string actionType)
        {
            _entityName = entityName.ToLower();
            _actionType = actionType;
        }

        // Without Redis, decisions are kept in memory for a minute per instance (a permission change reaches every
        // instance within that minute); with Redis they are shared and cleared when permissions change.
        static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string value, DateTime until)> Local = new();
        static readonly TimeSpan LocalTtl = TimeSpan.FromSeconds(60);

        private string CacheGet(string key)
        {
            if (_db != null) return _db.StringGet(key);
            return Local.TryGetValue(key, out var e) && e.until > DateTime.UtcNow ? e.value : null;
        }

        private void CacheSet(string key, string value)
        {
            if (_db != null) { _db.StringSet(key, value); return; }
            Local[key] = (value, DateTime.UtcNow + LocalTtl);
        }

        /// <summary>Drops this instance's in-memory decisions for a workspace (Redis entries are cleared by AccessController).</summary>
        public static void ClearLocal(string tenant)
        {
            foreach (var k in Local.Keys) if (k.Contains(":" + tenant + ":")) Local.TryRemove(k, out _);
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContextAccessor = context.HttpContext.RequestServices.GetService<IHttpContextAccessor>();
            var entitiesManager = context.HttpContext.RequestServices.GetService<IEntitiesManager>();
            var permissionmatrixManager = context.HttpContext.RequestServices.GetService<IPermissionmatrixManager>();
            var redis = context.HttpContext.RequestServices.GetService<IConnectionMultiplexer>();
            _db = redis?.GetDatabase(); // null: no Redis (serverless) -> short in-memory cache

            var claimsIdentity = httpContextAccessor.HttpContext.User.Identity as ClaimsIdentity;

            string role = claimsIdentity.FindFirst(ClaimTypes.Role)?.Value;
            string username = claimsIdentity.FindFirst(ClaimTypes.Name)?.Value;
            string user_id = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string owner_name = claimsIdentity.FindFirst(ClaimTypes.Actor)?.Value;

            int role_id = int.Parse(role);
            int userID = int.Parse(user_id);
            context.HttpContext.Items["OwnerName"] = owner_name;
            string permissionKey = GeneratePermissionKey(username, owner_name, role, _entityName, _actionType);
            string cachedPermission;

            bool isAuthorized = false;
            bool makeDatabaseCall;

            // To get last update time 
            string timeKey = $"{permissionKey}:time";
            string cachedTime = CacheGet(timeKey);

            // If cachedTime is null, then its first visit of user 
            if (!string.IsNullOrEmpty(cachedTime))
            {
                DateTime currentDateTime = DateTime.Now;
                DateTime lastUpdateTime = DateTime.ParseExact(cachedTime, "yyyy-MM-dd HH:mm:ss", null);

                TimeSpan timeDifference = currentDateTime - lastUpdateTime;

                //After 30 minutes, we need to update redis
                if (timeDifference > TimeSpan.FromMinutes(30))
                {
                    makeDatabaseCall = true;
                }
                else
                {
                    cachedPermission = CacheGet(permissionKey);


                    if ((!string.IsNullOrEmpty(cachedPermission)) && (cachedPermission == "1"))
                    {
                        isAuthorized = true;
                        makeDatabaseCall = false;

                    }
                    else
                    {
                        makeDatabaseCall = true;
                    }
                }
            }
            else
            {
                makeDatabaseCall = true;
            }

            if (makeDatabaseCall)
            {
                // If the permission doesn't exist in cache, fetch it from the database
                EntitiesModel entitiesModel = entitiesManager.GetEntitiesByName(_entityName);
                PermissionMatrixModel PermissionMatrixModel = null;
                if (entitiesModel != null)
                {
                    int entity_id = entitiesModel.entity_id;
                    PermissionMatrixModel = permissionmatrixManager.HasEntityPermission(role_id, userID, entity_id);
                }

                if (PermissionMatrixModel != null)
                {
                    switch (_actionType.ToLower())
                    {
                        case "get":
                            isAuthorized = PermissionMatrixModel.can_read == 1;
                            break;
                        case "post":
                            isAuthorized = PermissionMatrixModel.can_write == 1;
                            break;
                        case "put":
                            isAuthorized = PermissionMatrixModel.can_update == 1;
                            break;
                        case "delete":
                            isAuthorized = PermissionMatrixModel.can_delete == 1;
                            break;
                        default:
                            isAuthorized = false;
                            break;
                    }
                }
                else
                {
                    isAuthorized = false;
                }

                // Store the permission in the cache for future requests
                CacheSet(permissionKey, isAuthorized ? "1" : "0");
                DateTime currentDateTime = DateTime.Now;
                CacheSet(timeKey, currentDateTime.ToString("yyyy-MM-dd HH:mm:ss"));

            }

            // If permission check fails, short-circuit the request:
            if (!isAuthorized)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // If permission check succeeds, proceed with the next action:
            await next();
        }

        private string GeneratePermissionKey(string username, string owner_name, string role, string entity, string action)
        {
            return $"{username}:{owner_name}:{role}:{entity}:{action}";
        }
    }

}