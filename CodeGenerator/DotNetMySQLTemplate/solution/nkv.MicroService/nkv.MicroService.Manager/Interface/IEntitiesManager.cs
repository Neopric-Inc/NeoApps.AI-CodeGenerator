using nkv.MicroService.Model;
using nkv.MicroService.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace nkv.MicroService.Manager.Interface
{
    public interface IEntitiesManager
    {
        APIResponse GetEntities(int page, int itemsPerPage, List<OrderByModel> orderBy);
        APIResponse SearchEntities(string searchKey, int page, int itemsPerPage, List<OrderByModel> orderBy);
        APIResponse FilterEntities(List<FilterModel> filterModels, string andOr, int page, int itemsPerPage, List<OrderByModel> orderBy);
        APIResponse GetEntitiesByID(int entity_id);
        EntitiesModel GetEntitiesByName(string entity_name);
        APIResponse UpdateEntitiesWithToken(int entity_id, EntitiesModel model, string token);
        APIResponse AddEntitiesWithToken(EntitiesModel model, string token);
        APIResponse DeleteEntitiesWithToken(int entity_id, string token);
        APIResponse DeleteMultipleEntitiesWithToken(List<DeleteMultipleModel> deleteParam, string andOr, string token);
    }
}
