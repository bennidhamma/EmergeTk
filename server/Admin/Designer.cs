using System;
using System.IO;
using EmergeTk;
using EmergeTk.Model;
using EmergeTk.Model.Security;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Administration
{
	public class Designer : Generic, IAdmin
	{
        Label label;

        private string template;

        private string filePath;

		#region IAdmin implementation 
		
		public string AdminName {
			get {
				return "Views";
			}
		}
		
		public string Description {
			get {
				return "Auto-generate widget templates for model forms and scaffolds.";
			}
		}
		
		public Permission AdminPermission
		{
			get {
				return Permission.GetOrCreatePermission("View Templates");
			}	
		}
		
		#endregion 
				
		public override void Initialize ()
		{
			Label l = RootContext.CreateWidget<Label>(this);
			l.Text = "<h3>Model Forms</h3>";			
			
			Type[] modelTypes = TypeLoader.GetTypesOfBaseType( typeof(AbstractRecord) );
			foreach( Type t in modelTypes )
			{
				LinkButton lb = RootContext.CreateWidget<LinkButton>(this);
				lb.Label = "New " + t.FullName + " ModelForm<BR>";
				lb.StateBag["t"] = t;
				lb.OnClick += new EventHandler<ClickEventArgs>( newModelForm );
			}
			//find all types that derive from AbstractRecord

			l = RootContext.CreateWidget<Label>(this);
			l.Text = "<P/><h3>Scaffold Templates</h3>";			
			
			foreach( Type t in modelTypes )
			{
				LinkButton lb = RootContext.CreateWidget<LinkButton>(this);
				lb.Label = "New " + t.FullName + " Scaffold Template<BR>";
				lb.StateBag["t"] = t;
				lb.OnClick += new EventHandler<ClickEventArgs>( newScaffoldTemplate );
			}
			
			
            this.label = RootContext.CreateWidget<Label>(this);
			
			
			
		}

		public void newScaffoldTemplate( object sender, ClickEventArgs ea )
		{
			
			Generic g = RootContext.CreateWidget<Generic>(this);
			g.TagName = "Code";
			Type t = ea.Source.StateBag["t"] as Type;
			ColumnInfo[] fields = ColumnInfoManager.RequestColumns(t);
			template = @"<Widget xmlns:emg=""http://www.emergetk.com/"">";
			foreach( ColumnInfo ci in fields )
			{
				template += string.Format( 
@"	
	<div class=""field field{0}"">
		<strong>{0}</strong> : {{{0}}}
	</div>", ci.Name );
			}
			template += 
@"
	<div class=""buttonPane"">
		<emg:PlaceHolder Id=""EditPlaceHolder""/>
		<emg:PlaceHolder Id=""DeletePlaceHolder""/>		
	</div>
</Widget>";
			
			log.Debug(template);
			
			FileInfo newFi = ThemeManager.Instance.RequestNewPhysicalFilePath( "Views" + Path.DirectorySeparatorChar + t.FullName.Replace('.', Path.DirectorySeparatorChar) + ".scaffoldtemplate" );
            Directory.CreateDirectory( newFi.Directory.FullName );
            
            this.filePath = newFi.FullName;
            
            //System.Web.HttpContext.Current.Server.MapPath();

			log.Debug("Writing new template out to file at " + newFi.FullName);
			
            if (File.Exists(newFi.FullName))
            {
                this.label.Text = "A custom scaffold template for this type already exists.";
                ConfirmButton cb = this.RootContext.CreateWidget<ConfirmButton>(this);
                cb.OnConfirm += new EventHandler<ClickEventArgs>(cb_OnConfirm);
                cb.Label = "Overwrite existing file";
            }
            else
            {
                this.SaveXml();
            }
		}			
			
		public void newModelForm( object sender, ClickEventArgs ea )
		{
			Generic g = RootContext.CreateWidget<Generic>(this);
			g.TagName = "Code";
			Type t = ea.Source.StateBag["t"] as Type;
			ColumnInfo[] fields = ColumnInfoManager.RequestColumns(t);
			template = @"<Widget xmlns:emg=""http://www.emergetk.com/"">";
			foreach( ColumnInfo ci in fields )
			{
				template += string.Format( 
@"	
	<div class=""editFieldInput editFieldInput{0}"">
		<emg:PlaceHolder Id=""{0}Label""/>
		<emg:PlaceHolder Id=""{0}Input""/>
	</div>", ci.Name );
			}
			template += 
@"
	<div class=""buttonPane"">
		<emg:PlaceHolder Id=""SubmitButton""/>
		<emg:PlaceHolder Id=""CancelButton""/>
		<emg:PlaceHolder Id=""DeleteButton""/>
	</div>
</Widget>";
			
			log.Debug(template);
			
			FileInfo newFi = ThemeManager.Instance.RequestNewPhysicalFilePath( "Views" + Path.DirectorySeparatorChar + t.FullName.Replace('.', Path.DirectorySeparatorChar) + ".modelform" );
            Directory.CreateDirectory( newFi.Directory.FullName );
            
            this.filePath = newFi.FullName;
					
			log.Debug("Writing new template out to file at " + filePath);
			
            if (File.Exists(filePath))
            {
                this.label.Text = "A custom model form for this type already exists.";
                ConfirmButton cb = this.RootContext.CreateWidget<ConfirmButton>(this);
                cb.OnConfirm += new EventHandler<ClickEventArgs>(cb_OnConfirm);
                cb.Label = "Overwrite existing file";
            }
            else
            {
                this.SaveXml();
            }
			
		}

        void cb_OnConfirm(object sender, ClickEventArgs ea)
        {
            ea.Source.Remove();
            this.SaveXml();
        }

        private void SaveXml()
        {
            TextWriter tw = new StreamWriter(filePath);
            tw.Write(template);
            tw.Close();
            tw.Dispose();

            template = System.Web.HttpUtility.HtmlEncode(template);
            this.label.Text = template;
        }

		public override string BaseXml {
			get { 
				return
@"
<Widget xmlns:emg=""http://www.emergetk.com/"">
	<h1>EmergeTk Designer</h1>
	<h2>Generate Model Form</h2>
	
</Widget>
";
			}
		}

	}
}
