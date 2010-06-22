// ObjectViewer.cs
//	
//

using System;
using System.IO;
using System.Xml;
using EmergeTk;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
	
	
	public class ObjectViewer : Template
	{
		private static new readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(ObjectViewer));
		string template = null;

        private bool autoDataBind = true;
        public bool AutoDataBind
        {
            get { return autoDataBind; }
            set { autoDataBind = value; }
        }
		
		public override AbstractRecord Record {
			get { return base.Record; }
			set { 
				base.Record = value;				
			}
		}
		
		public override void Initialize ()
		{
			if( UseRecordAsSource )
			{
				Source = Record;
				LoadTemplate();
			}
		}
		
		AbstractRecord source;
		public AbstractRecord Source {
			get 
			{
				return source;
			}
			set
			{
				source = value;
				ClearChildren();
				if( source != null )
					LoadTemplate();
			}
		}
		
		public bool UseRecordAsSource { get; set; }
		
		protected override void PostClone ()
		{
			base.PostClone ();
			
			source = null;
			
			if( UseRecordAsSource )
			{
				Source = Record;
			}
		}
		
		public string Template {
			get {
				return template;
			}
			set {
				template = value;
				LoadTemplate();	
			}
		}

		void LoadTemplate()
		{
			if( template == null || source == null )
				return;
			Type t = source.GetType();
			
			string view = t.FullName.Replace('.', Path.DirectorySeparatorChar) + "." + template;
			
			XmlNode node = ThemeManager.Instance.RequestView( view );
			if( node != null )
			{
				this.Record = source;
				this.Parse( node );
				Init();
                if (this.autoDataBind )
                {
					//this.ClearDataBoundAttributes();
					foreach( Widget w in this.Widgets )
					{
						w.BindsTo = t;
						w.DataBindWidget(source,true);
					}
                }
			}
			else
			{
				log.Warn( "object template does not exist", view);
			}
		}
	}
}
