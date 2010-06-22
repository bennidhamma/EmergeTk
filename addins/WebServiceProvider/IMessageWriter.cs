using System;
using System.Collections.Generic;
using EmergeTk.Model;
using EmergeTk.Model.Security;
using System.Linq;
using System.Text;

namespace EmergeTk.WebServices
{
    public interface IMessageWriter
    {
        void OpenRoot(string name);
        void CloseRoot();
        void OpenObject();
        void CloseObject();
        void OpenList(string name);
        void CloseList();
        void OpenProperty(string name);
        void CloseProperty();
        void WriteScalar(string scalar);
        void WriteScalar(int scalar);
        void WriteScalar(bool scalar);
        void WriteScalar(double scalar);
        void WriteScalar(float scalar);
        void WriteScalar(DateTime scalar);
        void WriteScalar(Decimal scalar);
        void WriteScalar(Object scalar);
        void WriteProperty(string name, string scalarValue); //shortcut for scalar props
        void WriteProperty(string name, int scalarValue); //shortcut for scalar props
        void Flush();
    }
}
