using System;
using System.Collections.Generic;
using System.Text;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
	/// <summary>
	/// ListBuilder presents two side by side SelectLists with Add/Remove buttons to move items between lists
	/// </summary>
	public class ListBuilder<T> : Generic, IDataBindable, IDataSourced where T : AbstractRecord, new()
	{
		#pragma warning disable 67
		//TODO: what is intended here?
		public event EventHandler OnSelectedOptionsChanged;
		#pragma warning restore 67

		private bool isDataBound = false;
		private IRecordList<T> availableOptions = null;
		private IRecordList<T> selectedOptions = null;
		private SelectList<T> availableOptionsSelectList;
		private SelectList<T> selectedOptionsSelectList;

		private Button addAll;
		private Button addSelected;
		private Button removeSelected;
		private Button removeAll;

		public IRecordList<T> AvailableOptions
		{
			get { return availableOptions; }
			set
			{
				availableOptions = value;
				if (availableOptionsSelectList != null)
				{
					availableOptionsSelectList.DataSource = value;
					availableOptionsSelectList.DataBind();
					ClearSelected();
					RaisePropertyChangedNotification("AvailableOptions");
				}
			}
		}

		public IRecordList<T> SelectedOptions
		{
			get { return selectedOptions; }
			set
			{
				selectedOptions = value;
				if (selectedOptionsSelectList != null)
				{
					selectedOptionsSelectList.DataSource = value;
					selectedOptionsSelectList.DataBind();
					RaisePropertyChangedNotification("SelectedOptions");
					if (OnSelectedOptionsChanged != null)
						OnSelectedOptionsChanged(this, null);
				}
			}
		}

		private string viewTemplate;
		public string ViewTemplate
		{
			get
			{
				return viewTemplate;
			}
			set
			{
				viewTemplate = value;
				if (availableOptionsSelectList != null)
					availableOptionsSelectList.ViewTemplate = value;
				if (selectedOptionsSelectList != null)
					selectedOptionsSelectList.ViewTemplate = value;
			}
		}

		public override void Initialize()
		{
			addAll = Find<Button>("buttonAddAll");
			addSelected = Find<Button>("buttonAddSelected");
			removeSelected = Find<Button>("buttonRemoveSelected");
			removeAll = Find<Button>("buttonRemoveAll");

			availableOptionsSelectList = Find<SelectList<T>>("availableOptions");
			if (availableOptionsSelectList != null)
			{
				availableOptionsSelectList.ViewTemplate = ViewTemplate;
				availableOptionsSelectList.SelectedItems = new RecordList<T>();
				availableOptionsSelectList.DataSource = AvailableOptions ?? availableOptionsSelectList.SelectedItems;
				availableOptionsSelectList.DataBind();
				availableOptionsSelectList.OnChanged += AvailableOptions_OnChanged;
				availableOptionsSelectList.SelectNone();
			}
			selectedOptionsSelectList = Find<SelectList<T>>("selectedOptions");
			if (selectedOptionsSelectList != null)
			{
				selectedOptionsSelectList.ViewTemplate = ViewTemplate;
				selectedOptionsSelectList.SelectedItems = new RecordList<T>();
				selectedOptionsSelectList.DataSource = SelectedOptions ?? selectedOptionsSelectList.SelectedItems;
				selectedOptionsSelectList.DataBind();
				selectedOptionsSelectList.OnChanged += SelectedOptions_OnChanged;
				selectedOptionsSelectList.SelectNone();
			}

			base.Initialize();
		}

		private void ClearSelected()
		{
			availableOptionsSelectList.SelectedItems = new RecordList<T>();
			availableOptionsSelectList.SelectNone();
			selectedOptionsSelectList.SelectedItems = new RecordList<T>();
			selectedOptionsSelectList.SelectNone();
		}

		void AvailableOptions_OnChanged(object sender, ChangedEventArgs e)
		{
			addSelected.Enabled = availableOptionsSelectList.SelectedItems.Count != 0;
			addAll.Enabled = (this.AvailableOptions ?? new RecordList<T>()).Count != 0;
		}

		void SelectedOptions_OnChanged(object sender, ChangedEventArgs e)
		{
			removeSelected.Enabled = selectedOptionsSelectList.SelectedItems.Count != 0;
			removeAll.Enabled = (this.SelectedOptions ?? new RecordList<T>()).Count != 0;
		}

		public void AddAll_OnClick(object sender, ClickEventArgs ea)
		{
			if (availableOptionsSelectList != null && availableOptions != null)
			{
				IRecordList<T> selected = SelectedOptions ?? new RecordList<T>();
				foreach (T t in AvailableOptions)
				{
					if( ! selected.Contains( t ) )
						selected.Add(t);
				}
				selected.Sort(new SortInfo("Name"));
				SelectedOptions = selected;
				AvailableOptions = new RecordList<T>();

				ClearSelected();
				if (null != addAll)
					addAll.Enabled = false;
				if (null != addSelected)
					addSelected.Enabled = false;
			}
		}

		public void AddSelected_OnClick(object sender, ClickEventArgs ea)
		{
			if (availableOptionsSelectList != null)
			{
				IRecordList<T> availableSelected = availableOptionsSelectList.SelectedItems;
				if (availableSelected.Count > 0)
				{
					IRecordList<T> available = AvailableOptions;
					IRecordList<T> selected = SelectedOptions ?? new RecordList<T>();

					foreach (T t in availableSelected)
					{
						if( ! selected.Contains( t ) )
							selected.Add(t);
						available.Remove(t);
					}
					selected.Sort(new SortInfo("Name"));
					AvailableOptions = available;
					SelectedOptions = selected;
					ClearSelected();
				}
			}
		}

		public void RemoveSelected_OnClick(object sender, ClickEventArgs ea)
		{
			if (selectedOptionsSelectList != null)
			{
				IRecordList<T> selectedSelected = selectedOptionsSelectList.SelectedItems;
				if (selectedSelected.Count > 0)
				{
					IRecordList<T> available = AvailableOptions ?? new RecordList<T>();
					IRecordList<T> selected = SelectedOptions;

					foreach (T t in selectedSelected)
					{
						if( ! available.Contains( t ) )
							available.Add(t);
						selected.Remove(t);
					}
					available.Sort(new SortInfo("Name"));
					AvailableOptions = available;
					SelectedOptions = selected;
					ClearSelected();
				}
			}
		}

		public void RemoveAll_OnClick(object sender, ClickEventArgs ea)
		{
			if (selectedOptionsSelectList != null && selectedOptions != null)
			{
				IRecordList<T> available = AvailableOptions ?? new RecordList<T>();
				foreach (T t in SelectedOptions)
				{
					if( ! available.Contains( t ) )
						available.Add(t);
				}
				available.Sort(new SortInfo("Name"));
				AvailableOptions = available;
				SelectedOptions = new RecordList<T>();

				ClearSelected();
				if (null != removeAll)
					removeAll.Enabled = false;
				if (null != removeSelected)
					removeSelected.Enabled = false;
			}
		}

		#region IDataSourced Members

		public IRecordList DataSource
		{
			get
			{
				return AvailableOptions;
			}
			set
			{
				AvailableOptions = (IRecordList<T>)value;
			}
		}

		public bool IsDataBound
		{
			get
			{
				return isDataBound;
			}
		}

		public void DataBind()
		{
			// do nothing because AvailableOptions property does this
		}

		public string PropertySource
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public AbstractRecord Selected
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		#endregion
	}
}
