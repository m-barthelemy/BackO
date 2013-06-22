using System;
using System.Collections.Generic;
using  System.Runtime.InteropServices;
using System.Xml;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;
using P2PBackup.Common.Virtualization;
using P2PBackupHub.Utilities;
using Libvirt;

namespace P2PBackupHub.Virtualization {
	public class LibvirtHandler : IVmProvider, IDisposable{

		IntPtr conn = IntPtr.Zero;
		public delegate void LogHandler(int code, Severity severity, string message);
		public event EventHandler<LogEventArgs> LogEvent;

		public string Name{get; private set;}

		/// <summary>
		/// Connect the specified url, userName and password.
		/// </summary>
		/// <param name='url'>
		/// driver[+transport]://[username@][hostname][:port]/[path][?extraparameters]
		///  driver : remote, xen, qemu, lxc, openvz, test
		///  transport : tcp, tls, unix, ssh, ext
		///  username : for ssh only (?)
		///  hostname : self explainatory. Should match exactly host's name if tls+cert or sasl+kerberos is used
		///  path : always "/" for Xen, always '/system' for qemu
		/// </param>
		public  bool Connect(string url, string userName, string password){
			try{
				//Libvirt.ConnectAuth auth = new Libvirt.ConnectAuth();

				conn = Libvirt.Connect.Open(url);
				if(conn == IntPtr.Zero){
					Logger.Append("HUBRN", Severity.ERROR, "Could not connect to Libvirt instance at '"+url+"'");
					return false;
				}
				ulong uver = 0;
				Libvirt.Connect.GetLibVersion(conn, ref uver);
				Logger.Append("HUBRN", Severity.INFO, "Instanciated connection to "+url+" using Libvirt version "+uver);
				return true;
			}
			catch(Exception e){
				Logger.Append("HUBRN", Severity.WARNING, "Could not instanciate VirtualMachinesManager : "+e.Message);
			}
			return false;
		}

		public List<P2PBackup.Common.Node> GetVMs(){

			int nbDomains = Libvirt.Connect.NumOfDomains(conn);
			int[] runningDomains = new int[nbDomains];
			Libvirt.Connect.ListDomains(conn, runningDomains, nbDomains);
			List<P2PBackup.Common.Node> vms = new List<P2PBackup.Common.Node>();
			Logger.Append("HUBRN", Severity.DEBUG, "Hypervisor "+this.Name+" has "+nbDomains+" domains(s) registered");
			//ArrayList vmNames = new ArrayList();
			// HOW to correctly retrieve the list of existing doamin IDs?? for now, hardcode 500
			for(int i=0; i<runningDomains.Length; i++){
				int domainID = runningDomains[i];
				try{
	                		IntPtr domainPtr = Libvirt.Domain.LookupByID(conn, domainID);
					if(domainPtr != IntPtr.Zero){
				                string domainName = Libvirt.Domain.GetName(domainPtr);
						P2PBackup.Common.Node vm = new P2PBackup.Common.Node();
						vm.Name = domainName;
						vm.OS = Libvirt.Domain.GetOSType(domainPtr);
						vm.Kind = KindEnum.Virtual;
						/*IntPtr buf = IntPtr.Zero;
						//Libvirt.Domain.GetUUIDString(domainPtr, buf);
						//Libvirt.Domain.G
						char[] uuid = new char[128];
						Libvirt.Domain.GetUUID(domainPtr, uuid);
						Console.WriteLine("uuid=");
						foreach(char c in uuid){
							Console.Write(c);
						}
						Console.WriteLine("");
						vm.InternalId = Marshal.PtrToStringAnsi(buf);*/
						/*Console.WriteLine(Domain.GetXMLDesc(domainPtr, 0));
						foreach (XmlNode deviceNode in  GetDomainXmlNodes(Domain.GetXMLDesc(domainPtr, 0))){
							Console.WriteLine(deviceNode.Name+" : "+deviceNode.Value);
						}*/
						vm.InternalId = GetDomainUuid(Domain.GetXMLDesc(domainPtr, 0)).InnerText;
				              	Domain.Free(domainPtr);
						vms.Add(vm);
					}
				}
				catch{} // domain ID doesn't exist
         		}

			return vms;
		}

		private XmlNode GetDomainUuid(string xmlDomainDescription){
		        XmlDocument xmlDescription = new XmlDocument();
		        xmlDescription.LoadXml(xmlDomainDescription);
		        XmlNode devNodeList = xmlDescription.SelectSingleNode("//domain/uuid");
		        //return (XmlNodeList)(from XmlNode xn in devNodeList select xn.SelectNodes("disk") as XmlNodeList);
				return devNodeList;
      		}


		public List<Disk> GetDisks(P2PBackup.Common.Node vm){
			IntPtr domainPtr = Libvirt.Domain.LookupByUUID(conn, vm.InternalId.ToCharArray());
			return new List<Disk>();
		}

		public LibvirtHandler() {
			this.Name = "libvirt";
		}

		public void Dispose(){
			Libvirt.Connect.Close(conn);
			conn = IntPtr.Zero;
		}
	}
}

