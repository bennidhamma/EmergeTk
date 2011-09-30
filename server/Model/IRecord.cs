using System;
using System.Collections.Generic;

namespace EmergeTk.Model
{
    public interface IRecord
    {
        string DefaultProperty { get; }
        void Delete();
        ColumnInfo[] Fields { get; }
        Type GetFieldTypeFromName(string name);
        int Id { get; }
        string ModelName { get; }
        object ObjectId { get; }
        IDataProvider Provider { get; }
        void Save();
        void Save(bool SaveChildren);
        object this[string Name] { get; set; }
        string ToString();
        object Value { get; set; }
    }
}
