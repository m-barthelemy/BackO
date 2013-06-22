using System;
using System.Configuration;
using System.Collections.Generic;
using System.Threading;
using P2PBackup.Common;
using P2PBackupHub.Utilities;

namespace P2PBackupHub {

	internal class TasksMonitor {
		private static bool stopRequest = false;
		private static int refreshInterval = 30000;
		
		private TasksMonitor(){
			int confRefreshInterval = 0;
			if(int.TryParse(ConfigurationManager.AppSettings["Scheduler.MonitorInterval"], out confRefreshInterval))
				confRefreshInterval *= 1000;
			if(confRefreshInterval <5000){ // refuse refresh interval < 5 seconds (for performance reasons)
				Logger.Append("CONFIG", Severity.WARNING, "Configuration value for key 'Scheduler.MonitorInterval' is to low ("+confRefreshInterval+"ms), setting it to "+refreshInterval);
				//confRefreshInterval = refreshInterval;
			}
			else
				refreshInterval = confRefreshInterval;
		}
		
		private static void Monitor(){
			while(!stopRequest){
				foreach(Task t in TaskScheduler.Instance().Tasks){
					if(t.RunStatus == TaskRunningStatus.Started){
						PeerNode theNode;
						if(t.BackupSet.HandledBy >0)
							theNode = Hub.NodesList.GetById(t.BackupSet.HandledBy);
						else
							theNode = Hub.NodesList.GetById(t.BackupSet.NodeId);
						try{
							if(theNode != null) theNode.AskStats(t.Id);
						}
						catch(NodeSecurityException nse){// 'transient state', node has disconnected and reconnected but is not (yet) authenticated
							Logger.Append("HUBRN", Severity.WARNING, "Could not ask task #"+t.Id+" status to client node : "+nse.Message);
						}
					}
				}
				Thread.Sleep(refreshInterval);
			}
			Logger.Append("HUBRN", Severity.TRIVIA, "Task monitor stopped");
		}
		
		internal static void Start(){
			Thread workerThread = new Thread(Monitor);
        		workerThread.Start();
			Logger.Append("HUBRN", Severity.INFO, "Started task monitor, frequency "+refreshInterval/1000+"s");
		}
		
		internal static void Stop(){
			stopRequest = true;
			Thread.MemoryBarrier();
		}
	}
}

