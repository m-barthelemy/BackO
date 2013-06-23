using System;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;

namespace P2PBackup.Common{

	[DataContract]
	public class NodeCertificate{

		[DataMember]
		[DisplayFormatOption(Size=4)]
		public int Id{get; set;}


		[DataMember]
		[DisplayFormatOption(Size=4)]
		public uint NodeId{get; set;}

		[DataMember]
		[DisplayFormatOption(Size=25)]
		public string CN{get; set;}

		[DataMember]
		[DisplayFormatOption(Size=25)]
		public string Issuer{get; set;}

		[DataMember]
		[DisplayFormatOption(Size=20)]
		public DateTime ValidFrom{get; set;}

		[DataMember]
		[DisplayFormatOption(Size=20)]
		public DateTime ValidUntil{get; set;}

		[DataMember]
		//[DisplayFormatOption(Size=32)]
		public byte[] Serial{get; set;}

		[DataMember]
		[DisplayFormatOption(Size=6)]
		public bool IsActive{get;set;}

		//public int EncryptedStoreId{get;set;}

		//secured members (not exported through serialization)
		public string PublicKey{get;set;}
		//public string PrivateKey{get;set;} // todo : store it as a "Password" object, which will provide encryption inside db


		public NodeCertificate (X509Certificate2 cert){
			this.CN = cert.Subject;
			this.ValidFrom = cert.NotBefore;
			this.ValidUntil = cert.NotAfter;
			this.Issuer = cert.Issuer;
			this.Serial = cert.GetSerialNumber();
			this.PublicKey = cert.PublicKey.Key.ToXmlString(false);
		}

		/// <summary>
		/// Default constructor, to be used only for serialization / DAL
		/// </summary>
		public NodeCertificate (){

		}

		public override string ToString () {
			return string.Format ("[NodeCertificate: Id={0}, NodeId={1}, CN={2}, Issuer={3}, ValidFrom={4}, ValidUntil={5}, Serial={6}, IsActive={7}, PublicKey={8}]", Id, NodeId, CN, Issuer, ValidFrom, ValidUntil, Serial, IsActive, PublicKey);
		}

	}
}

