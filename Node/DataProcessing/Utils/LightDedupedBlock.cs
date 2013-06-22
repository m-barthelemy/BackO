using System;
using ProtoBuf;

namespace Node.DataProcessing {

	//[Serializable]
	[ProtoContract]
	[ProtoInclude(10, typeof(FullDedupedBlock))]
	public class LightDedupedBlock {

		[ProtoMember(1)]
		public long ID;
		[ProtoMember(2)]
		public byte[] Checksum;// = new byte[20];
		[ProtoMember(3)]
		public int RefCounts;

		public LightDedupedBlock(){}

	}
}

