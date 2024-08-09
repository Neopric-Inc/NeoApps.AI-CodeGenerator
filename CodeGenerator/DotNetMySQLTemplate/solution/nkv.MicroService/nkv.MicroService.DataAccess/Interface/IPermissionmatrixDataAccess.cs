using nkv.MicroService.Model;
using nkv.MicroService.Utility;
using System.Collections.Generic;

namespace nkv.MicroService.DataAccess.Interface
{
    public interface IPermissionmatrixDataAccess
    {
        List<PermissionmatrixModel> GetAllPermissionmatrix(int page, int itemsPerPage, List<OrderByModel> orderBy);
        List<PermissionmatrixModel> SearchPermissionmatrix(string searchKey, int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null);
        List<PermissionmatrixModel> FilterPermissionmatrix(List<FilterModel> filterModels, string andOr, int page, int itemsPerPage, List<OrderByModel> orderBy);
        PermissionmatrixModel GetPermissionmatrixByID(int permission_id);
        PermissionmatrixModel GetPermissionmatrixByUserRoleID(int role_id, int permission_id, int entity_id);
        int GetAllTotalRecordPermissionmatrix();
        int GetSearchTotalRecordPermissionmatrix(string searchKey);
        bool UpdatePermissionmatrix(PermissionmatrixModel model);
        int GetFilterTotalRecordPermissionmatrix(List<FilterModel> filterBy, string andOr);
        long AddPermissionmatrix(PermissionmatrixModel model);
        bool DeletePermissionmatrix(int permission_id);
        bool DeleteMultiplePermissionmatrix(List<DeleteMultipleModel> deleteParam, string andOr);
        string GetPermissionmatrixOwnerIDByUserID(int user_id);

    }
}
