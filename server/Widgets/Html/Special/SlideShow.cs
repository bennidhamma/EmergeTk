using System;
using System.Collections.Generic;
using System.Text;
using EmergeTk.Model;
using System.Threading;

namespace EmergeTk.Widgets.Html
{
    public class SlideShow : Pane, IDataSourced
    {
        Image img,nextImg;
        ImageButton back, forward;

        private int interval = 30000;
        public int Interval
        {
            get { return interval; }
            set { interval = value; }
        }
        
        public string PropertySource { get { return null; } set {} }

        public override string ClientClass
        {
            get
            {
                return "Pane";
            }
        }

        public override void Initialize()
        {
            ClassName = "slideshow";
            img = RootContext.CreateWidget<Image>();
            img.Id = "CurrentImage";
            nextImg = RootContext.CreateWidget<Image>();
            nextImg.SetClientElementStyle("display", "'none'");

            if (dataSource != null)
            {
                img.Url = dataSource[index].Url;
                img.InvokeClientMethod("FadeShow", "2000");
            }

            back = RootContext.CreateWidget<ImageButton>();
            forward = RootContext.CreateWidget<ImageButton>();
            back.Url = ThemeManager.Instance.RequestClientPath( "/Images/Back.png" );
            forward.Url = ThemeManager.Instance.RequestClientPath( "/Images/Forward.png" );
            back.OnClick += new EventHandler<ClickEventArgs>(back_OnClick);
            forward.OnClick += new EventHandler<ClickEventArgs>(forward_OnClick);
            //forward.SetClientElementStyle("float", "'right'");
            Add(img, nextImg, back, forward);
            TimerCallback tc = new TimerCallback(delegate(object o)
            {
                try
                {
                    rollImage();
                }
                catch { }
            });
            picTimer = new Timer(tc, null, interval, interval);
        }

        void forward_OnClick(object sender, ClickEventArgs ea)
        {
            rollImage();
        }

        void back_OnClick(object sender, ClickEventArgs ea)
        {
            if (dataSource != null)
                if (Index == 0)
                    Index = dataSource.Count - 1;
                else
                    Index--;
        }

        private void rollImage()
        {
            if (dataSource != null)
                Index = (Index + 1) % dataSource.Count;
        }

        private IRecordList<Model.ImageRecord> dataSource;

        public IRecordList<Model.ImageRecord> DataSource
        {
            get { return dataSource; }
            set { dataSource = value; }
        }

        bool isDataBound = false;
        public bool IsDataBound
        {
            get
            {
                return isDataBound;
            }
            set
            {
                isDataBound = value;
            }
        }

        int index = 0;
        public int Index
        {
            get
            {
                return index;
            }
            set
            {
                if (value != index)
                {
                    index = value;
                    if (img != null)
                    {
                        img.InvokeClientMethod("FadeOutAndIn", "200,2000");
                        img.Url = dataSource[index].Url;
                        nextImg.Url = dataSource[(index+1)%dataSource.Count].Url;
                    }
                }

            }
        }

        public void DataBind()
        {
            //img.Url = dataSource[index].Url;    
        }

        IRecordList IDataSourced.DataSource
        {
            get
            {
                return DataSource;
            }
            set
            {
                if (value is IRecordList<Model.ImageRecord>)
                    DataSource = value as IRecordList<Model.ImageRecord>;
            }
        }

        public AbstractRecord Selected {
        	get {
        		throw new NotImplementedException();
        	}
        	set {
        		throw new NotImplementedException();
        	}
        }
        
        Timer picTimer;
        ~SlideShow()
        {
            if (picTimer != null)
                picTimer.Dispose();
        }

    }
}
