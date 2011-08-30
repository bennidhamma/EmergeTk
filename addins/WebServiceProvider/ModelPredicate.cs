using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using EmergeTk.Model;
using EmergeTk.Model.Search;

namespace EmergeTk.WebServices
{
    [RestService(ModelName = "modelPredicate", ServiceManager = typeof(AuthenticatedPostOnlyServiceManager))]
    public class ModelPredicate : AbstractRecord
    {
        public string ColumnName { get; set; }
        public object PredicateValue { get; set; }
        public FilterOperation Operation { get; set; }
        public ModelPredicate()
        {
            this.Operation = FilterOperation.Equals;
        }

        static public explicit operator FilterInfo(ModelPredicate pred)
        {
            return new FilterInfo(pred.ColumnName, pred.PredicateValue, pred.Operation);
        }
    }
}
