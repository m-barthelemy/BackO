using System;
using System.IO;
using System.Collections.Generic;

namespace Node.DataProcessing{
	public abstract class IDataProcessorStream :Stream{
		
		/*double Time{get;set;}*/
		
		/*int BlockSize{get;set;}
		int MinBlockSize{get;}*/
		
		public abstract List<IFileBlockMetadata> BlockMetadata{get;set;}
		public abstract void FlushMetadata();
	}
}

