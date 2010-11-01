using EmergeTk;
using EmergeTk.Widgets.Html;
using System;
using System.Web;
using System.Text.RegularExpressions;

namespace EmergeTk.Widgets.Html
{	
	public class FileUpload : HtmlElement
	{
		private static new readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(FileUpload));
		Label label;
		private string buttonLabel = "Upload";
		
		public string CanonizedFileName
		{
			get {	
				if( postedFile == null )
					return null;
				return FileUpload.CanonizeFileName( ExtractLocalFileName( postedFile.FileName ) ); }
		}
		
		private HttpPostedFile postedFile;
		public HttpPostedFile File
		{
			get { return postedFile; }
		}

		public Label Label {
			get {
				return label;
			}
			set {
				label = value;
			}
		}

		public string ButtonLabel
		{
			get { return this.buttonLabel; }
			set 
			{ 
				this.buttonLabel = value;
				this.SetClientElementStyle("width", Util.ToJavaScriptString(ButtonLabel.Length.ToString() + "em"));
				RaisePropertyChangedNotification("ButtonLabel");
			}
		}

		public override void Initialize ()
		{
			AppendClass("file-upload");
			Button uploadButton = RootContext.CreateWidget<Button>(this);
			uploadButton.AppendClass("upload-button");
			uploadButton.Label = this.buttonLabel;
			uploadButton.SetClientElementStyle("width", Util.ToJavaScriptString(ButtonLabel.Length.ToString() + "em"));
			uploadButton.InvokeClientMethod("MakeUploadButton");
			uploadButton.OnClick += Upload;
			
			label = RootContext.CreateWidget<Label>(this);
			label.Bold = true;
			label.Visible = false;
			label.Inline = true;	
			return;
			/*
			TagName = "form";
			DefaultTagPrefix = "html";
			this["enctype"] = "multipart/form-data";
			this["encoding"] = "multipart/form-data";
			this["method"] = "post";
			file = RootContext.CreateWidget<HtmlElement>();
			file.DefaultTagPrefix = "html";
			file.TagName = "input";
			file["type"] = "file";
			file["size"] = "8";
			file["name"] = "file";
			ProgressButton b = RootContext.CreateWidget<ProgressButton>();
			b.Label = "Upload";
			b.OnClick += Upload;
			b.SetClientAttribute("formNode",ClientId + ".elem");
			
			Add(file, label, b);*/
		}

		public void Upload( object sender, ClickEventArgs ea )
		{
			log.Debug("uploading....", this.RootContext.HttpContext.Request.Files.Count );
			if( this.RootContext.HttpContext.Request.Files.Count > 0 )
			{
				//file.Visible = false;
				label.Visible = true;
				postedFile = this.RootContext.HttpContext.Request.Files[0];
				//ea.Source.Visible = false;
				label.Text = ExtractLocalFileName(postedFile.FileName);
				if (OnFileUploaded != null)
				{
					OnFileUploaded(this, EventArgs.Empty);
				}
                InvokeChangedEvent(null, postedFile);
			}
		}
		
		public static string CanonizeFileName(string f)
		{
			Regex r = new Regex(@"[^\w\.]");
			return r.Replace(f,"-");
		}
		
		public event EventHandler OnFileUploaded;
		
		public static string ExtractLocalFileName(string FullName)
		{
			if( FullName.Contains( "\\" ) )
			{
				FullName = FullName.Substring( FullName.LastIndexOf("\\")+1 );				
			}
			return FullName;
		}
	}
}
