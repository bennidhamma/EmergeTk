// FileController.cs created with MonoDevelop
// User: ben at 12:19 P 06/05/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.IO;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
	public class FileController : Generic
	{
		private new static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(FileController));
		public event EventHandler OnFileUploaded;
		
		FileRecord file;
		
		FileUpload fu;
		Pane viewPane;
		string relativeUrl, templateName;
		string saveDirectory;
		
		public string SaveDirectory
		{
			get
			{
				if( saveDirectory == null )
				{
					saveDirectory = RootContext.HttpContext.Server.MapPath( "/Storage/" );		
					Directory.CreateDirectory( Path.GetDirectoryName( saveDirectory ) );
				}
				return saveDirectory;
			}
			set
			{
				saveDirectory = value;	
			}
		}
		
		public FileRecord File {
			get {
				if( file == null && Record is FileRecord )
					file = (FileRecord)Record;
				return file;
			}
			set {
				file = value;
				RaisePropertyChangedNotification("File");
				if( Initialized )
					Setup();
			}
		}

		public string RelativeUrl {
			get {
				return relativeUrl;
			}
			set {
				relativeUrl = value;
			}
		}

		public string TemplateName {
			get {
				return templateName;
			}
			set {
				templateName = value;
			}
		}
		
		public override void PostDataBind ()
		{
			if( Record is FileRecord )
			{
				File = (FileRecord)Record;
				Setup();
			}
		}
		
		public FileController()
		{
		}
		
		public override void Initialize ()
		{
			fu = Find<FileUpload>("uploader");
			viewPane = Find<Pane>("viewPane");
			if( File != null )
				Setup();
			fu.OnFileUploaded += new EventHandler( FileUploaded );
		}

		private void Setup()
		{
			log.Debug("callign setup ", File, File.RelativeUrl);
			if( File != null )
			{
				if( File.Size > 0 )
				{
					viewPane.Visible = true;
					DataBindWidget();
				}
				Record = file;				
				relativeUrl = File.RelativeUrl;
			}
		}

		private void FileUploaded( object sender, EventArgs ea )
		{
			string path = null;
			string uploadedName = fu.File.FileName;
			
			string ext = null;
			if( uploadedName.Contains(".") )
				ext = uploadedName.Substring( uploadedName.LastIndexOf('.')+1 );
			
			if( ! string.IsNullOrEmpty( templateName ) )
			{
				path = templateName.Replace("$FileName", uploadedName );
				path = path.Replace("$Extension",ext);
			}
			else if( File.RelativeUrl != null )
			{
				path = RootContext.HttpContext.Server.MapPath(File.RelativeUrl);
				//ensure that directory exists.
				Directory.CreateDirectory( Path.GetDirectoryName( path ) );
			}
			else
			{
				path = Path.Combine(SaveDirectory, fu.File.FileName);
			}
			
			log.Debug("mapped path: ", path, "size:", fu.File.ContentLength);
			fu.File.SaveAs( path );  //TODO: will this overwrite existing file?  what if two people upload the same file at the same time?
			File.PhysicalPath = path;
			log.Debug("FileRecord File is ", Record);
			File.Size = fu.File.ContentLength;
			File.UploadedBy = Context.Current.CurrentUser;
			File.UploadedOn = DateTime.Now;
			File.RelativeUrl = path;
			File.OriginalFileName = fu.File.FileName;
			File.OriginalExtension = ext;
			File.Version++;
			Find<Link>("downloadLink").Text = path;
			
			viewPane.Visible = File.Size > 0;
			FileUpload fu2 = RootContext.CreateWidget<FileUpload>();
			fu.Replace(fu2);
			fu = fu2;
			fu.OnFileUploaded += new EventHandler( FileUploaded );
			if( file.Size == 0 )
				return;

			//this.Record = file;
			DataBindWidget(file,true);

			if( OnFileUploaded != null )
				OnFileUploaded( this, null );
			log.Debug("uploading file ", file.RelativeUrl, file.FriendlySize);
			DataBindWidget(file, true);
		}
	}
}
