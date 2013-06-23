using System;
using ProtoBuf;

namespace Node.DataProcessing{

	//[Serializable]
	[ProtoContract]
	public class FullDedupedBlock:LightDedupedBlock{

		[ProtoMember(4)]
		public int StartPos;//{get;set;}
		[ProtoMember(5)]
		public uint[] StorageNodes;//{get;set;}
		[ProtoMember(6)]
		public int Length;//{get;set;}
		[ProtoMember(7)]
		public string DataChunkName;//{get;set;}
		
		public FullDedupedBlock():base(){
			RefCounts = 0;
			Length = 0;
			this.StorageNodes = new uint[2];
		}

		public override string ToString () {
			return string.Format ("[Id={0}, CheckSum={1}(blksize={7}), DataChunkName={2}, 1stStorageNode=[{3}], StartPos={4}, Size={5}, RefCounts={6}]",
				this.ID, ((this.Checksum == null)? "<null!>":Convert.ToBase64String(this.Checksum)), this.DataChunkName, 
			                      ((this.StorageNodes.Length >0)?this.StorageNodes[0]:0), this.StartPos, this.Length, this.RefCounts, BitConverter.ToInt32(this.Checksum, 16));
		}
	}
}

