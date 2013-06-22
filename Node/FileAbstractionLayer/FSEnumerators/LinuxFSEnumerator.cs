#if OS_UNIX
using System;
using System.Collections;
using System.Collections.Generic;
//using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mono.Unix;
using Mono.Unix.Native;
using Node.Utilities;
using P2PBackup.Common;

namespace Node.DataProcessing{
	
	/// <summary>
	/// Browses an *nix filesystem, calling native readdir().
	/// Replaces default Mono EnumerateFileSystemEntries which is just unusable .
	/// </summary>
	public class LinuxFSEnumerator:IFSEnumeratorProvider, IDisposable{
		
		//private const int SYS_getdents = 78;
		//private IntPtr fd;
		//private byte[] dentBuf;
		List<IntPtr> openDirs;
		
		
		public LinuxFSEnumerator(){
			openDirs = new List<IntPtr>();
		}
		
		public IEnumerable/*<Dirent>*/ GetFSEnumerator(string path){
			
			IntPtr dir = Syscall.opendir(path);
			openDirs.Add(dir);
			if(dir == IntPtr.Zero){
				Logger.Append(Severity.ERROR, "Could not open directory "+path+": "+Syscall.GetLastError().ToString());
				yield break;
			}
			Dirent nextentry;
			while ((nextentry = Syscall.readdir(dir)) != null) {
				if (nextentry.d_name != "." && nextentry.d_name != ".." ){
					nextentry.d_name =  path+"/"+nextentry.d_name;
					yield return nextentry;
				}
			}
			Syscall.closedir(dir);
			openDirs.RemoveAt(openDirs.Count-1);
			yield break;
		}
		
		public void Dispose(){
			for(int i = openDirs.Count-1; i==0; i--){
				Syscall.closedir(openDirs[i]);
				openDirs[i] = IntPtr.Zero;
			}
		}
		
		
		
		//[DllImport("libc")]
	 	//private static extern int syscall(int call, int fd, out byte[] buffer, int bufLength);
		
	}
}

//struct linux_dirent {
//    unsigned long  d_ino;     /* Numéro d'inœud */
//    unsigned long  d_off;     /* Distance au prochain dirent */
//    unsigned short d_reclen;  /* Longueur de ce dirent */
//    char           d_name []; /* Nom de fichier (fini par 0) */
//                        /* La longueur est en fait (d_reclen - 2 -
//                           offsetof(struct linux_dirent, d_name) */
//    char           pad;       /* Octet nul de remplissage */
//    char           d_type;    /* Type de fichier (seulement depuis Linux 2.6.4 ;
//                                 sa position est (d_reclen - 1)) */
//
//}


#endif