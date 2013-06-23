using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Configuration;
using System.Reflection;
using System.Web.Razor;
using System.Web.Services.Description;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.Admin; // request logging at /requestlogs
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;

using P2PBackup.Common;
using SharpBackupWeb.Utilities;

namespace Backo.Api.WebServices{



	public class AppHost : AppHostHttpListenerBase {

		//internal static ICacheClient SessionsCache{get;set;}

		public AppHost() : base("BackO Webservices API", typeof(Backo.Api.WebServices.AppHost).Assembly) { }

		public override void Configure(Funq.Container container) {
			//LogManager.LogFactory = new DebugLogFactory(); // TODO : remove for production

			JsConfig.DateHandler = JsonDateHandler.ISO8601;
			//JsConfig.EmitCamelCaseNames = true;
			//JsConfig<Ubiquity.Content.SupportedModes>.SerializeFn = val => val.ToString().ToCamelCase();

			container.Register<ICacheClient>(new MemoryCacheClient(){FlushOnDispose = false});
			//var sf = new SessionFeature();

			Plugins.Add(new SessionFeature());
			AuthFeature authService = new AuthFeature(() => new UserSession()/*new AuthUserSession()*/,
		      	new IAuthProvider[] {
		    		new UserAuthenticationProvider(), //HTML Form post of UserName/Password credentials
		      	}
				
			);
			authService.ServiceRoutes = new Dictionary<Type, string[]> {
			    { typeof(AuthService), new[]{"/api/auth", "/api/auth/{provider}"} }
			};
			Plugins.Add(authService	);
			// enable /requestlogs  for admins
			RequestLogsFeature rlf = new RequestLogsFeature();
			rlf.RequiredRoles = new string[]{"SuperAdmin"};
			Plugins.Add(rlf);

			SetConfig(new EndpointHostConfig {
			    DebugMode = true, //Show StackTraces in service responses during development
			    WriteErrorsToResponse = false, //Disable exception handling
			    //DefaultContentType = ContentType.Json, //Change default content type
			    AllowJsonpRequests = true, //Enable JSONP requests

			});

			this.RequestFilters.Add((httpReq, httpResp, requestDto) =>
		    {
				/*var sessionId = httpReq.GetCookieValue("user-session");
				if (sessionId == null)
				{
					httpResp.ReturnAuthRequired();
				}*/
		    });


			this.ServiceExceptionHandler += ServiceExceptionThrown;
			/*this.ServiceExceptionHandler = (request, ex) => {
				((HttpRequestContext)request).
			};*/
			// Now add specific routes that directly map to base objects:
			Routes.Add<P2PBackup.Common.Node>("/api/Node/{Id}", "GET,PUT,POST");
			Routes.Add<StorageGroup>("/api/StorageGroups/{Id}", "GET,PUT,POST,DELETE");
			Routes.Add<NodeGroup>("/api/NodeGroups/{Id}", "GET,PUT,POST,DELETE");
			Routes.Add<BackupSet>("/api/BackupSet/{Id}", "GET,PUT,POST");
			Routes.Add<P2PBackup.Common.User>("/api/Users/{Id}", "GET,PUT,POST,DELETE");
			Routes.Add<Hypervisor>("/api/Hypervisors/", "GET");
			Routes.Add<Hypervisor>("/api/Hypervisors/{Id}", "PUT,POST,DELETE");
			Routes.Add<Password>("/api/Passwords/{Id}", "PUT,POST,DELETE");
		}

		private Object ServiceExceptionThrown(Object sender, Exception ex){
			Logger.Append(Severity.ERROR, "Request error : exception type='"+sender.GetType()+"', "+ex.ToString());

			throw ex;
		}
	}

	public class WSRunner/*:IStartableComponent*/{

		private AppHost appHost;
		private bool stopRequested = false;

		public void Setup(){}

		public  void Start(){

			var listeningOn =  "http://"+ConfigurationManager.AppSettings["Web.ListenIp"]
				+":"+ConfigurationManager.AppSettings["Web.ListenPort"]+"/";
			Logger.Append(Severity.DEBUG, "Creating HTTP APPlication host at "+ listeningOn);
			appHost = new AppHost();
			appHost.Init();
			appHost.Start(listeningOn);
			appHost.ReceiveWebRequest += HandleReceiveWebRequest;
			appHost.ExceptionHandler += HandleServiceException;
			//appHost.CatchAllHandlers.Add(
			Logger.Append(Severity.INFO, "HTTP Host Created at "+ listeningOn);
			while(true && ! stopRequested)
				System.Threading.Thread.Sleep(5000);

		}

		public void Stop(){
			Logger.Append(Severity.INFO, "Stopping HTTP Host ...");
			appHost.Stop();
			stopRequested = true;
		}


		void HandleServiceException(IHttpRequest req, IHttpResponse res, string operation, Exception ex){
			Logger.Append(Severity.ERROR, operation+" : "+ ex.ToString());
			Logger.Append(Severity.DEBUG, operation+" : req was: "+req.ToString()+", result was : "+res.ToString());
		}


		void HandleReceiveWebRequest (HttpListenerContext context){
			//Logger.Append("WSAPI", Severity.DEBUG2, "Got "+ context.Request.HttpMethod+" request from "+context.Request.RemoteEndPoint+" for "+context.Request.RawUrl);
			// std common log format

			Console.WriteLine (
				context.Request.RemoteEndPoint.Address.ToString()
				+" "+"-"
				+" "+"-"
				+" ["+DateTime.Now.ToString("%d/%MM/%yyyy:%H:%m:ss %z")+"]"
				+" \""+context.Request.HttpMethod+" "+context.Request.RawUrl+" "+context.Request.ProtocolVersion.ToString()+"\""
				+" "+context.Response.StatusCode
				+" "+context.Response.ContentLength64
			);


		}


	}

	public abstract class AppServiceBase : ServiceStack.ServiceInterface.Service{
	    public UserSession UserSession{
			get{
				return SessionAs<UserSession>();
			}
		}


	}

	// cache some content (users avatar pictures...)
	public class CacheAttribute : Attribute, IHasRequestFilter {

		public int Priority{get{ return 1;}}

		public void RequestFilter(IHttpRequest req, IHttpResponse res, object requestDto){
			res.AddHeader("Cache-Control", "max-age=2419200"); // cache static content for approx. 1 month
		}

		 public IHasRequestFilter Copy() { return this; } 
	}






//}
}

