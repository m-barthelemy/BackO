using System;
using System.Collections.Generic;
using P2PBackup.Common;

namespace Node.Snapshots{

	/// <summary>
	/// Skeleton to define a custom application plugin
	/// </summary>
	public class SpecialObjectPluginSkeletton: ISpecialObject{

		public string Name{get{return "myPluginName";}}
		public string Version{get{return "0.1";}}
		public bool IsProxyingPlugin{get{return false;}}

		public List<BasePath> BasePaths{get;private set;}
		public List<string> ExplodedComponents{get; private set;}
		public event EventHandler<LogEventArgs> LogEvent;
		public SPOMetadata Metadata{get;set;}
		public RestoreOrder RestorePosition{get; private set;}

		public SpecialObjectPluginSkeletton(){
			this.RestorePosition = RestoreOrder.AfterStorage;
			this.BasePaths = new List<BasePath>(); // initialize it even if unused
			this.ExplodedComponents = new List<string>();// initialize it even if unused
		}

		public void SetItems(List<string> spoPaths){
			// here we receive a list of components of our app that we want to backup
			// for example /myGreatApp/instance1/componentXX

			// AND if needed, we need to fill this.BasePaths with everything that needs to be backuped

		}

		public void Freeze(){
			if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "About to freze my super appp!"));
			//do everything that needs to be done with your app for it to be consistent on snapshot
			// everything can mean 'write pending data to files on disk', 'commit transactions', 'close transaction log'...

		}

		public void Resume(){
			// do everything needed to get your app working again after the freeze
		}
			
		// should return a dictionary <actiontype, string name> describing
		// what will be done if restore is really performed
		//eg. ActionType.StopService, "mysuperservice"
		//		ActionType.Reboot, ""
		public void PrepareRestore(List<string> spoPaths){

		}

		public void Restore(){

		}
		
	}
}

