// Reuses some great code from the Mono Project (http://www.go-mono.com)
// credits : 
// Author:
//	Sebastien Pouliot <sebastien@ximian.com>
//
// (C) 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Configuration;
using P2PBackupHub.Utilities;
using Mono.Security.Authenticode;
using Mono.Security.X509;
using Mono.Security.X509.Extensions;
using P2PBackup.Common;
	
namespace P2PBackupHub {
	internal class CertificateManager {
		
		public CertificateManager() {
		}
		
		
		static X509Certificate LoadCertificate(string filename) {
			FileStream fs = new FileStream (filename, FileMode.Open, FileAccess.Read, FileShare.Read);
			byte[] rawcert = new byte [fs.Length];
			fs.Read (rawcert, 0, rawcert.Length);
			fs.Close ();
			return new X509Certificate (rawcert);
		}

		private static void WriteCACertificate(byte[] rawcert){
			string fileName = ConfigurationManager.AppSettings["Security.CACertificate"];
			try{
				using (FileStream fs = File.Open (fileName, FileMode.OpenOrCreate, FileAccess.Write)){
					fs.Write (rawcert, 0, rawcert.Length);
				}
			}
			catch(Exception e){
				Logger.Append("SETUP", Severity.CRITICAL, "Could not save CA certificate to '"+fileName
					+"', error : "+e.Message);
			}
		}
		
		/*private static void WriteHubMotherCert(string key){
			string fileName = ConfigurationManager.AppSettings["Security.MotherCertificateFile"];
			try{
				using (StreamWriter fs = new StreamWriter(fileName, false)){
					fs.Write (key, 0, key.Length);
				}
			}
			catch(Exception e){
				Logger.Append("SETUP", Severity.CRITICAL, "Could not save Hub Mother certificate to '"+fileName
					+"', error : "+e.Message);
			}
		}*/

		//static string MonoTestRootAgency = "<RSAKeyValue><Modulus>v/4nALBxCE+9JgEC0LnDUvKh6e96PwTpN4Rj+vWnqKT7IAp1iK/JjuqvAg6DQ2vTfv0dTlqffmHH51OyioprcT5nzxcSTsZb/9jcHScG0s3/FRIWnXeLk/fgm7mSYhjUaHNI0m1/NTTktipicjKxo71hGIg9qucCWnDum+Krh/k=</Modulus><Exponent>AQAB</Exponent><P>9jbKxMXEruW2CfZrzhxtull4O8P47+mNsEL+9gf9QsRO1jJ77C+jmzfU6zbzjf8+ViK+q62tCMdC1ZzulwdpXQ==</P><Q>x5+p198l1PkK0Ga2mRh0SIYSykENpY2aLXoyZD/iUpKYAvATm0/wvKNrE4dKJyPCA+y3hfTdgVag+SP9avvDTQ==</Q><DP>ISSjCvXsUfbOGG05eddN1gXxL2pj+jegQRfjpk7RAsnWKvNExzhqd5x+ZuNQyc6QH5wxun54inP4RTUI0P/IaQ==</DP><DQ>R815VQmR3RIbPqzDXzv5j6CSH6fYlcTiQRtkBsUnzhWmkd/y3XmamO+a8zJFjOCCx9CcjpVuGziivBqi65lVPQ==</DQ><InverseQ>iYiu0KwMWI/dyqN3RJYUzuuLj02/oTD1pYpwo2rvNCXU1Q5VscOeu2DpNg1gWqI+1RrRCsEoaTNzXB1xtKNlSw==</InverseQ><D>nIfh1LYF8fjRBgMdAH/zt9UKHWiaCnc+jXzq5tkR8HVSKTVdzitD8bl1JgAfFQD8VjSXiCJqluexy/B5SGrCXQ49c78NIQj0hD+J13Y8/E0fUbW1QYbhj6Ff7oHyhaYe1WOQfkp2t/h+llHOdt1HRf7bt7dUknYp7m8bQKGxoYE=</D></RSAKeyValue>";
		static string defaultIssuer = "CN=SHBackup Root CA";
		static string defaultSubject = "CN=SHBackup";


