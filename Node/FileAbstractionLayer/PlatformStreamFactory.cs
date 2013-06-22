using System;
using System.IO;
using Node.Utilities;
using P2PBackup.Common;

namespace Node{

	public class PlatformStreamFactory	{

		public static readonly PlatformStreamFactory _instance = new PlatformStreamFactory();

		private PlatformStreamFactory (){
			
		}
		
		public static PlatformStreamFactory Instance(){
			return _instance;	
		}
		
		public Stream GetPlatformStream(bool isChunkStoreStream, string fileName, FileMode fileMode){
			if(!isChunkStoreStream)
				return new FileStream(fileName, fileMode);
			switch(Utilities.PlatForm.Instance().OS){
#if OS_UNIX
				case "Linux":
					return  new LinuxStream(fileName, fileMode);
				case "FreeBSD":
					return new FileStream(fileName, fileMode);
#endif

#if OS_WIN
				case "NT5.1": case"NT6.0": case "NT6.1": case "NT6.2":
					return new FileStream(fileName, fileMode);
#endif
				default:
					Logger.Append(Severity.DEBUG, "Could not find appropriate platform file stream for OS '"+Utilities.PlatForm.Instance().OS+"'");
				return null;
			}
		}
	}
}

