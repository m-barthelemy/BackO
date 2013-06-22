using System;
using System.Data;
using System.Data.Common;
using System.Configuration;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.PostgreSQL;
using ServiceStack.OrmLite.MySql;
using ServiceStack.OrmLite.SqlServer;
using P2PBackup.Common;


namespace P2PBackupHub.DAL {

	internal class DAL {
		private static readonly DAL _instance = new DAL();

		internal static DAL Instance{get{return _instance;}}

		private OrmLiteConnectionFactory dbFactory;

		private DAL() {
			switch (ConfigurationManager.AppSettings["Storage.DBHandle.Provider"].ToLower()){
			case "npgsql": case "postgres":
				OrmLiteConfig.DialectProvider = PostgreSqlDialect.Provider;
				break;
			case "mysql":case "mysql.data":
				OrmLiteConfig.DialectProvider = MySqlDialect.Provider;
				break;
			case "sqlserver":
				OrmLiteConfig.DialectProvider = SqlServerDialect.Provider;
				break;
			default:
				throw new ArgumentException("could not select database driver/provider from configuration value '"+ConfigurationManager.AppSettings["Storage.DBHandle.Provider"]+"'");
			}
			OrmLiteConfig.DialectProvider.DefaultStringLength = 2048;
			dbFactory = 
				new OrmLiteConnectionFactory(ConfigurationManager.AppSettings["Storage.DBHandle."+ConfigurationManager.AppSettings["Storage.DBHandle.Provider"]]);
		}

		internal void CreateTables(){

			using(IDbConnection db = GetDb()){
				db.Open();
				db.CreateTables(false, 
			        	typeof(BackupSet), 
		                typeof(P2PBackup.Common.Node),
		                typeof(Hypervisor),
		                typeof(StorageGroup),
				        typeof(NodeGroup),
		                typeof(ScheduleTime),
		                typeof(Task),
		                typeof(P2PBackup.Common.User),
				        typeof(Password),
		                typeof(P2PBackupHub.State),
		                typeof(NodeCertificate),
				        typeof(TaskLogEntry),
				        typeof(MailParameters),
				        typeof(Plugin)
			     );
			}
		}

		internal IDbConnection GetDb(){
			return dbFactory.OpenDbConnection();
		}

	}
}

