using System;
using System.Collections.Generic;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceHost;

using P2PBackup.Common;
using SharpBackupWeb.Utilities;

namespace Backo.Api.WebServices {

	[Route("/api/Users/")]
	public class GetUsers : IReturn<List<User>>{
	}

	[Route("/api/Users/Current")]
	public class GetCurrentUser : IReturn<User>{
	}

	[Authenticate]
	public class UsersWS :AppServiceBase{

		public List<User> Get(GetUsers req){
			return RemotingManager.GetRemoteObject().GetUsers();
		}

		public User Get(GetCurrentUser req){
			return RemotingManager.GetRemoteObject().GetCurrentUser();
		}

		public User Post(User u){
			return RemotingManager.GetRemoteObject().CreateUser(u);
		}

		public User Put(User u){
			return RemotingManager.GetRemoteObject().UpdateUser(u);
		}

		public void Delete(User u){
			RemotingManager.GetRemoteObject().DeleteUser(u);
		}

		public UsersWS (){

		}
	}
}

