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
    public enum SelectionMode
    {
        Single,
        Multiple
    }

	/// <summary>
	/// If no <SelectItem/> children are present, becomes a multiselect list.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class SelectList<T> : Repeater<T> where T : AbstractRecord, new()
    {
        public SelectList()
        {
            this.OnRowAdded += SelectList_OnRowAdded;
		}

        private SelectionMode mode;
        public SelectionMode Mode
        {
            get { return mode; }
            set { mode = value;
            	RaisePropertyChangedNotification("Mode");
            }
        }

		private string group;
		public string Group
		{
			get { return group; }
			set { group = value; }
		}

		public bool RowSelect { get; set; }

		private string labelFormat;
        public string LabelFormat
        {
            get { return labelFormat; }
            set { 
                labelFormat = value;
//              if (Initialized)
//              {
//                  selectLabel["Html"] = labelFormat;
                    //DataBindWidget();
//              }
                RaisePropertyChangedNotification("LabelFormat");
            }
        }

        //private Label selectLabel;
        public override void Initialize()
        {        	
        	if( ViewTemplate == null )
			{
        		Label selectLabel = RootContext.CreateWidget<Label>(this);
        		selectLabel.TagName = "label";
        		Literal l = RootContext.CreateWidget<Literal>(selectLabel);
        		if( ! String.IsNullOrEmpty(labelFormat) )
	            {
	            	//call indexer to trigger databinding test.
	            	l["Html"] = labelFormat;
	            	selectLabel.ClassName = "selectLabel";                
	            }
	            SelectItem si = RootContext.CreateWidget<SelectItem>(selectLabel);
	            si.Mode = this.mode;
	            si.Init();
	            si.OnChanged += new EventHandler<EmergeTk.ChangedEventArgs>(widget_OnChanged);
        	}
			else
			{
				InitializeViewTemplate();
				if (!RowSelect)
				{
					SelectItem si = Template.Find<SelectItem>();
					if (si != null)
					{
						si.Mode = this.Mode;
						if (group != null)
							si.Group = group;
					}
				}
			}
            base.Initialize();
        }

        private IRecordList<T> selectedItems;
        public IRecordList<T> SelectedItems
        {
            get 
            {            	
            	return this.selectedItems; 
            }
            set
     		{
                if (this.selectedItems != null)
                {
                    this.selectedItems.OnRecordAdded -= SelectedItems_OnUpdate;
                    this.selectedItems.OnRecordRemoved -= SelectedItems_OnUpdate;
                }
                this.selectedItems = value;
                if( this.selectedItems != null )
                {
                	this.selectedItems.OnRecordAdded += SelectedItems_OnUpdate;
                	this.selectedItems.OnRecordRemoved += SelectedItems_OnUpdate;
                }
                this.SyncSelectedItems();
            }
        }

        private void SyncSelectedItems()
      	{
            if (this.IsDataBound )
            {
                List<Template> templateList = this.FindAll<Template>();
                if( templateList != null )
                {
	                foreach (Template template in templateList)
	                {
						SetSelected(template, GetSelected(template));
	                }
	            }
            }
        }

		private bool GetSelected(Template t)
		{
			return this.SelectedItems.Contains(t.Record);
		}

		private void SetSelected(Template t, bool selected)
		{
			if (RowSelect)
			{
				if (selected)
					t.AppendClass("selected");
				else
					t.RemoveClass("selected");
			}
			else
			{
				SelectItem si = t.Find<SelectItem>();
				if (si != null)
				{
					si.Selected = selected;
				}
			}
		}

		public void ToggleItemSelection(T t)
		{
			Template templateItem = null;
			List<Template> items = FindAll<Template>();
			foreach (Template item in items)
           	{
				if (item.Record != t && this.mode == SelectionMode.Single)
					SetSelected(item, false);
                if( item.Record == t )
                {
                	templateItem = item;
                	//TODO: add a check to selectionMode if we want to prevent deselecting
                	//the sole item in a singlemode select list.
					SetSelected(item, !GetSelected(templateItem));
                }
            }

			if (templateItem != null)
			{
				if (this.SelectedItems != null)
				{
					if (GetSelected(templateItem) && !this.SelectedItems.Contains(t))
					{
						this.SelectedItems.Add(t);
					}
					else if (!GetSelected(templateItem) && this.SelectedItems.Contains(t))
					{
						this.SelectedItems.Remove(t);
					}
				}
			}
		}

		public void SelectAll()
		{
			if( this.mode == SelectionMode.Single )
			{
				throw new InvalidOperationException("Cannot select all on selection mode single.");
			}			
			
			this.SelectedItems = this.DataSource.Copy() as IRecordList<T>;
			this.InvokeChangedEvent(null, this.SelectedItems);
		}

		public void SelectNone()
		{
			if( this.mode == SelectionMode.Single )
			{
				throw new InvalidOperationException("Cannot select none on selection mode single.");
			}
			
			this.SelectedItems = new RecordList<T>();
			this.InvokeChangedEvent(null, this.SelectedItems);
		}

		void ItemSelectHandler(object sender, Template t)
		{
			if (this.mode == SelectionMode.Single)
			{
				System.Console.WriteLine("disabling other items");
				this.SelectedItems.Clear();
			}

			if (t != null)
			{
				bool oldState, newState = false;
				if (RowSelect)
				{
					oldState = GetSelected(t);
					newState = !oldState;
					SetSelected(t, newState);
				}
				else
				{
					SelectItem si = (SelectItem)sender;
					oldState = !si.Selected;
					newState = si.Selected;
				}
				AbstractRecord sRecord = t.Record;
				if (sRecord == null)
					throw new Exception("How is record null?");
				if (this.SelectedItems != null)
				{
					if (newState && !this.SelectedItems.Contains(sRecord))
					{
						this.SelectedItems.Add(sRecord);
					}
					else if (!newState && this.SelectedItems.Contains(sRecord))
					{
						this.SelectedItems.Remove(sRecord);
					}
				}
				InvokeChangedEvent(oldState, newState);
			}

		}

		/// <summary>
		/// Only called if we have a SelectItem
		/// </summary>
        void widget_OnChanged(object sender, ChangedEventArgs ea)
        {
			ItemSelectHandler(sender, ea.Source.FindAncestor<Template>());
        }

		void row_OnClick(object sender, ClickEventArgs e)
		{
			ItemSelectHandler(sender, e.Source as Template);
		}

		void SelectList_OnRowAdded(object sender, RowEventArgs<T> e)
        {
            if (this.selectedItems != null && this.selectedItems.Contains(e.Record))
            {
				SetSelected(e.Template, true);
			}
            if( ViewTemplate != null )
            {
				if (!RowSelect)
				{
					SelectItem si = e.Template.Find<SelectItem>();
					if (si != null)
					{
						si.OnChanged += widget_OnChanged;
					}
				}
				else
				{// multi-select list
					e.Template.OnClick += row_OnClick;
				}
			}
        }

        private void SelectedItems_OnUpdate(object sender, RecordEventArgs ea )
        {
            this.SyncSelectedItems();
        }
    }
}
