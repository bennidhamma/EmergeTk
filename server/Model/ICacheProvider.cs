// ICacheProvider.cs created with MonoDevelop
// User: ben at 10:19 AM 12/31/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;

namespace EmergeTk.Model
{
	public interface ICacheProvider
	{
		bool Set( string key, AbstractRecord value );
		bool Set(string key, object value);
		object GetObject( string key );
		AbstractRecord GetLocalRecord( string key );
		AbstractRecord GetLocalRecord (RecordDefinition rd);
		void PutLocal ( string key, AbstractRecord value);
		T GetRecord<T>(string key) where T : AbstractRecord, new();
		AbstractRecord GetRecord(Type t, string key);
		object[] GetList( params string[] key );
		void Remove(string key);
		void Remove(AbstractRecord record);
		//This function needs to ensure that no stale references to the record exist anywhere in the cache service.
		void Update (AbstractRecord record);
		void FlushAll();
		
		//safe list functions.
		void AppendStringList(string key, string value);
		string[] GetStringList(string key);

		//TODO: 2.0 Interface Methods (take advantage of Redis better)
		bool Set(string key, string value);
		string GetString(string key);
	}

}
