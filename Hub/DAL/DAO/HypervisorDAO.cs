using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using ServiceStack.OrmLite;
using P2PBackup.Common;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;

namespace P2PBackupHub.DAL {

	public class HypervisorDAO {

		IDbConnection dbc;
		private User sessionUser;


		public HypervisorDAO() {
		}

		public HypervisorDAO(User currentUser) {
			sessionUser = currentUser;
		}

		public Hypervisor GetById(int id){
			using (dbc = DAL.Instance.GetDb()){
				Hypervisor hv =  dbc.Select<Hypervisor>(h => h.Id == id)[0];
				hv.Password = PasswordManager.Get(hv.PasswordId);
				return hv;
			}
		}

		public List<Hypervisor> GetAll(){
			using (dbc = DAL.Instance.GetDb()){
				return dbc.Select<Hypervisor>();
			}
		}

		public Hypervisor Save(Hypervisor hv){
			hv.Id = IdManager.GetId();
			using (dbc = DAL.Instance.GetDb()){
				dbc.Insert<Hypervisor>(hv);
			}
			return hv;
		}

		public Hypervisor Update(Hypervisor hv){
			using (dbc = DAL.Instance.GetDb()){
				dbc.Update<Hypervisor>(hv);
			}
			return hv;
		}

		public void Delete(Hypervisor hv){
			using (dbc = DAL.Instance.GetDb()){
				dbc.Delete<Hypervisor>(hv);
			}
		}

	}
}

