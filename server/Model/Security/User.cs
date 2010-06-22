using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using EmergeTk.Widgets.Html;
using System.Web;

namespace EmergeTk.Model.Security
{
    public class User : AbstractRecord
    {
    	public static List<User> activeUsers = new List<User>();
    	
        static Type defaultType;
        static User()
        {
       		defaultType = typeof(User);
        	
        	//would be nice to listen to load events here.        	
        }       

        public User()
        {
            byte[] bytes = new byte[4];
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(bytes);
            this.salt = (int)BitConverter.ToInt32(bytes, 0);
        }

        private string ComputeSaltedPassword(string plainTextPassword)
        {
            UnicodeEncoding encoder = new UnicodeEncoding();
            byte[] passwordBytes = encoder.GetBytes(plainTextPassword);

            byte[] saltBytes = BitConverter.GetBytes(this.Salt);

            byte[] saltedPasswordBytes = new byte[passwordBytes.Length + saltBytes.Length];
            passwordBytes.CopyTo(saltedPasswordBytes, 0);
            saltBytes.CopyTo(saltedPasswordBytes, passwordBytes.Length);

            SHA512 hasher = SHA512.Create();
            byte[] finalHash = hasher.ComputeHash(saltedPasswordBytes);
            return Convert.ToBase64String(finalHash);
        }