		// TODO : cleanup and reorganization, as the code below is a almost a direct copypaste from Mono project's Makecert tool.
		internal /* byte[]*/PKCS12 GenerateCertificate(/*string[] args,*/ bool isHubRootCA, bool isHubCert, string subjectName, string[] alternateDnsNames){
			if(isHubRootCA && isHubCert)
				throw new Exception("incompatible options isHubRootCA & isHubCert");
			Logger.Append("HUBRN", Severity.INFO, "Asked to create "+((isHubCert)?"hub certificate, ":"")+((isHubRootCA)?"root CA, ":"")+((!isHubCert&&!isHubRootCA)?" node certificate for '"+subjectName+"'":""));
			string rootKey = ConfigurationManager.AppSettings["Security.CAKey"];
			string rootCert = ConfigurationManager.AppSettings["Security.CACertificate"];
			byte[] sn = Guid.NewGuid().ToByteArray ();
			string issuer = defaultIssuer;
			DateTime notBefore = DateTime.Now;
			DateTime notAfter = DateTime.Now.AddYears(5);

			RSA issuerKey = (RSA)RSA.Create();
			//issuerKey.FromXmlString(MonoTestRootAgency);
			RSA subjectKey = (RSA)RSA.Create();

			bool selfSigned = isHubRootCA;
			string hashName = "SHA1";

			BasicConstraintsExtension bce = null;
			ExtendedKeyUsageExtension eku = null;
			SubjectAltNameExtension alt = null;
			string p12pwd = null;
			X509Certificate issuerCertificate = null;

			try{	
				if (subjectName == null)
					throw new Exception ("Missing Subject Name");
				if(!subjectName.ToLower().StartsWith("cn="))
					subjectName = "CN="+subjectName;
				
				/*if (alternateDnsNames != null){
					alt = new SubjectAltNameExtension(null, alternateDnsNames, null, null);
				}*/
				if(!isHubRootCA){
					issuerCertificate = LoadCertificate(rootCert);
					issuer = issuerCertificate.SubjectName;
					
				//case "-iv":
					// TODO password
					PrivateKey pvk = PrivateKey.CreateFromFile(rootKey);
					issuerKey = pvk.RSA;
				}
				
				// Issuer CspParameters options				
				if(isHubRootCA){
					//subjectName = defaultSubject;
					string pvkFile = rootKey;
					if (File.Exists (pvkFile)) {// CA key already exists, reuse
						PrivateKey key = PrivateKey.CreateFromFile(pvkFile);
						subjectKey = key.RSA;
					}
					else {
						PrivateKey key = new PrivateKey ();
						key.RSA = subjectKey;
						key.Save(pvkFile);
						// save 'the Mother Of All Keys'
						//WriteHubMotherCert(issuerKey.ToXmlString(true));
					}
				}
				else{
					p12pwd = "";
				}

				// serial number MUST be positive
				if ((sn [0] & 0x80) == 0x80)
					sn [0] -= 0x80;

				if (selfSigned){
					if (subjectName != defaultSubject){
						issuer = subjectName;
						issuerKey = subjectKey;
						//issuerKey = Hub.MotherKey;
					}
					else {
						subjectName = issuer;
						subjectKey = issuerKey;
					}
				}

				X509CertificateBuilder cb = new X509CertificateBuilder(3);
				cb.SerialNumber = sn;
				cb.IssuerName = issuer;
				cb.NotBefore = notBefore;
				cb.NotAfter = notAfter;
				cb.SubjectName = subjectName;
				cb.SubjectPublicKey = subjectKey;
				
				// extensions
				if (bce != null)
					cb.Extensions.Add (bce);
				if (eku != null)
					cb.Extensions.Add (eku);
				if (alt != null)
					cb.Extensions.Add(alt);
				// signature
				cb.Hash = hashName;
				byte[] rawcert = cb.Sign(issuerKey);

				if (isHubRootCA) // Hub CA
					WriteCACertificate(rawcert);
				else{
					PKCS12 p12 = new PKCS12();
					p12.Password = p12pwd;

					ArrayList list = new ArrayList();
					// we use a fixed array to avoid endianess issues 
					// (in case some tools requires the ID to be 1).
					list.Add (new byte [4] { 1, 0, 0, 0 });
					Hashtable attributes = new Hashtable(1);
					attributes.Add (PKCS9.localKeyId, list);

					p12.AddCertificate (new X509Certificate(rawcert), attributes);
					if (issuerCertificate != null)
						p12.AddCertificate(issuerCertificate);
					p12.AddPkcs8ShroudedKeyBag(subjectKey, attributes);
					/*var x509cert2 = new System.Security.Cryptography.X509Certificates.X509Certificate2();
					x509cert2.Import(p12.GetBytes(), "", 
						System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.PersistKeySet| System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable );
					return  x509cert2;*/
					//return p12.GetBytes();
					return p12;
				}
				Logger.Append("HUBRN", Severity.INFO, "Created requested key/cert for '"+subjectName+"'.");
			}
			catch (Exception e) {
				Logger.Append("HUBRN", Severity.ERROR, "Error generating certificate for '"+subjectName+"' : " + e.ToString ());
				
			}
			return null;
		}
	}
}

