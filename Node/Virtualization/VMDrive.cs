using System;
namespace Node.Virtualization{
	public class VMDrive{
		
		private string deviceName; // block device seen by the VM
 		private string deviceFile; // file on host
		private string deviceType;

		public string DeviceFile {
			get {
				return this.deviceFile;
			}
			set {
				deviceFile = value;
			}
		}

		public string DeviceName {
			get {
				return this.deviceName;
			}
			set {
				deviceName = value;
			}
		}

		public string DeviceType {
			get {
				return this.deviceType;
			}
			set {
				deviceType = value;
			}
		}

 // type : disk, cdrom...
		public VMDrive (){
			deviceFile = "";
			deviceName = "";
			deviceType = "";
		}
	}
}

