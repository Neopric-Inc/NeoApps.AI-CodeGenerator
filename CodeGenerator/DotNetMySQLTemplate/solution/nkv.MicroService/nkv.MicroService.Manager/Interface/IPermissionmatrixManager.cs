using nkv.MicroService.Model;
using nkv.MicroService.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace nkv.MicroService.Manager.Interface
{
    public interface IPermissionmatrixManager
    {
        APIResponse GetPermissionmatrix(int page, int itemsPerPage, List<OrderByModel> orderBy);
        APIResponse SearchPermissionmatrix(string searchKey, int page, int itemsPerPage, List<OrderByModel> orderBy);
        APIResponse FilterPermissionmatrix(List<FilterModel> filterModels, string andOr, int page, int itemsPerPage, List<OrderByModel> orderBy);
        APIResponse GetPermissionmatrixByID(int permission_id);
        PermissionMatrixModel HasEntityPermission(int role_id, int user_id, int entity_id);
        string GetPermissionmatrixOwnerIDByUserID(int user_id);
        APIResponse UpdatePermissionmatrixWithToken(int permission_id, PermissionMatrixModel model, string token);
        APIResponse AddPermissionmatrixWithToken(PermissionMatrixModel model, string token);
        APIResponse DeletePermissionmatrixWithToken(int permission_id, string token);
        APIResponse DeleteMultiplePermissionmatrixWithToken(List<DeleteMultipleModel> deleteParam, string andOr, string token);
    }
}
