
using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Configuration;
using MySql.Data.MySqlClient;
using EmergeTk;
using EmergeTk.Model;
using System.IO;

namespace emergetool
{
	
	public class DatabaseSynchronizer
	{
		
//		EmergeTk.EmergeTkLog log = EmergeTk.EmergeTkLogManager.GetLogger(typeof(DatabaseSynchronizer));
		
		public IDataProvider provider;
		
		/// <summary>
		/// whether or not to print extra messages about the program's output
		/// </summary>
		private bool verbose;
		public bool Verbose {
			set { verbose = value; }	
		}
		
		/// <summary>
		/// whether or not to ignore types in the EmergeTk namespace
		/// </summary>
		private bool ignoreEmergeTk;
		public bool IgnoreEmergeTk {
			set { ignoreEmergeTk = value; }	
		}
		
		/// <summary>
		/// list of types that are derived from AbstractRecord
		/// </summary>
		
		private List<Type> TypeList
		{
			get 
			{
				Type[] derivedTypes = TypeLoader.GetTypesOfBaseType( typeof(AbstractRecord) );
				List<Type> typeList = new List<Type>();
				
				foreach ( Type t in derivedTypes ) {
					string assemblyName = t.Assembly.FullName;
					if ( ! t.IsAbstract && (! ignoreEmergeTk || (ignoreEmergeTk && ! assemblyName.Contains("EmergeTk"))) )
						typeList.Add( t );
				}
				return typeList;
			}
		}
		
		private List<string> dllList;
		
		private ColumnInfo[] relationTableColumns;
		
		/// <summary>
		/// constructor
		/// </summary>		
		public DatabaseSynchronizer() {
			provider = DataProvider.DefaultProvider;
			verbose = false;
			ignoreEmergeTk = true;
			dllList = new List<String>();
			relationTableColumns = ColumnInfoManager.GetRelationTableColumns();
		}
		
		/// <summary>
		/// try loading the given assembly into the current environment; if it fails throws exception
		/// </summary>
		/// <param name="assemblyName">
		/// A <see cref="System.String"/>
		/// the name of the assembly to be loaded
		/// </param>
		/// <exception cref="ArgumentException">
		/// Thrown if there are problems with loading the given assembly
		/// </exception>
		public void LoadAssembly(string assemblyName) 
		{
			try {
				System.Reflection.Assembly.LoadFile(assemblyName);
				dllList.Add(assemblyName);
			}
			catch 
			{
				throw new ArgumentException("problems loading assembly " + assemblyName);
			}
		}
		
		public void SetConnectionString(string cString) {
			provider.SetConnectionString(cString);	
		}
		
		/// <summary>
		/// loops through the TypeList (see above) and determines if a table exists in the
		/// database for it, returning SQL add table statements that would fix any missing tables.
		/// if the type is generic, no analysis is done
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/>
		/// SQL statements to add databases to fix synchronization issues
		/// </returns>
		public string GenerateCreateTableStatements()
		{
			string result = printVerbose("");
			
			result += printVerbose( "GENERATING CREATE TABLE STATEMENTS FOR THE FOLLOWING DLL(S): " );
			foreach (string s in dllList) result += printVerbose(s);
			result += printVerbose("");
			
			if (ignoreEmergeTk) {
				result += printVerbose( "IGNORING EmergeTk NAMESPACE" );
				result += printVerbose( "" );
			}
			
			foreach ( Type t in TypeList )
			{
				string tName;
				if ( ! t.IsGenericType ) {
					//TODO: this is a bug -- on creating an instance of static objects that save to
					//		the database immediately, we get phantom tables in the database we 
					// 		are analyzing
					tName = ( Activator.CreateInstance( t ) as AbstractRecord ).ModelName;
					if ( ! provider.TableExistsNoCache( tName ) ) 
						result += provider.GenerateAddTableStatement( t ) + "\n";
					
					// check for relational tables 
					ColumnInfo[] ci = ColumnInfoManager.RequestColumns( t );
					foreach ( ColumnInfo c in ci ) 
					{
						if ( AbstractRecord.TypeIsRecordList( c.Type ) )
						{
							string relationTable = tName + "_" + c.Name;
							if ( ! provider.TableExistsNoCache( relationTable ) )
								result += provider.GenerateAddChildTableStatement( relationTable, c.IsDerived ) + "\n";
						}
					}
				}
				else 
					result += printVerbose( "skipping generic type " + t.Name );
			}
			
			return result;
		}	
		
