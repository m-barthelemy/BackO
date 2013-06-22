using System;
using System.Collections.Generic;
using System.Threading;
using P2PBackup.Common;
using P2PBackupHub.Utilities;

namespace P2PBackupHub {
	
	/// <summary>
	/// This class handles notifications related to a tasks status :
	/// save to DB, notify by mail on error, start another task upon previous task completion...
	/// </summary>
	public class TaskPublisher {
		private static TaskPublisher _instance;
		private Queue<Task> notifyQueue;
		
		private TaskPublisher() {
			notifyQueue = new Queue<Task>();
		}
		
		internal static TaskPublisher Instance(){
			if(_instance == null)
				_instance = new TaskPublisher();
			return _instance;
		}
			
		internal void Notify(Task t){
			if(t.BackupSet.Notifications == null) return;
			// Archiving
			//if(t.RunningStatus == TaskRunningStatus.Done || t.RunningStatus == TaskRunningStatus.Cancelled)
				//(new DBHandle()).CompleteBackupTracking(t);
			//if(t.RunningStatus == TaskRunningStatus.Done
			foreach(TaskNotification notification in t.BackupSet.Notifications ){
				if(notification.Status == t.RunStatus){
					NotifierFactory.GetNotifier(notification.Notifier).Fire(t);
					Logger.Append("HUBRN", Severity.DEBUG, "Notified provider '"+notification.Notifier+"' for task "+t.Id+" with status "+t.RunStatus);
				}
			}
			
		}
	}
}

