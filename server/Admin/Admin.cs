using System;
using System.Reflection;
using EmergeTk;
using EmergeTk.Model;
using EmergeTk.Widgets.Html;

namespace EmergeTk
{
	public class Admin : Context
	{
		static Type[] iadmins;
		Pane adminList;
		Widget currentAdmin;
		
		static Admin()
		{
			iadmins = TypeLoader.GetTypesOfInterface(typeof(IAdmin));			
		}
		
		#region Authorize
		public override void Initialize ()
		{
			CheckRunAdmin();
						
			log.Info("Admin initializing going to EnsureAccess");
			EnsureAccess( EmergeTk.Model.Security.Permission.GetOrCreatePermission("Admin"), Authorized);			
		}

		private void Authorized(object sender, EventArgs ea )
		{
			Label label = EmergeTk.Widgets.Html.Label.InsertLabel( this, "h1", "EmergeTk Administration" );
			label.AppendClass("Admin");
			
			Button logout = CreateWidget<Button>(this);
			logout.Label = "logout";
			logout.OnClick += delegate {
				LogOut();	
				Reload();
			};

			adminList = RootContext.CreateWidget<Pane>(this);
			
			ShowList();
		}
		#endregion
		
		public void ShowList()
		{
			if( currentAdmin != null )
			{
				currentAdmin.Remove();
			}

			adminList.ClearChildren();
			adminList.Visible = true;
			
			foreach( Type t in iadmins )
			{
				IAdmin ia =(IAdmin) RootContext.CreateUnkownWidget(t);
				
				if( CurrentUser != null && CurrentUser.CheckPermission( ia.AdminPermission ) )
				{				
					AdminEntry ae = RootContext.CreateWidget<AdminEntry>(adminList);
					ae.Admin = ia;
					ae.Init();	
				}
			}
		}

		public void ShowDetail( Widget w )
		{
			currentAdmin = w;
			adminList.Visible = false;
			Add( w );
		}
		
		public void CheckRunAdmin()
		{
			//
			// TODO: This needs some kind of security, for now it's security through obsecurity because 
			// this is a totally private, internal URL
			//
			string admin = this.HttpContext.Request.QueryString["admin"];
			string run = this.HttpContext.Request.QueryString["run"];
			log.Debug("Admin.CheckRunAdmin checking url...",this.HttpContext.Request.QueryString,admin,run);
			
			if ( string.IsNullOrEmpty(admin) || string.IsNullOrEmpty(run) )
			{
				log.Debug("Admin.CheckRunAdmin admin or run is either empty or null",admin,run);
				return;
			}
			else
				log.Debug("Admin.CheckRunAdmin going to try and run!");
			
			Type adminType = TypeLoader.GetType(admin);			
			System.Reflection.Assembly LoadedAssembly = System.Reflection.Assembly.GetAssembly(adminType);			
			object myObject = LoadedAssembly.CreateInstance(admin,false,BindingFlags.ExactBinding,null,new Object[] {},null,null);
			MethodInfo myMethod = LoadedAssembly.GetType(admin).GetMethod(run);
			myMethod.Invoke(myObject,null);
			log.Info("Invoked method",adminType,myObject,myMethod);
		}
	}

	public class AdminEntry : Generic
	{
		public IAdmin Admin { get; set; }

		public override void Initialize ()
		{
			Admin root = (Admin)RootContext;
			
			Pane adminInfoPane = this.RootContext.CreateWidget<Pane>(this);
			adminInfoPane.AppendClass("clearfix");
			
			HtmlElement header = HtmlElement.Create("h2");
			adminInfoPane.Add( header );
			LinkButton lb = RootContext.CreateWidget<LinkButton>(header);
			lb.Label = Admin.AdminName;
			lb.OnClick += delegate {
				AdminPane ap = RootContext.CreateWidget<AdminPane>();
				ap.Admin = Admin;
				ap.Init();
				root.ShowDetail( ap );
			};
			Label description = Label.InsertLabel(adminInfoPane, "p", Admin.Description );
			description.AppendClass("admin-description");
			
			this.AppendClass("Admin");
		}

	}
	
	

	public class AdminPane : Generic
	{
		public IAdmin Admin { get; set; }

		public override void Initialize ()
		{
			Admin root = (Admin)RootContext;
			
			Pane adminInfoPane = this.RootContext.CreateWidget<Pane>(this);
			adminInfoPane.AppendClass("clearfix");
						
			Label.InsertLabel(adminInfoPane,"h2",Admin.AdminName);
			Label description = Label.InsertLabel(adminInfoPane, "p", Admin.Description );
			description.AppendClass("admin-description");
			LinkButton returnToList = RootContext.CreateWidget<LinkButton>(adminInfoPane);
			returnToList.Label = "return";
			returnToList.OnClick += delegate{ root.ShowList(); };
			Add( (Widget)Admin );
		}

	}
}