		/// <summary>
		/// loops through the TypeList, and if a table exists for that type, checks that the
		/// columns are correct based on the OR/M.  returns SQL statements to add and remove 
		/// columns appropriately
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/>
		/// SQL statements to add and remove columns from tables that are out of sync with the OR/M
		/// </returns>
		public string GenerateAlterColumnStatements()
		{	
			string result = printVerbose("");
			
			result += printVerbose( "GENERATING ALTER TABLE STATEMENTS FOR THE FOLLOWING DLL(S): " );
			foreach (string s in dllList) result += printVerbose(s);
			result += printVerbose("");
			
			if (ignoreEmergeTk) {
				result += printVerbose( "IGNORING EmergeTk NAMESPACE" );
				result += printVerbose( "" );
			}
			
			foreach ( Type t in TypeList )
			{
				result += GenerateAlterSingleTableStatement( t );
			}
			return result;
		}
		
		/// <summary>
		/// checks the given type's associated table in the database and generates SQL statements
		/// to add, remove, and modify any columns to fix synchronization issues.  if the type has 
		/// no table, or is generic, the columns are not analyzed
		/// </summary>
		/// <param name="t">
		/// A <see cref="Type"/>
		/// the type that is associated with the table to check
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// SQL statements to fix any synchronization issues
		/// </returns>
		public string GenerateAlterSingleTableStatement( Type t )
		{

			string tableName;
		
			if ( ! t.IsGenericType ) 
				//TODO: this is an unsolvable bug -- on creating an instance of objects that save to
				//		the database immediately, we get phantom tables in the database we 
				// 		are analyzing
				tableName = ( Activator.CreateInstance( t ) as AbstractRecord ).ModelName;			
			else 
				return printVerbose( "skipping generic type " + t.Name );
			
			string result = printVerbose( "analyzing type " + t.Name + " with table name '" + tableName + "'");
			
			ColumnInfo[] expectedColumns = ColumnInfoManager.RequestColumns( t );
			IEnumerable<ColumnInfo> initAddColumns = new List<ColumnInfo>();

			if ( ! provider.TableExistsNoCache( tableName ) ) 
			{
				result += printVerbose( "table " + tableName + " does not exist: skipping column analysis" );
			}	
			else
			{
				result += CheckIdColumn( tableName );
				
				DataTable actualColumnsTable = provider.GetColumnTable( tableName );
				List<ColumnInfo> actualColumns = new List<ColumnInfo>();
								
				// this is essentially an except( actual, expected ) with some extra logic to check that types match
				foreach ( DataRow r in actualColumnsTable.Rows ) 
				{
					// don't look at identity column here
					if ( ! (r[0] as string).Equals( provider.GetIdentityColumn() ) ) 
					{	
						MethodInfo mi = typeof(ColumnInfoManager).GetMethod("RequestColumn", new Type[] { typeof( string ) } );
						mi = mi.MakeGenericMethod( t );
						ColumnInfo col =  mi.Invoke(null, new object[] { r[0] }) as ColumnInfo;
						
						if (col == null) // doesn't exist in the expected columns
						{
							result += provider.GenerateRemoveColStatement( r[0] as string, tableName ) + "\n";
							//removeColumns.Add( r[0] as string );
							continue;
						}
						
						actualColumns.Add( col );
											
						if ( ! provider.GetSqlTypeFromType( col, tableName ).Equals( (r[1]) ) ) {// check that types match
							result += provider.GenerateFixColTypeStatement( col, tableName ) + "\n";
						}
					}
				}
				
				initAddColumns = Enumerable.Except( expectedColumns, actualColumns );
				
			}
	
			// check relation tables, and build the addColumns list from initAddColumns
			foreach ( ColumnInfo c in expectedColumns )
			{	
				if ( (! AbstractRecord.TypeIsRecordList( c.Type ) ) && initAddColumns.Contains( c ) )
					result += provider.GenerateAddColStatement( c, tableName ) + "\n";
				else if ( AbstractRecord.TypeIsRecordList( c.Type ) ) 
				{
					string relationTable = tableName + "_" + c.Name;
					if (DataProvider.LowerCaseTableNames) relationTable = relationTable.ToLower();

					result += CheckRelationTable( relationTable, c );
				}
			}
			
			return result;
		}
				
