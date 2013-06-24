using System;
using Irony.Parsing;

namespace shbc {

	[Language("Backo server sql-like querying language")]
	public class BackOGrammar_old : Grammar{
		public BackOGrammar_old() :base(false){

			var lineComment = new CommentTerminal("line_comment", "--", "\n", "\r\n");
			NonGrammarTerminals.Add(lineComment);

			var number = new NumberLiteral("number");
			var string_literal = new StringLiteral("string", "'", StringOptions.AllowsDoubledQuote);
			var Id_simple = TerminalFactory.CreateSqlExtIdentifier(this, "id_simple"); //covers normal identifiers (abc) and quoted id's ([abc d], "abc d")
			var comma = ToTerm(",");
			var dot = ToTerm(".");
			var CREATE = ToTerm("CREATE"); 
			var NULL = ToTerm("NULL");
			var NOT = ToTerm("NOT");
			//var ADD = ToTerm("ADD"); 
			//var COLUMN = ToTerm("COLUMN"); 
			var UPDATE = ToTerm("UPDATE");
			var SET = ToTerm("SET"); 
			var DELETE = ToTerm("DELETE");
			var SELECT = ToTerm("SELECT"); 
			var FROM = ToTerm("FROM");
			var COUNT = ToTerm("COUNT");
			var JOIN = ToTerm("JOIN");
			//var BY = ToTerm("BY");

		}
	}
}

