using System;
using System.Text;
using System.Security.Cryptography;
using Node.Utilities;
using P2PBackup.Common;

namespace Node{

	/// <summary>
	/// 3-peers authentication using crypto hashes.
	/// Does authentication between 2 nodes (client-storage for example), 
	///   by hashing and verifying a common key transmitted by the 3rd peer (the Hub)
	/// </summary>
	internal class SessionVerificator{

		private RSACryptoServiceProvider myKeyPairCrypto;
		private string clientKey;
		private bool clientVerification = false; 
 
		internal bool LocalVerification{get;set;}

		internal SessionVerificator(RSACryptoServiceProvider mKP, string cK){
			myKeyPairCrypto= mKP;
			clientKey = cK;
		}

		/// <summary>
		/// Creates digital signature with users private key. 
		/// Used for client-storage node authentication AND as a key for symmetric data chunks encryption, if required
		/// Data to be signed consist of an 'hard-to-guess' value, we chose to generate a GUID.
		/// </summary>
		/// <param name="original">the original message</param>
		/// <returns>the digital signature in Base64String</returns>
		/*internal string CreateDigitalSignature(byte[] original){

			SHA1Managed hashAlg = new SHA1Managed();
			byte[] hashedData = hashAlg.ComputeHash(original);
			byte[] signature = myKeyPairCrypto.SignHash(hashedData, null);//sign with users privatekey
			//byte[] signature = myKeyPairCrypto.Encrypt(hashedData, false);//sign with users privatekey
			return Convert.ToBase64String(signature);
		}*/

		internal string CreateDigitalSignature(string clearSecret){
			byte[] encryptedSecret = myKeyPairCrypto.Encrypt(Convert.FromBase64String(clearSecret));//sign with other peer's public key
			return Convert.ToBase64String(encryptedSecret);
		}

		/// <summary>
		/// Checks the digital signature for client1 and client2
		/// </summary>
		/// <param name="command">DS1 or DS2</param>
		/// <param name="original">original message</param>
		/// <param name="digSig">digital signature as string</param>
		internal bool CheckDigitalSignature(string original, string peerNodeSignature){
			//Console.WriteLine("DEBUG : Verification.CheckDigitalSignature : called with args "+original+"  "+digSig);
			Logger.Append (Severity.DEBUG, "Checking digital signature of original="+original+" against peer pubkey received from hub...");

			SHA1Managed hashAlg = new SHA1Managed();
			byte[] hashedData = hashAlg.ComputeHash(Convert.FromBase64String(original));
			byte [] signature = Convert.FromBase64String(peerNodeSignature);
			
			RSACryptoServiceProvider RSACrypto = new RSACryptoServiceProvider();
			RSACrypto.FromXmlString(clientKey);
			clientVerification = RSACrypto.VerifyHash(hashedData, null, signature);//verifies the digital signature with client2 public key

			if(clientVerification == false) {
				Logger.Append(Severity.ERROR, "verification of node public key failed.");
			}

			return clientVerification;
		}
		

	}
}
