using System;
using Alphaleonis.Win32.Vss;

namespace Node.Misc{
	
	/// <summary>
	/// Dirty object whose only role is keeping an instance of VSS snapshot on Xp, 
	/// for it not to be deleted by the system before backup ends.
	/// </summary>
	internal class VSSObjectHandle{
		internal static IVssBackupComponents o;
		public static void StoreObject(IVssBackupComponents b){
			o = b;
		}
	}
}

