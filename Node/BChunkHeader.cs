using System;
using P2PBackup.Common;

namespace Node{
	
	[Serializable]
	public struct BChunkHeader{
		
		public string Version{get;set;}
		public DataProcessingFlags DataFlags{get;set;}
		public long TaskId{get;set;}
		public uint OwnerNode{get;set;}
		public byte[] EncryptionMetaData{get;set;}

	}
}

