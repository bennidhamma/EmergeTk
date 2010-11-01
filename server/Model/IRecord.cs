using System;
using System.Collections.Generic;

namespace EmergeTk.Model
{
    public interface IRecord
    {
        void Bind(Binding b);
        List<Binding> Bindings { get; }
        void BindProperty(string name, NotifyPropertyChanged changedHandler);
        string DefaultProperty { get; }
        void Delete();
        ColumnInfo[] Fields { get; }
        Type GetFieldTypeFromName(string name);
        int Id { get; }
        string ModelName { get; }
        Dictionary<string, NotifyPropertyChanged> NotifyPropertyChangedHandlers { get; }
        object ObjectId { get; }
        event EventHandler<RecordEventArgs> OnChange;
        event EventHandler<RecordEventArgs> OnDelete;
        IDataProvider Provider { get; }
        void Save();
        void Save(bool SaveChildren);
        object this[string Name] { get; set; }
        string ToString();
        void Unbind(Binding b);
        object Value { get; set; }
    }
}
