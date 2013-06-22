using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using ServiceStack.OrmLite;
using P2PBackup.Common;

namespace P2PBackupHub.DAL {

	public class MailParametersDAO {

		IDbConnection dbc;
		private User sessionUser;

		public MailParametersDAO() {
		}

		public MailParametersDAO(User currentUser) {
			sessionUser = currentUser;
		}

		public MailParameters GetForBS(int bsid){
			using(dbc = DAL.Instance.GetDb()){
				return dbc.First<MailParameters>(mp => mp.BackupSetId == bsid);
			}
		}
	}
}

