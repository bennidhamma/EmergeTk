using System;
using Mono.Options;
using System.IO;

namespace emergecli
{
	[Command(Name="addcol")]
	public class AddColCommand : ICommand
	{
		public const string template = @"
DROP PROCEDURE IF EXISTS addcol;
delimiter //
CREATE PROCEDURE addcol() BEGIN
IF NOT EXISTS (
	   SELECT * FROM information_schema.COLUMNS
       WHERE COLUMN_NAME='{0}' AND TABLE_NAME='{1}' AND TABLE_SCHEMA=DATABASE()
       ) AND EXISTS (SELECT * from information_schema.TABLES where table_name = '{1}' and table_schema = DATABASE();
       THEN
	       ALTER TABLE {1} ADD COLUMN {0} {2} {3};
END IF;
END//
delimiter ;

CALL addcol();

DROP PROCEDURE IF EXISTS addcol;
";
		
		public string Table {get; set;}
		public string ColumnName {get; set;}
		public string Type {get; set;}
		public string Suffix {get; set;}		
		
		private string fileName = null;
		public string FileName
		{
			get {
				return fileName ?? string.Format ("add_{0}_to_{1}.sql", ColumnName, Table);
			}
			set {
				fileName = value;
			}
		}
		
		public OptionSet GetOptions (string[] args)
		{
			var o = new OptionSet () {
				{"t|table=", v => this.Table = v},
				{"c|column=", v => this.ColumnName = v},
				{"T|type=", v => this.Type = v},
				{"s|suffix:", v => this.Suffix = v},
				{"f|file:", v => this.FileName = v}
			};
			return o;
		}
		
		public bool Validate ()
		{
			return Table != null && ColumnName != null && Type != null;
		}
		
		public void Run ()
		{
			File.WriteAllText (FileName, string.Format (template, ColumnName, Table, Type, Suffix));
		}
	}
}

