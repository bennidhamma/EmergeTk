using System;
using System.Collections.Generic;
using EmergeTk.Model;
using EmergeTk.Model.Security;
using EmergeTk.Widgets.Html;

namespace EmergeTk
{
	//
	//Context EventArgs
	//
	
	public class MouseEventArgs : EventArgs {
		public int X, Y;
		public MouseEventArgs( int x, int y ) { X = x; Y = y; }
	}
	
	public class UserEventArgs : EventArgs
    {
        public UserEventArgs(User user)
        {
            this.user = user;
        }

        private User user;
        public User User
        {
            get { return this.user; }
        }

        // TODO: do we want to include an enum of what is happening to the user? or will the event that was raised tell us what we need?
    }
    
    public class CometEventArgs : EventArgs {
    
    }
    
    public class ContextEventArgs : EventArgs {
    	
    }
    
    public class BookmarkEventArgs : EventArgs {
    	public string Bookmark;
    	public BookmarkEventArgs( string bookmark )
    	{
    		this.Bookmark = bookmark;
    	}
    }
    
    public class HistoryFrameEventArgs : EventArgs {
    	public string Bookmark;
    	public HistoryFrameEventArgs( string bookmark )
    	{
    		this.Bookmark = bookmark;
    	}
    }
    
    //
    //Widget EventArgs
    //
    
    public class DelayedMouseEventArgs : EventArgs {
    	public Widget Source;
    	public int MouseX, MouseY;
    	public DelayedMouseEventArgs( Widget source, int x, int y )
    	{
    		Source = source;
    		MouseX = x;
    		MouseY = y;
    	}
    }
    
    public class DragAndDropEventArgs : EventArgs {
    	public Widget DroppedWidget;
    	public Widget Destination;
    	public int DropPosition;
    	public DragAndDropEventArgs( Widget droppedWidget, Widget destination, int position )
    	{
    		DroppedWidget = droppedWidget;
    		Destination = destination;
    		DropPosition = position;
    	}
    }
    
    public class ClickEventArgs : EventArgs {
    	public Widget Source;
    	
    	public ClickEventArgs(Widget source)
    	{
    		Source = source;
    	}
    }

	public class KeyPressEventArgs : EventArgs {
    	public Widget Source;
    	public int Code;
		public string Value;
    	public KeyPressEventArgs(Widget source, string code)
    	{
			Dictionary<string,object> jsonData = (Dictionary<string,object>)JSON.Default.Decode(code);
			
    		Source = source;
			Value = jsonData["value"] as string;
			Code = int.Parse(jsonData["code"] as string);
    	}
    }
    
    public class CloneEventArgs : EventArgs {
    	public Widget Source, Destination;
    	
    	public CloneEventArgs(Widget source, Widget destination)
    	{
    		Source = source;
    		Destination = destination;
    	}
    }
    
    public class ChangedEventArgs : EventArgs {
    	public Widget Source;
    	public object OldValue;
    	public object NewValue;
    	
    	public ChangedEventArgs( Widget source, object oldValue, object newValue )
    	{
    		Source = source;
    		OldValue = oldValue;
    		NewValue = newValue;
    	}
    }
    
    public class WidgetEventArgs : EventArgs
    {
    	public Widget Source;
    	public string Args;
    	public object Other;
    	
    	public WidgetEventArgs( Widget source, string args, object other )
    	{
    		Source = source;
    		Args = args;
    		Other = other;
    	}
    }
    
    public class TreeNodeSelectedEventArgs : EventArgs {
    	public Tree Source;
    	public TreeNode SelectedNode;
    	public TreeNodeSelectedEventArgs( Tree source, TreeNode selectedNode )
    	{
    		Source = source;
    		SelectedNode = selectedNode;
    	}
    }
    
    public class WikiEventArgs : EventArgs {
    	public WikiPane Pane;
    	public WikiPage Page;
    	
    	public WikiEventArgs( WikiPane pane, WikiPage page )
    	{
    		Pane = pane;
    		Page = page;
    	}
    }
    
    public class MessageBoxEventArgs : EventArgs {
    	public MessageBox MessageBox;
    	public MessageBoxButtons ButtonPressed;
    	
    	public MessageBoxEventArgs( MessageBox messageBox, MessageBoxButtons buttonPressed )
    	{
    		MessageBox = messageBox;
    		ButtonPressed = buttonPressed;
    	}
    }
    
    public class TabPaneSelectedEventArgs : EventArgs {
    	public Pane SelectedTab;
    	
    	public TabPaneSelectedEventArgs( Pane selectedTab )
    	{
    		SelectedTab = selectedTab;
    	}
    }
    
    public class SvgPanEventArgs : EventArgs {
    	public EmergeTk.Widgets.Svg.Host Host;
    	public int DX, DY;
    }
    
    //
    //Record EventArgs
    //
    
    public class RecordEventArgs : EventArgs {
    	public AbstractRecord Record;
    	public RecordEventArgs( AbstractRecord record )
    	{
    		Record = record;	
    	}
    }
    
     public class ModelFormEventArgs<T> : EventArgs where T : AbstractRecord, new() 
     {
    	public ModelForm<T> Form;
    	public ModelFormEventArgs( ModelForm<T> form )
    	{
    		Form = form;
    	}
    }
    
    public class ModelFormDeleteEventArgs<T> : ModelFormEventArgs<T> where T : AbstractRecord, new() 
     {
    	public bool Abort;
    	public ModelFormDeleteEventArgs(ModelForm<T> form):base( form )
    	{
    	}
    }

	public class StackEventArgs : EventArgs
	{
		
	}
}
