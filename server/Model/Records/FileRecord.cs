using System;
using System.Web;
using EmergeTk;
using EmergeTk.Model;
using EmergeTk.Model.Security;
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
			set { relativeUrl = value; }
		}
		
		[PropertyType(DataType.Ignore)]
		public string SaveDirectory
		{
			get { return saveDirectory; }
			set { saveDirectory = value;}
		}

		public virtual User UploadedBy {
			get { return uploadedBy; }
			set { uploadedBy = value;}
		}

		public virtual System.DateTime UploadedOn {
			get { return uploadedOn; }
			set { uploadedOn = value;}
		}

		public virtual int Size {
			get { return size; }
			set { size = value;}
		}

		public virtual string FileType {
			get { return fileType; }
			set { fileType = value;}
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
			set { description = value;}
		}

		public new int Version {
			get {
				return version;
			}
			set {
				version = value;			
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
			}
		}
	}
}
