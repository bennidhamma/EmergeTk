/** Copyright (c) 2006, All-In-One Creations, Ltd.
*  All rights reserved.
* 
* Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
* 
*     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
*     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
*     * Neither the name of All-In-One Creations, Ltd. nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
* 
* THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
**/
/**
 * Project: emergetk: stateful web framework for the masses
 * File name: XMLTypeBuilder.cs
 * Description: A type generator to generate simple types from XML record definitions.
 *   
 * Author: Ben Joldersma
 *   
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;

namespace EmergeTk.Model
{
    class XmlTypeBuilder
    {
        private static AssemblyBuilder asmBuilder = null;
        private static ModuleBuilder modBuilder = null;
        private static Type iRecordListType = null;

        private static void GenerateAssemblyAndModule() 
        {
            if (asmBuilder == null)
            {
                AssemblyName assemblyName = new AssemblyName();
                assemblyName.Name = "XmlTypes";
                AppDomain thisDomain = Thread.GetDomain();
                asmBuilder = thisDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                modBuilder = asmBuilder.DefineDynamicModule(asmBuilder.GetName().Name, false);
                iRecordListType = TypeLoader.GetType("EmergeTk.Model.IRecordList`1");
            }
        }
        
        public static Type CreateType(string name)
        {
            string basePath = System.Web.HttpContext.Current.Request.MapPath("") + "\\";
            if (!System.IO.File.Exists(basePath + name + ".model"))
            {
                throw new System.IO.FileNotFoundException("Could not load an xml record for model " + name + " at path: " + basePath + name + ".model");
            }
            GenerateAssemblyAndModule();
            TypeBuilder xmlTypeBuilder = modBuilder.DefineType(name, TypeAttributes.Public |
                TypeAttributes.Class, typeof(XmlRecord));
            
            Type xmlType = xmlTypeBuilder.CreateType();
            ColumnInfoManager.RegisterColumns(xmlType, ReadColumnInfos(name, xmlType));
            return xmlType;
        }

        public static ColumnInfo[] ReadColumnInfos(string name, Type xmlType)
        {
            string basePath = System.Web.HttpContext.Current.Request.MapPath("") + "\\";
            XmlDocument doc = new XmlDocument();
            XmlNode node;
            if (System.IO.File.Exists(basePath + name + ".model"))
            {
                doc.Load(basePath + name + ".model");
                node = doc.SelectSingleNode("Model");
            }
            else if (System.IO.File.Exists(basePath + "domain.model"))
            {
                doc.Load(basePath + "domain.model");
                node = doc.SelectSingleNode("//Model[@name,'" + name + "']");
            }
            else
                return null;
            List<ColumnInfo> columns = new List<ColumnInfo>();
            XmlNodeList fields = node.SelectNodes("Column");
            foreach (XmlNode fieldNode in fields)
            {
                string columnName = fieldNode.Attributes["Name"].Value;
                string typeName = fieldNode.Attributes["Type"].Value;
                Type columnType = TypeLoader.GetType(typeName);
                DataType dataType = DataType.None;
                if (columnType != null)
                {
                    if (columnType.IsEnum)
                    {
                        dataType = DataType.None;
                    }
                    else if (fieldNode.Attributes["Relationship"] == null ||
                        fieldNode.Attributes["Relationship"].Value == "One")
                        dataType = DataType.RecordSelect;
                    else
                    {
                        dataType = DataType.RecordList;
                        columnType = iRecordListType.MakeGenericType(columnType);
                    }
                }
                else
                {
                    dataType = (DataType)Enum.Parse(typeof(DataType), typeName);
                    columnType = DataTypeHelper.SystemForDataType(dataType);
                }
                columns.Add(new ColumnInfo(columnName, columnType, dataType, xmlType, false));
            }
            return columns.ToArray();
        }
    }
}
