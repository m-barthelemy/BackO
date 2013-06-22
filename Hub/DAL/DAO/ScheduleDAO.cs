using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using ServiceStack.OrmLite;
using P2PBackup.Common;

namespace P2PBackupHub.DAL {

	public class ScheduleDAO {

		IDbConnection dbc;
		User currentUser;

		public ScheduleDAO() {
		}

		internal List<ScheduleTime> GetForBS(int id){
			using (dbc = DAL.Instance.GetDb()){
				return dbc.Select<ScheduleTime>( sch=>sch.BackupSetId == id);
			}
		}

		internal void Save(List<ScheduleTime> bsSchedulePlan){
			using (dbc = DAL.Instance.GetDb()){
				dbc.InsertAll(bsSchedulePlan);
			}
		}

		internal void Delete(int bsId){
			using (dbc = DAL.Instance.GetDb()){
				dbc.Delete<ScheduleTime>( st => st.BackupSetId == bsId);
			}
		}
	}
}

