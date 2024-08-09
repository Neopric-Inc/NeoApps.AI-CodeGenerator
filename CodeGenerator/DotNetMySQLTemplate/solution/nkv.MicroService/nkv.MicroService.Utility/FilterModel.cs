using System;
using System.Collections.Generic;
using System.Text;

namespace nkv.MicroService.Utility
{
    public class FilterModel
    {
        public string ColumnName { get; set; }
        public FilterConditionEnum ColumnCondition { get; set; }
        public string ColumnValue { get; set; }
    }
    // public enum FilterConditionEnum
    // {
    //     Contains = 1,
    //     NotContains = 2,
    //     Equals = 3,
    //     GreaterThan = 4,
    //     GreaterThanEqual = 5,
    //     LessThan = 6,
    //     LessThanEqual = 7,
    // }
    public enum FilterConditionEnum
    {
        Contains = 1,
        NotContains = 2,
        GreaterThan = 3,
        LessThan = 4,
        Equals = 5,
        GreaterThanEqual = 6,
        LessThanEqual = 7,
    }
}
