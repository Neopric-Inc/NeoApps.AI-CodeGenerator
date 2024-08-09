using nkv.MicroService.Manager.RabitMQAPI.API;
using nkv.MicroService.DataAccess.Interface;
using nkv.MicroService.Manager.Interface;
using nkv.MicroService.Model;
using nkv.MicroService.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace nkv.MicroService.Manager.Impl
{
    public class PermissionmatrixManager : IPermissionmatrixManager
    {
        private readonly IPermissionmatrixDataAccess DataAccess = null;
        private readonly IRabitMQAsyncProducer _rabitMQAsyncProducer;
        public PermissionmatrixManager(IPermissionmatrixDataAccess dataAccess, IRabitMQAsyncProducer rabitMQAsyncProducer)
        {
            DataAccess = dataAccess;
            _rabitMQAsyncProducer = rabitMQAsyncProducer;
        }

        public APIResponse GetPermissionmatrix(int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var result = DataAccess.GetAllPermissionmatrix(page, itemsPerPage, orderBy);
            if (result != null && result.Count > 0)
            {
                var totalRecords = DataAccess.GetAllTotalRecordPermissionmatrix();
                var response = new { records = result, pageNumber = page, pageSize = itemsPerPage, totalRecords = totalRecords };
                return new APIResponse(ResponseCode.SUCCESS, "Record Found", response);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "No Record Found");
            }
        }

        public APIResponse GetPermissionmatrixByID(int permission_id)
        {
            var result = DataAccess.GetPermissionmatrixByID(permission_id);
            if (result != null)
            {
                return new APIResponse(ResponseCode.SUCCESS, "Record Found", result);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "No Record Found");
            }
        }

        public PermissionmatrixModel HasEntityPermission(int role_id, int user_id, int entity_id)
        {
            var result = DataAccess.GetPermissionmatrixByUserRoleID(role_id, user_id, entity_id);
            if (result != null)
            {
                return result;
            }
            else
            {
                return null;
            }
        }


        public APIResponse UpdatePermissionmatrixWithToken(int permission_id, PermissionmatrixModel model, string token)
        {
            model.permission_id = permission_id;

            var result = DataAccess.UpdatePermissionmatrix(model);
            if (result)
            {
                Dictionary<string, int> primary_key = new Dictionary<string, int>();
                primary_key.Add("permission_id", permission_id);
                _rabitMQAsyncProducer.SendAsyncMessage(model, primary_key, model.GetType().Name, token);
                return new APIResponse(ResponseCode.SUCCESS, "Record Updated");
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "Record Not Updated");
            }
        }

        public string GetPermissionmatrixOwnerIDByUserID(int user_id)
        {
            var result = DataAccess.GetPermissionmatrixOwnerIDByUserID(user_id);
            if (result != null)
            {
                return result;
            }
            else
            {
                return null;
            }
        }
        public APIResponse AddPermissionmatrixWithToken(PermissionmatrixModel model, string token)
        {
            var result = DataAccess.AddPermissionmatrix(model);
            if (result > 0)
            {
                model.permission_id = Convert.ToInt32(result);

                _rabitMQAsyncProducer.SendAsyncMessage(model, model.GetType().Name, token);
                return new APIResponse(ResponseCode.SUCCESS, "Record Created", result);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "Record Not Created");
            }
        }

        public APIResponse SearchPermissionmatrix(string searchKey, int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var result = DataAccess.SearchPermissionmatrix(searchKey, page, itemsPerPage, orderBy);
            if (result != null && result.Count > 0)
            {
                var totalRecords = DataAccess.GetSearchTotalRecordPermissionmatrix(searchKey);
                var response = new { records = result, pageNumber = page, pageSize = itemsPerPage, totalRecords = totalRecords };
                return new APIResponse(ResponseCode.SUCCESS, "Record Found", response);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "No Record Found");
            }
        }

        public APIResponse DeletePermissionmatrixWithToken(int permission_id, string token)
        {
            var result = DataAccess.DeletePermissionmatrix(permission_id);
            if (result)
            {
                Dictionary<string, int> primary_key = new Dictionary<string, int>();
                primary_key.Add("permission_id", permission_id);
                var className = GetType().Name.Replace("Manager", "Model");
                _rabitMQAsyncProducer.SendAsyncMessage(primary_key, className, token);
                return new APIResponse(ResponseCode.SUCCESS, "Record Deleted");
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "Record Not Deleted");
            }
        }

        public APIResponse DeleteMultiplePermissionmatrixWithToken(List<DeleteMultipleModel> deleteParam, string andOr, string token)
        {
            var result = DataAccess.DeleteMultiplePermissionmatrix(deleteParam, andOr);
            if (result)
            {
                _rabitMQAsyncProducer.SendAsyncMessage(deleteParam, GetType().Name, token);
                return new APIResponse(ResponseCode.SUCCESS, "Record Deleted");
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "Record Not Deleted");
            }
        }

        public APIResponse FilterPermissionmatrix(List<FilterModel> filterModels, string andOr, int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var result = DataAccess.FilterPermissionmatrix(filterModels, andOr, page, itemsPerPage, orderBy);
            if (result != null && result.Count > 0)
            {
                var totalRecords = DataAccess.GetFilterTotalRecordPermissionmatrix(filterModels, andOr);
                var response = new { records = result, pageNumber = page, pageSize = itemsPerPage, totalRecords = totalRecords };
                return new APIResponse(ResponseCode.SUCCESS, "Record Found", response);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "No Record Found");
            }
        }
    }
}
