using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoCodeAppGenerator.ParameterModel
{
    public class TransactionalParameterModel
    {
        public string ParentEntity { get; set; } // The parent entity

        public TransactionalModel transactional { get; set; }
    }
    public class TransactionalModel
    {
        public List<string> Sequence { get; set; }
        public Dictionary<string, string> Relations { get; set; }
        public List<PreConditionModel> PreConditions { get; set; }
    }


    public class PreConditionModel
    {
        public string Entity { get; set; }
        public List<ConditionModel> Conditions { get; set; }
    }

    public class ConditionModel
    {
        public string Type { get; set; }
        public LogicModel Logic { get; set; }
        public List<ActionModel> Then { get; set; }
        public List<ActionModel> Else { get; set; }
    }

    public class LogicModel
    {
        public string Field { get; set; }
        public string Operator { get; set; }
        public QueryModel Query { get; set; }
        public ConditionModel NestedIf { get; set; } // Recursive for nested conditions
    }

    public class ActionModel
    {
        public string Type { get; set; }
        public string Field { get; set; }
        public string Operator { get; set; }
        public QueryModel Query { get; set; }
    }

    public class QueryModel
    {
        public List<string> Select { get; set; }
        public string From { get; set; }
        public List<WhereConditionModel> Where { get; set; }
    }

    public class WhereConditionModel
    {
        public string Field { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
    }


}
