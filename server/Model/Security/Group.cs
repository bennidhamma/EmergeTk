using System;
using System.Collections.Generic;
using System.Text;

namespace EmergeTk.Model.Security
{
    public class Group : AbstractRecord
    {
        private string name;
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        private RecordList<User> users;
        public RecordList<User> Users
        {
            get
            {
                if (this.users == null)
                {
                    this.lazyLoadProperty<User>("Users");
                }
                return this.users;
            }
            set { this.users = value; }
		}

        public override void Save(bool SaveChildren, bool IncrementVersion, System.Data.Common.DbConnection conn)
        {
            base.Save(SaveChildren, IncrementVersion, conn);
            this.SaveRelations("Users");
        }
    }
}
