using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Collections;
using System.Web.Services.Protocols;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;
using P2PBackup.Common.Virtualization;
using VimApi;

/// <summary>
/// VM ware handler.
/// For vm WS sdk to Work on unix/mono, we must generate the VimService WITHOUT the tricks explained on SDK page,
/// that is without commenting Xml attributes, and without building the VimService.XmlSerializer.
/// The drawback here is a serious performance penalty on Windows, where this class takes a long time to initialize
/// </summary>
namespace VMWare {

	public class VMWareHandler:IVmProvider, IDisposable{
		protected static VimService service;
		protected ServiceContent serviceContent;
		protected ManagedObjectReference serviceRef;

		private ManagedObjectReference propertiesCollector;
		private ManagedObjectReference rootF;


		/*public delegate void LogHandler(int code, Severity severity, string message);
		public event LogHandler LogEvent;*/

		public delegate void LogHandler(int code, Severity severity, string message);
		public event EventHandler<LogEventArgs> LogEvent;

		public string Name{get{return "vmware";}}

		//internal static VirtualMachineConfigInfo VmConfig{get; private set;}

		public  bool Connect(string url, string userName, string password){
			//try{
				//LogEvent(this, new LogEventArgs(700, Severity.INFO, "Connecting to hypervisor "+url));
				service = new VimService();
				service.Url = url;
				service.CookieContainer = new System.Net.CookieContainer();

				serviceContent = service.RetrieveServiceContent(serviceRef);
				propertiesCollector = serviceContent.propertyCollector;
				rootF = serviceContent.rootFolder;

				if (serviceContent.sessionManager != null) {
					UserSession us = service.Login(serviceContent.sessionManager, userName, password, null);
					if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.TRIVIA, "Got session '"+us.userName+"@"+us.loginTime+"'"));
				}
				if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "Instanciated connection to hypervisor "+url));
				return true;
			//}
			//catch(Exception e){
				//Logger.Append("HUBRN", Severity.ERROR, "Could not connect to hypervisor '"+url+"' : "+e.ToString());
			//	return false;
			//}
		}

			


		public VMWareHandler() {
			CreateServiceRef("ServiceInstance");
		}

		public void Dispose(){
			service.Logout(serviceRef);
		}

		private void CreateServiceRef(string svcRefVal) {
			 // Manage bad certificates our way:
			 System.Net.ServicePointManager.CertificatePolicy = new CertPolicy();
			 serviceRef = new ManagedObjectReference();
			 serviceRef.type = "ServiceInstance";
			 // could be ServiceInstance for "HostAgent" and "VPX" for VPXd
			 serviceRef.Value = svcRefVal;
		}


		public List<P2PBackup.Common.Node> GetVMs(){ 

			List<P2PBackup.Common.Node> vms = new List<P2PBackup.Common.Node>();
			string[] props = new string[] { "name", "config", "guest.ipAddress", "guest.guestFamily" };
			//Console.WriteLine("GetVMs : calling GetObjectProperties ");
			ObjectContent[] vmObjects = GetObjectProperties("VirtualMachine", props, null);
			if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "GetVMs : got "+vmObjects.Length+" Objects"));
			if (vmObjects == null) 
				return vms;
			ObjectContent oc = null;
			ManagedObjectReference mor = null;
			DynamicProperty[] pcary = null;
			DynamicProperty pc = null;

			for (int oci = 0; oci < vmObjects.Length; oci++) {
				oc = vmObjects[oci];
				mor = oc.obj;
				pcary = oc.propSet;
				
				if(mor.type != "VirtualMachine" || pcary == null)
					continue;

				P2PBackup.Common.Node vm = new P2PBackup.Common.Node();
		   		for (int i = 0; i < pcary.Length; i++){
				       	pc = pcary[i];	
					if(pc.name == "guest.ipAddress")
						vm.IP = pc.val.ToString();
					else if(pc.name == "guest.guestFamily")
						vm.OS = pc.val.ToString();
					else if(pc.name == "name")
						vm.Name = pc.val.ToString();
					if (pc == null)
						continue;
   					if (pc.val is VirtualMachineConfigInfo){
	       					VirtualMachineConfigInfo vmci = (VirtualMachineConfigInfo)pc.val;
						vm.InternalId = vmci.uuid;
						//vm.NodeName = vmci.name;
						vm.Kind = KindEnum.Virtual;
						vm.Version = vmci.version;

   					}
		 		} // end for
				vms.Add(vm);
				if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.TRIVIA, "Found VM name '"+vm.Name+"', ip="+vm.IP+", uuid="+vm.InternalId));
				//GetDisks(vm);
			}
			if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.TRIVIA, "Discovery end."));
			return vms;
		}

		private ManagedObjectReference GetVmMoRef(P2PBackup.Common.Node vm){
			//VimApi.ServiceContent localServiceContent = service.RetrieveServiceContent(serviceRef);
			ManagedObjectReference searchIndex =  serviceContent.searchIndex;
			LogEvent(this, new LogEventArgs(0, Severity.TRIVIA, "Searching VM with uuid '"+vm.InternalId+"'"));
			ManagedObjectReference vmMor = service.FindByUuid(searchIndex, null, vm.InternalId, true);
			if(vmMor != null)
				LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VM search returned vmMor '"+vmMor.Value+"'"));
			else
				throw new Exception("Didn't found any matching VM");
			return vmMor;
		}

		public string GetVmMMorefId(P2PBackup.Common.Node vm){
			return GetVmMoRef(vm).Value;
		}

		public List<Disk> GetDisks(P2PBackup.Common.Node vm){
			//Logger.Append("HUBRN", Severity.TRIVIA, "Searching for vm with uuid '"+vm.InternalId+"' disks...");
			List<Disk> vmRawDisks = new List<Disk>();
			//try{

				
				string[] props = new string[] { "config"};
				ObjectContent[] vmObjects = GetObjectProperties("VirtualMachine", props, GetVmMoRef(vm));

				ObjectContent oc = null;
				ManagedObjectReference mor = null;
				DynamicProperty[] pcary = null;
				DynamicProperty pc = null;
				for (int oci = 0; oci < vmObjects.Length; oci++) {
					oc = vmObjects[oci];
					mor = oc.obj;
					pcary = oc.propSet;
					
					if(mor.type != "VirtualMachine" || pcary == null)
						continue;
					//Console.WriteLine("Id : " + mor.Value); //   VM MoRef

			   		for (int i = 0; i < pcary.Length; i++){
					       pc = pcary[i];
	   					if (pc.val is VirtualMachineConfigInfo){

							VirtualMachineConfigInfo vmci = (VirtualMachineConfigInfo)pc.val;
							//VmConfig = vmci;								
							/*Console.WriteLine("\t Version : " + vmci.version);
							Console.WriteLine("\t files.vmPathName : " + vmci.files.vmPathName);
							// changeTrackingEnabled
							Console.WriteLine("\t guestId : " + vmci.guestId);
							Console.WriteLine("\t disk devices : ");*/
							foreach (VirtualDevice vDev in vmci.hardware.device){
								//Console.WriteLine("\t\t"+vDev.deviceInfo.label+" "+vDev.deviceInfo.dynamicProperty.ToString());
								//if (vDev.backing is
								if (vDev.backing is VirtualDiskFlatVer2BackingInfo ||
									vDev.backing is VirtualDiskFlatVer1BackingInfo ||
							    	vDev.backing is VirtualDiskSparseVer2BackingInfo ||
							    	vDev.backing is VirtualDiskRawDiskMappingVer1BackingInfo ||
							    	vDev.backing is VirtualDiskRawDiskVer2BackingInfo
							    	)
								{
									Disk rawDrive = new Disk();
									VirtualDiskFlatVer2BackingInfo vDisk = (VirtualDiskFlatVer2BackingInfo)vDev.backing;
									
									//rawDrive.BlockDevice = vDisk.fileName;
									//rawDrive.DriveType = System.IO.DriveType.Fixed;
									//rawDrive.DriveFormat = "VMFS";
									rawDrive.SnapshotType = SnapshotType.VADP;
									rawDrive.Path = vDisk.fileName; //.Substring(vDisk.fileName.IndexOf("]"+1));
									if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.TRIVIA, "Found disk '"+vDev.deviceInfo.label+"' with path '"+rawDrive.Path+"'"));

									// independant_persistent disks are not part of snapshots, thus unbackupable
									if(vDisk.diskMode ==  "independent_persistent" 
								   		|| vDisk.dynamicType != null
								   	
								   		){
										if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.INFO, "The disk '"+vDisk.fileName+"' won't be accessible : mode="+vDisk.diskMode+", dynamic="+vDisk.dynamicType));
										rawDrive.Enabled = false;
									}
									else if(vDev.backing is VirtualDiskRawDiskMappingVer1BackingInfo ){
										if ( ((VirtualDiskRawDiskMappingVer1BackingInfo)vDev.backing).compatibilityMode == "physical"){
											if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.INFO, "The disk '"+vDisk.fileName+"' won't be accessible since it's a physical RDM"));
											rawDrive.Enabled = false;
										}
									}
									
									else
										rawDrive.Enabled = true;
									/*Console.WriteLine("\t\t  diskMode: " + vDisk.diskMode);
									Console.WriteLine("\t\t  fileName: " + vDisk.fileName);
									String dataStoreName = vDisk.fileName.Substring(vDisk.fileName.IndexOf("[")+1, vDisk.fileName.LastIndexOf("]") - 1);
									//Console.WriteLine("\t\t  fileName: " + vDisk.dynamicProperty);
									Console.WriteLine("\t\t  dynamic: " + vDisk.dynamicType);*/
									/*if (vDisk.dynamicProperty == null) continue;
									foreach (DynamicProperty dp in vDisk.dynamicProperty){
										Console.WriteLine("\t\t* " + dp.name + " : " + dp.val);
									}*/
									vmRawDisks.Add(rawDrive);
								}
							}
	   					}
			 		} // end for
				}

			//}
			//catch(Exception ex){
				//Logger.Append("HUBRN", Severity.ERROR, "Could not get vm disks : "+ex.ToString());
			//}

			return vmRawDisks;
		}


		public string Snapshot(P2PBackup.Common.Node vm, string snapshotName){

			ManagedObjectReference searchIndex =  this.serviceContent.searchIndex;
			ManagedObjectReference vmMor = service.FindByUuid(searchIndex, null, vm.InternalId, true);
			LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VM search returned vmMor '"+vmMor.Value+"'"));
			LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "About to create a snapshot with name '"+snapshotName+"'"));
			ManagedObjectReference taskMor = service.CreateSnapshot_Task(vmMor, snapshotName, "", false, true);
			//TaskInfo ti = new TaskInfo();
			if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.TRIVIA, "Snapshot task has mor '"+taskMor.Value+"'"));
			TaskInfo snapTaskInfo = new TaskInfo();
			while(true){
				try{
					ObjectContent[] oCary = GetObjectProperties("Task", new string[]{"info" }, taskMor);
					snapTaskInfo = snapTaskInfo = (TaskInfo)oCary[0].propSet[0].val;
					if(LogEvent != null) LogEvent(this, new LogEventArgs(700, Severity.DEBUG, "Snapshot task completion "+snapTaskInfo.progress+"%, state="+snapTaskInfo.state.ToString()));
					if(snapTaskInfo.state == TaskInfoState.running || snapTaskInfo.state == TaskInfoState.queued)
						System.Threading.Thread.Sleep(10000);
					else
						break;
					}
				catch{}
			}
			if(snapTaskInfo.state != TaskInfoState.success){
				throw new Exception("Couldn't get VM snapshot, task ended with status "+snapTaskInfo.state+", reason="+snapTaskInfo.reason.ToString());

			}
			ManagedObjectReference snapMor = (ManagedObjectReference)snapTaskInfo.result;

		//	Console.WriteLine ("VMWareHandler.Snapshot') : task mor="+snapName);
			if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.INFO, "Created VM snapshot with mor '"+snapMor.Value+"'"));
			return snapMor.Value;
		}

		internal VirtualMachineSnapshotInfo GetSnapshot(P2PBackup.Common.Node vm){

			ObjectContent[] oCary = GetObjectProperties("VirtualMachine", new string[]{"snapshot" }, null/*GetVmMoRef(vm)*/);
			ObjectContent oc = null;
			ManagedObjectReference mor = null;
			//ManagedObjectReference snapMor = null;
			DynamicProperty[] pcary = null;
			//DynamicProperty pc = null;
			for (int oci = 0; oci < oCary.Length; oci++) {
				oc = oCary[oci];
				mor = oc.obj;
				pcary = oc.propSet;
				
				if(mor.type != "VirtualMachine" || pcary == null)
					continue;
				//for (int i = 0; i < pcary.Length; i++){
				 //   pc = pcary[i];
   					if (pcary[pcary.Length-1].val is VirtualMachineSnapshotInfo)
						return  (VirtualMachineSnapshotInfo)pcary[pcary.Length-1].val;
			}
			return null;
		}

		internal VirtualMachineConfigInfo GetVmConfig(P2PBackup.Common.Node vm){

			ObjectContent[] oCary = GetObjectProperties("VirtualMachine", new string[]{"config" }, null/*GetVmMoRef(vm)*/);
			ObjectContent oc = null;
			ManagedObjectReference mor = null;
			//ManagedObjectReference snapMor = null;
			DynamicProperty[] pcary = null;
			//DynamicProperty pc = null;
			for (int oci = 0; oci < oCary.Length; oci++) {
				oc = oCary[oci];
				mor = oc.obj;
				pcary = oc.propSet;
				
				if(mor.type != "VirtualMachine" || pcary == null)
					continue;
				//for (int i = 0; i < pcary.Length; i++){
				 //   pc = pcary[i];
   					if (pcary[pcary.Length-1].val is VirtualMachineConfigInfo)
						return  (VirtualMachineConfigInfo)pcary[pcary.Length-1].val;
			}
			return null;
		}

		public string DeleteSnapshot(P2PBackup.Common.Node vm, string snapMoref){

			string deleteTask = null;

			ObjectContent[] oCary = GetObjectProperties("VirtualMachine", new string[]{"snapshot" }, null/*GetVmMoRef(vm)*/);
			ObjectContent oc = null;
			ManagedObjectReference mor = null;
			ManagedObjectReference snapMor = null;
			DynamicProperty[] pcary = null;
			DynamicProperty pc = null;
			for (int oci = 0; oci < oCary.Length; oci++) {
				oc = oCary[oci];
				mor = oc.obj;
				pcary = oc.propSet;
				
				if(mor.type != "VirtualMachine" || pcary == null)
					continue;
				for (int i = 0; i < pcary.Length; i++){
				    pc = pcary[i];
   					if (pc.val is VirtualMachineSnapshotInfo){
						VirtualMachineSnapshotInfo snapInfo = (VirtualMachineSnapshotInfo)pc.val;
						ManagedObjectReference theSnapMor = snapInfo.currentSnapshot;
						if(theSnapMor.Value == snapMoref){
							snapMor = theSnapMor;
							break;
						}
					}
				}
				if(snapMor != null) break;
			}
			if(snapMor != null){
				if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.INFO, "Deleting snapshot '"+snapMor.Value+"' for virtual node "+vm.ToString()));

				try{
					deleteTask = service.RemoveSnapshot_Task(snapMor, true).Value;
				}
				catch{} // timed-out getting vsphere response, ignore.

			}
			return deleteTask;

		}


		private ObjectContent[] GetObjectProperties(string objectType, string[] properties, ManagedObjectReference o){

			// Create a Filter Spec to Retrieve Contents for...
			TraversalSpec rpToVm = new TraversalSpec();
			rpToVm.name = "rpToVm";
			rpToVm.type = "ResourcePool";
			rpToVm.path = "vm";
			rpToVm.skip = false;

			// Recurse through all ResourcePools
			TraversalSpec rpToRp = new TraversalSpec();
			rpToRp.name = "rpToRp";
			rpToRp.type = "ResourcePool";
			rpToRp.path = "resourcePool";
			rpToRp.skip = false;

			rpToRp.selectSet = new SelectionSpec[] { new SelectionSpec(), new SelectionSpec() };
			rpToRp.selectSet[0].name = "rpToRp";
			rpToRp.selectSet[1].name = "rpToVm";

			// Traversal through ResourcePool branch
			TraversalSpec crToRp = new TraversalSpec();
			crToRp.name = "crToRp";
			crToRp.type = "ComputeResource";
			crToRp.path = "resourcePool";
			crToRp.skip = false;
			crToRp.selectSet = new SelectionSpec[] { new SelectionSpec(), new SelectionSpec() };
			crToRp.selectSet[0].name = "rpToRp";
			crToRp.selectSet[1].name = "rpToVm";

			// Traversal through host branch
			TraversalSpec crToH = new TraversalSpec();
			crToH.name = "crToH";
			crToH.type = "ComputeResource";
			crToH.path = "host";
			crToH.skip = false;

			// Traversal through hostFolder branch
			TraversalSpec dcToHf = new TraversalSpec();
			dcToHf.name = "dcToHf";
			dcToHf.type = "Datacenter";
			dcToHf.path = "hostFolder";
			dcToHf.skip = false;
			dcToHf.selectSet = new SelectionSpec[] { new SelectionSpec() };
			dcToHf.selectSet[0].name = "visitFolders";

			// Traversal through vmFolder branch
			TraversalSpec dcToVmf = new TraversalSpec();
			dcToVmf.name = "dcToVmf";
			dcToVmf.type = "Datacenter";
			dcToVmf.path = "vmFolder";
			dcToVmf.skip = false;
			dcToVmf.selectSet = new SelectionSpec[] { new SelectionSpec() };
			dcToVmf.selectSet[0].name = "visitFolders";

			// Recurse through all Hosts
			TraversalSpec HToVm = new TraversalSpec();
			HToVm.name = "HToVm";
			HToVm.type = "HostSystem";
			HToVm.path = "vm";
			HToVm.skip = false;
			HToVm.selectSet = new SelectionSpec[] { new SelectionSpec() };
			HToVm.selectSet[0].name = "visitFolders";

			// Recurse thriugh the folders
			TraversalSpec visitFolders = new TraversalSpec();
			visitFolders.name = "visitFolders";
			visitFolders.type = "Folder";
			visitFolders.path = "childEntity";
			visitFolders.skip = false;
			visitFolders.selectSet = new SelectionSpec[] { new SelectionSpec(), new SelectionSpec(), new SelectionSpec(), new SelectionSpec(), new SelectionSpec(), new SelectionSpec(), new SelectionSpec() };
			visitFolders.selectSet[0].name = "visitFolders";
			visitFolders.selectSet[1].name = "dcToHf";
			visitFolders.selectSet[2].name = "dcToVmf";
			visitFolders.selectSet[3].name = "crToH";
			visitFolders.selectSet[4].name = "crToRp";
			visitFolders.selectSet[5].name = "HToVm";
			visitFolders.selectSet[6].name = "rpToVm";
			SelectionSpec[] selectionSpecs = new SelectionSpec[] { visitFolders, dcToVmf, dcToHf, crToH, crToRp, rpToRp, HToVm, rpToVm };

			PropertySpec[] propspecary = new PropertySpec[] { new PropertySpec() };
			propspecary[0].all = false;
			propspecary[0].allSpecified = true;
			//propspecary[0].pathSet = new string[] { "name" };
			// propspecary[0].type = "ManagedEntity";
			propspecary[0].pathSet = properties;
			propspecary[0].type = objectType;

			PropertyFilterSpec spec = new PropertyFilterSpec();
			spec.propSet = propspecary;
			spec.objectSet = new ObjectSpec[] { new ObjectSpec() };
			if(o == null)
				spec.objectSet[0].obj = rootF;
			else
				spec.objectSet[0].obj = o;
			spec.objectSet[0].skip = false;
			spec.objectSet[0].selectSet =  selectionSpecs;

		
			ObjectContent[] content = null;
			//try{
				content = service.RetrieveProperties(propertiesCollector, new PropertyFilterSpec[] { spec });
			//}
			//catch(Exception e){
				//Logger.Append("HUBRN", Severity.ERROR, "Error retrieving Vmware properties : "+e.ToString());
			//}
			return content;
		}


	
	}// end class



	public class CertPolicy : ICertificatePolicy {
	      private enum CertificateProblem  : uint {
	         CertEXPIRED                   = 0x800B0101,
	         CertVALIDITYPERIODNESTING     = 0x800B0102,
	         CertROLE                      = 0x800B0103,
	         CertPATHLENCONST              = 0x800B0104,
	         CertCRITICAL                  = 0x800B0105,
	         CertPURPOSE                   = 0x800B0106,
	         CertISSUERCHAINING            = 0x800B0107,
	         CertMALFORMED                 = 0x800B0108,
	         CertUNTRUSTEDROOT             = 0x800B0109,
	         CertCHAINING                  = 0x800B010A,
	         CertREVOKED                   = 0x800B010C,
	         CertUNTRUSTEDTESTROOT         = 0x800B010D,
	         CertREVOCATION_FAILURE        = 0x800B010E,
	         CertCN_NO_MATCH               = 0x800B010F,
	         CertWRONG_USAGE               = 0x800B0110,
	         CertUNTRUSTEDCA               = 0x800B0112
	      }

	      private static Hashtable problem2text_;
	      private Hashtable request2problems_; // WebRequest -> ArrayList of error codes

	      public CertPolicy() {
	         if (problem2text_ == null) {
	            problem2text_ = new Hashtable();

	            problem2text_.Add((uint) CertificateProblem.CertEXPIRED, 
	               "A required certificate is not within its validity period.");
	            problem2text_.Add((uint) CertificateProblem.CertVALIDITYPERIODNESTING,
	               "The validity periods of the certification chain do not nest correctly.");
	            problem2text_.Add((uint) CertificateProblem.CertROLE,
	               "A certificate that can only be used as an end-entity is being used as a CA or visa versa.");
	            problem2text_.Add((uint) CertificateProblem.CertPATHLENCONST,
	               "A path length constraint in the certification chain has been violated.");
	            problem2text_.Add((uint) CertificateProblem.CertCRITICAL,
	               "An extension of unknown type that is labeled 'critical' is present in a certificate.");
	            problem2text_.Add((uint) CertificateProblem.CertPURPOSE,
	               "A certificate is being used for a purpose other than that for which it is permitted.");
	            problem2text_.Add((uint) CertificateProblem.CertISSUERCHAINING,
	               "A parent of a given certificate in fact did not issue that child certificate.");
	            problem2text_.Add((uint) CertificateProblem.CertMALFORMED,
	               "A certificate is missing or has an empty value for an important field, such as a subject or issuer name.");
	            problem2text_.Add((uint) CertificateProblem.CertUNTRUSTEDROOT,
	               "A certification chain processed correctly, but terminated in a root certificate which isn't trusted by the trust provider.");
	            problem2text_.Add((uint) CertificateProblem.CertCHAINING,
	               "A chain of certs didn't chain as they should in a certain application of chaining.");
	            problem2text_.Add((uint) CertificateProblem.CertREVOKED,
	               "A certificate was explicitly revoked by its issuer.");
	            problem2text_.Add((uint) CertificateProblem.CertUNTRUSTEDTESTROOT,
	               "The root certificate is a testing certificate and the policy settings disallow test certificates.");
	            problem2text_.Add((uint) CertificateProblem.CertREVOCATION_FAILURE,
	               "The revocation process could not continue - the certificate(s) could not be checked.");
	            problem2text_.Add((uint) CertificateProblem.CertCN_NO_MATCH,
	               "The certificate's CN name does not match the passed value.");
	            problem2text_.Add((uint) CertificateProblem.CertWRONG_USAGE,
	               "The certificate is not valid for the requested usage.");
	            problem2text_.Add((uint) CertificateProblem.CertUNTRUSTEDCA,
	               "Untrusted CA");
	         }

	         request2problems_ = new Hashtable();
	      }

	      // ICertificatePolicy
	      public bool CheckValidationResult(ServicePoint sp, X509Certificate cert, WebRequest request, int problem) {
	         if (problem == 0) {
	            // Check whether we have accumulated any problems so far:
	            ArrayList problemArray = (ArrayList) request2problems_[request];
	            if (problemArray == null) {
	               // No problems so far
	               return true;
	            }

	            string problemList = "";
	            foreach (uint problemCode in problemArray) {
	               string problemText = (string) problem2text_[problemCode];
	               if (problemText == null) {
	                  problemText = "Unknown problem";
	               }
	               problemList += "* " + problemText + "\n\n";
	            }

	            request2problems_.Remove(request);
	            //System.Console.WriteLine("There were one or more problems with the server certificate:\n\n" + problemList);
	            return true;


	         } else {
	            // Stash the problem in the problem array:
	            ArrayList problemArray = (ArrayList) request2problems_[request];
	            if (problemArray == null) {
	               problemArray = new ArrayList();
	               request2problems_[request] = problemArray;
	            }
	            problemArray.Add((uint) problem);
	            return true;
	         }
	      }   
   	}	
}

