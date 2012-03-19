using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace EmergeTk.Model.Security
{
    public class User : AbstractRecord
    {
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(User));
		private static readonly EmergeTkLog securityViolationLog = EmergeTkLogManager.GetLogger("PotentialOnlyCSRF");
		public static List<User> activeUsers = new List<User>();
		private static readonly long loginTokenLifetime = Setting.GetValueT<long>("LoginTokenLifetime", 1440);
		private static readonly string preventionReferrerHost = Setting.GetValueT<string>("CSRFPreventionReferrerHost");
    	
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

		private DateTime? lastLoginDate;
		public DateTime? LastLoginDate 
		{
			get
			{
				return lastLoginDate;
			}
			set
			{
				lastLoginDate = value;
			}
		}

		private DateTime? currentLoginDate;
		public DateTime? CurrentLoginDate 
		{
			get
			{
				return currentLoginDate;
			}
			set
			{
				if (!loading)
					LastLoginDate = currentLoginDate;

				currentLoginDate = value;
			}
		}

        public void GenerateAndSetNewSessionToken()
        {
            this.SessionToken = Util.GetBase32Guid();
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
				if (HttpContext.Current != null)
				{
					u = FindBySessionToken();
					HttpContext.Current.Items["user"] = u;
				}
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
			//log.Debug("setting login cookie");
			HttpCookie userIdCookie = new HttpCookie("UserId", Id.ToString());
            userIdCookie.Expires = DateTime.UtcNow.AddYears(1);
            HttpContext.Current.Response.Cookies.Add(userIdCookie);
			SetRoleCookie(Roles.Count > 0 ? Roles.JoinToString(",") : "");
			//throw new Exception("setting login cookie");
		}
		
		public void SetRoleCookie(string role)
		{
			HttpCookie sessionCookie = new HttpCookie("Role", role);
            sessionCookie.Expires = DateTime.UtcNow.AddDays(1);
            HttpContext.Current.Response.Cookies.Add(sessionCookie);
		}

		public static string GetRequestToken()
		{
			if (HttpContext.Current == null)
				return null;
			else
				return HttpContext.Current.Request.Headers["x-LoginToken"];
		}
		
		public static User GetUserFromTokenHeader()
		{
			User user = null;
			string token = GetRequestToken();
			if (!String.IsNullOrEmpty(token))
			{
				user = User.FindBySessionToken(token);
			}
			return user;
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
    	
        public static User LoginUser(string username, string password)
        {
            // TODO: do we need to capture TableNotFoundException? right now the Login widget does that
            
            User user = AbstractRecord.Load(defaultType, new FilterInfo("Name", username )) as User;
            if (user != null)
            {
                if (user.ComputeSaltedPassword(password) == user.Password)
                {
					//only provide a new session token if there is none currently.
					//to allow multiple logins.
					if (user.SessionToken == string.Empty)
					{
						user.GenerateAndSetNewSessionToken();
						user.CurrentLoginDate = DateTime.UtcNow;
					}
					user.Save();

                    return user;
                }
            }
            return null;
        }

		public static User FindBySessionToken()
		{
			return GetUserFromTokenHeader();
		}
		
		public static User FindBySessionToken(string token)
        {
        	User user = AbstractRecord.Load(defaultType, new FilterInfo("SessionToken", token )) as User;
			return user;
        }

		public static void CheckReferrerHost()
		{
			if (!String.IsNullOrEmpty(preventionReferrerHost) && HttpContext.Current != null)
			{
				string referrer = HttpContext.Current.Request.UrlReferrer.Host;
				if (!String.IsNullOrEmpty(referrer) && preventionReferrerHost != referrer)
				{
					securityViolationLog.ErrorFormat("HttpContext Referring Domain Host '{0}' does not match configuration '{1}'. Request Host:{2}", referrer, preventionReferrerHost, HttpContext.Current.Request.UserHostAddress);
					throw new UnauthorizedAccessException("Invalid Referring Domain Host.");
				}
			}
		}

		public static void AuthenticateUser()
		{
			CheckReferrerHost();

			string token = GetRequestToken();
			string host = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
			User user = User.Current;
			if (null != user)
			{
				HttpCookie cookie = HttpContext.Current.Request.Cookies["UserId"];
				if (null != cookie && !String.IsNullOrEmpty(cookie.Value))
				{
					if (user.Id != Convert.ToInt32(cookie.Value))
					{
						securityViolationLog.ErrorFormat("User {0} with given token {1} does not match supplied cookie {2}. Request Host:{3}", user.Id, token, cookie.Value, host);
						throw new UnauthorizedAccessException("Invalid Credentials.");
					}
				}
				else
				{//could this EVER be ok or should we prevent access?
					securityViolationLog.ErrorFormat("Unusual! Missing UserId cookie. Why? User {0} with given token {1}. Request Host:{2}", user.Id, token, host);
					throw new UnauthorizedAccessException("Invalid Credentials.");
				}

				if (user.CurrentLoginDate.Value.AddMinutes(loginTokenLifetime) < DateTime.UtcNow)
				{//blank out the session thus forcing user to login and get NEW session
					user.SessionToken = String.Empty;
					user.Save();
					log.Info(string.Format("User session has timed out for {0}. Id:{1}", user.Name, user.Id));
					throw new TimeoutException("Expired Credentials.");
				}
			}
			else
			{
				securityViolationLog.ErrorFormat("No user found with given token: {0}. OK during Login request only. Request Host:{1}", token, host);
				throw new UnauthorizedAccessException("Invalid Credentials.");
			}
		}
    }
}
