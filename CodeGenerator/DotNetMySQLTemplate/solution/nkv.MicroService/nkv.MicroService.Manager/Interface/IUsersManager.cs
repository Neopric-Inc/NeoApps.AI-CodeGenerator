using nkv.MicroService.Model;
using nkv.MicroService.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace nkv.MicroService.Manager.Interface
{
    public interface IUsersManager
    {
        APIResponse GetUsers(int page, int itemsPerPage, List<OrderByModel> orderBy);
        APIResponse SearchUsers(string searchKey, int page, int itemsPerPage, List<OrderByModel> orderBy);
        APIResponse FilterUsers(List<FilterModel> filterModels, string andOr, int page, int itemsPerPage, List<OrderByModel> orderBy);
        APIResponse GetUsersByID(int user_id);
        UsersModel AuthenticateUser(string username, string password);
        APIResponse UpdateUsersWithToken(int user_id, UsersModel model, string token);
        APIResponse AddUsersWithToken(UsersModel model, string token);
        APIResponse DeleteUsersWithToken(int user_id, string token);
        APIResponse DeleteMultipleUsersWithToken(List<DeleteMultipleModel> deleteParam, string andOr, string token);
    }
}
