using System;
using System.Collections.Generic;
using P2PBackup.Common;
using System.Runtime.Serialization.Formatters.Binary;

using VimApi;

namespace VMWare{

	public class VMWareVmConfig : ISpecialObject{


		public string Name{get{return "VMWare VM configuration";}}
		public string Version{get{return "0.1";}}
		public bool IsProxyingPlugin{get{return true;}}

		public BasePathConfig Config{get;set;}
		public List<BasePath> BasePaths{get; private set;}
		public List<string> ExplodedComponents{get; private set;}
		public SPOMetadata Metadata{get;set;}
		public event EventHandler<LogEventArgs> LogEvent;
		public RestoreOrder RestorePosition{get; private set;}

		private ProxyTaskInfo pti;

		public VMWareVmConfig(){}

		public VMWareVmConfig(ProxyTaskInfo pti){
			this.RestorePosition = RestoreOrder.BeforeStorage;
			this.pti = pti;
			this.BasePaths = new List<BasePath>(); // unused , but initialize it anyway
			this.ExplodedComponents = new List<string>();
		}

		public void SetItems(List<string> spoPaths){
			VMWareHandler vmwh = new VMWareHandler();

			try{
				vmwh.Connect(pti.Hypervisor.Url, pti.Hypervisor.UserName, pti.Hypervisor.Password.Value);
				VirtualMachineConfigInfo vmci = vmwh.GetVmConfig(pti.Node);
				System.IO.MemoryStream ms = new System.IO.MemoryStream();
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(ms, vmci);
				ms.Flush ();
				Console.WriteLine ("VMWareSPO serialized vm config called ");
				//serialize vmci to Metadata!!!
				this.Metadata = new SPOMetadata();
				this.Metadata.Metadata.Add(pti.Node.InternalId, ms.ToArray());
			}
			catch(Exception e){
				if(LogEvent != null) LogEvent(this, new LogEventArgs(850, Severity.WARNING, e.Message));
				Console.WriteLine ("VMWareSPO error : "+e.ToString());
			}
		}

		public void Freeze(){
			// N/A
		}
		
		public void Resume(){
			// N/A
		}

		public void PrepareRestore(List<string> spoPaths){

		}

		public void Restore(){

		}

	
		public void Dispose(){

		}



	}


}

