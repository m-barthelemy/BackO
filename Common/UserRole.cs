using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;

namespace P2PBackup.Common{
	
	[DataContract]
	[Serializable]
	public class UserRole{

		[DataMember]
		public RoleEnum Role{get;set;}

		[DataMember]
		public List<int> GroupsInRole{get;set;}
		//public int UserId {get;set;}

		public UserRole(){
			//Role = RoleEnum.None;
			this.GroupsInRole = new List<int>();
		}

		public override string ToString (){
			return string.Format ("[Role={0}, GroupsInRole={1}]", Role, ( (GroupsInRole==null)?"<none>":string.Join(",", GroupsInRole)));
		}
	}
	
}
