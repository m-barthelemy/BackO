using System;
using System.Diagnostics;
using System.Collections.Generic;
using P2PBackup.Common;

using Node.Utilities;

namespace Node.DataProcessing{
	
	// Stub for now. will be implemented once btrfs gets a 100% reliable way to find changes between snaps
	// For now it cannot signal deleted items with the current implementation (btrfs find-new)
	public class BtrfsProvider:IIncrementalProvider{
		
		System.Collections.IEnumerable fsprov;
		IFileProvider prov = ItemProvider.GetProvider();

		public short Priority{
			get{return 3;}
		}

		public string Name{
			get{
				return "BtrfsProvider";
			}
		}

		public bool ReturnsOnlyChangedEntries{get{return true;}}

		public bool IsEnabled{get;set;}
		
		public BtrfsProvider(){
		}
		
		// checks if we have a reference to find last backup generation-id
		public bool CheckCapability(){
			return false;	
		}
		
		public IEnumerable<IFSEntry> GetNextEntry(BasePath path, string snapshottedPath){
			// http://www.tummy.com/journals/entries/jafo_20101101_193113
			fsprov = FSEnumeratorProvider.GetFSEnumeratorProvider().GetFSEnumerator(snapshottedPath);
			
			yield return prov.GetItemByPath(snapshottedPath);
		}
		
		public void SignalBackup(){
		  // nothing to do here.	
		}

		public byte[] GetMetadata(){
				return null;
		}

		public void SetReferenceMetadata(byte[] metadata){}
	}
}

