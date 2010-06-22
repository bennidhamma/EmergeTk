using System;
using System.Collections.Generic;
using EmergeTk;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
	public class Stack : Generic
	{
		private static new readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(Stack));
		Stack<Widget> stack = new Stack<Widget>();
		Widget currentWidget;
		public event EventHandler<StackEventArgs> OnPush;
		public event EventHandler<StackEventArgs> OnPop;

		public override void Initialize ()
		{
			base.Initialize ();
		}

		public override void Add (Widget c)
		{
			HelpPush( c );
			base.Add( c );
		}

		public void Push( Widget w )
		{
			HelpPush(w);
			base.Add(w);
		}
		
		public void ShowStackChildren()
		{
			log.Debug("stack children:" + this);
			foreach( Widget w in Widgets )
			{
				log.Debug(w);
			}
			log.Debug("done with stack children");
		}
		
		public Widget Peek()
		{
			return currentWidget;
		}

		private void HelpPush(Widget w)
		{
			log.Debug( "PUSHING" ); 
			if(  currentWidget != null )
			{
				currentWidget.Visible = false;
				stack.Push(currentWidget);
			}
			currentWidget = w;
			if( OnPush != null )
				OnPush( this, new StackEventArgs() );
		}
		
		private Widget BuildFrameWidget(Widget source)
		{
			Widget w = null;
			if( source.StateBag.ContainsKey("Widget") )
			{
				Type t = TypeLoader.GetType((string)source.StateBag["Widget"]);
				w = RootContext.CreateUnkownWidget(t,null,this,source.Record);
			}
			else if( source.StateBag.ContainsKey("Template") )
			{
				ObjectViewer objView = RootContext.CreateWidget<ObjectViewer>(this, source.Record);
				objView.Template = (string)source.StateBag["Template"];
				objView.Source = source.Record;				
				w = objView;
			}
			
			System.Diagnostics.Debug.Assert( w != null, "w != null" );
			return w;
		}

		public void Push(object sender, ClickEventArgs ea)
		{			
			Widget w = BuildFrameWidget(ea.Source);
			w.Init();
			w.DataBindWidget(ea.Source.Record);	
		}
		
		public void PushInitDataBind(Widget w, AbstractRecord r)
		{			
			Push(w);
			w.Init();
			w.DataBindWidget(r);
		}
		
		public void PushInit(object sender, ClickEventArgs ea)
		{
			Widget w = BuildFrameWidget(ea.Source);			
			w.Init();
		}

		public Widget Pop()
		{			
			log.Debug( "POPPING" ); 
			if( currentWidget != null )
			{
				currentWidget.Visible = false;
				currentWidget.Remove();
			}
			currentWidget = stack.Pop();
			if( currentWidget != null )
			{
				currentWidget.Visible = true;
			}
			if( OnPop != null )
				OnPop( this, new StackEventArgs() );
			return currentWidget;
		}

		public void Pop(object sender, ClickEventArgs ea)
		{
			Pop();
		}
	}
}
