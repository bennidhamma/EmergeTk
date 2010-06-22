using System;
using System.Collections.Generic;
using System.Text;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Model.Security
{
    public class Group : AbstractRecord
    {
        private string name;
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        private RecordList<User> users;
        public RecordList<User> Users
        {
            get
            {
                if (this.users == null)
                {
                    this.lazyLoadProperty<User>("Users");
                }
                return this.users;
            }
            set { this.users = value; }
        }

        public override Widget GetPropertyEditWidget(Widget parent, ColumnInfo column, IRecordList records)
        {
            switch (column.Name)
            {
                case "Users":
                    SelectList<User> slr = Context.Current.CreateWidget<SelectList<User>>();
                    slr.Mode = SelectionMode.Multiple;
                    slr.LabelFormat = "{Name}";
                    slr.SelectedItems = this.Users;
                    slr.DataSource = DataProvider.LoadList<User>();
                    slr.DataBind();
                    return slr;
                default:
                    return base.GetPropertyEditWidget(parent, column, records);
            }
        }

        public override void Save(bool SaveChildren, bool IncrementVersion, System.Data.Common.DbConnection conn)
        {
            base.Save(SaveChildren, IncrementVersion, conn);
            this.SaveRelations("Users");
        }
    }
}
