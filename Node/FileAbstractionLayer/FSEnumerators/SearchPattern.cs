using System;
using System.Collections.Generic;
using Node.Utilities;
using P2PBackup.Common;

namespace Node.DataProcessing{
	
	/// <summary>
		/// Provdes files selection according to patterns.
		/// -We accept the following :
		/// NULL, * , *.*, ***, *xxxx  (all treated as "*", that is : everything)
		/// xxxxx?, xxx?xxx, xxx??xx, xxx??xx*  (1 question mark replaces 1 character).
		/// we only allow 1 serie of '??' (xxx??xxx is correct, xx??xxx?x is not)
		/// -We DON't ACCEPT :
		/// *xxxx?, *xxxx (invalid, will be treated as "*")
		/// xxx*xxx (will we treated as xxx*)
		/// </summary>
		/// <param name='pattern'>
		/// Pattern.
		/// </param>
	internal class SearchPattern{
		private List<string> pattern;
		int[] firstJokerPos; 
		int[] jokerLength; 
		int[] wildJokerPos; 
		bool defaultBehavior;

		internal SearchPattern (List<string> patterns, bool defaultBehavior){
			this.defaultBehavior = defaultBehavior;
			if(patterns == null || patterns.Count == 0)return;
			pattern = patterns;
			firstJokerPos = new int[pattern.Count];
			jokerLength = new int[pattern.Count];
			wildJokerPos = new int[pattern.Count];
			for(int p =0; p< pattern.Count; p++){
				if(pattern[p] == null) pattern[p] = string.Empty;
				int itemLength = pattern[p].Length;
				firstJokerPos[p] = pattern[p].IndexOf('?');
				jokerLength[p] = 0;
				for(int i=firstJokerPos[p]+1; i< itemLength;i++){
					if(pattern[p][i] == '?')
						jokerLength[p]++;
					else
						break;
				}
				wildJokerPos[p] = pattern[p].IndexOf("*");
				if( (wildJokerPos[p] >= 0 && wildJokerPos[p] < firstJokerPos[p]) || firstJokerPos[p]+jokerLength[p] != pattern[p].LastIndexOf('?'))
					Logger.Append(Severity.ERROR, "Path search pattern ("+pattern[p]+") is invalid!");
			}
		}
		
		internal bool Matches(string item){
			if(pattern == null || pattern.Count == 0) return defaultBehavior;
			try{
				for(int p =0; p<pattern.Count; p++){
					//Console.WriteLine("looking for pattern "+pattern[p]+" in item "+item+" (wildJPos="+wildJokerPos[p]);
					// pure string patterns
					if(wildJokerPos[p] < 0 && firstJokerPos[p] <0) 
						if( pattern[p] == item) return true;
						else continue;
					// *xxx, x.xxx patterns
					//Console.WriteLine("pattern "+pattern[p]+", wildPos = "+wildJokerPos[p]);
					if(wildJokerPos[p] == 0){
						if(pattern[p].Length == 1 || pattern[p] == "*.*") // '*' case
							return true;
						else // *.xxx case
							if(item.IndexOf(pattern[p].Substring(1)) >=0)
								return true;
					}
					// xxx??xxx patterns
					else if(firstJokerPos[p] >=0 && item.Substring(0, firstJokerPos[p]) == pattern[p].Substring(0, firstJokerPos[p])
						&& item.Substring(firstJokerPos[p]+jokerLength[p]) == pattern[p].Substring(firstJokerPos[p]+jokerLength[p]) )
						return true;
					
				}
			}
			catch(Exception e){
				Console.WriteLine ("exception : "+e.Message+" --- "+e.StackTrace);	
			}
			return false;
		}
	}
}

