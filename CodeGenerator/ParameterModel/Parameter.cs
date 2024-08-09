using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoCodeAppGenerator.ParameterModel
{
    public class ApiParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }
        public string Description { get; set; }
    }

    public class Property
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }
        public string Description { get; set; }
    }

    public class Join
    {
        public string Type { get; set; }
        public string Table { get; set; }
        public string On { get; set; }
    }

    public class Condition
    {
        public string Table { get; set; }
        public string Column { get; set; }
        public string Operator { get; set; }
        public string Parameter { get; set; }
    }

    public class DataBaseQuery
    {
        public string Type { get; set; }
        public List<string> Tables { get; set; }
        public List<string> Columns { get; set; }
        public List<Join> Joins { get; set; }
        public List<Condition> Conditions { get; set; }
        public List<string> GroupBy { get; set; }
        public List<string> OrderBy { get; set; }
        public string Limit { get; set; }
    }

    public class ApiFlow
    {
        public string Endpoint { get; set; }
        public string Method { get; set; }
        public string Description { get; set; }
        public List<ApiParameter> Parameters { get; set; }
        public DTO DTO { get; set; }

        public DataBaseQuery Query { get; set; }
    }
    public class DTO
    {
        public string Name { get; set; }
        public List<Property> Properties { get; set; }
    }

    public class GenerateSql
    {
        /*public static string generateSQL(ApiFlow apiFlow)
        {
            var baseQuery = $"{apiFlow.Query.Type.ToUpper()} {string.Join(", ", apiFlow.Query.Columns)} FROM {string.Join(", ", apiFlow.Query.Tables)}";

            var joinStatements = string.Join(" ", apiFlow.Query.Joins.Select(j => $"{j.Type.ToUpper()} JOIN {j.Table} ON {j.On}"));

            var conditionStatements = string.Join(" AND ", apiFlow.Query.Conditions.Select(c => $"{c.Table}.{c.Column} {c.Operator} @{c.Parameter}"));

            var groupByStatement = apiFlow.Query.GroupBy.Any() ? $"GROUP BY {string.Join(", ", apiFlow.Query.GroupBy)}" : "";

            var orderByStatement = apiFlow.Query.OrderBy.Any() ? $"ORDER BY {string.Join(", ", apiFlow.Query.OrderBy)}" : "";

            var limitStatement = !string.IsNullOrEmpty(apiFlow.Query.Limit) ? $"LIMIT {apiFlow.Query.Limit}" : "";

            return $"{baseQuery} {joinStatements} WHERE {conditionStatements} {groupByStatement} {orderByStatement} {limitStatement};";
        }*/

        public static string generateSQL(ApiFlow apiFlow)
        {
            var fromTables = new List<string>(apiFlow.Query.Tables);

            var joinStatements = apiFlow.Query.Joins.Count > 0 ? string.Join(" ", apiFlow.Query.Joins.Select(j => $"{j.Type.ToUpper()} JOIN {j.Table} ON {j.On}")) : "";

            // Remove tables from fromTables that are already present in joinTables
            fromTables.RemoveAll(table => apiFlow.Query.Joins.Any(join => join.Table == table));

            var fromClause = fromTables.Any() ? $"FROM {string.Join(", ", fromTables)}" : "";

            var conditionStatements = apiFlow.Query.Conditions.Count > 0 ? $"WHERE {string.Join(" AND ", apiFlow.Query.Conditions.Select(c => $"{c.Table}.{c.Column} {c.Operator} @{c.Parameter}"))}" : "";

            var groupByStatement = apiFlow.Query.GroupBy.Any() ? $"GROUP BY {string.Join(", ", apiFlow.Query.GroupBy)}" : "";

            var orderByStatement = apiFlow.Query.OrderBy.Any() ? $"ORDER BY {string.Join(", ", apiFlow.Query.OrderBy)}" : "";

            var limitStatement = !string.IsNullOrEmpty(apiFlow.Query.Limit) ? $"LIMIT {apiFlow.Query.Limit}" : "";

            // Combine all the query parts
            var sqlQuery = $"{apiFlow.Query.Type.ToUpper()} {string.Join(", ", apiFlow.Query.Columns)} {fromClause} {joinStatements} {conditionStatements} {groupByStatement} {orderByStatement} {limitStatement};";

            return sqlQuery;
        }

        public static string GenerateDTO(ApiFlow apiFlow)
        {
            var dtoProperties = string.Join("\n", apiFlow.DTO.Properties.Select(p =>
            {
                string requiredAttribute = p.Required ? "[Required]" : "";
                return $"    {requiredAttribute}\n    public {p.Type} {p.Name} {{ get; set; }}";
            }));

            return $"public class {apiFlow.DTO.Name}Model\n{{\n{dtoProperties}\n}}";
        }



    }



}
