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
using System.Collections.Generic;
using System.Text;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
    public class RecordSelect<T> : DropDown,IDataSourced where T : AbstractRecord
    {
    	string propertySource;
    	
        private IRecordList<T> dataSource;
        public IRecordList<T> DataSource
        {
            get { return dataSource; }
            set { dataSource = value; 
            	RaisePropertyChangedNotification("DataSource");
            }
        }

        private T selectedRecord;

        public T SelectedRecord
        {
            get { return selectedRecord; }
            set { 
            	if( this.dataSource == null || ! this.dataSource.Contains( value ) )
            		return;
            	selectedRecord = value;
            	if( value != null )
	            	SelectedId = value.Id.ToString(); 
            	RaisePropertyChangedNotification("SelectedRecord"); 
            }
        }

        public override void Initialize()
        {
            base.OnChanged += new EventHandler<EmergeTk.ChangedEventArgs>(RecordSelect_OnChanged);
			BindsTo = typeof(T);
        }

		public event EventHandler<ChangedEventArgs> OnRecordChanged;
		
        void RecordSelect_OnChanged(object sender, ChangedEventArgs ea)
        {
        	AbstractRecord oldRecord = this.selectedRecord;
            if (this.SelectedIndex == 0 && NullChoice)
            {
            	this.selectedRecord = null;
            }           
            else
            {
            	this.SelectedRecord = this.dataSource[this.SelectedIndex + (NullChoice ? -1 : 0) ];
            }
            if( OnRecordChanged != null )
            {
           		OnRecordChanged( this, new ChangedEventArgs( this, oldRecord,  this.selectedRecord ) );
            }
		}

        private string sourceProperty;

        public string SourceProperty
        {
            get { return sourceProperty; }
            set { sourceProperty = value; }
        }

        private bool nullChoice = true;
        public bool NullChoice
        {
            get { return nullChoice; }
            set { nullChoice = value; }
        }
        
        public void DataBind()
        {
            if (DataSource == null)
            {
            	if( propertySource != null && this.Record != null && this.Record[propertySource] is IRecordList<T> )
					dataSource = this.Record[propertySource] as IRecordList<T>;
				else		
					return;
 			}
            List<string> ids = new List<string>(dataSource.ToIdArray());
            List<string> opts;
            if (sourceProperty != null)
            {
                opts = new List<string>(dataSource.ToStringArray(sourceProperty));
            }
            else
            {
                opts = new List<string>(dataSource.ToStringArray());
            }
            
            if (nullChoice)
            {
                ids.Insert(0, "NULL");
                opts.Insert(0, "--Select--");
            }
            if( SelectedIndex > opts.Count )
            	SelectedIndex = 0;
            Options = opts;
            Ids = ids;
            UpdateClient();
            AbstractRecord.RegisterNewListener(typeof(T), RecordAddedHandler);
            dataBound = true;
        }

        bool dataBound = false;
        public bool IsDataBound { get { return dataBound; } set { dataBound = value; RaisePropertyChangedNotification("IsDataBound");} }

        public RecordSelect() { DefaultProperty = "SelectedRecord"; ClientClass = "DropDown"; }

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
                RaisePropertyChangedNotification("DataSource");
            }
        }

        public virtual string PropertySource {
        	get {
        		return propertySource;
        	}
        	set {
        		propertySource = value;
        		RaisePropertyChangedNotification("PropertySource");
        	}
        }

        #endregion
        public AbstractRecord Selected {
        	get {
        		return SelectedRecord;
        	}
        	set {
        		SelectedRecord = value as T;
			}
		}
		
        public static IDataSourced CreateSelector( int size )
        {
        	if (Context.Current != null ) 
        	{
	        	if( size > int.MaxValue )
	        	{
	        		return Context.Current.CreateWidget<Autocomplete>();
	        	}
	        	else
	        	{
	        		return Context.Current.CreateWidget<RecordSelect<T>>();
	        	}
        	}
        	return null;
        }
    }
}
