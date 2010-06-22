/**
 * Project: emergetk: stateful web framework for the masses
 * File name: .cs
 * Description:
 *   
 * @author Ben Joldersma, All-In-One Creations, Ltd. http://all-in-one-creations.net, Copyright (C) 2006.
 *   
 * @see The GNU Public License (GPL)
 */
/* 
 * This program is free software; you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation; either version 2 of the License, or 
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful, but 
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
 * or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
 * for more details.
 * 
 * You should have received a copy of the GNU General Public License along 
 * with this program; if not, write to the Free Software Foundation, Inc., 
 * 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 */
using System;
using System.Collections;
using System.Collections.Generic;
using EmergeTk.Widgets.Html;
using EmergeTk.Model;

namespace EmergeTk
{
	/// <summary>
	/// Summary description for WidgetCollection.
	/// </summary>
	public class WidgetCollection : IEnumerable
	{
		//private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(WidgetCollection));				
		
		private List<Widget> ordered;

		public Widget this[ string key ]
		{
			get
			{
                if (!initialized){ initialize(); return null; }
				foreach( Widget w in ordered )
					if( w.Id == key )
						return w;
				return null;
			}
//			set
//			{
//                if (key == null)
//                {
//                    throw new System.ArgumentNullException("Key", "No valid Id for key.  Did you instantiate with Context.CreateWidget<>?  If not, you must explicitly set the widget's Id.");
//                }
//                Add(value);
//			}
		}

        bool initialized = false;
        private void initialize()
        {
            ordered = new List<Widget>();
            initialized = true;
        }

		public Widget this[ int index ]
		{
			get
			{
				try
				{
	                if (!initialized) { initialize(); return null; }
					return ordered[ index ] as Widget;
				}
				catch( Exception e )
				{
					Debug.Trace("error accessing index: {1}, ordered: {2}, details: {0}, ", Util.BuildExceptionOutput(e),index,ordered );
					foreach( Widget w in ordered )
						Debug.Trace("widget {0} in list.",w);
					throw new Exception("error accessing ordinal based WdigetCollection default indexer.", e);
				}
			}
//			set
//			{
//                Add(value);
//			}
		}

		public TextBox GetTextBox( string key )
		{
			return Find( key ) as TextBox;
		}

		public Label GetLabel( string key )
		{
			return Find( key ) as Label;
		}

		public Button GetButton( string key )
		{
			return Find( key ) as Button;
		}

		public int Count { get { return ordered.Count; } }
        
		public Widget Find( string key )
		{
			Widget theOne = null;
			foreach( Widget c in this )
			{
				if( c.Id == key )
				{
					return c;
				}
				if( c.IsParent )
				{
					theOne = c.Widgets.Find( key );
					if( theOne != null )
					{
						return theOne;
					}
				}
			}
			return theOne;
		}

        public T Find<T>() where T : Widget
        {
        	return Find<T>(null);
        }

		public T Find<T>(string k) where T : Widget
        {
            foreach( Widget c in this )
			{
				if( c is T && ( k == null || c.Id == k ) )
				{
					return (T)c;
				}
				if( c.IsParent )
				{
					T c2 = c.Widgets.Find<T>(k);
					if( c2 != null )
						return c2;
				}
			}
			return null;
        }
        
        public T Find<T,R>(string k, R r) where T : Widget where R : AbstractRecord
        {
            foreach( Widget c in this )
			{
				if( c is T && ( k == null || c.Id == k ) && c.Record is R && ( r == null || c.Record == r ) )
				{
					return c as T;
				}
				if( c.IsParent )
				{
					T c2 = c.Widgets.Find<T,R>(k,r);
					if( c2 != null )
						return c2;
				}
			}
			return null;
        }
		
		public Widget Find(Type type, string k)
        {
            foreach( Widget c in this )
			{
				if( c.GetType() == type && ( k == null || c.Id == k ) )
				{
					return c;
				}
				if( c.IsParent )
				{
					Widget c2 = c.Widgets.Find(type, k);
					if( c2 != null )
						return c2;
				}
			}
			return null;
        }
        
        public List<T> FindAll<T>() where T : class
        {
        	List<T> items = null;
        	foreach( Widget c in this )
			{
				if( c is T )
				{
					if( items == null )
						items = new List<T>();
					items.Add( c as T );
				}
				if( c.Widgets != null )
				{				
					List<T> childItems = c.Widgets.FindAll<T>();
					if( childItems != null )
					{
						if( items == null )
							items = childItems;
						else
							items.AddRange( childItems.ToArray() );
					}
				}
			}
			return items;
        }
		
		public List<Widget> FindAll(Type type)
		{
			List<Widget> items = null;
        	foreach( Widget c in this )
			{
				if( c.GetType() == type )
				{
					if( items == null )
						items = new List<Widget>();
					items.Add( c );
				}
				if( c.Widgets != null )
				{				
					List<Widget> childItems = c.Widgets.FindAll(type);
					if( childItems != null )
					{
						if( items == null )
							items = childItems;
						else
							items.AddRange( childItems.ToArray() );
					}
				}
			}
			return items;
		}

		public Widget Find( Type type )
		{
			foreach( Widget c in this )
			{
				if( c.GetType() == type )
				{
					return c;
				}
				if( c.IsParent )
				{
					Widget c2 = c.Widgets.Find( type );
					if( c2 != null )
						return c2;
				}
			}
			return null;
		}

		public void Remove( Widget c )
		{
			ordered.Remove(c);
		}

		#region IEnumerable Members

		public IEnumerator GetEnumerator()
		{
            if (!initialized) initialize();
			return ordered.GetEnumerator();
		}

		#endregion

		#region IList Members

		public bool IsReadOnly
		{
			get
			{
				// TODO:  Add WidgetCollection.IsReadOnly getter implementation
				return false;
			}
		}

		public void RemoveAt(int index)
		{
			ordered.RemoveAt( index );
		}

		public void Insert(int index, Widget value)
		{
            if (!initialized) initialize();
			ordered.Insert( index, value );
		}

		public bool Contains(Widget value)
		{
            if (!initialized) initialize();
			foreach( Widget w in ordered )
				if( w == value )
					return true;
			return false;
		}

		public void Clear()
		{
            ordered.Clear();
		}

		public int IndexOf(Widget value)
		{
			return ordered.IndexOf( value );
		}

		public int Add(Widget value)
		{
            if (!initialized) initialize();
			ordered.Add( value );
            return ordered.Count - 1;
		}

		public bool IsFixedSize
		{
			get
			{
				// TODO:  Add WidgetCollection.IsFixedSize getter implementation
				return false;
			}
		}

		#endregion

	}
}
