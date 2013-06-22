using System;
using System.Data;
using System.Data.Common;
using System.Configuration;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.PostgreSQL;
//using Npgsql;
using P2PBackup.Common;

namespace P2PBackupHub {
	public class DBInit {

		DbProviderFactory dbpf;
		DbConnectionStringBuilder dbcs;

		public DBInit() {
			/*try{
				dbpf = DbProviderFactories.GetFactory(ConfigurationManager.AppSettings["Storage.DBHandle.Provider"]);
				dbcs = dbpf.CreateConnectionStringBuilder();
				dbcs.ConnectionString = ConfigurationManager.AppSettings["Storage.DBHandle."+ConfigurationManager.AppSettings["Storage.DBHandle.Provider"]];
			}
			catch(Exception e){
				Console.WriteLine("FATAL : Unable to instanciate Storage DB provider '"+ConfigurationManager.AppSettings["Storage.DBHandle.Provider"]
				                  +"'. Check the parameter 'Storage.DBHandle' inside the configuration file, and make sure the provider is installed."+Environment.NewLine);
				
				// Let's give the user some useful hints
				DataTable providersTable =  DbProviderFactories.GetFactoryClasses();
				string providers = String.Empty;
				Console.WriteLine("Currently registered providers are : "+providers+Environment.NewLine);
				foreach(DataRow dr in providersTable.Rows){
					Console.WriteLine("* "+dr[2]+"\t("+dr[3]+")");
					providers += dr[2]+", ";
				}
				Logger.Append("HUBRN", Severity.CRITICAL, "Unable to instanciate Storage DB provider '"
				              +ConfigurationManager.AppSettings["Storage.DBHandle.Provider"]+"'. Available providers are "+providers+".");
				throw(e);
			}*/
		}

		internal void CreateTables(){
			switch (ConfigurationManager.AppSettings["Storage.DBHandle.Provider"].ToLower()){
			case "npgsql": case "postgres":
				OrmLiteConfig.DialectProvider = PostgreSqlDialect.Provider;
				break;
			default:
				throw new ArgumentException("could not select database driver/provider from configuration value '"+ConfigurationManager.AppSettings["Storage.DBHandle.Provider"]+"'");
			}
			//OrmLiteConfig.DialectProvider = new OrmLite//PostgreSqlDialect.Provider;
			//OrmLiteConnectionFactory dbFactory = 
			//	new OrmLiteConnectionFactory("Server=127.0.0.1;Port=5432;User Id=ubiquity;Password=ubiquity;Database=ubiquity;pooling=true;MinPoolSize=5");
			DBHandle dbh = new DBHandle();
			using(IDbConnection db = dbh.GetConnection()){
				db.CreateTables(false, 
				                typeof(BackupSet), 
				                typeof(P2PBackup.Common.Node),
				                typeof(Hypervisor),
				                typeof(StorageGroup),
				                typeof(ScheduleTime),
				                typeof(Task),
				                typeof(P2PBackup.Common.User),
				                typeof(NodeGroup),
				                typeof(UserRole)
				                );
			}


		}


	}
}

