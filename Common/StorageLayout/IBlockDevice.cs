using System;
using System.IO;

namespace P2PBackup.Common.Volumes {

	public interface IBlockDevice {

		Stream BlockStream{get;}
	}
}

