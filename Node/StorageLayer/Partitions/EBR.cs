// Reuses great code from the Cosmos OS Project, Under New BSD License.
// See https://cosmos.codeplex.com




using System;
using P2PBackup.Common.Volumes;

namespace Node.StorageLayer  {

	internal class EBR :MBR{

		/*internal IBMPartitionInformation[] Partitions{get; private set;}

		internal readonly uint SectorSize = 512;

		private string drivePath;

		internal BlockDevice blockdevice{get;private set;}
*/

		/*internal MBR(string drivePath){
			this.drivePath = drivePath;
			this.blockdevice = new BlockDevice(this.drivePath);
		}*/

		internal EBR(byte[] firstSectors){
			//this.drivePath = drivePath;
			//this.blockdevice = new BlockDevice(this.drivePath);
			this.Sector = firstSectors;
			this.Partitions = new IBMPartitionInformation[2];
			this.Partitions[0] = new IBMPartitionInformation(this, 0);
			this.Partitions[1] = new IBMPartitionInformation(this, 1);
			//for(int i=0; i<128; i++){
				//this.Partitions[i] = new IBMPartitionInformation(this, i);
			//}
           /* this.Partitions[0] = new IBMPartitionInformation(this, 0);
            this.Partitions[1] = new IBMPartitionInformation(this, 1);
            this.Partitions[2] = new IBMPartitionInformation(this, 2);
            this.Partitions[3] = new IBMPartitionInformation(this, 3);*/

            Refresh();
		}



       /* public static void Initialise(){
            for (int i = 0; i < Cosmos.Hardware2.Device.Devices.Count; i++){
                Device d = Cosmos.Hardware2.Device.Devices[i];
                if (d is Disk){
                    MBR mbr = new MBR(d as Disk);
                    if (mbr.IsValid())
                        Cosmos.Hardware2.Device.Devices.Add(mbr);
                }
            }
        }*/

       /* internal EBR(BlockDevice bd){
           this.blockdevice = bd;

            this.Partitions = new IBMPartitionInformation[128];
            this.Partitions[0] = new IBMPartitionInformation(this, 0);
            this.Partitions[1] = new IBMPartitionInformation(this, 1);
            this.Partitions[2] = new IBMPartitionInformation(this, 2);
            this.Partitions[3] = new IBMPartitionInformation(this, 3);
            Refresh();
        }*/

       // internal byte[] Sector;

      /*  internal byte[] Code{

            get{
                byte[] b = new byte[440];
                Array.Copy(this.Sector, b, 440);
                return b;
            }
            set{
                byte[] b;
                if (value.Length != 440){
                    b = new byte[440];
                    Array.Copy(value, b, 0);
                }
                else{
                    b = value;
                }
                Array.Copy(value, this.Sector, 0);
            }

        }

        internal uint DiskSignature{
            get{
                return BitConverter.ToUInt32(this.Sector, 440);
            }
            set{
                byte[] b = BitConverter.GetBytes(value);
                Sector[440] = b[0];
                Sector[441] = b[1];
                Sector[442] = b[2];
                Sector[443] = b[3];
            }
        }

        internal ushort Null{
            get{
                return BitConverter.ToUInt16(this.Sector, 444);
            }
            set{
                byte[] b = BitConverter.GetBytes(value);
                Sector[444] = b[0];
                Sector[445] = b[1];
            }
        }*/

        

       internal ushort EBRSignature{
            get{
                return BitConverter.ToUInt16(this.Sector,510);
            }
       }

       //internal void Refresh(){
            // disable and remove any partitions we already added
			//if(!string.IsNullOrEmpty(this.drivePath))
           // 	this.Sector = blockdevice.ReadStartBlock();
            //add partitons back to device list
       // }
        /*public void Save(){
            Sector[510] = 0x55;
            Sector[511] = 0xaa;
            blockdevice.WriteBlock(0, Sector);
        }*/

        internal override bool IsValid(){

            return EBRSignature == 0x55aa;
        }

        

        /*internal string Name
        {
            get { throw new NotImplementedException(); }
        }*/

	}




  //  }


    internal class EBRPartition : Partition{

        private EBR Ebr;
        private uint Start, Length;
        private BlockDevice blockDev;

        internal EBRPartition(IBMPartitionInformation info, EBR ebr){
            Ebr = ebr;
            Start = info.StartLBA;
            Length = info.LengthLBA;
            //Identifier = info.PartitionType;
            blockDev = ebr.blockdevice;
        }

        internal uint BlockSize{
            get { return blockDev.BlockSize; }
        }

        internal ulong BlockCount{
            get { return Length; }
        }

        /*public void ReadBlock(ulong aBlock, byte[] aBuffer)
        {
            blockDev.ReadBlock(aBlock + Start, aBuffer);
        }

        public void WriteBlock(ulong aBlock, byte[] aContents)
        {
            blockDev.WriteBlock(aBlock + Start, aContents);
        }

        public string Name
        {
            get { return "MBR Partition [Type="+Identifier + "] in MBR " + blockDev.Name; }
        }*/
    }
}

