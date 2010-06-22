using System;
using System.Collections.Generic;
using System.Text;
using EmergeTk.Model;
using EmergeTk.Model.Security;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Administration
{
    public class Users : Generic, IAdmin
    {
        private TabPane tabs;

        public Users() { }
		
		#region IAdmin implementation 
		
		public string AdminName {
			get {
				return "Security";
			}
		}
		
		public string Description {
			get {
				return "Manage Users, Roles, Groups and Permissions.";
			}
		}
		
		public Permission AdminPermission
		{
			get {
				return Permission.GetOrCreatePermission("View Security");
			}	
		}
		
		#endregion 

        public override void Initialize()
        {
        	RootContext.EnsureAccess( Permission.Root, delegate {
	            this.tabs = RootContext.CreateWidget<TabPane>(this);
	
	            Scaffold<User> userPane = SetupType<User>();
	            userPane.ReadOnlyFields.Add(ColumnInfoManager.RequestColumn<User>("Salt"));
	            userPane.ReadOnlyFields.Add(ColumnInfoManager.RequestColumn<User>("Password"));
	            userPane.ReadOnlyFields.Add(ColumnInfoManager.RequestColumn<User>("SessionToken"));
	            
				// Temporarily hide Groups because we're not using them and the security link 
				// on admin is unavailable because of EmergeTk.Model.TableNotFoundException.
	            //SetupType<Group>();
	            SetupType<Role>();
	            SetupType<Permission>();
	        });
        }

        Scaffold<T> SetupType<T>() where T : AbstractRecord, new()
        {
            Pane pane = RootContext.CreateWidget<Pane>(this.tabs);
            pane.Label = typeof(T).Name + "s";

            Scaffold<T> scaffold = RootContext.CreateWidget<Scaffold<T>>(pane);
            scaffold.DataSource = DataProvider.LoadList<T>();
            scaffold.DestructivelyEdit = true;
            
            return scaffold;
        }

        public override void PostInitialize()
        {
        }
    }
}
