using System;
using System.Collections.Generic;
using EmergeTk.Model;

namespace EmergeTk
{
    public interface IDataBindable
    {
        //properties
        object Value { get; set; }
        string DefaultProperty { get; }
        Dictionary<string, NotifyPropertyChanged> NotifyPropertyChangedHandlers { get; }

        //methods
        void BindProperty(string name, NotifyPropertyChanged changedHandler);
        void Bind(Binding b);
        void Unbind(Binding b);
        Type GetFieldTypeFromName(string name);
        List<Binding> Bindings { get; }
        object ObjectId { get; }
        object this[string key] { get; set; }  
    }
}
