using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;

namespace P2PBackup.Common {
	
	[DataContract]public enum RoleEnum{
		[EnumMember]SuperAdmin=10, 
		[EnumMember]Admin=4, 
		[EnumMember]SuperViewer=3,  
		[EnumMember]Viewer=2, 
		[EnumMember]ClientUser=1, 
		[EnumMember]None=0
	}
	
	//Represents users that can connect to Hub using web UI or command-line
	[Serializable]
	[DataContract]
	[KnownType(typeof(RoleEnum))]
	[KnownType(typeof(UserRole))]
	public class User {

		[DataMember(Order = 0)]
		[DisplayFormatOption(Size=6)]
		public int Id{get;set;}
			
		[DataMember(Order = 1)]
		[DisplayFormatOption(Size=20)]
		public string Name{get;set;}
			
		[DataMember(Order = 2)]
		[DisplayFormatOption(Size=20)]
		[Index(true)]
		public string Login{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=30)]
		public string Email{get;set;}

		//[DataMember]
		//[DisplayFormatOption(Size=12)]
		[Ignore]
		[DisplayFormatOption(Display=false)]
		public Password Password{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=8)]
		public int PasswordId{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=5)]
		public bool IsEnabled{get;set;}
			
		[DataMember]
		[DisplayFormatOption(Size=20,FormatAs=DisplayFormat.Time)]
		public DateTime LastLoginDate{get;set;}

		/*[DataMember]
		[DisplayFormatOption(Size=6)]
		public CultureInfo Culture{get; set;}*/

		[DataMember]
		[DisplayFormatOption(Size=6)]
		public string Culture{get; set;}
			
		[DataMember]
		[DisplayFormatOption(Size=45)]
		public List<UserRole> Roles{get; set;}
		
		/*public List<NodeGroup> accessibleGroups{get;set;}*/

		public void SetCulture(string cultureName){
			//Culture = (new CultureInfo(cultureName));
			this.Culture = cultureName;
		}

		public User(){
			this.Roles = new List<UserRole>();
			this.Culture = "en-US";
		}

		/*public User(int id){
			this.Id = id;
			this.Roles = new List<UserRole>();
		}
		*/
		public RoleEnum GetRoleForGroup(int nodeGroupId){
			RoleEnum maxRoleForGroup = RoleEnum.None;
			foreach(UserRole ur in this.Roles){
				// don't dive further if user is superadming	
				if(ur.Role == RoleEnum.SuperAdmin) return ur.Role;
				foreach(int nodeGroup in ur.GroupsInRole){
					if(nodeGroup == nodeGroupId){
						if(ur.Role > maxRoleForGroup)
							maxRoleForGroup = ur.Role;
					}
				}
			}	
			return maxRoleForGroup;
		}
		
		public List<int> GetGroupsForRole(RoleEnum wantedRole){
			foreach(UserRole ur in this.Roles){
				if(ur.Role >= wantedRole)
					return ur.GroupsInRole;
			}
			return null;
		}
		
		public bool IsSuperAdmin(){
			foreach(UserRole ur in this.Roles){
				if(ur.Role == RoleEnum.SuperAdmin)
					return true;
			}
			return false;
		}
		
		public bool IsSuperAdminOrViewer(){
			foreach(UserRole ur in this.Roles){
				if(ur.Role == RoleEnum.SuperAdmin || ur.Role == RoleEnum.SuperViewer)
					return true;
			}
			return false;
		}
	}
}

