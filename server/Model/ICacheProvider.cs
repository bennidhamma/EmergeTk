// ICacheProvider.cs created with MonoDevelop
// User: ben at 10:19 AM 12/31/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using Mono.Addins;

namespace EmergeTk.Model
{
	public interface ICacheProvider
	{
		bool Set( string key, AbstractRecord value );
		bool Set(string key, object value);
		object GetObject( string key );
		AbstractRecord GetLocalRecord( string key );
		void PutLocal ( string key, AbstractRecord value);
		T GetRecord<T>(string key) where T : AbstractRecord, new();
		AbstractRecord GetRecord(Type t, string key);
		object[] GetList( params string[] key );
		void Remove(string key);
		void Remove(AbstractRecord record);
		void FlushAll();
		
		//safe list functions.
		void AppendStringList(string key, string value);
		string[] GetStringList(string key);
	}

}
