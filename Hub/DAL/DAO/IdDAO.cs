using System;
using System.Data;
using System.Data.Common;
using ServiceStack.OrmLite;
using P2PBackup.Common;
//using P2PBackup.DAL;

namespace P2PBackupHub.DAL {
	public class StateDAO {
		public StateDAO() {
		}

		public int GetLast(){
			using (IDbConnection dbc = DAL.Instance.GetDb()){
				return dbc.GetScalar<State, int>(State => Sql.Max(State.LastId));
			}
		}

		public void Save(State state){
			using (IDbConnection dbc = DAL.Instance.GetDb()){
				dbc.Insert(state);
			}
		}
	}
}

