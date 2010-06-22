using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Svg
{
	public enum ChartType
	{
		Bar,
		Line,
		Scatter,
		Pie
	}

    public class Chart<T> : Host, IDataSourced where T : AbstractRecord
	{

		public AbstractRecord Selected {
    		get {
    			throw new NotImplementedException();
    		}
    		set {
    			throw new NotImplementedException();
    		}
    	}

        public event EventHandler<DelayedMouseEventArgs> OnNodeDelayedMouseOverHandler;
        public event EventHandler<DelayedMouseEventArgs> OnNodeDelayedMouseOutHandler;
        public event EventHandler<ClickEventArgs> OnNodeClickedHandler;
        public event EventHandler<DragAndDropEventArgs> 
            OnXReceiveDropHandler, 
            OnYReceiveDropHandler, 
            OnColorReceiveDropHandler,
            OnSizeReceiveDropHandler;

		string propertySource;
		public string PropertySource { get { return propertySource; } set { propertySource = value; } }
		
		private string xSeries;
        private bool xRequiresBinding = false;
		/// <summary>
		/// Property XSources (string)
		/// </summary>
        public string XSeries
		{
			get
			{
				return this.xSeries;
			}
			set
			{
				this.xSeries = value;
                xRequiresBinding = true;
			}
		}

        private string ySeries;
        private bool yRequiresBinding = false;
		/// <summary>
		/// Property YSources (string)
		/// </summary>
        public string YSeries
		{
			get
			{
				return this.ySeries;
			}
			set
			{
				this.ySeries = value;
                yRequiresBinding = true;
			}
		}

        private string sizeSeries;
        private bool sizeRequiresBinding = false;
        public string SizeSeries
        {
            get { return sizeSeries; }
            set { sizeSeries = value; sizeRequiresBinding = true; }
        }

        private string colorSeries;
        private bool colorRequiresBinding = false;
        public string ColorSeries
        {
            get { return colorSeries; }
            set { colorSeries = value; colorRequiresBinding = true; }
        }
		
        private string yLabel;
		
		/// <summary>
		/// Property YLabel (string)
		/// </summary>
		public string YLabel
		{
			get
			{
				return this.yLabel;
			}
			set
			{
				this.yLabel = value;
			}
		}

		private string xLabel;
		
		/// <summary>
		/// Property XLabel (string)
		/// </summary>
		public string XLabel
		{
			get
			{
				return this.xLabel;
			}
			set
			{
				this.xLabel = value;
			}
		}

		private ChartType type;
		
		/// <summary>
		/// Property Type (ChartType)
		/// </summary>
		public ChartType Type
		{
			get
			{
				return this.type;
			}
			set
			{
				this.type = value;
			}
		}

        private IRecordList<T> dataSource;
		
		/// <summary>
		/// Property DataSource (RecordList)
		/// </summary>
        public IRecordList<T> DataSource
		{
			get
			{
				return this.dataSource;
			}
			set
			{
				this.dataSource = value;
			}
		}

        private Dictionary<T,Widget> recordToPoints = new Dictionary<T,Widget>();

        public Dictionary<T,Widget> RecordToPoints
        {
            get { return recordToPoints; }
            set { recordToPoints = value; }
        }

        private Rect background;
        public Rect Background { get { return background; } }

		public Chart()
		{
			this.ClientClass = "SvgHost";
		}

        private Svg.Group g;

        float
                xmin, xmax, xdelta,
                ymin, ymax, ydelta,
                colormin, colormax, colordelta,
                sizemin, sizemax, sizedelta;

        Type xType, yType;

		public void DataBind()
		{
        	if (DataSource == null)
            {
            	if( propertySource != null && this.Record != null && this.Record[propertySource] is IRecordList<T> )
					dataSource = this.Record[propertySource] as IRecordList<T>;
				else		
					return;
 			}
		    if( dataSource == null || dataSource.Count == 0 )
				return;
            findMinMax(xSeries, out xmin, out xmax, out xdelta);
            findMinMax(ySeries, out ymin, out ymax, out ydelta);
            findMinMax(colorSeries, out colormin, out colormax, out colordelta);
            findMinMax(sizeSeries, out sizemin, out sizemax, out sizedelta);
            if (!rendered)
            {
                setupGradients();
                g = RootContext.CreateWidget<Svg.Group>();
                g.Scale(1, -1);
                g.Translate(0, -900);
                background = g.DrawRect(0, 0, 1000, 1000, "url(#backgroundGradient)");
                
                for (int i = 0; i <= 10; i++)
                {
                    Line gridX = new Line("xgrid" + i, i * 100, 0, i * 100, 1000, Color.DarkGray);
                    Line gridY = new Line("xgrid" + i, 0, i * 100, 1000, i * 100, Color.DarkGray);
                    g.Add(gridX, gridY);
                }
                g.DrawLine(0, 0, 0, 1000, Color.Black);
                g.DrawLine(0, 0, 1000, 0, Color.Black);
                Add(g);

                //drag regions
                if (OnXReceiveDropHandler != null && OnYReceiveDropHandler != null)
                {
                    Rect yAxisDropTarget = new Rect("yAxisDropTarget", -50, 0, 50, 1000, "blue");
                    yAxisDropTarget.ClassName = "dragTarget";
                    yAxisDropTarget.OnReceiveDrop += OnYReceiveDropHandler;
                    Rect xAxisDropTarget = new Rect("xAxisDropTarget", 0, -50, 1000, 50, "blue");
                    xAxisDropTarget.ClassName = "dragTarget";
                    xAxisDropTarget.OnReceiveDrop += OnXReceiveDropHandler;
                    g.Add(xAxisDropTarget, yAxisDropTarget);
                }
            }

            addLegend();

            xType = DataSource[0][xSeries].GetType();
            yType = DataSource[0][ySeries].GetType();
			
            ViewBox = new Rectangle( -100, -100, 1200, 1100 );

            foreach (T r in dataSource)
                DrawPoint(r);
                
			xRequiresBinding = yRequiresBinding = sizeRequiresBinding = colorRequiresBinding = false;
            setLabels();
            dataBound = true;
		}

        bool dataBound = false;
        public bool IsDataBound { get { return dataBound; } set { dataBound = value; } }

        public void DrawPoint(T r)
        {
            float x = coerceValue(r[xSeries]);
            float y = coerceValue(r[ySeries]);
            float color = coerceValue(r[colorSeries]);
            float size = coerceValue(r[sizeSeries]);

            x = prepareValue(xmin, xdelta, x, 1000);
            y = prepareValue(ymin, ydelta, y, 1000);
            size = prepareValue(sizemin, sizedelta, size, 25) + 5;
            string colorString = colorFromRange(colormin, colormax, colordelta, color);
            if (!rendered)
            {
                Circle c = g.DrawCircle(x, y, size, colorString, "black");
                if (OnNodeDelayedMouseOverHandler != null)
                    c.OnDelayedMouseOver += OnNodeDelayedMouseOverHandler;
                if (OnNodeDelayedMouseOutHandler != null)
                    c.OnDelayedMouseOut += OnNodeDelayedMouseOutHandler;
                if( OnNodeClickedHandler != null )
                    c.OnClick += OnNodeClickedHandler;
                c.Record = r;
                this.recordToPoints[r] = c;
            }
            else
            {
                if( ! recordToPoints.ContainsKey(r) ) return;
                Circle c = recordToPoints[r] as Circle;
                if (xRequiresBinding || c.X != x)
                    c.X = x;
                if (yRequiresBinding || c.Y != y)
                    c.Y = y;
                if (sizeRequiresBinding || c.R != size)
                    c.R = size;
                if (colorRequiresBinding || c.Fill != colorString)
                    c.Fill = colorString;
            }
        }

        private void setupGradients()
        {
            Gradient gr = RootContext.CreateWidget<Gradient>();
            gr.Stops = string.Format("0% {0} 100% {1}", colorStart.ToHtmlColor(), colorEnd.ToHtmlColor());
            gr.GradientId = "colorSeriesGradient";
            gr.Type = GradientType.linearGradient;
            gr.Direction = new Vector(0, 1, 0);
            Add(gr);

            gr = RootContext.CreateWidget<Gradient>();
            gr.Stops = "0% #ccd 100% #ffe";
            gr.GradientId = "backgroundGradient";
            gr.Type = GradientType.linearGradient;
            gr.Direction = new Vector(1, 1, 0);
            Add(gr);
        }

        private Text colorKeyLabel, 
            sizeKeyLabel, 
            xKeyLabel, 
            yKeyLabel,
            colorMinLabel, 
            colorMaxLabel, 
            sizeMinLabel,
            sizeMaxLabel;
        private Svg.Group xLabelsGroup, yLabelsGroup;
        private void addLegend()
        {
            if (!rendered)
            {
                Svg.Group legendValues = RootContext.CreateWidget<Svg.Group>();
                legendValues.Id = "legendValues";
                legendValues.Scale(1, -1);
                Rect r = new Rect("colorKey", 1020, 100, 50, 200, "url(#colorSeriesGradient)");
                if (OnColorReceiveDropHandler != null)
                {
                    r.OnReceiveDrop += OnColorReceiveDropHandler;
                }

                Path p = RootContext.CreateWidget<Path>();
                p.Fill = "Black";
                p.D = "m1036,500 l12,0 l20,200 l-50,0z";
                g.Add(p);
                if (OnSizeReceiveDropHandler != null)
                {
                    p.OnReceiveDrop += OnSizeReceiveDropHandler;
                }

                colorMinLabel = legendValues.DrawText(1045, -80, colormin.ToString(), "middle", fontSize);
                colorMaxLabel = legendValues.DrawText(1045, -320, colormax.ToString(), "middle", fontSize);
                sizeMinLabel = legendValues.DrawText(1045, -480, sizemin.ToString(), "middle", fontSize);
                sizeMaxLabel = legendValues.DrawText(1045, -710, sizemax.ToString(), "middle", fontSize);
                g.Add(r, legendValues);
                Svg.Group legendLabels = RootContext.CreateWidget<Svg.Group>();
                legendLabels.Id = "legendLabels";
                legendLabels.Scale(1, -1);
                legendLabels.Rotate(-90);
                colorKeyLabel = legendLabels.DrawText(100, 1100, colorSeries, "start", fontSize);
                sizeKeyLabel = legendLabels.DrawText(500, 1100, sizeSeries, "start", fontSize);
                g.Add(legendLabels);
            }
            else
            {
                if (colormin.ToString() != colorMinLabel.InnerText)
                {
                    colorKeyLabel.InnerText = colorSeries;
                    colorMinLabel.InnerText = colormin.ToString();
                    colorMaxLabel.InnerText = colormax.ToString();
                }
                if (sizemin.ToString() != sizeMinLabel.InnerText)
                {
                    sizeKeyLabel.InnerText = sizeSeries;
                    sizeMinLabel.InnerText = sizemin.ToString();
                    sizeMaxLabel.InnerText = sizemax.ToString();
                }
            }
        }

        private float prepareValue(float min, float delta, float current, float newMax)
        {
            return ((current - min) / delta) * newMax;
        }

        Vector colorStart = new Vector(100, 50, 0);
        Vector colorEnd = new Vector(255,150,255);

        private string colorFromRange(float min, float max, float delta, float current)
        {
            Vector colorStart = new Vector(100, 50, 0);
            Vector colorEnd = new Vector(255, 150, 255);
            Vector colorRange = colorEnd - colorStart;
            Vector newColor = (colorRange * ((current - min) / delta)) + colorStart;
            return newColor.ToHtmlColor();
        }

        private void findMinMax(string column, out float min, out float max, out float delta)
        {
            Type t = DataSource[0][column].GetType();
            min = max = coerceValue(DataSource[0][column]);

            foreach (T r in dataSource)
            {
                float x = coerceValue(r[column]);
                if( x < min ) min = x;
                else if (x > max ) max = x;
            }

            delta = max - min;

            min = (float)Math.Floor(min);
            min = (float)Math.Ceiling(min);
            
            if (delta < 10 || t.IsEnum)
            {
                min -= 1;
                max += 1;
            }
            else if( t == typeof(DateTime) )
            {
                min *= 0.9999f;
                max *= 1.0001f;
            }
            else
            {
                min -= min % 5;
                max += 5 - min % 5;
            }
            delta = max - min;
        }

        private float coerceValue(object o)
        {
            if (o is DateTime)
            {
                DateTime d = (DateTime)o;
                return (float)d.Ticks;
            }

            else if (o is IConvertible)
            {
                return Convert.ToSingle(o);
            }

            /*if (o.GetType().IsEnum)
            {
                return (float)((int)o);
            }*/

            throw new System.ArgumentOutOfRangeException("o", o, "could not convert o to float.");
        }
        /*
        private object originalize(float f, Type t)
        {
            int num_quanta = 10;
            if (t.IsEnum)
            {
                f = (float)Math.Round(f);
                
            }
            else if (t.IsSubclassOf(typeof(Record)))
            {

            }
        }*/

        List<string> xlabels, ylabels;
        
        private void buildLabels(float min, float max, float delta, List<string> labels, Type t)
        {
            int numTicks = 10;
            if (delta < numTicks)
                numTicks = (int)delta;
            if (t == typeof(DateTime))
            {
                numTicks = 5;
                bool inDays = true;
                DateTime minDate = new DateTime((long)min);
                TimeSpan ts = new TimeSpan((long)delta);
                if( ts.Days < 1 )
                {
                    inDays = false;
                }
                float space = delta / numTicks;

                for (int i = 0; i < numTicks; i++)
                {
                    TimeSpan currTimeSpan = new TimeSpan((long)space*i);
                    DateTime currDate = minDate + currTimeSpan;
                    if( inDays )
                        labels.Add(currDate.ToShortDateString());
                    else
                        labels.Add(currDate.ToShortTimeString());
                }
            }
            else if (t.IsEnum)
            {
                //enums are buffered by 1 on each side.
                labels.Add(string.Empty);
                labels.AddRange(Enum.GetNames(t));
                labels.Add(string.Empty);
            }
            else
            {
                float space = delta / numTicks;
                for (int i = 0; i < numTicks; i++)
                {
                    labels.Add((min + (space * i)).ToString("N0"));
                }
            }
        }

        int fontSize = 25, headerSize = 35;
        private void setLabels()
        {
            xlabels = new List<string>();
            ylabels = new List<string>();

            buildLabels( xmin, xmax, xdelta, xlabels, xType);
            buildLabels( ymin, ymax, ydelta, ylabels, yType );

            if (!rendered || this.xKeyLabel.InnerText != XSeries)
            {
                if (rendered)
                {
                    xLabelsGroup.Remove();
                    xKeyLabel.InnerText = XSeries;
                }
                xLabelsGroup = RootContext.CreateWidget<Svg.Group>();
                xLabelsGroup.Translate(0, (int)(-1.5 * fontSize));
                xLabelsGroup.Scale(1, -1);
                g.Add(xLabelsGroup);
                xKeyLabel = xLabelsGroup.DrawText(500, 30, xSeries, "middle", headerSize);
                int quanta_length = 1000 / xlabels.Count;
                for (int i = 0; i < xlabels.Count; i++)
                {
                    xLabelsGroup.DrawText(i * quanta_length, 0, xlabels[i], "middle", fontSize);
                }
            }

            if (!rendered || yKeyLabel.InnerText != YSeries)
            {
                if (rendered)
                {
                    yLabelsGroup.Remove();
                    yKeyLabel.InnerText = YSeries;
                }
                yLabelsGroup = RootContext.CreateWidget<Svg.Group>();
                yLabelsGroup.Translate((int)(-1.2 * fontSize), 0);
                yLabelsGroup.Scale(1, -1);
                yLabelsGroup.Rotate(-90);
                g.Add(yLabelsGroup);
                yKeyLabel = yLabelsGroup.DrawText(500, -10, ySeries, "middle", headerSize);
                int quanta_length = 1000 / ylabels.Count;
                for (int i = 0; i < ylabels.Count; i++)
                {
                    yLabelsGroup.DrawText(i * quanta_length, 10, ylabels[i], "middle", fontSize);
                }
            }
        }

        #region IDataSourced Members

        IRecordList IDataSourced.DataSource
        {
            get
            {
                return dataSource as IRecordList;
            }
            set
            {
                dataSource = value as IRecordList<T>;
            }
        }

        #endregion
    }
}
