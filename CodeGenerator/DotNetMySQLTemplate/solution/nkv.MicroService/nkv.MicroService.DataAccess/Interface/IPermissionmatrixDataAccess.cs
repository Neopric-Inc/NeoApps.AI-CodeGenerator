using nkv.MicroService.Model;
using nkv.MicroService.Utility;
using System.Collections.Generic;

namespace nkv.MicroService.DataAccess.Interface
{
    public interface IPermissionmatrixDataAccess
    {
        List<PermissionMatrixModel> GetAllPermissionmatrix(int page, int itemsPerPage, List<OrderByModel> orderBy);
        List<PermissionMatrixModel> SearchPermissionmatrix(string searchKey, int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null);
        List<PermissionMatrixModel> FilterPermissionmatrix(List<FilterModel> filterModels, string andOr, int page, int itemsPerPage, List<OrderByModel> orderBy);
        PermissionMatrixModel GetPermissionmatrixByID(int permission_id);
        PermissionMatrixModel GetPermissionmatrixByUserRoleID(int role_id, int permission_id, int entity_id);
        int GetAllTotalRecordPermissionmatrix();
        int GetSearchTotalRecordPermissionmatrix(string searchKey);
        bool UpdatePermissionmatrix(PermissionMatrixModel model);
        int GetFilterTotalRecordPermissionmatrix(List<FilterModel> filterBy, string andOr);
        long AddPermissionmatrix(PermissionMatrixModel model);
        bool DeletePermissionmatrix(int permission_id);
        bool DeleteMultiplePermissionmatrix(List<DeleteMultipleModel> deleteParam, string andOr);
        string GetPermissionmatrixOwnerIDByUserID(int user_id);

    }
}
