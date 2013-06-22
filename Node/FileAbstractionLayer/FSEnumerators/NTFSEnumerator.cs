#if OS_WIN
using System;
using System.Collections; //.Generic;
using Alphaleonis.Win32.Security;
using Node.Utilities;
using P2PBackup.Common;

namespace Node.DataProcessing{
	public class NTFSEnumerator:IFSEnumeratorProvider{
		public NTFSEnumerator(){}
		
		public IEnumerable/*<Alphaleonis.Win32.Filesystem.FileSystemEntryInfo>*/ GetFSEnumerator(string path){
			using(new Alphaleonis.Win32.Security.PrivilegeEnabler(Privilege.Backup, Privilege.Restore)){
				//try{
					if(Alphaleonis.Win32.Filesystem.File.Exists(path)){
						Console.WriteLine ("NTFSEnumerator:GetFSEnumerator()    * *  * * * **  * * ** * is FILE "+path);
						 return Alphaleonis.Win32.Filesystem.Directory.GetFullFileSystemEntries(path.Substring(0, path.LastIndexOf(System.IO.Path.DirectorySeparatorChar)), path.Substring(path.LastIndexOf(System.IO.Path.DirectorySeparatorChar)), System.IO.SearchOption.TopDirectoryOnly);	
					}
					else{
						//Console.WriteLine ("    * *  * * * **  * * ** * is directory "+path);
						
					
						return Alphaleonis.Win32.Filesystem.Directory.GetFullFileSystemEntries(path, "*.*", System.IO.SearchOption.TopDirectoryOnly);
					//}
					
						/*foreach(Alphaleonis.Win32.Filesystem.FileSystemEntryInfo entry in Alphaleonis.Win32.Filesystem.Directory.GetFullFileSystemEntries(path, "*", System.IO.SearchOption.TopDirectoryOnly)){
							Console.WriteLine ("    * * * * * GetFSEnumerator() : found entry "+entry.FullPath);
							yield return entry;
						}*/
					}
				/*}
				catch(Exception e){
					Logger.Append(Severity.ERROR, "Unable to crawl root path "+path+": "+e.Message);
					return null;
				}*/
			}
			 
		}

		public void Dispose(){

		}
	}
}

#endif