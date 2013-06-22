/*using System;
using System.Xml;
using System.Linq;
using System.Collections;
using Node.Utilities;
using Libvirt;
using P2PBackup.Common;

namespace Node.Virtualization{

	public class VirtualMachinesManager	{
		IntPtr conn = IntPtr.Zero;
		public VirtualMachinesManager (){
			try{
				conn = Connect.Open("");
				ulong uver = 0;
				Connect.GetLibVersion(conn, ref uver);
				Logger.Append(Severity.INFO, "Instanciated VirtualMachinesManager using Libvirt version "+uver);
			}
			catch(Exception e){
				Logger.Append(Severity.WARNING, "Could not instanciate VirtualMachinesManager : "+e.Message);
			}
		}
		
		public string[] GetVMNames(){
			int nbDomains = Connect.NumOfDomains(conn);
			
			ArrayList vmNames = new ArrayList();
			// HOW to correctly retrieve the list of existing doamin IDs?? for now, hardcode 50
			for(int domainID = 1; domainID < 50; domainID++){
				//Console.WriteLine("domainid="+domainID);
				try{
	                IntPtr domainPtr = Domain.LookupByID(conn, domainID);
					if(domainPtr != IntPtr.Zero){
		                string domainName = Domain.GetName(domainPtr);
						vmNames.Add(domainName);
		              	Domain.Free(domainPtr);
					}
				}
				catch(Exception ptrE){
					Logger.Append(Severity.ERROR, "Error Getting VM #"+domainID+ " : "+ptrE.Message);
				}
         	}
			Logger.Append(Severity.DEBUG2, "Got "+nbDomains+" VMs");
			return (string[])vmNames.ToArray(typeof(string));
			
		}
		
		public VMDrive[] GetVMDrives(string vmName){
			ArrayList drives = new ArrayList();
			Logger.Append(Severity.DEBUG2, "Getting drives of VM '"+vmName+"'");
			try{
				IntPtr domainPtr = Domain.LookupByName(conn, vmName);	
				foreach (XmlNode deviceNode in  GetDomainBlockDevices(Domain.GetXMLDesc(domainPtr, 0))){
					VMDrive vmd = new VMDrive();
					vmd.DeviceFile = deviceNode.SelectSingleNode("source").Attributes["file"].Value;
					//IntPtr vol = StorageVol.LookupByPath(conn, vmd.DeviceFile);
					vmd.DeviceName = deviceNode.SelectSingleNode("target").Attributes["dev"].Value;
					vmd.DeviceType = deviceNode.Attributes["device"].Value;
	              	drives.Add(vmd);
	          	}
				return (VMDrive[])drives.ToArray(typeof(VMDrive));
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "Error Getting drives of VM '"+vmName+"' : "+e.Message);
			}
			return null;
		}
		
		 private XmlNodeList GetDomainBlockDevices(string xmlDomainDescription){
	        XmlDocument xmlDescription = new XmlDocument();
	        xmlDescription.LoadXml(xmlDomainDescription);
	        XmlNodeList devNodeList = xmlDescription.SelectNodes("//domain/devices/disk");
	        //return (XmlNodeList)(from XmlNode xn in devNodeList select xn.SelectNodes("disk") as XmlNodeList);
			return devNodeList;
      	}
		
		
		internal string BuildVmsJson(){
			string json = "[";
			foreach(string vmName in GetVMNames()){
				if (vmName == null)
					continue;
				json +="{name:\""+vmName+"\", checked:false, leaf:false, children:[";
				foreach(VMDrive d in GetVMDrives(vmName)){	
					json += "{name:\""+d.DeviceName+"\", file:\""+d.DeviceFile+"\", type:\""+d.DeviceType+"\", checked:false, leaf:true}, ";
				}
				json += "]}, ";
			}
			json += "]";
			return json;
		}
	}
}*/

