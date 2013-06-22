using System;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using P2PBackup.Common;

namespace P2PBackupHub {

	// We defined a special 'Password' type to ensure these kind of values are automatically encrypted before
	// being stored into database.
	// Handles storage, retrieval, value encryption/decryption
	// TODO : maybe use a fixed ptr to limit memory copies of the clear value (??)
	internal class PasswordManager {

		private PasswordManager() {
		}

		internal static Password Get(int id){
			Password p = new DAL.PasswordDAO().GetEncryptedPassword(id);
			if(p.Value != null && p.Value != string.Empty){
				RSACryptoServiceProvider rsaEncryptor = (RSACryptoServiceProvider)Hub.Certificate.PrivateKey;
				byte[] plainData = rsaEncryptor.Decrypt( Convert.FromBase64String(p.Value), false);
				p.Value = Encoding.UTF8.GetString(plainData);
			}
			// TODO!!! decryption happens here
			return p;
		}

		internal static Password Add(Password password){
			/*ContentInfo contentInfo = new ContentInfo(encoder.GetBytes(password));
			EnvelopedCms envelop = new EnvelopedCms(contentInfo);
			CmsRecipient recip = new CmsRecipient(Hub.Certificate);
			envelop.Encrypt(recip);
			byte[] encoded = envelop.Encode();
			password.Value = Convert.ToBase64String(envelop.Encode());*/

			// TODO!!! encryption happens here
			RSACryptoServiceProvider rsaEncryptor = (RSACryptoServiceProvider)Hub.Certificate.PublicKey.Key;
			byte[] enc = rsaEncryptor.Encrypt( Encoding.UTF8.GetBytes(password.Value), false);
			password.Value = Convert.ToBase64String(enc);
			//return (new DBHandle()).SavePassword(password);
			return new DAL.PasswordDAO().Save(password);
		}

		internal static void Update(Password password){
			RSACryptoServiceProvider rsaEncryptor = (RSACryptoServiceProvider)Hub.Certificate.PublicKey.Key;
			byte[] enc = rsaEncryptor.Encrypt( Encoding.UTF8.GetBytes(password.Value), false);
			password.Value = Convert.ToBase64String(enc);
			new DAL.PasswordDAO().Update(password);
		}

		internal static void Delete(int passwordId){
			new DAL.PasswordDAO().Delete(passwordId);
		}
	}
}

