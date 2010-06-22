using System;

namespace EmergeTk.Model
{
    public interface ICustomLoadable<T> where T : AbstractRecord, new()
    {
        T CustomLoad(params FilterInfo[] filters);
    }
}
