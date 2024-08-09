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
    public class EntitiesManager : IEntitiesManager
    {
        private readonly IEntitiesDataAccess DataAccess = null;
        private readonly IRabitMQAsyncProducer _rabitMQAsyncProducer;
        public EntitiesManager(IEntitiesDataAccess dataAccess, IRabitMQAsyncProducer rabitMQAsyncProducer)
        {
            DataAccess = dataAccess;
            _rabitMQAsyncProducer = rabitMQAsyncProducer;
        }

        public APIResponse GetEntities(int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var result = DataAccess.GetAllEntities(page, itemsPerPage, orderBy);
            if (result != null && result.Count > 0)
            {
                var totalRecords = DataAccess.GetAllTotalRecordEntities();
                var response = new { records = result, pageNumber = page, pageSize = itemsPerPage, totalRecords = totalRecords };
                return new APIResponse(ResponseCode.SUCCESS, "Record Found", response);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "No Record Found");
            }
        }

        public APIResponse GetEntitiesByID(int entity_id)
        {
            var result = DataAccess.GetEntitiesByID(entity_id);
            if (result != null)
            {
                return new APIResponse(ResponseCode.SUCCESS, "Record Found", result);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "No Record Found");
            }
        }
        public EntitiesModel GetEntitiesByName(string entity_id)
        {
            var result = DataAccess.GetEntitiesByName(entity_id);
            if (result != null)
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public APIResponse UpdateEntitiesWithToken(int entity_id, EntitiesModel model, string token)
        {
            model.entity_id = entity_id;

            var result = DataAccess.UpdateEntities(model);
            if (result)
            {
                Dictionary<string, int> primary_key = new Dictionary<string, int>();
                primary_key.Add("entity_id", entity_id);
                _rabitMQAsyncProducer.SendAsyncMessage(model, primary_key, model.GetType().Name, token);
                return new APIResponse(ResponseCode.SUCCESS, "Record Updated");
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "Record Not Updated");
            }
        }

        public APIResponse AddEntitiesWithToken(EntitiesModel model, string token)
        {
            var result = DataAccess.AddEntities(model);
            if (result > 0)
            {
                model.entity_id = Convert.ToInt32(result);

                _rabitMQAsyncProducer.SendAsyncMessage(model, model.GetType().Name, token);
                return new APIResponse(ResponseCode.SUCCESS, "Record Created", result);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "Record Not Created");
            }
        }

        public APIResponse SearchEntities(string searchKey, int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var result = DataAccess.SearchEntities(searchKey, page, itemsPerPage, orderBy);
            if (result != null && result.Count > 0)
            {
                var totalRecords = DataAccess.GetSearchTotalRecordEntities(searchKey);
                var response = new { records = result, pageNumber = page, pageSize = itemsPerPage, totalRecords = totalRecords };
                return new APIResponse(ResponseCode.SUCCESS, "Record Found", response);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "No Record Found");
            }
        }

        public APIResponse DeleteEntitiesWithToken(int entity_id, string token)
        {
            var result = DataAccess.DeleteEntities(entity_id);
            if (result)
            {
                Dictionary<string, int> primary_key = new Dictionary<string, int>();
                primary_key.Add("entity_id", entity_id);
                var className = GetType().Name.Replace("Manager", "Model");
                _rabitMQAsyncProducer.SendAsyncMessage(primary_key, className, token);
                return new APIResponse(ResponseCode.SUCCESS, "Record Deleted");
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, "Record Not Deleted");
            }
        }

        public APIResponse DeleteMultipleEntitiesWithToken(List<DeleteMultipleModel> deleteParam, string andOr, string token)
        {
            var result = DataAccess.DeleteMultipleEntities(deleteParam, andOr);
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

        public APIResponse FilterEntities(List<FilterModel> filterModels, string andOr, int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var result = DataAccess.FilterEntities(filterModels, andOr, page, itemsPerPage, orderBy);
            if (result != null && result.Count > 0)
            {
                var totalRecords = DataAccess.GetFilterTotalRecordEntities(filterModels, andOr);
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
