using nkv.MicroService.Model;
using nkv.MicroService.Utility;
using System.Collections.Generic;

namespace nkv.MicroService.DataAccess.Interface
{
    public interface IUsersDataAccess
    {
        List<UsersModel> GetAllUsers(int page, int itemsPerPage, List<OrderByModel> orderBy);
        List<UsersModel> SearchUsers(string searchKey, int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null);
        List<UsersModel> FilterUsers(List<FilterModel> filterModels, string andOr, int page, int itemsPerPage, List<OrderByModel> orderBy);
        UsersModel GetUsersByID(int user_id);
        UsersModel AuthenticateUser(string username, string password_hash);
        int GetAllTotalRecordUsers();
        int GetSearchTotalRecordUsers(string searchKey);
        bool UpdateUsers(UsersModel model);
        int GetFilterTotalRecordUsers(List<FilterModel> filterBy, string andOr);
        long AddUsers(UsersModel model);
        bool DeleteUsers(int user_id);
        bool DeleteMultipleUsers(List<DeleteMultipleModel> deleteParam, string andOr);

    }
}
