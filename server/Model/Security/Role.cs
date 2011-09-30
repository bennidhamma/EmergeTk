using System;
using System.Collections.Generic;
using System.Text;

namespace EmergeTk.Model.Security
{
    public class Role : AbstractRecord, ISingular
    {
        private string name;
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        private RecordList<Permission> permissions;
        public RecordList<Permission> Permissions
        {
            get
            {
            	if( this.permissions == null )
	                this.lazyLoadProperty<Permission>("Permissions");
                return this.permissions;
            }
            set { this.permissions = value; }
		}

        public override void Save(bool SaveChildren, bool IncrementVersion, System.Data.Common.DbConnection conn)
        {
            base.Save(SaveChildren, IncrementVersion, conn);
            this.SaveRelations("Permissions");
        }
		
		public override string ToString()
		{
			return Name;
		}

        public static Role GetRole(string name)
        {
            return Role.Load<Role>(new FilterInfo("Name", name));
        }
        
		static Role administrator;
        public static Role Administrator
        {
        	get {
				if ( administrator == null )
					administrator = GetRole("Administrator");
				
				if ( administrator == null )
				{
					log.Warn("Creating new adminsitrator role");
					administrator = new Role();
					administrator.Name = "Administrator";
					administrator.Permissions.Add( Permission.Root );
					administrator.Save();
					administrator.SaveRelations("Permissions",administrator.Permissions, true );					
				}
				
        		return administrator;
        	}
        }
    }
}
