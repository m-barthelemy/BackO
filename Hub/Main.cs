using System;
using System.Collections.Generic;
using System.Configuration;
using System.Security;
using P2PBackup.Common;
using P2PBackupHub.Utilities;

namespace P2PBackupHub {

	public class MainClass {

		private static bool SetupMode; //{get; private set;}
		private static bool UpgradeMode;
		[STAThread]
		static void Main(string[] args){
			
			// usually false, unless first start
			bool generateCA = false; // true to generate a root CA
			bool generateHubCert = false; // true to generate the hub's certificate
			string setupPassword = "";
			int param = 0;
			while(param < args.Length){
				string currentParam = args[param];
				switch (currentParam.ToLower()){
				case "-h": case "--help":
					PrintHelp();
					return;
				case "--config":case "-c":
					param++;
					ConfigurationManager.OpenExeConfiguration(args[param]);
					
					break;
				case "--setup": 
					Console.WriteLine("Running Setup mode");
					SetupMode = true;
					break;
				case "--upgrade": 
					Console.WriteLine("Running Upgrade mode");
					UpgradeMode = true;
					break;
				case "--gencert": 
					//CertificateManager cm = new CertificateManager();
					generateHubCert = true;
					generateCA = true;
					break;
				case "--password":
					if(args.Length <= param+1)
						throw new Exception ("syntax : --password <the_password>");
					param++;
					setupPassword = args[param];
					break;
					//case "--fake-data": 
					//Console.WriteLine("Inserting fake data (for advanced debug & test purposes)");
					
					//break;
				}
				param = param +1;
			}	
			
			if(!Utils.ConfigFileExists() && ! SetupMode){
				Console.WriteLine("Configuration file does not exist, exiting...");
				PrintHelp();
				Environment.Exit(4);
			}
			if(generateCA ){
				P2PBackupHub.CertificateManager cm = new P2PBackupHub.CertificateManager();
				cm.GenerateCertificate(true, false, Environment.MachineName, null);
			} 
			if(generateHubCert){
				P2PBackupHub.CertificateManager cm = new P2PBackupHub.CertificateManager();
				byte[] hubCert = cm.GenerateCertificate(false, true, Environment.MachineName, null).GetBytes();
				//System.Security.Cryptography.X509Certificates.X509Certificate2 hc = cm.GenerateCertificate(false, true, Environment.MachineName, null);

				if(ConfigurationManager.AppSettings["Security.CertificateFile"] == null){
					Console.WriteLine("Cannot save certificate: Parameter Security.CertificateFile is missing. Consider updating configuration file.");
					Environment.Exit(5);
				}
				try{
					System.IO.FileStream certStream = new System.IO.FileStream(ConfigurationManager.AppSettings["Security.CertificateFile"], System.IO.FileMode.CreateNew);
					certStream.Write(hubCert, 0, hubCert.Length);
					//certStream.Write(hc.RawData, 0, hc.RawData.Length);

				}
				catch(Exception e){
					Console.WriteLine("Cannot save certificate to '"+ConfigurationManager.AppSettings["Security.CertificateFile"]+"': "+e.Message);
					Environment.Exit(6);
				}
			}
			
			if(SetupMode){
				if(setupPassword == string.Empty)
					throw new Exception("--password must be followed by a value");
				
				// initialize db
				//DAL init = new DAL();
				Console.WriteLine("Creating db tables...");
				P2PBackupHub.DAL.DAL.Instance.CreateTables();
				Console.WriteLine("Inserting initial default data...");
				InitializeData(setupPassword);
				Console.WriteLine("...done");
				
			}

			if(UpgradeMode)
				P2PBackupHub.DAL.DAL.Instance.CreateTables();
			// catch fatal exceptions to log them before exiting
			AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
			
			// ** Start the real stuff **
			try{ 
				P2PBackupHub.IdManager.Instance.Start();
				//SecurityManager.SecurityEnabled = true; // obsolete.
				Hub hub = new Hub();
				hub.Run();
				Logger.Append("START", Severity.INFO, "Security is "+(SecurityManager.SecurityEnabled ? "ON" : "OFF"));
			}
			catch(Exception e){
				Utilities.Logger.Append("HUBRN", Severity.ERROR, e.Message+"---"+e.StackTrace);
			}
			/*Password totototo = new Password();
			totototo.Value = "Ausvad10+";
			PasswordManager.Add(totototo);*/
		}
		
		private static void PrintHelp(){
			Console.WriteLine("Usage : ");
			Console.WriteLine(AppDomain.CurrentDomain.FriendlyName+" is normally started as a service, without any argument/parameter.");
			Console.WriteLine("However, it can accept the following parameters :");
			Console.WriteLine("\t--config, <config_file>, -c <config_file>: \t Specifies a custom config file (default is "+AppDomain.CurrentDomain.FriendlyName+".config)");
			Console.WriteLine("\t--setup: \t starts in setup mode, allowing to do first-run configuration through the web GUI");
			Console.WriteLine("\t--password <the_password>: \t used with --setup, sets the password used for authentication in th GUI");
			Console.WriteLine("\t--gencert: \t when used with --setup, generate root CA cert and server certificates");
			Console.WriteLine("* If you run the server for the first time or haven't configured it yet, run :");
			Console.WriteLine("\t"+AppDomain.CurrentDomain.FriendlyName+" --gencert --setup --password my_pass");
			Console.WriteLine("Then run the web Gui, that will by default connect to the server using port 9999, and follow the instructions ");
			
		}

		private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e){
			try{
				Logger.Append("HUBRN", Severity.CRITICAL, "Unhandlable error : "+e.ExceptionObject);	
			}catch{}
			Console.WriteLine("Unhandlable error : "+((Exception)e.ExceptionObject).ToString());
		}

		private static void InitializeData(string setupPassword){
			// node group with ID=0 mandatory, so it's the very first object to create
			NodeGroup defaultNG = new NodeGroup();
			defaultNG.Description = "Default group";
			defaultNG.Name = "Default";
			new DAL.NodeGroupDAO().Save(defaultNG);
			
			Password adminP = new Password();
			adminP.Value = setupPassword;
			adminP = PasswordManager.Add(adminP);
			
			User admin = new User();
			admin.Login = "admin";
			admin.Name = "Superadmin user";
			admin.Email = "admin@mybigcompany.tld";
			//admin.Roles.Add(new UserRole{Role = RoleEnum.SuperAdmin});
			admin.IsEnabled = true;
			var adminRole = new UserRole();
			adminRole.Role = RoleEnum.SuperAdmin;
			adminRole.GroupsInRole = new List<int>{0,1,2,3,4,5,6,7,8,9,10};
			admin.PasswordId = adminP.Id;
			admin.Roles = new List<UserRole>();
			admin.Roles.Add(adminRole);
			
			admin = new DAL.UserDAO().Save(admin);
			
			BackupSet dummy = new BackupSet();
			dummy.IsTemplate = true;
			dummy.Name = "Default BackupSet";
			dummy.Operation = TaskOperation.Backup;
			dummy.Parallelism = new Parallelism(){Kind = ParallelismLevel.Disk, Value = 2};
			dummy.RetentionDays = 7;
			BasePath dp = new BasePath();
			dp.Path = "*";
			dp.Recursive = true;
			dp.Type = "FS:local";
			dummy.BasePaths.Add(dp);
			dummy = new DAL.BackupSetDAO().Save(dummy);
			
			
			
		}
	}
}

