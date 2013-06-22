using System;
using System.Data;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.PostgreSQL;
using Npgsql;

namespace P2PBackupHub {
	public class Initialize {


		public Initialize() {
		}

		private void InitializeDatabase(){
			OrmLiteConfig.DialectProvider = PostgreSqlDialect.Provider;
			OrmLiteConnectionFactory dbFactory = 
				new OrmLiteConnectionFactory("Server=127.0.0.1;Port=5432;User Id=ubiquity;Password=ubiquity;Database=ubiquity;pooling=true;MinPoolSize=5");
			
			using(IDbConnection db = dbFactory.OpenDbConnection()){
				db.CreateTables(false, 
				                typeof(Address), 
				                typeof(Phone), 
				                typeof(Group),
				                typeof(People), 
				                typeof(DataSource), 
				                typeof(Ubiquity.Security.Password),
				                typeof(MailMessage),
				                typeof(DataSourceAuthentication),
				                typeof(Tag),
				                typeof(Todo),
				                typeof(Project),
				                typeof(State),
				                typeof(Event)
				                );
			}
		}
	}
}

