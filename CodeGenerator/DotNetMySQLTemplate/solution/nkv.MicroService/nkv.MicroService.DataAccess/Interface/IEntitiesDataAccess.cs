using nkv.MicroService.Model;
using nkv.MicroService.Utility;
using System.Collections.Generic;

namespace nkv.MicroService.DataAccess.Interface
{
    public interface IEntitiesDataAccess
    {
        List<EntitiesModel> GetAllEntities(int page, int itemsPerPage, List<OrderByModel> orderBy);
        List<EntitiesModel> SearchEntities(string searchKey, int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null);
        List<EntitiesModel> FilterEntities(List<FilterModel> filterModels, string andOr, int page, int itemsPerPage, List<OrderByModel> orderBy);
        EntitiesModel GetEntitiesByID(int entity_id);
        EntitiesModel GetEntitiesByName(string entity_name);
        int GetAllTotalRecordEntities();
        int GetSearchTotalRecordEntities(string searchKey);
        bool UpdateEntities(EntitiesModel model);
        int GetFilterTotalRecordEntities(List<FilterModel> filterBy, string andOr);
        long AddEntities(EntitiesModel model);
        bool DeleteEntities(int entity_id);
        bool DeleteMultipleEntities(List<DeleteMultipleModel> deleteParam, string andOr);

    }
}
