using System;
using System.Collections.Generic;
using System.Text;
using EmergeTk.Model;

namespace EmergeTk
{
    public interface IDataSourced
    {
        IRecordList DataSource { get; set; }
        bool IsDataBound { get; }
        void DataBind();
		string PropertySource { get; set; }
		AbstractRecord Selected { get; set; }
    }
}
