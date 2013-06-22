using System;
using System.Runtime.Serialization;
//using System.Xml.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;

namespace P2PBackup.Common {

	[Serializable]
	[KnownType(typeof(Password))]
	[DataContract(Name = "Hypervisor", Namespace = " http://schemas.datacontract.org/")]
	public class Hypervisor {

		[DataMember]
		[DisplayFormatOption(Size=3)]
		public int Id{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=15)]
		public string Name{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=10)]
		public string Kind{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=30)]
		public string Url{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=15)]
		public string UserName{get;set;}

		[DataMember]
		[DisplayFormatOption(Display=false)]
		public int PasswordId{get;set;}
		/*	get{return passwordId;}
			set{passwordId = value;}
		}*/

		[Ignore]// don't persist password as an embedded object
		[IgnoreDataMember] // don't transmit password real value
		[DisplayFormatOption(Size=12)]
		public Password Password{get;set;}
			/*get{return password; }
			set{
				password = value;
				if(password != null)
					passwordId = password.Id;
			}
		}*/

		[DataMember]
		[DisplayFormatOption(Size=20)]
		public DateTime LastDiscover{get;set;}

		public Hypervisor(){

		}

		public override string ToString () {
			return string.Format ("[Hypervisor: Id={0}, Name={1}, Kind={2}, Url={3}, UserName={4}, Password={5}, LastDiscover={6}]", Id, Name, Kind, Url, UserName, Password.Id, LastDiscover);
		}
	}
}