		/// <summary>
		/// check that the given relation table exists and matches the correct format for relation tables
		/// and returns alter statements to fix/add the table if needed
		/// </summary>
		/// <param name="table">
		/// A <see cref="System.String"/>
		/// the name of the relation table to analyze
		/// </param>
		/// <param name="c">
		/// A <see cref="ColumnInfo"/>
		/// the ColumnInfo from the parent type that is attached to this relation table
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// the alter statements that would fix any problems with the table
		/// </returns>
		private string CheckRelationTable( string table, ColumnInfo c ) 
		{
			string result = printVerbose("analyzing relation table " + table);
			
			if ( ! provider.TableExistsNoCache ( table ) )
			{
				result += printVerbose("table " + table + " does not exist: skipping column analysis");
			}
			else // check that the relation table is set up properly
			{
				
				DataTable relationDT = provider.GetColumnTable( table );
				foreach ( DataRow r in relationDT.Rows ) 
				{
					bool found = false;
					foreach ( ColumnInfo col in relationTableColumns ) 
					{
						if ( col.Name.ToLower().Equals( (r[0] as string).ToLower() ) ) // not case sensitivity
						{
							found = true;
							break;
						}
					}
					
					if ( ! found )
						result += provider.GenerateRemoveColStatement( r[0] as string, table ) + "\n";
				}
				
				foreach ( ColumnInfo col in relationTableColumns )
				{
					bool found = false;
					foreach ( DataRow r in relationDT.Rows ) 
					{
						if ( col.Name.ToLower().Equals( (r[0] as string).ToLower() ) )
						{
							found = true;
							if ( ! provider.GetSqlTypeFromType( col, table ).ToLower().Equals( (r[1] as string).ToLower() ) )
								result += provider.GenerateFixColTypeStatement( col, table ) + "\n";
							break;
						}
					}
					
					if ( ! found )
						result += provider.GenerateAddColStatement( col, table ) + "\n";
				}
			}
			
			return result;	
		}
		
		/// <summary>
		/// check that the IdColumn of the given table exists and is correctly set up
		/// </summary>
		/// <param name="table">
		/// A <see cref="System.String"/>
		/// the name of the table to analyze
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// the alter statements that would fix any problems with the IdColumn of the table
		/// </returns>
		private string CheckIdColumn( string table ) 
		{
			string result = provider.CheckIdColumn( table );
			if (result.Equals(""))
				return "";
			else 
				return result + "\n";
		}
		
		/// <summary>
		/// if verbose printout is true, returns the given message lead by a 
		/// comment character; returns the empty string otherwise
		/// </summary>
		/// <param name="message">
		/// A <see cref="System.String"/>
		/// the message to print
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		private string printVerbose(string message) 
		{
			if (verbose)
				return provider.GetCommentCharacter() + " " + message + "\n";
			else
				return "";
		}
		
	}
	
}			