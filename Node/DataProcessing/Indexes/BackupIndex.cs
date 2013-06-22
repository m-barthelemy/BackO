/*using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using Node.Utilities;
using P2PBackup.Common;

namespace Node.DataProcessing{
	
	internal class BackupIndex{
		private string version;
		private Backup backup;
		private string name;
		private string fullName;
		
		internal IndexHeader Header{get;set;}
		private long headerLength;
		//private Stream outputStream;
		private IDataProcessorStream outputStream; // for index writing
		//private static IDataProcessorStream indexStream; // for index reading
		IDataProcessorStream ns;
		private  MemoryStream indexStream;
		
		//StreamWriter fileWriter; // DEBUG : xml index
		//XmlSerializer serializer; // DEBUG : xml index
		private  BinaryFormatter formatter;
		
		internal string Name{
			get{return name;}
		}
		
		internal string FullName{
			get{return fullName;}
		}
		
		internal BackupIndex(){
			
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Node.DataProcessing.BackupIndex"/> class FOR RESTORE.
		/// </summary>
		/// <param name='name'>
		/// Name.
		/// </param>
		internal BackupIndex(string name){
			this.name = name;
			this.fullName = Path.Combine(Utilities.ConfigManager.GetValue("Backups.IndexFolder"), name);
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Node.DataProcessing.BackupIndex"/> class FOR RESTORE.
		/// </summary>
		/// <param name='backup'>
		/// Backup.
		/// </param>
		internal BackupIndex(Backup backup){
			this.backup = backup;
			this.version = Utilities.PlatForm.Instance().NodeVersion;
			//TimeSpan ts = (DateTime.UtcNow - new DateTime(1970,1,1,0,0,0));
			//double unixTime = ts.TotalSeconds;
			
				name = "t"+backup.TaskId + ".idx";
			fullName = Path.Combine(Utilities.ConfigManager.GetValue("Backups.IndexFolder"), name);
			
			//outputStream = new FileStream(fullName, FileMode.CreateNew);
			
			ns = new NullSinkStream(new FileStream(fullName, FileMode.CreateNew), PipelineMode.Write);
			//outputStream = new CompressorStream(ns, CompressorAlgorithm.Lzo, (int)1024*512);
			outputStream = new GZCompressorStream(ns, System.IO.Compression.CompressionMode.Compress);
			formatter = new BinaryFormatter();
			Logger.Append(Severity.INFO, "Created backup index '"+fullName+"'");
			
			// DEBUG : we also build an xml index to check its correctness
			//fileWriter = new StreamWriter(name + ".p2px", true);
			//serializer =  new XmlSerializer(   (typeof(Backup));
			BuildHeader();
		}
		
		internal BackupIndex(long taskId){
			
			name = "s"+taskId + ".idx";
			fullName = Path.Combine(Utilities.ConfigManager.GetValue("Backups.IndexFolder"), name);
			
			//outputStream = new FileStream(fullName, FileMode.CreateNew);
			
			ns = new NullSinkStream(new FileStream(fullName, FileMode.CreateNew), PipelineMode.Write);
			//outputStream = new CompressorStream(ns, CompressorAlgorithm.Lzo, (int)1024*512);
			outputStream = new GZCompressorStream(ns, System.IO.Compression.CompressionMode.Compress);
			formatter = new BinaryFormatter();
			Logger.Append(Severity.INFO, "Created synthetic index '"+fullName+"'");
		}
		
		internal void OpenByTaskId(long taskId){
			
			string pattern = "t"+taskId+".idx";
			
			//fullName = Path.Combine(Utilities.ConfigManager.GetValue("Backups.IndexFolder"), name);
			string[] matchingFiles = Directory.GetFiles(Utilities.ConfigManager.GetValue("Backups.IndexFolder"), pattern);
			if(matchingFiles.Length != 1)
				throw new Exception("Unable to find index file by name ("+pattern+"), found "+matchingFiles.Length+" entries");
			//else
			//	return OpenByName(matchingFiles[0]);
			OpenByName(matchingFiles[0]);
			//return null;
			
		}
		
		internal void OpenByName(string indexName){
			//BackupIndex backupIndex = new BackupIndex(indexName);
			//name = "s"+indexName + ".idx";
			name = indexName;
			fullName = Path.Combine(Utilities.ConfigManager.GetValue("Backups.IndexFolder"), name);
			try{
				 ns = new NullSinkStream(new FileStream(fullName, FileMode.Open), PipelineMode.Read);
				//outputStream = new CompressorStream(ns, CompressorAlgorithm.Lzo, (int)1024*512);
				GZCompressorStream gz = new GZCompressorStream(ns, System.IO.Compression.CompressionMode.Decompress);
				indexStream = new MemoryStream();
				byte[] buf = new byte[8192];
				int read = 0;
				while( (read = gz.Read(buf, 0, buf.Length)) > 0)
					indexStream.Write(buf, 0, read);
				formatter = new BinaryFormatter();
				indexStream.Position = 0;
				Logger.Append(Severity.INFO, "Opened backup index '"+fullName+"'");
				this.Header = (IndexHeader)formatter.Deserialize(indexStream);
				headerLength = indexStream.Position;
			}
			catch(Exception e){
				Logger.Append (Severity.ERROR, "Could not read index "+fullName+": "+e.Message+" --- "+e.StackTrace);
				throw e;
			}
			this.name = indexName;
			this.fullName = Path.Combine(Utilities.ConfigManager.GetValue("Backups.IndexFolder"), indexName);
			//return backupIndex;
		}
		*/
		/*internal void CreateSynthetic(long taskId){
			//BackupIndex backupIndex = new BackupIndex(indexName);
			try{
				IDataProcessorStream ns = new NullSinkStream(new FileStream(backupIndex.FullName, FileMode.Open), PipelineMode.Read);
				//outputStream = new CompressorStream(ns, CompressorAlgorithm.Lzo, (int)1024*512);
				GZCompressorStream gz = new GZCompressorStream(ns, System.IO.Compression.CompressionMode.Decompress);
				indexStream = new MemoryStream();
				byte[] buf = new byte[8192];
				int read = 0;
				while( (read = gz.Read(buf, 0, buf.Length)) > 0)
					indexStream.Write(buf, 0, read);
				formatter = new BinaryFormatter();
				indexStream.Position = 0;
				Logger.Append(Severity.INFO, "Opened backup index '"+backupIndex.FullName+"'");
				this.Header = (IndexHeader)formatter.Deserialize(indexStream);
			}
			catch(Exception e){
				Logger.Append (Severity.ERROR, "Could not read index "+backupIndex.FullName+": "+e.Message+" --- "+e.StackTrace);
				throw e;
			}
			this.name = indexName;
			this.fullName = Path.Combine(Utilities.ConfigManager.GetValue("Backups.IndexFolder"), indexName);
			//return backupIndex;
		}*/
		
		/*
		private void BuildHeader(){
			Header = new IndexHeader{
			  Version=this.version,
			  BackupType = backup.Bs.ScheduleTimes[0].Level,
			  TaskId = backup.TaskId,
			  MaxChunkSize = backup.MaxChunkSize,
			  RootDrives = backup.RootDrives
			};                                 
		}
		
		internal void WriteHeaders(){
			formatter.Serialize(outputStream, Header);
			//serializer.Serialize(fileWriter.BaseStream, header);
		}
		
		internal void AddChunk(BChunk chunk){
			lock(formatter){
				Console.WriteLine("backupindex : AddChunk serialize, chunk "+chunk.Name+", items="+chunk.Files.Count);
				formatter.Serialize(outputStream, chunk);
				//outputStream.Flush();
				Console.WriteLine("backupindex : AddChunk SERIALIZED to "+fullName);
				//serializer.Serialize(fileWriter.BaseStream, chunk);
			}
		}
		
		internal void Terminate(){
			//outputStream.Flush();
			if(outputStream != null){
			Console.WriteLine("backupindex : Terminate() called");
			ns.Flush();
			outputStream.Close();
			
			if(outputStream != null)
				outputStream.Dispose();
			if(indexStream != null)
				indexStream.Dispose ();
			}
			else{
				indexStream.Dispose();	
				ns.Close();
			}
			Console.WriteLine("backupindex : Terminate() done");
		}
		
		internal BChunk ReadChunk(){
			try{
				return (BChunk)formatter.Deserialize(indexStream);
			}
			catch(Exception){
				//Console.WriteLine("ReadChunk() : "+e.Message+" ---- "+e.StackTrace);
				return null;
			}
		}
		
		internal BChunk ReadChunk(string basePath){
			try{
				BChunk curChunk = null;
				while( (curChunk == ReadChunk ()) ){
					//if(basePath.IndexOf(curChunk.RootDriveName) > -1)
						return curChunk;
				}
				return null;
			}
			catch(Exception){
				//Console.WriteLine("ReadChunk() : "+e.Message+" ---- "+e.StackTrace);
				return null;
			}
		}
		
		internal List<BChunk> ReadAllChunks(){
			List<BChunk> readChunks = new List<BChunk>();
			while(true){
				try{
					readChunks.Add( (BChunk)formatter.Deserialize(indexStream));
				}
				catch(Exception){
					//Console.WriteLine("ReadChunk() : "+e.Message+" ---- "+e.StackTrace);
					return readChunks;
				}
			}
		}
	}
}
*/