        private string name;
        public string Name
        {
            get { return this.name; }
            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    this.name = value;
                }
            }
        }

        public string PlainTextPassword
        {
            get { return String.Empty; }
            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    this.password = this.ComputeSaltedPassword(value);
                    this.NotifyChanged("Password");
                }
            }
        }

        private string password;
        public string Password
        {
            get { return this.password; }
            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    this.password = value;
                }
            }
        }
        
        private int salt = -1;
        public int Salt
        {
            get { return this.salt; }
            set { this.salt = value; }
        }

        private string sessionToken;
        public string SessionToken
        {
            get { return this.sessionToken; }
            set { this.sessionToken = value; }
        }

        private RecordList<Role> roles;
        bool rolesChanged = false;
        public RecordList<Role> Roles
        {
            get
            {
                if (this.roles == null)
                {
                    this.lazyLoadProperty<Role>("Roles");
                }
                return this.roles;
            }
            set { 
            	this.roles = value;
        		if( this.roles != null )
        		{
		            this.roles.OnRecordAdded += new EventHandler<RecordEventArgs>( delegate( object sender, RecordEventArgs ea ) {
	                	rolesChanged = true;
	                } );
	                this.roles.OnRecordRemoved += new EventHandler<RecordEventArgs>( delegate( object sender, RecordEventArgs ea ) {
	                	rolesChanged = true;
	                } );
	            }
	        }
        }

		public string RoleString
		{
			get
			{
				return Util.Join(Roles.ToStringArray("Name"));
			}
		}
		
        public IRecordList<Group> GetGroups()
        {
            return this.LoadParents<Group>(ColumnInfoManager.RequestColumn<Group>("Users"));
        }
        
        public virtual bool CheckPermission(Permission p)
        {
            foreach (Role role in this.Roles)
            {
                if (role.Permissions.Contains(p))
                {
                    return true;
                }
            }
            return false;
        }
        
        public RecordList<Permission> Permissions
        {
        	get {
        		RecordList<Permission> permissions = new EmergeTk.Model.RecordList<Permission>();
        		foreach( Role role in this.Roles )
        		{
        			foreach( Permission p in role.Permissions )
        				permissions.Add( p );
        		}
        		return permissions;
        	}
        }

        public static Type DefaultType {
        	get {
        		return defaultType;
        	}
        	set {
        		defaultType = value;
        	}
        }

        private bool passwordIsVerified;
        public bool PasswordIsVerified {
        	get {
        		return passwordIsVerified;
        	}
        	set {
        		passwordIsVerified = value;
        	}
        }

        public void GenerateAndSetNewSessionToken()
        {
            this.SessionToken = Util.GetBase32Guid();
        }

        public override Widget GetPropertyEditWidget(Widget parent, ColumnInfo column, IRecordList records)
        {
            switch (column.Name)
            {
            	case "PlainTextPassword":
                    VerifyPassword vpw = Context.Current.CreateWidget<VerifyPassword>();
                    vpw.User = this;
                    return vpw;
                case "Roles":
                	SelectList<Role> slr = Context.Current.CreateWidget<SelectList<Role>>();
                    slr.Mode = SelectionMode.Multiple;
                    slr.LabelFormat = "{Name}";
                    slr.SelectedItems = this.Roles;
                    slr.DataSource = DataProvider.LoadList<Role>();
                    slr.DataBind();
                    slr.OnChanged += new EventHandler<EmergeTk.ChangedEventArgs>(SelectList_OnChanged);
                    
                    return slr;
                default:
                    return base.GetPropertyEditWidget(parent, column, records);
            }
        }
        
        public override void Save(bool SaveChildren, bool IncrementVersion, System.Data.Common.DbConnection conn)
        {
            //log.Debug("Executing User Save override - this.roles=", this.Roles);

           base.Save(SaveChildren, IncrementVersion, conn);
            
            if( rolesChanged ) 
            {
            	this.SaveRelations("Roles");
            	foreach( User u in activeUsers )
            	{
            		if( u == this )
            			u.roles = this.roles.Copy() as RecordList<Role>;
            	}
            }
        }

        private const int newPasswordLength = 8;
        public static string GetNewDefaultPassword()
        {
            byte[] passwordBytes = new byte[User.newPasswordLength];
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(passwordBytes);
            return Convert.ToBase64String(passwordBytes);
        }

        void SelectList_OnChanged(object sender, ChangedEventArgs ea )
        {
            ModelForm<User> mf = ea.Source.FindAncestor<ModelForm<User>>();
            if (mf != null)
            {
                if (mf.SavedFields == null)
                    mf.SavedFields = new List<string>();
                if (!mf.SavedFields.Contains("Roles"))
                    mf.SavedFields.Add("Roles");
            }
        }

        public override string ToString()
        {
        	return name;
        }
        
        public override object Value {
        	get { return name; }
        	set { name = (string)value; }
        }

		public static bool IsRoot
		{
			get
			{
				return Current != null && Current.CheckPermission(Permission.Root);
			}
		}
		
		public static User Current
		{
			get 
			{	
				if( HttpContext.Current != null && HttpContext.Current.Items["user"] != null )
				{
					return 	(User)HttpContext.Current.Items["user"];
				}
				User u = null;
				if(Context.Current != null)
					u = Context.Current.CurrentUser;
				else if( HttpContext.Current != null )
					u = FindBySessionToken();
				else
					u = null;
				if( HttpContext.Current != null )
					HttpContext.Current.Items["user"] = u;
				return u;
			}
			set
			{
				if( HttpContext.Current != null )
					HttpContext.Current.Items["user"] = value;
			}
		}
		
		public void SetLoginCookie()
		{
			HttpCookie sessionCookie = new HttpCookie("LoginToken", SessionToken);
            sessionCookie.Expires = DateTime.Now.AddDays(1);
            HttpContext.Current.Response.Cookies.Add(sessionCookie);
		}
		
		public static User GetUserFromCookie()
		{
			HttpCookie cookie = HttpContext.Current.Request.Cookies["LoginToken"];
            if (cookie != null && !String.IsNullOrEmpty(cookie.Value))
            {
            	User user = User.FindBySessionToken( cookie.Value );
                return user;
            }
			return null;
		}
		
		public static User GetUserFromQueryStringToken()
		{
			string token = HttpContext.Current.Request.QueryString["token"];
			if (!String.IsNullOrEmpty(token))
            {
            	User user = User.FindBySessionToken( token );
                return user;
            }
			return null;
		}
		
		public static void RegisterActiveUser( User u )
    	{
    		//note: only call RegisterActiveUser if you wish the user to receive role change notifications.
   			activeUsers.Add( u );
    	}
    	
    	public static void UnregisterActiveUser( User u )
    	{
    		while( activeUsers.Contains( u ) )
    		{
    			activeUsers.Remove( u );
    		}
    	}
    	
        public static User AuthenticateUser(string username, string password)
        {
            // TODO: do we need to capture TableNotFoundException? right now the Login widget does that
            
            User user = AbstractRecord.Load(defaultType, new FilterInfo("Name", username )) as User;
            if (user != null)
            {
                if (user.ComputeSaltedPassword(password) == user.Password)
                {
                    if (String.IsNullOrEmpty(user.SessionToken))
                    {
                        user.GenerateAndSetNewSessionToken();
						user.Save();
                    }
                    return user;
                }
            }
            return null;
        }
        
        public static User FindBySessionToken( string token )
        {
        	return AbstractRecord.Load(defaultType, new FilterInfo("SessionToken", token )) as User;
        }
		
		public static User FindBySessionToken()
		{
			return GetUserFromCookie() ?? GetUserFromQueryStringToken();
		}
    }
}
