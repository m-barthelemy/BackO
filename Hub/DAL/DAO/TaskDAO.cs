using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using ServiceStack.OrmLite;
using P2PBackup.Common;
using P2PBackupHub.Utilities;

namespace P2PBackupHub.DAL {

	public class TaskDAO {

		IDbConnection dbc;
		User currentUser;


		public TaskDAO() {
		}

		public TaskDAO(User sessionUser) {
			currentUser = sessionUser;
		}

		/// <summary>
		/// Save the specified Task objedct including its encryption key, but not it's logentries.
		/// </summary>
		/// <param name='t'>
		/// T.
		/// </param>
		public Task Save(Task t){
			using (dbc = DAL.Instance.GetDb()){
				Password keyP = PasswordManager.Add(new Password{Value=t.EncryptionKey});
				t.EncryptionKeyId = keyP.Id;
				t.Id = IdManager.GetId();
				//bs.Generation = 0;
				dbc.Insert(t);
			}
			return t;
		}

		/// <summary>
		/// Update the specified Task object, but not its encryptionkey nor its logentries
		/// </summary>
		/// <param name='t'>
		/// T.
		/// </param>
		public Task Update(Task t){
			using (dbc = DAL.Instance.GetDb()){
				//bs.Generation ++;
				dbc.Update(t);
			}
			return t;
		}

		/// <summary>
		/// Complete the specified task by updating it, and also inserts logentries.
		/// </summary>
		/// <param name='t'>
		/// The task to mark as complete.
		/// </param>
		public void Complete(Task t){
			Update(t);
			using (dbc = DAL.Instance.GetDb()){
				dbc.InsertAll<TaskLogEntry>(t.LogEntries);
			}
		}

		public Task UpdateStatus(int nodeId, long taskId, TaskRunningStatus newStatus){
			using (dbc = DAL.Instance.GetDb()){
				var checkTaskNode = dbc.Select<Task>(t=>t.NodeId == nodeId && t.Id == taskId);
				if(checkTaskNode.Count  == 1){
					Task t = checkTaskNode[0];
					t.RunStatus = newStatus;
					return Update(t);
				}
				else
					throw new ArgumentException("Could not find task #"+taskId+" belonging to node #"+nodeId);
			}
		}

		public int UpdateInterrupted(){
			using (dbc = DAL.Instance.GetDb()){
				var interruptedTasks = dbc.Select<Task>(t => t.RunStatus == TaskRunningStatus.PendingStart || t.RunStatus == TaskRunningStatus.Started);
				foreach(Task t in interruptedTasks){
					t.RunStatus = TaskRunningStatus.Cancelled;
					t.Status = TaskStatus.Error;
				}
				dbc.UpdateAll(interruptedTasks);
				return interruptedTasks.Count;
			}

		}

		/// <summary>
		/// Gets a task by identifier. Includes the encryption key.
		/// </summary>
		/// <returns>
		/// The by identifier.
		/// </returns>
		/// <param name='id'>
		/// Identifier.
		/// </param>
		public Task GetById(int id){
			using (dbc = DAL.Instance.GetDb()){
				Task t =  dbc.GetById<Task>(id);
				t.EncryptionKey = PasswordManager.Get(t.EncryptionKeyId).Value;
				return t;
			}
		}

		/// <summary>
		/// Gets all the task mathing parameters. Encryption key is not retrieved
		/// </summary>
		/// <returns>
		/// The all.
		/// </returns>
		/// <param name='start'>
		/// Start.
		/// </param>
		/// <param name='limit'>
		/// Limit.
		/// </param>
		/// <param name='orderbyField'>
		/// Orderby field.
		/// </param>
		/// <param name='sortDirection'>
		/// Sort direction.
		/// </param>
		public List<Task> GetAll(int start, int limit, string orderbyField, string sortDirection){
			var ev = OrmLiteConfig.DialectProvider.ExpressionVisitor<Task>();
			if(sortDirection.ToUpper() == "ASC")
				ev.OrderBy(orderbyField);
			//else
			//	ev.OrderByDescending(orderbyField);
			ev.Limit(start, limit);
			using(dbc = DAL.Instance.GetDb())
				return dbc.Select<Task>( ev);
			//return DAL.GetDb().Select<People>( e =
		}
		
		public List<Task> GetAll(int start, int limit){
			return GetAll(start, limit, "Id", "ASC");
		}


		/// <summary>
		/// Gets the last task successfully ran for this backupset, 
		/// and with the specified level (if not null).
		/// Used to get the 'reference' or 'parent' task when performing a 'Refresh'-level backup.
		/// </summary>
		/// <returns>
		/// The last reference task.
		/// </returns>
		/// <param name='bsId'>
		/// BackupSet identifier.
		/// </param>
		/// <param name='level'>
		/// Level.
		/// </param>
		public Task GetLastReferenceTask(int bsId, BackupLevel level){
			Console.WriteLine("\t\tGetLastReferenceTask() : asked ref for bs#"+bsId+" with level="+level);
			using (dbc = DAL.Instance.GetDb()){
				/*var refTask =  dbc.Select<Task>( t=> t.BackupSetId == bsId
					&& t.Operation == TaskOperation.Backup
				        && t.Status == TaskStatus.Ok
				        && t.RunStatus == TaskRunningStatus.Done
					&& t.Level == level.Value);*/
				               // && ((level.HasValue)? level.Value : t.Level != null));

				var ev = OrmLiteConfig.DialectProvider.ExpressionVisitor<Task>();
				if(level != BackupLevel.Default)
					ev = ev.Where(t=> t.BackupSetId == bsId
				              && t.Operation == TaskOperation.Backup
				              && t.Status == TaskStatus.Ok
				              && t.RunStatus == TaskRunningStatus.Done
				              && t.Level == level);
				else
					ev = ev.Where(t=> t.BackupSetId == bsId
					              && t.Operation == TaskOperation.Backup
					              && t.Status == TaskStatus.Ok
					              && t.RunStatus == TaskRunningStatus.Done);

				ev = ev.OrderByDescending( t => t.Id);
				var result1 = dbc.Select(ev);

				if(result1.Count >0)
					return result1[0];
				else
					return null;

			}
		}

