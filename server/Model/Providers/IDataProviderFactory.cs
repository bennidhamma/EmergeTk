using System;

namespace EmergeTk.Model.Providers
{
	public interface IDataProviderFactory
	{
		IDataProvider GetProvider(Type t);
		IDataProvider GetProvider(Type t, int id);
		int RequestId(Type t);
	}
	
	public class DefaultProviderFactory : IDataProviderFactory
	{
		#region IProviderFactory implementation
		public IDataProvider GetProvider (Type t)
		{
			return DataProvider.DefaultProvider;
		}
		
		public IDataProvider GetProvider (Type t, int id)
		{
			return DataProvider.DefaultProvider;
		}

		public int RequestId (Type t)
		{
			 return DataProvider.DefaultProvider.GetNewId( AbstractRecord.GetDbSafeModelName(t) );
		}
		#endregion		
	}
}

