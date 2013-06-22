using System;

namespace Node {


	public class GPT :PartitionManager {
		struct Header{
			public string Signature;
			public uint Revision;
			public uint HeaderSize;
			public uint FirstLBA;
			public uint BackupLBA;
			public uint PartitionsFirstLBA;
			public uint PartitionsLastLBA;
			public long DiskUuid;
		}
		public byte[] Sector{get;set;}
		int LocationOffset{get;set;}
		internal GPTPartitionInformation[] Partitions{get; private set;}

		internal readonly uint SectorSize = 512;

		public GPT(byte[] sectors){

			this.Sector = sectors;
		}
	}

	public class GPTPartitionInformation{
		public string Guid{get;set;}
	}
} 

