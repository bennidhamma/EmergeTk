using System;

namespace EmergeTk.Model
{
	[Obsolete]
    public class DataGridColumn
    {
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private IDataBindable editTemplate;
        public IDataBindable EditTemplate
        {
            get { return editTemplate; }
            set { editTemplate = value; }
        }

        private Widget viewTemplate;
        public Widget ViewTemplate
        {
            get { return viewTemplate; }
            set { viewTemplate = value; }
        }

        private Widget headerTemplate;
        public Widget HeaderTemplate
        {
            get { return headerTemplate; }
            set { headerTemplate = value; }
        }

        public DataGridColumn() { }

        public DataGridColumn(string name, Widget viewTemplate, IDataBindable editTemplate, Widget headerTemplate)
        {
            this.name = name;
            this.viewTemplate = viewTemplate;
            this.editTemplate = editTemplate;
            this.headerTemplate = headerTemplate;
        }
    }
}
