using System;
using System.Threading;
//using Ubiquity.Content;
//using Ubiquity.Utilities;
using P2PBackupHub.Utilities;
using P2PBackup.Common;

namespace P2PBackupHub{

	public class State{
		public int LastId{get;set;}
	}

	// quick and dirty unique ids provider.
	// TODO : make this thing serious, more robust/elegant
	
	public class IdManager/*:IStartableComponent*/{
		
		private static readonly IdManager _idm = new IdManager();
		private bool started = false;
		
		private static int id;
		private static bool stopping = false;
		
		private IdManager(){
			
		}
		
		public static IdManager Instance{get{return _idm;}}
		
		public void Setup(){
			id = 0;
		}
		
		public void Start(){
			if(started)
				throw new Exception("Already started");
			started = true;
			if(id == 0)
				id = new P2PBackupHub.DAL.StateDAO().GetLast()/*.LastIdManagerId*/;
			GetId();
			Logger.Append("START", Severity.DEBUG, "IDManager will start after ID #"+id);
		}
		
		public void Stop(){
			stopping = true;/*
			State state = new State();
			state.LastId = id;
			new P2PBackupHub.DAL.StateDAO().Save(state);*/
			Logger.Append("STOP", Severity.INFO, "IDManager stopped at ID #"+id);
		}
		
		public static int GetId(){
			if(!stopping){
				int newId = Interlocked.Increment(ref id);
				new P2PBackupHub.DAL.StateDAO().Save(new State{LastId = newId});
				return newId;
			}
			else
				throw new InvalidOperationException("IdManager is stopping");
		}
		
	}
	
	
}

