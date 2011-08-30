using System;
using System.Web;
using EmergeTk;
using EmergeTk.Model;
using EmergeTk.Model.Security;
using EmergeTk.Widgets.Html;
using System.IO;

namespace EmergeTk.Model
{
	public class FileRecord : AbstractRecord
	{
		//private static new readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(FileRecord));
	
		private User uploadedBy;
		private DateTime uploadedOn = DateTime.UtcNow;
		private string relativeUrl, fileType, description = string.Empty;
		private string originalFileName, originalExtension, saveDirectory, physicalPath;
		private int size, version = -1;

		public virtual string RelativeUrl {
			get { return relativeUrl; }
			set { relativeUrl = value; this.NotifyChanged("RelativeUrl");}
		}
		
		[PropertyType(DataType.Ignore)]
		public string SaveDirectory
		{
			get { return saveDirectory; }
			set { saveDirectory = value; NotifyChanged("SaveDirectory"); }
		}

		public virtual User UploadedBy {
			get { return uploadedBy; }
			set { uploadedBy = value; this.NotifyChanged("UploadedBy");}
		}

		public virtual System.DateTime UploadedOn {
			get { return uploadedOn; }
			set { uploadedOn = value; this.NotifyChanged("UploadedOn");}
		}

		public virtual int Size {
			get { return size; }
			set { size = value; this.NotifyChanged("Size");this.NotifyChanged("FriendlySize");}
		}

		public virtual string FileType {
			get { return fileType; }
			set { fileType = value; this.NotifyChanged("FileType");}
		}

		public virtual string FriendlySize {
			get
            {
				if( size < 1000 )
					return size + " bytes";
				else if( size < 1000000 )
					return size / 1000 + "Kb";
				else
					return Math.Round(size / 1000000.0f,1) + "Mb";
			}
		}	
		
		public string Description {
			get { return description; }
			set { description = value; this.NotifyChanged("Description");}
		}

		public new int Version {
			get {
				return version;
			}
			set {
				version = value;			
				this.NotifyChanged("Version");
			}
		}

		public string OriginalFileName {
			get {
				return originalFileName;
			}
			set {
				originalFileName = value;
			}
		}

		public string OriginalExtension {
			get {
				return originalExtension;
			}
			set {
				originalExtension = value;
			}
		}
		
		[PropertyType(DataType.Ignore)]
		public string PhysicalPath
		{
			get
			{
				return physicalPath;	
			}
			set
			{
				physicalPath = value;
				NotifyChanged("PhysicalPath");
			}
		}
        
		public override Widget GetEditWidget(Widget parent, ColumnInfo column, IRecordList records)
		{
			EnsureId();
			FileController fc = Context.Current.CreateWidget<FileController>();
			fc.SaveDirectory = SaveDirectory;
			fc.File = this;
			fc.Record = this;
			fc.OnFileUploaded += new EventHandler( delegate( object sender, EventArgs ea ) {
				if( File.Exists(PhysicalPath) )
				{
					size = (int)new FileInfo(PhysicalPath).Length;	
				}
				else
					size = 0;
				uploadedBy = Context.Current.CurrentUser;
				uploadedOn = DateTime.UtcNow;
				version++;
			});
			
			return fc;
		}
	}
}
