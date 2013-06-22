using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Node.Utilities;
using P2PBackup.Common;

namespace Node.DataProcessing{
	
	public class EncryptorStream:IDataProcessorStream{
		
		private IDataProcessorStream innerStream;
		private long length;
		private byte[] encryptionMetadata;
		private long position;
		RSACryptoServiceProvider key;
		X509Certificate2 cert;
		ICryptoTransform transform;
		CryptoStream outStream;
		
		public override List<IFileBlockMetadata> BlockMetadata{get;set;}
		
		public byte[] EncryptionMetadata{get;set;}
		
		// if encrypt is false, we prepare the stream to decrypt
		//public EncryptorStream(IDataProcessorStream myInnerStream, bool encrypt, RSACryptoServiceProvider clientKey){
		public EncryptorStream(IDataProcessorStream myInnerStream, bool encrypt, byte[] sessionKey, byte[] iv){
			this.BlockMetadata = new List<IFileBlockMetadata>();
			try{
				this.innerStream = myInnerStream;
				length = 0;
				//GetKeyPair(out keyPair, encrypt);
				//key = clientKey;
				
				/*aes.KeySize = 256;
	            aes.BlockSize = 128;
	            aes.Mode = CipherMode.CBC;
	            transform = aes.CreateEncryptor();*/

				if(encrypt){
					AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
					if(sessionKey != null)
						Console.WriteLine ("EncryptorStream: session key length="+sessionKey.Length+", iv length="+iv.Length+", block size="+aes.BlockSize);
					else
						Console.WriteLine ("EncryptorStream: session key IS NULL!");
					aes.Key = sessionKey;
					aes.IV = iv;
					this.EncryptionMetadata = sessionKey;
					transform = aes.CreateEncryptor();
					/*RSAPKCS1KeyExchangeFormatter keyFormatter = new RSAPKCS1KeyExchangeFormatter(key);
		            byte[] keyEncrypted = keyFormatter.CreateKeyExchange(aes.Key, aes.GetType());
		
		            // Create byte arrays to contain
		            // the length values of the key and IV.
		            byte[] LenK = new byte[4];
		            byte[] LenIV = new byte[4];*/
				/*if(encrypt){
		            int lKey = keyEncrypted.Length;
		            LenK = BitConverter.GetBytes(lKey);
		            int lIV = aes.IV.Length;
		            LenIV = BitConverter.GetBytes(lIV);
					EncryptionMetadata = new byte[4+4+lKey+lIV];
					Array.Copy(LenK, EncryptionMetadata, 4);
					Array.Copy(LenIV, 0, EncryptionMetadata, 4, 4);
					Array.Copy(keyEncrypted, 0, EncryptionMetadata, 8, lKey);
					Array.Copy(aes.IV, 0, EncryptionMetadata, 8+lKey, lIV	);*/
					outStream = new CryptoStream(innerStream, transform, CryptoStreamMode.Write);
				}
				else{
					// http://msdn.microsoft.com/fr-fr/library/system.security.cryptography.x509certificates.x509certificate2.aspx
				}
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "Could not initialize encryption: "+e.Message+" --- "+e.StackTrace);	
			}
		}
		
		
		public override bool CanRead{
			get{ return true;}
		}
		
		public override bool CanWrite{
			get{ 
				return true;
			}
		}
		
		public override bool CanSeek{
			get{return true;}
		}
		
		public override long Position{
			get{ return position;}
			set{ 
				position = value;
				Seek(position, SeekOrigin.Begin);
			}
		}
		
		public override long Length{
			get{ return length;}	
		}
		
		public override void SetLength(long value){
			length = value;
			innerStream.SetLength(length);
		}
		
		public override void Flush(){
			//Console.WriteLine("encryptor : before flush");
			outStream.Flush();
			//Console.WriteLine("encryptor : after flush");
		}
		
		public override void FlushMetadata(){
			innerStream.BlockMetadata.AddRange(this.BlockMetadata);
			this.BlockMetadata.Clear();
			innerStream.FlushMetadata();
		}
		
		public override int Read(byte[] destBuffer, int offset, int count){
			return 0;
			// http://msdn.microsoft.com/fr-fr/library/system.security.cryptography.x509certificates.x509certificate2.aspx
		}
		
		
		
		public override void Write(byte[] fromBuffer, int offset, int count){
			
			outStream.Write(fromBuffer, offset, count);
			length += count-offset;
			
		}
		
		public override long Seek(long offset, SeekOrigin origin){
			return offset;
		}
		
		public override void Close(){
			outStream.Close();
		}
		
		/*private void GetKeyPair(out RSACryptoServiceProvider keyPair, bool encrypt){
			
			//Console.WriteLine("certfile="+ConfigManager.GetValue("Security.CertificateFile"));
			cert = new X509Certificate2(ConfigManager.GetValue("Security.CertificateFile"));
			if(encrypt)
				keyPair = (RSACryptoServiceProvider)cert.PublicKey.Key;
			else // decrypt
				keyPair = (RSACryptoServiceProvider)cert.PrivateKey;
			//keyPair = new RSACryptoServiceProvider();
			//keyPair.ImportCspBlob(cert.PrivateKey.);
			//keyPair.FromXmlString(cert.PrivateKey.ToXmlString(true));
		}*/
	}
}

