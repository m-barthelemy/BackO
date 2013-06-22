using System;
using System.Collections;

namespace Node.DataProcessing{
	/*public interface IFileProvider{
		//IFile
		IFile GetItem(IEnumerable enumeratorProviderItem);
		// void CreateItem //for writing chunk,, log, index
		// void CreateItemForRestore // for creating item to be restores
	}*/
	
	public interface IFileProvider{
		/// <summary>
		/// Gets the item. To be used during normal backup operations
		/// </summary>
		/// <returns>
		/// The item.
		/// </returns>
		/// <param name='enumeratorProviderItem'>
		/// Enumerator provider item.
		/// </param>
		IFSEntry GetItem(object enumeratorProviderItem);
		/// <summary>
		/// Gets the item. To be used with special operations (restore, index creation, chunk storage)
		/// </summary>
		/// <returns>
		/// The item.
		/// </returns>
		/// <param name='fullName'>
		/// Full name.
		/// </param>
		IFSEntry GetItemByPath(string fullName);

		IFSEntry GetEmptyItem(); // used to create an FSEntry that does not come from enumerating the filesystem.
		// void CreateItem //for writing chunk,, log, index
		// void CreateItemForRestore // for creating item to be restores
	}
	
	/*public class LinuxFileProvider:IFileProvider{
		
		public IFile GetItem(IEnumerable enumeratorProviderItem){
			//return new UnixFile((string)enumeratorProviderItem.GetEnumerator().Current);
			return new UnixFile((string)enumeratorProviderItem);
		}
	}*/
	
	public class LinuxFileProvider:IFileProvider{
		
		public IFSEntry GetItem(object enumeratorProviderItem){
			//return new UnixFile((string)enumeratorProviderItem.GetEnumerator().Current);
			//return new LinuxFile((string)enumeratorProviderItem);
			return new PosixFile((Mono.Unix.Native.Dirent)enumeratorProviderItem);
		}
		public IFSEntry GetItemByPath(string fullName){
			//return new UnixFile((string)enumeratorProviderItem.GetEnumerator().Current);
			return new PosixFile(fullName);
		}
		public IFSEntry GetEmptyItem(){
			return new PosixFile();
		}
	}
	
	/*public class NTFileProvider:IFileProvider{
		
		public IFile GetItem(IEnumerable enumeratorProviderItem){
			return new NTBackupFile((string)enumeratorProviderItem.GetEnumerator().Current);
		}
	}*/

#if OS_WIN
	public class NTFileProvider:IFileProvider{
		/*public IFile GetItem(string fullName){
			Console.WriteLine("getitem() called by name string");
			return new NTBackupFile(fullName);
		}*/
		public IFSEntry GetItem(object enumeratorProviderItem){
			return new NTBackupFile((Alphaleonis.Win32.Filesystem.FileSystemEntryInfo)enumeratorProviderItem);
		}
		
		public IFSEntry GetItemByPath(string fullName){
			return new NTBackupFile(fullName);
		}

		public IFSEntry GetEmptyItem(){
			return new NTBackupFile();
		}
		
	}
	
	/*public class NTFileProviderXP:IFileProvider{

		public IFSEntry GetItem(object enumeratorProviderItem){
			return new NTBackupFileXP((Alphaleonis.Win32.Filesystem.FileSystemEntryInfo)enumeratorProviderItem);
		}
		
		public IFSEntry GetItemByPath(string fullName){
			return new NTBackupFileXP(fullName);
		}
		public IFSEntry GetEmptyItem(){
			return new NTBackupFileXP();
		}
		
	}*/
#endif	
	public class ItemProvider{
		
		public static IFileProvider GetProvider(){

#if OS_UNIX
			if(Utilities.PlatForm.IsUnixClient())
				return new LinuxFileProvider();
#endif
#if OS_WIN
			//else{
				//if(Alphaleonis.Win32.Vss.OperatingSystemInfo.IsAtLeast(Alphaleonis.Win32.Vss.OSVersionName.WindowsServer2003))
					return new NTFileProvider();
				//else // Special case with XP which doesn't support FindXXStreamW()
					//return new NTFileProviderXP();
			//}
#endif
			return null;
		}
	}
}

