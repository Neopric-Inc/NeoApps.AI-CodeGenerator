using System;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using nkv.MicroService.Manager.Interface;
using nkv.MicroService.Model;
using nkv.MicroService.Utility;
using log4net;
using System.Collections.Generic;

namespace nkv.MicroService.API.Controllers
{
    [Authorize]
    [ApiController]
    public class EntitiesController : ControllerBase
    {
        ILog log4Net;
        IEntitiesManager Manager;
        ValidationResult ValidationResult;
        public EntitiesController(IEntitiesManager manager)
        {
            log4Net = this.Log<EntitiesController>();
            Manager = manager;
            ValidationResult = new ValidationResult();
        }
        [HttpGet]
        [Route(APIEndpoint.DefaultRoute)]
        public ActionResult Get(int page = 1, int itemsPerPage = 100, string orderBy = null)
        {
            try
            {
                if (page <= 0)
                {
                    ValidationResult.AddFieldError("Id", "Invalid page number");
                }
                if (ValidationResult.IsError)
                {
                    return BadRequest(new APIResponse(ResponseCode.ERROR, "Validation failed", ValidationResult));
                }
                List<OrderByModel> orderModelList = UtilityCommon.ConvertStringOrderToOrderModel(orderBy);
                return Ok(Manager.GetEntities(page, itemsPerPage, orderModelList));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, "Exception", ex.Message));
            }
        }
        [HttpGet]
        [Route(APIEndpoint.DefaultRoute + "/search")]
        public ActionResult Search(string searchKey, int page = 1, int itemsPerPage = 100, string orderBy = null)
        {
            try
            {
                if (string.IsNullOrEmpty(searchKey))
                {
                    ValidationResult.AddEmptyFieldError("SearchKey");
                }
                else if (!string.IsNullOrEmpty(searchKey) && searchKey.Length < 3)
                {
                    ValidationResult.AddFieldError("SearchKey", "Minimum 3 chracters required for search");
                }
                if (page <= 0)
                {
                    ValidationResult.AddFieldError("Id", "Invalid page number");
                }
                if (ValidationResult.IsError)
                {
                    return BadRequest(new APIResponse(ResponseCode.ERROR, "Validation failed", ValidationResult));
                }
                List<OrderByModel> orderModelList = UtilityCommon.ConvertStringOrderToOrderModel(orderBy);
                return Ok(Manager.SearchEntities(searchKey, page, itemsPerPage, orderModelList));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, "Exception", ex.Message));
            }
        }

        [HttpGet]
        [Route(APIEndpoint.DefaultRoute + "/{entity_id}")]
        public ActionResult GetById(int entity_id)
        {
            try
            {
                if (entity_id <= 0) { ValidationResult.AddEmptyFieldError("entity_id"); }

                if (ValidationResult.IsError)
                {
                    return BadRequest(new APIResponse(ResponseCode.ERROR, "Validation failed", ValidationResult));
                }
                return Ok(Manager.GetEntitiesByID(entity_id));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, "Exception", ex.Message));
            }
        }

        [HttpPost]
        [Route(APIEndpoint.DefaultRoute)]
        public ActionResult Post(EntitiesModel model)
        {
            try
            {
                string token = Request.Headers["Authorization"];

                // Check if the token is present
                if (string.IsNullOrEmpty(token))
                {
                    return StatusCode(401, new APIResponse(ResponseCode.UNAUTHORIZED, "Unauthorized", "Token not provided"));
                }

                // Extract the actual token from the Authorization header (e.g., "Bearer token")
                token = token.Replace("Bearer ", "");
                return Ok(Manager.AddEntitiesWithToken(model, token));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, "Exception", ex.Message));
            }
        }

        [HttpPut]
        [Route(APIEndpoint.DefaultRoute + "/{entity_id}")]
        public ActionResult Put(int entity_id, EntitiesModel model)
        {
            try
            {
                string token = Request.Headers["Authorization"];

                // Check if the token is present
                if (string.IsNullOrEmpty(token))
                {
                    return StatusCode(401, new APIResponse(ResponseCode.UNAUTHORIZED, "Unauthorized", "Token not provided"));
                }

                // Extract the actual token from the Authorization header (e.g., "Bearer token")
                token = token.Replace("Bearer ", "");
                if (entity_id <= 0) { ValidationResult.AddEmptyFieldError("entity_id"); }

                if (ValidationResult.IsError)
                {
                    return BadRequest(new APIResponse(ResponseCode.ERROR, "Validation failed", ValidationResult));
                }
                return Ok(Manager.UpdateEntitiesWithToken(entity_id, model, token));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, "Exception", ex.Message));
            }
        }
        [HttpDelete]
        [Route(APIEndpoint.DefaultRoute + "/{entity_id}")]
        public ActionResult Delete(int entity_id)
        {
            try
            {
                string token = Request.Headers["Authorization"];

                // Check if the token is present
                if (string.IsNullOrEmpty(token))
                {
                    return StatusCode(401, new APIResponse(ResponseCode.UNAUTHORIZED, "Unauthorized", "Token not provided"));
                }

                // Extract the actual token from the Authorization header (e.g., "Bearer token")
                token = token.Replace("Bearer ", "");
                if (entity_id <= 0) { ValidationResult.AddEmptyFieldError("entity_id"); }
                if (ValidationResult.IsError)
                {
                    return BadRequest(new APIResponse(ResponseCode.ERROR, "Validation failed", ValidationResult));
                }
                return Ok(Manager.DeleteEntitiesWithToken(entity_id, token));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, "Exception", ex.Message));
            }
        }
        [HttpDelete]
        [Route(APIEndpoint.DefaultRoute + "/Multiple")]
        public ActionResult DeleteMultiple(List<DeleteMultipleModel> deleteParam, string andOr = "AND")
        {
            try
            {
                string token = Request.Headers["Authorization"];

                // Check if the token is present
                if (string.IsNullOrEmpty(token))
                {
                    return StatusCode(401, new APIResponse(ResponseCode.UNAUTHORIZED, "Unauthorized", "Token not provided"));
                }

                // Extract the actual token from the Authorization header (e.g., "Bearer token")
                token = token.Replace("Bearer ", "");
                if (deleteParam == null) { ValidationResult.AddEmptyFieldError("DeleteParam"); }
                else if (deleteParam.Count <= 0) { ValidationResult.AddEmptyFieldError("DeleteParam"); }
                if (string.IsNullOrEmpty(andOr)) { ValidationResult.AddEmptyFieldError("andOr"); }
                else if (andOr.ToUpper() == "OR" || andOr.ToUpper() == "AND") { }
                else { ValidationResult.AddFieldError("andOr", "Invalid value(only OR / AND allowed)"); }
                if (ValidationResult.IsError)
                {
                    return BadRequest(new APIResponse(ResponseCode.ERROR, "Validation failed", ValidationResult));
                }
                return Ok(Manager.DeleteMultipleEntitiesWithToken(deleteParam, andOr.ToUpper(), token));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, "Exception", ex.Message));
            }
        }

        [HttpPost]
        [Route(APIEndpoint.DefaultRoute + "/filter")]
        public ActionResult Filter(List<FilterModel> filterColumnList, string andOr = "AND", int page = 1, int itemsPerPage = 100, string orderBy = null)
        {
            try
            {
                if (string.IsNullOrEmpty(andOr))
                {
                    ValidationResult.AddEmptyFieldError("andOr");
                }
                else if (andOr.ToUpper() == "OR" || andOr.ToUpper() == "AND") { }
                else { ValidationResult.AddFieldError("andOr", "Invalid value(only OR / AND allowed)"); }
                if (page <= 0)
                {
                    ValidationResult.AddFieldError("Id", "Invalid page number");
                }
                if (filterColumnList == null)
                {
                    ValidationResult.AddFieldError("FilterColumnList", "Filter Column Required");
                }
                else if (filterColumnList != null && filterColumnList.Count <= 0)
                {
                    ValidationResult.AddFieldError("FilterColumnList", "Filter Column Required");
                }

                if (ValidationResult.IsError)
                {
                    return BadRequest(new APIResponse(ResponseCode.ERROR, "Validation failed", ValidationResult));
                }
                List<OrderByModel> orderModelList = UtilityCommon.ConvertStringOrderToOrderModel(orderBy);
                return Ok(Manager.FilterEntities(filterColumnList, andOr, page, itemsPerPage, orderModelList));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, "Exception", ex.Message));
            }
        }
    }
}
