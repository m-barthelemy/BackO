using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using ServiceStack.OrmLite;
using P2PBackup.Common;

namespace P2PBackupHub.DAL {

	public class CertificateDAO {

		IDbConnection dbc;
		User currentUser;

		public CertificateDAO() {
		}

		public List<NodeCertificate> GetAll(){
			using(dbc = DAL.Instance.GetDb())
				return dbc.Select<NodeCertificate>();
		}

		/*internal NodeCertificate GetBySerial(string serial){
			using(dbc = DAL.Instance.GetDb()){
				var foundCert = dbc.Select<NodeCertificate>(nc => nc.Serial == serial);
				if(foundCert.Count == 1)
					return foundCert[0];
				else
					return null;
			}
		}*/

		internal NodeCertificate GetBySerial(byte[] serial){
			using(dbc = DAL.Instance.GetDb()){
				var foundCert = dbc.Select<NodeCertificate>(nc => nc.Serial == serial);
				if(foundCert.Count == 1)
					return foundCert[0];
				else
					return null;
			}
		}

		internal NodeCertificate Save(NodeCertificate cert){
			cert.Id = IdManager.GetId();
			using(dbc = DAL.Instance.GetDb())
				dbc.Insert(cert);
			return cert;
		}	
	}
}