		internal List<P2PBackup.Common.Task> GetTaskHistory(string[] bsId, DateTime startDate, DateTime endDate, List<TaskRunningStatus> status, string sizeOperator, long size, int limit, int offset, out int totalCount){
			var ev = OrmLiteConfig.DialectProvider.ExpressionVisitor<Task>();
			ev = ev.Where(t => t.StartDate >= startDate);
			if(bsId != null)
				ev.And(t => Sql.In(t.BackupSetId, bsId));
			if(endDate > DateTime.MinValue)
				ev.And(t => t.EndDate <= endDate);
			if(status != null && status.Count >0)
				ev.And(t => Sql.In(t.RunStatus, status));
			if(sizeOperator == "<" || sizeOperator == "<=")
				ev.And(t => t.FinalSize <= size);
			else 
				ev.And(t => t.FinalSize >= size);
				
			using (dbc = DAL.Instance.GetDb()){
				int nbResults = dbc.QueryScalar<int>(ev.ToCountStatement()); 
				ev.Limit(offset, limit);
				var results = dbc.Select(ev);
				totalCount = nbResults; //results.Count;
				return results;
			}
		}

		internal Task GetTaskHistory(long taskId){
			using(dbc = DAL.Instance.GetDb()){
				Task t = dbc.Single<Task>("Id={0}", taskId);
				t.LogEntries = GetTaskLogEntries(taskId);
				return t;
			}
		}

		internal List<TaskLogEntry> GetTaskLogEntries(long taskId){
			var ev = OrmLiteConfig.DialectProvider.ExpressionVisitor<TaskLogEntry>();
			ev.Where(t=>t.TaskId == taskId);
			ev.OrderBy(t=>t.Date);
			using(dbc = DAL.Instance.GetDb()){
				return dbc.Select<TaskLogEntry>( ev);
			}
		}

		internal List<P2PBackup.Common.Task> GetExpiredBackups(){
			using(dbc = DAL.Instance.GetDb()){
				return dbc.Select<Task>( t => t.Operation == TaskOperation.Backup || t.Operation == TaskOperation.Restore
					&& t.RunStatus != TaskRunningStatus.Expired
				        && (
						/*t.StartDate == null
						||*/ t.StartDate.AddDays(100) > DateTime.Now)
				);
			}
		}

		internal List<Task> GetTasksHistory(int bsId, DateTime startDate, DateTime endDate){
			using(dbc = DAL.Instance.GetDb()){
				return dbc.Select<Task>( t => t.BackupSetId == bsId && t.StartDate >= startDate
				                        && t.EndDate == endDate);

			}
		}
		/*internal List<P2PBackup.Common.Task> GetExpiredBackups_o(){
			List<P2PBackup.Common.Task> expired = new List<P2PBackup.Common.Task>();
			string query = "SELECT * FROM \"Task\" bk, \"BackupSet\"  tsk WHERE bk.bsid=tsk.id" 
				+" AND bk.operation in ('Backup', 'Restore')"
					+" AND bk.runningstatus <> 'Expired'"
					+" AND bk.id < ("
					+"  SELECT max(b.id) FROM backups b, \"TaskSets\"  t "
					+"    WHERE b.bsid=t.id "
					+"    AND  t.id=bk.bsid "
					+"    AND b.level='Full'"
					+"    AND ("
					+"      b.datebegin IS NULL"
					+"      OR (b.datebegin+(t.retentiondays*24*60*3600)) < "+Utils.GetUnixTimeFromDateTime(DateTime.Now)
					+"    )"
					+") ORDER BY bk.id";
			//Console.WriteLine("getexpiredbackups(): query= "+query);
			//string query = "SELECT * from backups b, \"TaskSets\" t WHERE b.bsid=t.id AND (b.dateend"
			//	+"+(t.retentiondays*24*60*3600)) > "+Utils.GetUnixTimeFromDateTime(DateTime.Now);
			using (DbDataReader reader = new DBHandle().QueryAndGetReader(query)){
				
				while(reader.Read()){
					try{
						P2PBackup.Common.Task t = GetTaskFromReader(reader, DateTime.Now, DateTime.Now);
						expired.Add(t);
					}
					catch(Exception ex){
						Logger.Append("HUBRN", Severity.ERROR, "Unable to retrieve expired backups from DB: "+ex.Message+"---"+ex.StackTrace+" (query was "+query+")");
					}
				}
				
				reader.Close();
				reader.Dispose();
			}
			return expired;
		}*/
	}
}

