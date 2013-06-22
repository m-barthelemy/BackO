// http://cosmos.codeplex.com

using System;
using System.IO;

namespace Node.StorageLayer {

public class BlockDevice /*: Device */{

		public string Path{get;set;}

        public BlockDevice(string path) {
			this.Path = path;
        }

		public uint BlockSize {get;set;}

		public ulong BlockCount{get;set;}
       

        public byte[] ReadStartBlock(){
			Stream sectorReader = PlatformStreamFactory.Instance().GetPlatformStream(false, this.Path, FileMode.Open);
			// Read 3 sectores : 1st one == MBR, 2nd ans 3rd *may* contain GPT table
			byte[] sector = new byte[512*3];
			int bytesToRead = sector.Length;
			int bytesRead = 0;
			while (bytesRead < sector.Length){
				int read = sectorReader.Read(sector, bytesRead, bytesToRead);
				bytesRead += read;
				bytesRead -= read;
				if(read == 0 && bytesRead < sector.Length) // error happened
					throw new Exception ("Unable to read disk 3 first sectors (read "+bytesRead+" bytes)");

			}
			return sector;

		}

        /*public void WriteBlock(ulong aBlock,
                                        byte[] aContents);*/

        /// <summary>
        /// Tells whether this storage device is already used by for example a FS implementation or a partitioning implementation.
        /// </summary>
        public virtual bool Used {
            get;
            set;
        }
    }

}