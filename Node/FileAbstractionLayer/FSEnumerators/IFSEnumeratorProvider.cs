using System;
using System.Collections;//.Generic;
using Node.Utilities;
using P2PBackup.Common;

namespace Node.DataProcessing{
	/*public interface IFSEnumeratorProvider{
		
		IEnumerator GetFSEnumerator(string path);
	}*/
	public interface IFSEnumeratorProvider:IDisposable{
		
		IEnumerable GetFSEnumerator(string path);
	}
	
	public class FSEnumeratorProvider{
		private	FSEnumeratorProvider(){			
		}
		
		public static IFSEnumeratorProvider GetFSEnumeratorProvider(){
#if OS_UNIX
			if(Utilities.PlatForm.IsUnixClient())
				return new LinuxFSEnumerator();
#endif
#if OS_WIN
				
			//else{
				//
					return new NTFSEnumerator();
#endif
			//}
			return null;
		}
	}
	
	/*public class UnixFSEnumerator:IFSEnumeratorProvider{
		public UnixFSEnumerator(){}
		
		public IEnumerator GetFSEnumerator(string path){
			return System.IO.Directory.EnumerateFileSystemEntries(path).GetEnumerator();
		}
	}
	
	public class NTFSEnumerator:IFSEnumeratorProvider{
		public NTFSEnumerator(){}
		
		public IEnumerator GetFSEnumerator(string path){
			return Alphaleonis.Win32.Filesystem.Directory.GetFullFileSystemEntries(path, "*", System.IO.SearchOption.TopDirectoryOnly).GetEnumerator();
			
		}
	}*/
	
	/*public class UnixFSEnumerator:IFSEnumeratorProvider{
		
		public UnixFSEnumerator(){}
		
		public IEnumerable GetFSEnumerator(string path){
			return System.IO.Directory.EnumerateFileSystemEntries(path);
			//Mono.Unix.UnixDirectoryInfo ud = new Mono.Unix.UnixDirectoryInfo(PathBrowser);
			//ud.
		}
	}*/
	
	
	
	
}

