using System;
using System.Configuration;
using System.Collections.Generic;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Mvc;
using SharpBackupWeb.Utilities;
using P2PBackup.Common;

namespace Backo.Api.WebServices{

	public class UserAuthenticationProvider : CredentialsAuthProvider{

		public override object Authenticate(IServiceBase authService, IAuthSession session, Auth request) {
			var response = base.Authenticate(authService, session, request);
			var auth = response as AuthResponse;
			Logger.Append(Severity.DEBUG, "User '"+auth.UserName+"' is trying to authenticate from "+authService.RequestContext.IpAddress);
			var curSession = (UserSession)authService.GetSession();
			//IRemoteOperations remoteOperation = RemotingManager.GetRemoteObject();
			//User authUser = remoteOperation.BeginSession(login.Text, password.Text);
			if (response is AuthResponse && curSession != null){
				return curSession;



			}
			return response;
		}

	    public override bool TryAuthenticate(IServiceBase authService, string userLogin, string password) {
	        Logger.Append(Severity.DEBUG, "User '"+userLogin+"' is trying to authenticate from "+authService.RequestContext.IpAddress);

			IRemoteOperations remoteOperation = RemotingManager.GetRemoteObject();
			//User authUser = remoteOperation.BeginSession(login.Text, password.Text);
			remoteOperation.Login(userLogin, password);
			User p = remoteOperation.GetCurrentUser();
        	if (p == null)      
				return false;

			var s = (UserSession)authService.GetSession();
			s.UserId = p.Id;
			//s.UserAuthId = p.Id.ToString();
			s.UserName = p.Login;
			s.Culture = p.Culture;
			s.CreatedAt = DateTime.UtcNow;
			//s.DisplayName = p.FirstName+" "+p.MiddleName+" "+p.LastName;
			//s.Email = p.MailOffice;
			s.Roles = new List<string>();
			s.Roles.Add("GlobalAdmin");
			s.IsAuthenticated = true;

				
			return true;
	    }

	    public override void OnAuthenticated(IServiceBase authService, IAuthSession session, IOAuthTokens tokens, Dictionary<string, string> authInfo){
			Logger.Append(Severity.INFO, "User '"+session.UserName+"' ("+session.DisplayName+") has logged from "+authService.RequestContext.IpAddress);
	        //Fill the IAuthSession with data which you want to retrieve in the app eg:
			int sessionTimeout = 3600; // default to 1h
			int.TryParse(ConfigurationManager.AppSettings["UserSession.Timeout"], out sessionTimeout);
			if(sessionTimeout == 0)
				sessionTimeout = 3600;
			/*Console.WriteLine ("Session cookie is "+
	        	authService.RequestContext.Cookies["ss-id"].Value
			                   );*/
			System.Net.Cookie userCookie = new System.Net.Cookie();
			userCookie.Expires = DateTime.Now.AddSeconds(sessionTimeout);
			userCookie.Name = "user-id";
			userCookie.Value = ((UserSession)session).UserId.ToString();
			userCookie.Path = "/";
			authService.RequestContext.Cookies.Add("user-id", userCookie);
	        authService.SaveSession( // defaults to memory session cache, which is perfect for now
				session, 
				new TimeSpan(0, 0, sessionTimeout)
			);
			session.ReferrerUrl = "http://localhost/html/";

	    }
	}


	// needs System.web.Mvc
	/*public abstract class ControllerBase : ServiceStackController<AuthUserSession> {
	    //public IDbConnectionFactory Db { get; set; }
	    //public ILog Log { get; set; }
	   // //Common extension point for all controllers. Inherits from ServiceStack to take advantage of SS powerpack + auth.
	    public override string LoginRedirectUrl {
	        get {
	            return "/html/auth.html?redirect={0}";
	        }
	    }
	}*/

	public class UserSession : AuthUserSession/*, P2PBackup.Common.User*/ {
    	public long UserId { get; set; }
	}
}