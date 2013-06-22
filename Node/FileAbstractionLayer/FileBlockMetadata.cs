using System;
using System.Collections.Generic;
using System.Runtime.Serialization;


namespace Node.DataProcessing{

	public interface IFileBlockMetadata{
		
	}
	
	/*[Serializable]
	public class ClientDedupedBlock:IFileBlockMetadata{
		//public long Offset{get;set;}
		//public byte[] Checksum;
		public ulong Id{get; private set;}
		public ClientDedupedBlock(ulong id){
			Id = id;
			//this.Checksum = cksum;
		}
	}*/

	[Serializable]
	public class ClientDedupedBlocks:IFileBlockMetadata{
		//public long Offset{get;set;}
		//public byte[] Checksum;
		public List<long>  Ids{get; private set;}
		public ClientDedupedBlocks(List<long> ids){
			this.Ids = ids;
			//this.Checksum = cksum;
		}
	}

	[Serializable]
	public class StorageDedupedBlock:IFileBlockMetadata{
		public long Offset{get;set;}
		public byte[] Checksum;
		public StorageDedupedBlock (){
		}
	}
	
	[Serializable]
	public class SparseRegion:IFileBlockMetadata{
		public long Offset{get;set;}
		public int Length;
		public SparseRegion(){
		}
	}
	
	[Serializable]
	/// <summary>
	/// When doing incr/diff , put this object to signal that a file's content has not changed (but metadata has).
	/// </summary>
	public class UnchangedDataItem:IFileBlockMetadata{
		
		public UnchangedDataItem(){
		}
	}
	
	[Serializable]
	public class RenamedOrMovedItem:IFileBlockMetadata{
		public enum ChangeType{rename,move,delete}
		public ChangeType Change{get;set;}
		//public string Destination{get;set;}
		public long OldParentId{get;set;}
		public string OldName{get;set;}
		public RenamedOrMovedItem(){
		}
	}

	[Serializable]
	public class DeletedItem:IFileBlockMetadata{
		public DeletedItem(){}
	}

	[Serializable]
	public class DataRegion:IFileBlockMetadata{
		public long StartPos{get;set;}
		public int Length{get;set;}
		public string Chunk{get;set;}
		public DataRegion(){}
	}

	[Serializable]
	public class FileBlockMetadata{
		public List<IFileBlockMetadata> BlockMetadata{get;set;}
		public List<long> DedupedBlocks{get;set;}

		public FileBlockMetadata(){
			this.BlockMetadata = new List<IFileBlockMetadata>();
			this.DedupedBlocks = new List<long>();
		}
	}
}

