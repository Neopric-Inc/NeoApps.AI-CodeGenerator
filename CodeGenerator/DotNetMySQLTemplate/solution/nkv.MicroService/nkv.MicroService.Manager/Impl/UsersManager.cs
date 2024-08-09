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
    public class UsersManager : IUsersManager
    {
        private readonly IUsersDataAccess DataAccess = null;
        private readonly IRabitMQAsyncProducer _rabitMQAsyncProducer;
        public UsersManager(IUsersDataAccess dataAccess, IRabitMQAsyncProducer rabitMQAsyncProducer)
        {
            DataAccess = dataAccess;
            _rabitMQAsyncProducer = rabitMQAsyncProducer;
        }

        public APIResponse GetUsers(int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var result = DataAccess.GetAllUsers(page, itemsPerPage, orderBy);
            if (result != null && result.Count > 0)
            {
                var totalRecords = DataAccess.GetAllTotalRecordUsers();
                var response = new { records = result, pageNumber = page, pageSize = itemsPerPage, totalRecords = totalRecords };
                return new APIResponse(ResponseCode.SUCCESS, "Record Found", response);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "No Record Found");
            }
        }

        public APIResponse GetUsersByID(int user_id)
        {
            var result = DataAccess.GetUsersByID(user_id);
            if (result != null)
            {
                return new APIResponse(ResponseCode.SUCCESS, "Record Found", result);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "No Record Found");
            }
        }

        public UsersModel AuthenticateUser(string username, string password)
        {
            var result = DataAccess.AuthenticateUser(username, password);
            if (result != null)
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public APIResponse UpdateUsersWithToken(int user_id, UsersModel model, string token)
        {
            model.user_id = user_id;

            var result = DataAccess.UpdateUsers(model);
            if (result)
            {
                Dictionary<string, int> primary_key = new Dictionary<string, int>();
                primary_key.Add("user_id", user_id);
                _rabitMQAsyncProducer.SendAsyncMessage(model, primary_key, model.GetType().Name, token);
                return new APIResponse(ResponseCode.SUCCESS, "Record Updated");
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "Record Not Updated");
            }
        }

        public APIResponse AddUsersWithToken(UsersModel model, string token)
        {
            var result = DataAccess.AddUsers(model);
            if (result > 0)
            {
                model.user_id = Convert.ToInt32(result);

                _rabitMQAsyncProducer.SendAsyncMessage(model, model.GetType().Name, token);
                return new APIResponse(ResponseCode.SUCCESS, "Record Created", result);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "Record Not Created");
            }
        }

        public APIResponse SearchUsers(string searchKey, int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var result = DataAccess.SearchUsers(searchKey, page, itemsPerPage, orderBy);
            if (result != null && result.Count > 0)
            {
                var totalRecords = DataAccess.GetSearchTotalRecordUsers(searchKey);
                var response = new { records = result, pageNumber = page, pageSize = itemsPerPage, totalRecords = totalRecords };
                return new APIResponse(ResponseCode.SUCCESS, "Record Found", response);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "No Record Found");
            }
        }

        public APIResponse DeleteUsersWithToken(int user_id, string token)
        {
            var result = DataAccess.DeleteUsers(user_id);
            if (result)
            {
                Dictionary<string, int> primary_key = new Dictionary<string, int>();
                primary_key.Add("user_id", user_id);
                var className = GetType().Name.Replace("Manager", "Model");
                _rabitMQAsyncProducer.SendAsyncMessage(primary_key, className, token);
                return new APIResponse(ResponseCode.SUCCESS, "Record Deleted");
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "Record Not Deleted");
            }
        }

        public APIResponse DeleteMultipleUsersWithToken(List<DeleteMultipleModel> deleteParam, string andOr, string token)
        {
            var result = DataAccess.DeleteMultipleUsers(deleteParam, andOr);
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

        public APIResponse FilterUsers(List<FilterModel> filterModels, string andOr, int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var result = DataAccess.FilterUsers(filterModels, andOr, page, itemsPerPage, orderBy);
            if (result != null && result.Count > 0)
            {
                var totalRecords = DataAccess.GetFilterTotalRecordUsers(filterModels, andOr);
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
