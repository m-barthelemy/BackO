using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using ServiceStack.OrmLite;
using P2PBackup.Common;

namespace P2PBackupHub.DAL {

	public class UserDAO {

		IDbConnection dbc;
		private User sessionUser;

		public UserDAO() {
		}


		public UserDAO(User currentUser) {
			sessionUser = currentUser;
		}

		public  User AuthenticateUser(string login, string password){
			if(login == null || password == null || login == String.Empty || password == String.Empty)
				return null; 
			using(dbc = DAL.Instance.GetDb()){
				User auth = dbc.First<User>( u => u.Login == login);
				Console.WriteLine("user login3="+auth.Login+", clear pass="+PasswordManager.Get(auth.PasswordId).Value);
				if(auth != null){
					if(PasswordManager.Get(auth.PasswordId).Value == password){
						auth.LastLoginDate = DateTime.Now;
						dbc.Update<User>(auth);
						return auth;
					}
				}

			}
			return null;

		}

		public List<User>GetAll(){
			using(dbc = DAL.Instance.GetDb()){
				return dbc.Select<User>();
			}

		}

		public User Save(User u){
			u.Id = IdManager.GetId();
			using(dbc = DAL.Instance.GetDb()){
				dbc.Insert<User>(u);
			}
			return u;
		}

		public User Update(User u){
			using(dbc = DAL.Instance.GetDb()){
				dbc.Update<User>(u);
			}
			return u;
		}

		public User Delete(User u){
			using(dbc = DAL.Instance.GetDb()){
				dbc.Delete<User>(u);
				PasswordManager.Delete(u.PasswordId);
			}
			return u;
		}
	}
}
