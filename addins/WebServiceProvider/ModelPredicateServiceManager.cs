using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmergeTk.Model;
using EmergeTk.Model.Security;
using SimpleJson;

namespace EmergeTk.WebServices
{
    class ModelPredicateServiceManager : IRestServiceManager
    {
        #region IRestServiceManager implementation
        public string GetHelpText()
        {
            return String.Empty;
        }

        public void Authorize(RestOperation operation, JsonObject recordNode, AbstractRecord record)
        {
            User u = User.Current;
            if (u == null)
                throw new UnauthorizedAccessException("Must be authenticated to interact with ModelPredicates");
            if (record == null)
                return;
        }

        public bool AuthorizeField(RestOperation op, AbstractRecord record, string property)
        {
            return true;
        }

        public AbstractRecord GenerateExampleRecord()
        {
			return new ModelPredicate { Key = "Brand", Term = 19, Operation = FilterOperation.Equals };
        }

        public string GenerateExampleFields(String method)
        {
            return "*";
        }

        public List<RestTypeDescription> GetTypeDescriptions()
        {
            return null;
        }
        #endregion
    }
}
