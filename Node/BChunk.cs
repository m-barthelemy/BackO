using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Node.DataProcessing.Utils;
using Node.DataProcessing;
using Node.Utilities;

namespace Node{
	
	// A Bchunk is an unit ready to get stored by a node.
	// it has a maximum fixed size, and can consist of:
	// - several files, smaller than Bchunk naxsize, packed together
	// -or-
	// - one piece of file, if file is bigger than Bchunk maxsize.
	// It represent the minimal stored unit on nodes.
	// this way we achieve good performances, avoiding to transmit, calculate, index on hub, and keep track 
	// of, many small files. This choice was made because the majority o files on an average system are very small (<1M)
	// and because, on storage nodes, it saves disk blocks and inodes (if storage is on *nix).
	// It is always created on the client node. It can be compressed and/or encrypted (both locally before sending))
	// A Bchunk index file is a binary file which contains :
	// -A version field
	// -A clientName file
	[Serializable]
	internal class BChunk{

		private List<IFSEntry> entries;
		private List<uint> storageDestinations;
			
		[NonSerialized] private ManualResetEventSlim sentEvent;
		
		internal ManualResetEventSlim SentEvent{
			get{return sentEvent;}
			set{sentEvent = value;}
		}

		internal string Name{get; private set;}
		internal long OriginalSize{get; set;}
		internal long Size {get; set;}
		internal long TaskId{get;private set;}
		internal int Order{get;set;}
		internal Int16 RootDriveId{get; set;}
		internal bool Sent{get; set;}

		internal string Sum{get; set;}
		internal bool Fetched{get; set;}

		internal List<uint> StorageDestinations{
			get{ return storageDestinations;}
		}
	
		
		//add nodeid to the list and return destinations count (to check if redundancy level is satisfied)
		internal void AddDestination(uint nodeId){
				storageDestinations.Add(nodeId);
			//return (storageDestinations.Count == this.Redundancy);
		}

		
		internal List<IFSEntry> Items{
			get{ return entries;}
		}

		internal BChunk(long taskId){
			this.TaskId = taskId;
			entries = new List<IFSEntry>();
			storageDestinations = new List<uint>();
			this.OriginalSize = 0;
			this.Size = 0;
			this.Order = 0;
			this.Sum = String.Empty;
			this.Name = CreateChunkName();
			this.sentEvent = new ManualResetEventSlim(false);
		}

		internal void Add(IFSEntry bf){
			entries.Add(bf);	
		}
		
		private string CreateChunkName(){
				return Guid.NewGuid().ToString().ToUpper();
		}
		

		/*public long Encrypt(){	
			string cPath = ConfigManager.GetValue("Backups.TempFolder") + Path.DirectorySeparatorChar + name;
			File.Move(cPath, cPath + ".wrk");
			
			RSACryptoServiceProvider keyPair = null;
			GetKeyPair(out keyPair);
			FileStream fsCrypt = null;
			FileStream fsKey = null;
			FileStream fsInput = null;
			CryptoStream cs = null;
			byte [] key;
			byte [] IV;
			long writtenBytes = 0;
			try{
				//Console.WriteLine("DEBUG: 1-BChunk.EncryptFile source="+cPath+".wrk  dest="+cPath);
				fsKey = new FileStream(cPath, FileMode.Create);
				//Console.WriteLine("DEBUG: 2-BChunk.EncryptFile created fskey "+cPath);
				// generate the session key
				RijndaelManaged AES = new RijndaelManaged();
				AES.GenerateIV();
				AES.GenerateKey();
				key = AES.Key;
				IV = AES.IV;

				using (fsKey){
					// encrypt iv and key with the public key
					byte [] encryptedIV = keyPair.Encrypt(AES.IV, false);
					byte [] encryptedKey = keyPair.Encrypt(AES.Key, false);
					// write the encrypted iv and key
					fsKey.Write(encryptedIV, 0, encryptedIV.Length);
					fsKey.Flush();
					fsKey.Write(encryptedKey, 0, encryptedKey.Length);
					fsKey.Flush();
				}
				//Console.WriteLine("DEBUG: 3-BChunk.EncryptFile creating fsCrypt"+cPath);
				fsCrypt = new FileStream(cPath, FileMode.Append);
				//Console.WriteLine("DEBUG: 4-BChunk.EncryptFile, initialized encrypted file "+cPath);
				ICryptoTransform aesencrypt = AES.CreateEncryptor(key, IV);
				cs = new CryptoStream(fsCrypt, aesencrypt, CryptoStreamMode.Write);
				fsInput = new FileStream(cPath+".wrk", FileMode.Open);
				//Console.WriteLine("DEBUG: 5-BChunk.EncryptFile, opened source file "+cPath+".wrk");
				//int data;
				
				// write all data to the crypto stream and flush it
				
				byte[] buffed = new byte[8192] ;
				int rdbyte = 0;
				while(rdbyte<fsInput.Length){
					fsInput.Position = rdbyte;
					int i = fsInput.Read(buffed,0,buffed.Length) ;
					
					cs.Write(buffed, 0, i);
					rdbyte=rdbyte+i ;
					}
				
				
				cs.FlushFinalBlock();
				writtenBytes = fsCrypt.Length;
				Logger.Append(Severity.DEBUG, "BChunk.Encrypt", "Encrypted chunk "+name);
			}
			catch (Exception ex){
				Logger.Append(Severity.ERROR, "BChunk.Encrypt", "Cannot encrypt Bchunk : "+ex.Message);
				// TODO : rethrow to abort backup in case of impossible encryption? or keep doing backup partly unencrypted is better? 
			}
			finally{	
				fsInput.Close();
				cs.Close();
				fsCrypt.Close();
				File.Delete(cPath + ".wrk");
			}
			return writtenBytes;
		}*/
		
		/* TO USE EXISTING CERTIFICATE
		 byte[] l_EncryptedData = null;
        byte[] l_Data = null;
        X509Certificate2 l_X509Certificate2 = FindCertificateBySerialNoAndName(a_CertificateName, a_SerialNo);
        if (l_X509Certificate2 != null)
        {
          RSACryptoServiceProvider l_RSA = (RSACryptoServiceProvider)l_X509Certificate2.PublicKey.Key;
          using (FileStream l_FileStream = new FileStream(a_FilePath, FileMode.Open))
          {
            l_Data = new byte[l_FileStream.Length];
            l_FileStream.Read(l_Data, 0, (int)l_FileStream.Length);
          }
          l_EncryptedData = l_RSA.Encrypt(l_Data, true); 
        }

        using (FileStream l_FileStream = new FileStream(a_FilePath + ".enc", FileMode.Create, FileAccess.Write))
        {
          l_FileStream.Write(l_EncryptedData, 0, l_EncryptedData.Length);
        }
        
        */
		

		
	}
}

