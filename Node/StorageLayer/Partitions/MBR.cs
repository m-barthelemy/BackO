// Reuses great code from the Cosmos OS Project, Under New BSD License.
// See https://cosmos.codeplex.com




using System;
using P2PBackup.Common.Volumes;

namespace Node.StorageLayer  {

	internal class MBR :PartitionManager{

		internal IBMPartitionInformation[] Partitions{get; set;}
		internal  uint SectorSize{get;set;}
		private string drivePath;
		internal BlockDevice blockdevice{get;private set;}
		internal byte[] Sector;


		internal MBR(){
			this.Partitions = new IBMPartitionInformation[4];
            this.Partitions[0] = new IBMPartitionInformation(this, 0);
            this.Partitions[1] = new IBMPartitionInformation(this, 1);
            this.Partitions[2] = new IBMPartitionInformation(this, 2);
            this.Partitions[3] = new IBMPartitionInformation(this, 3);
		}

		internal MBR(string drivePath){
			this.drivePath = drivePath;
			this.blockdevice = new BlockDevice(this.drivePath);
		}

		internal MBR(byte[] firstSectors){
			//this.drivePath = drivePath;
			//this.blockdevice = new BlockDevice(this.drivePath);
			this.Sector = firstSectors;
			this.Partitions = new IBMPartitionInformation[4];
            this.Partitions[0] = new IBMPartitionInformation(this, 0);
            this.Partitions[1] = new IBMPartitionInformation(this, 1);
            this.Partitions[2] = new IBMPartitionInformation(this, 2);
            this.Partitions[3] = new IBMPartitionInformation(this, 3);
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

        internal MBR(BlockDevice bd){
           this.blockdevice = bd;

            this.Partitions = new IBMPartitionInformation[4];
            this.Partitions[0] = new IBMPartitionInformation(this, 0);
            this.Partitions[1] = new IBMPartitionInformation(this, 1);
            this.Partitions[2] = new IBMPartitionInformation(this, 2);
            this.Partitions[3] = new IBMPartitionInformation(this, 3);
            Refresh();
        }

       

        internal byte[] Code{

            get{
                byte[] b = new byte[440];
                Array.Copy(Sector, b, 440);
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
                Array.Copy(value, Sector, 0);
            }

        }

        internal uint DiskSignature{
            get{
                return BitConverter.ToUInt32(Sector, 440);
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
                return BitConverter.ToUInt16(Sector, 444);
            }
            set{
                byte[] b = BitConverter.GetBytes(value);
                Sector[444] = b[0];
                Sector[445] = b[1];
            }
        }

        

       internal ushort MBRSignature{
            get{
                return BitConverter.ToUInt16(Sector,510);
            }
       }

       internal void Refresh(){
            // disable and remove any partitions we already added
			if(!string.IsNullOrEmpty(this.drivePath))
            	this.Sector = blockdevice.ReadStartBlock();
            //add partitons back to device list
        }
        /*public void Save(){
            Sector[510] = 0x55;
            Sector[511] = 0xaa;
            blockdevice.WriteBlock(0, Sector);
        }*/

        internal virtual bool IsValid(){

            return MBRSignature == 0x55aa;
        }

        

        internal string Name
        {
            get { throw new NotImplementedException(); }
        }

	}





	internal class IBMPartitionInformation{

        private MBR mbr;
        private int offset;

        internal IBMPartitionInformation(MBR mbr, int index){
            this.mbr=mbr;
            this.offset=446+16*index;
        }

        internal byte Status{
            get{
                return mbr.Sector[offset];
            }
            set {
                mbr.Sector[offset] = value;
            }
        }

        internal bool Bootable{
            get{
                return (Status & 0x80) == 0x80;
            }
            set{
                Status = (byte)((Status & 0x7f) | (value ? 0x80 : 0));
            }
        }

        internal PartitionTypes PartitionType{
            get{


				return (PartitionTypes)mbr.Sector[offset + 4]; //BitConverter.ToUInt32(new byte[]{mbr.Sector[offset + 4]}, 1);
            }
            set{
				byte[] b = BitConverter.GetBytes((uint)value);
                mbr.Sector[offset + 4] = b[0];
            }
        }

        internal uint StartLBA{
            get{
                return BitConverter.ToUInt32(mbr.Sector, offset+8);
            }
            set{
                byte[] b = BitConverter.GetBytes(value);
                mbr.Sector[offset + 8] = b[0];
                mbr.Sector[offset + 9] = b[1];
                mbr.Sector[offset + 10] = b[2];
                mbr.Sector[offset + 11] = b[3];
            }
        }

        internal uint LengthLBA{
            get{
                return BitConverter.ToUInt32(mbr.Sector, offset + 12);
            }
            set{
                byte[] b = BitConverter.GetBytes(value);
                mbr.Sector[offset + 12] = b[0];
                mbr.Sector[offset + 13] = b[1];
                mbr.Sector[offset + 14] = b[2];
                mbr.Sector[offset + 15] = b[3];
            }
        }

        internal bool ValidPartition() {
            return (PartitionType > 0 && (Status == 0x00 || Status == 0x80));
        }

        internal Partition GetPartitionDevice(){
            if (ValidPartition())
                return new MBRPartition(this, mbr);
            return null;
        }
    }


    internal class MBRPartition : Partition{

        private MBR Mbr;
        private uint Start, Length;
        private BlockDevice blockDev;

        internal MBRPartition(IBMPartitionInformation info, MBR mbr){
            Mbr = mbr;
            Start = info.StartLBA;
            Length = info.LengthLBA;
            //Identifier = info.PartitionType;
            blockDev = mbr.blockdevice;
        }

        internal uint BlockSize{
            get { return blockDev.BlockSize; }
        }

        internal ulong BlockCount{
            get { return Length; }
        }

       
    }
}

