using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using ServiceStack.OrmLite;
using P2PBackup.Common;

namespace P2PBackupHub.DAL {

	public class PasswordDAO {

		IDbConnection dbc;
		private User sessionUser;

		public PasswordDAO() {
		}

		public Password GetEncryptedPassword(int id){
			using(dbc = DAL.Instance.GetDb()){
				Console.WriteLine("passDAO pass="+dbc.GetById<Password>(id).Value);
				return dbc.GetById<Password>(id);
			}
		}

		public Password Save(Password p){
			using(dbc = DAL.Instance.GetDb()){
				p.Id = IdManager.GetId();
				dbc.Insert<Password>(p);
			}
			return p;
		}

		public Password Update(Password p){
			using(dbc = DAL.Instance.GetDb()){
				dbc.Update<Password>(p);
			}
			return p;
		}

		public void Delete(int passwordId){
			using(dbc = DAL.Instance.GetDb()){
				dbc.DeleteById<Password>(passwordId);
			}
		}
	}
}

