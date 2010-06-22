using System;
using System.Collections.Generic;
using System.Text;
using EmergeTk.Model;

namespace EmergeTk.Model.Security
{
    public class Permission : AbstractRecord, ISingular
    {
		static Permission()
		{
			PropertyConverter.AddConverter
				( new ConversionKey( typeof(string),typeof(Permission) ),
				 delegate( object s ) {
					return GetPermission((string)s);	
				});
		}
		
        private string name;
        //[Identity]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        
        public static Permission GetPermission(string name)
        {
        	return Permission.Load<Permission>(new FilterInfo("Name",name) );
        }
       
       	public static Permission GetOrCreatePermission( string name )
       	{
			//log.Info("Getting permission",name);
			
       		Permission p = GetPermission(name);
       		if( p != null )
       		{
			//	log.Info("Found permission",name);
       			return p;
       		}
       		p = new Permission();
       		p.name = name;
       		p.Save();
			
			Role.Administrator.Permissions.Add( p );
			//Role.Administrator.Save();
			Role.Administrator.SaveRelations("Permissions");

			log.Info("Added permission to Administrator",name,p,p.Id,p.Name);
			
            //InvalidateCache(p.GetType());

       		return p;
       	}
       	
		public override string ToString ()
		{
			return Name;
		}       	
       	
		static Permission root;
       	public static Permission Root {
       		get {
				if ( root == null )
					root = Permission.GetOrCreatePermission("Root");
       			return root;
       		}
       	}
    }	
}
