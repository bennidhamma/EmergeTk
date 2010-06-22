// ImageUpload.cs created with MonoDevelop
// User: ben at 10:04 A 27/02/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.IO;
using System.Text.RegularExpressions;
using EmergeTk;
using System.Web;

namespace EmergeTk.Widgets.Html
{
	public class ImageUpload : Generic
	{
		public event EventHandler OnImageUploaded;
		public ImageUpload()
		{
		}
		
		FileUpload fu;
		Image img;
		string savePath;
		string saveFormat;
		string physicalPath;
		string uid;
		
		public string SavePath {
			get {
				return savePath;
			}
			set {
				savePath = value;
				if( img != null )
					img.Url = SavePath + "?" + (uid ?? Util.ConvertToBase32(DateTime.Now.Ticks));
				RaisePropertyChangedNotification( "SavePath" );
			}
		}

		public string SaveFormat {
			get {
				return saveFormat;
			}
			set {
				saveFormat = value;
				RaisePropertyChangedNotification( "SaveFormat" );
			}
		}

		public string PhysicalPath
		{
			get { return physicalPath; }
			set
			{
				physicalPath = value;
				RaisePropertyChangedNotification("PhysicalPath");
			}
		}

		public string ImageUid {
			get {
				return uid;
			}
			set {
				uid = value;
				if( img != null )
					img.Url = SavePath + "?" + (uid ?? Util.ConvertToBase32(DateTime.Now.Ticks));
				RaisePropertyChangedNotification("ImageUid");
			}
		}

		public bool SaveToLocalStorage { get; set; }
		public HttpPostedFile HttpPostedFile { get; set; }
		
		ConfirmButton unsetButton;
		public override void Initialize ()
		{
			Pane imgPane = RootContext.CreateWidget<Pane>(this);
			fu = RootContext.CreateWidget<FileUpload>(this);
			unsetButton = RootContext.CreateWidget<ConfirmButton>(this);
			unsetButton.Label = "remove image";
			unsetButton.OnConfirm += new EventHandler<ClickEventArgs>( delegate( object sender, ClickEventArgs ea ) {
				img.Visible = false;
				SavePath = null;
				HttpPostedFile = null;
				OnImageUploaded( this, null );
				unsetButton.Visible = false;
			});
			if( string.IsNullOrEmpty( SavePath ) )
				unsetButton.Visible = false;
			fu.OnFileUploaded += new EventHandler( fileUploaded );
			imgPane.ClassName = "Preview";
			img = RootContext.CreateWidget<Image>(imgPane);
			if( ! string.IsNullOrEmpty( SavePath ) )
				img.Url = SavePath + "?" + (uid ?? Util.ConvertToBase32(DateTime.Now.Ticks));
			else
				img.Visible = false;
			//imgPane.SetClientElementAttribute("align", Util.ToJavaScriptString("center") );
			ClassName = "ImageUpload";
		}
		
		Label errorLabel;
		public void fileUploaded( object sender, EventArgs ea )
		{			
			string name = fu.File.FileName;
			log.Debug("fileUploaded. ", fu.File, fu.File.FileName, fu.File.ContentType );
			Regex imageType = new Regex("image/(png|jpg|jpe|pjpeg|x-png|jpeg|gif)",RegexOptions.Compiled );
			if( imageType.IsMatch( fu.File.ContentType ) )
			{
				if (SaveToLocalStorage)
				{
					savePath = saveFormat.Replace("$FileName", name);
					savePath = savePath.Replace("$Extension", name.Substring(name.LastIndexOf('.') + 1));
					physicalPath = RootContext.HttpContext.Server.MapPath(savePath);
					fu.File.SaveAs(physicalPath);
					img.Url = savePath + "?" + (uid ?? Util.ConvertToBase32(DateTime.Now.Ticks));
				}
				else
				{
					HttpPostedFile = fu.File;
				}
				unsetButton.Visible = true;
				img.Visible = true;
				if( OnImageUploaded != null )
					OnImageUploaded( this, null );
				if( errorLabel != null )
					errorLabel.Visible = false;
			}
			else
			{
				
				log.Debug("image upload failed for content type ", fu.File.ContentType );
				if( errorLabel == null )
				{
					errorLabel = RootContext.CreateWidget<Label>(this);
					errorLabel.Text = "File is not a valid image type.  Image format must be one of: png, jpg or gif.";
					errorLabel.ClassName = "error";
				}
				else
					errorLabel.Visible = true;
								
			}
			FileUpload newFu = RootContext.CreateWidget<FileUpload>();
			fu.Replace(newFu);
			fu = newFu;
			fu.OnFileUploaded += new EventHandler(fileUploaded);
			
		}

		public void SetImageUrl(string url)
		{
			log.Debug("setting image url to " + url );
			img.Url = url;
			img.Visible = true;
		}
	}
}
