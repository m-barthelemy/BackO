using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using P2PBackup.Common;
using Node.Utilities;
using Community.CsharpSqlite.SQLiteClient;

namespace Node.DataProcessing{
	internal class Index_bk{
		
		internal string FullName{get;private set;}
		internal string Name{get; private set;}
		internal IndexHeader Header{get;set;}
		
		SqliteConnection indexDbConn;
		MemoryStream dataMs;
		BinaryFormatter dataFormatter;
		System.IO.Compression.GZipStream gz;
		
		System.Data.IDbCommand addChunkC;
		//SqliteParameter prootdrive = new SqliteParameter();
		SqliteParameter pid = new SqliteParameter();
		SqliteParameter pparentid = new SqliteParameter();
		SqliteParameter pname = new SqliteParameter();
		SqliteParameter pchunk = new SqliteParameter();
		SqliteParameter pdata = new SqliteParameter();
		
		internal Index_bk (){
			this.Header = new IndexHeader();
		}
		
		internal void Create(long taskId, bool isSynthetic){
			
			if(taskId <=0)
				throw new Exception("Task ID is invalid (got "+taskId+")");
			if(isSynthetic)
				this.Name = "s"+taskId+".idx";
			else
				this.Name = "t"+taskId+".idx";
			this.FullName = Path.Combine(Utilities.ConfigManager.GetValue("Backups.IndexFolder"), this.Name);
			//Console.WriteLine("Index Create() : index file "+this.FullName);
			if(File.Exists(this.FullName))
				throw new Exception("Index file already exists! ("+this.FullName+")");
			indexDbConn = new SqliteConnection();
			//indexDbConn.ConnectionString = "Version=3;Synchronous=off;Compress=True;data source=file:"+this.FullName;
			indexDbConn.ConnectionString = "Version=3,Synchronous=off,data source=file:"+this.FullName+"";
			indexDbConn.Open();
			// Disable journaling for faster inserts.
			string disableJournal = "PRAGMA journal_mode=off";
			// Disable synchronous (again, for speed)
			string disablesynchronous = "PRAGMA synchronous=off";
			// tune page_size to reduce index size
			string pageSize = "PRAGMA page_size=4096";
			System.Data.IDbCommand journalC =  indexDbConn.CreateCommand();
			journalC.CommandText = disableJournal;
			journalC.ExecuteNonQuery();
			journalC.CommandText = disablesynchronous;
			journalC.ExecuteNonQuery();
			journalC.CommandText = pageSize;
			journalC.ExecuteNonQuery();
			
			CreateTables();
			PrepareStatements();
			dataFormatter = new BinaryFormatter();
			dataMs = new MemoryStream();
			gz = new System.IO.Compression.GZipStream(dataMs, System.IO.Compression.CompressionMode.Compress, true);
			
		}
		
		internal void Open(long taskId){
			this.Name = "t"+taskId+".idx";
			this.FullName = Path.Combine(Utilities.ConfigManager.GetValue("Backups.IndexFolder"), this.Name);
			if(!File.Exists(FullName))
				throw new Exception("This index doesn't exists or doesn't have a local copy");
			indexDbConn = new SqliteConnection();
			//indexDbConn.ConnectionString = "Version=3;Synchronous=off;Compress=True;data source=file:"+this.FullName;
			indexDbConn.ConnectionString = "Version=3,Synchronous=off,data source=file:"+this.FullName+"";
			indexDbConn.Open();
			dataFormatter = new BinaryFormatter();
			dataMs = new MemoryStream();
			
			//get Header
			string headerQ = "SELECT data FROM header";
			System.Data.IDbCommand headerC =  indexDbConn.CreateCommand();
			headerC.CommandText = headerQ;
			System.Data.IDataReader hReader = headerC.ExecuteReader();
			hReader.Read();
			dataMs = new MemoryStream();
			long dataSize = hReader.GetBytes(0, 0, null, 0, 0);
			int offset = 0, bytesRead=0;
    		byte[] buffer = new byte[dataSize];
			//Console.WriteLine ("open() read="+hReader.GetBytes(0, 0, buffer, offset, BUFFER_SIZE));
			/*while((bytesRead = (int)hReader.GetBytes(0, offset, buffer, 0, 100)) > 0) {
				Console.WriteLine("open() read header : read="+bytesRead+", offset="+offset);
			    dataMs.Write(buffer, 0, bytesRead);
			    offset += bytesRead;
				
			}*/
			int curPos = 0;
			while (bytesRead < dataSize){
   				bytesRead += (int)hReader.GetBytes(0, curPos, buffer, curPos, (int)dataSize);
				curPos += bytesRead;
			}
			dataMs.Write(buffer, 0, bytesRead);
			dataMs.Flush();
			//MemoryStream gzMs = new MemoryStream();
			dataMs.Position = 0;
			using (MemoryStream uncompressedStream = new MemoryStream()){
				using (gz = new System.IO.Compression.GZipStream(dataMs, System.IO.Compression.CompressionMode.Decompress, true)){
					gz.CopyTo(uncompressedStream);
					
				}
				uncompressedStream.Position = 0;
				this.Header = (IndexHeader)dataFormatter.Deserialize(uncompressedStream);
			}
			
			dataMs.SetLength(0);
		}
		
		internal IEnumerable<IFSEntry> GetItemsEnumerator(string rootDriveName){
			string query = "SELECT i.data FROM items i, bchunks b, rootdrives r WHERE "
				+" i.bchunk = b.id AND b.rootdrive = r.id AND r.name = '"+rootDriveName+"'";
			System.Data.IDbCommand itemC =  indexDbConn.CreateCommand();
			itemC.CommandText = query;
			System.Data.IDataReader reader = itemC.ExecuteReader();
			while(reader.Read()){
				int dataSize = (int)reader.GetBytes(0, 0, null, 0, 0);
				if(dataSize == 0) continue;
				int offset = 0, bytesRead=0;
				byte[] buffer = new byte[dataSize];
				while (bytesRead < dataSize){
   					bytesRead += (int)reader.GetBytes(0, offset, buffer, offset, (int)dataSize);
					offset += bytesRead;
					Console.WriteLine("GetItemsEnumerator() : loop read="+bytesRead);
				}
				using (dataMs = new MemoryStream()){
					dataMs.Write(buffer, 0, bytesRead);
					dataMs.Position = 0;
					using(gz = new System.IO.Compression.GZipStream(dataMs, System.IO.Compression.CompressionMode.Decompress, true)){
						IFSEntry item = (IFSEntry)dataFormatter.Deserialize(gz);
						yield return item;
					}
				}
						
			}
			dataMs.Close();
		}
		
		internal IEnumerable<IFSEntry> GetItemsEnumerator(){
			string query = "SELECT i.data FROM items i";
			System.Data.IDbCommand itemC =  indexDbConn.CreateCommand();
			itemC.CommandText = query;
			System.Data.IDataReader reader = itemC.ExecuteReader();
    		
			while(reader.Read()){
				int dataSize = (int)reader.GetBytes(0, 0, null, 0, 0);
				if(dataSize == 0) continue;
				int offset = 0, bytesRead=0;
				byte[] buffer = new byte[dataSize];
				while (bytesRead < dataSize){
   					bytesRead += (int)reader.GetBytes(0, offset, buffer, offset, (int)dataSize);
					offset += bytesRead;
					Console.WriteLine("GetItemsEnumerator() : loop read="+bytesRead);
				}
				using (dataMs = new MemoryStream()){
					dataMs.Write(buffer, 0, bytesRead);
					//dataMs.Flush();
					dataMs.Position = 0;
					using(gz = new System.IO.Compression.GZipStream(dataMs, System.IO.Compression.CompressionMode.Decompress, true)){
						IFSEntry item = (IFSEntry)dataFormatter.Deserialize(gz);
						yield return item;
					}
				}
						
			}
			dataMs.Close();
		}
		
		internal IFSEntry SearchItem(IFSEntry itemToSearch){
			string query = "SELECT data FROM items WHERE id="+itemToSearch.ID;
			System.Data.IDbCommand itemC =  indexDbConn.CreateCommand();
			itemC.CommandText = query;
			System.Data.IDataReader reader = itemC.ExecuteReader();
			while(reader.Read()){
				int dataSize = (int)reader.GetBytes(0, 0, null, 0, 0);
				if(dataSize == 0) continue;
				int offset = 0, bytesRead=0;
				byte[] buffer = new byte[dataSize];
				while (bytesRead < dataSize){
   					bytesRead += (int)reader.GetBytes(0, offset, buffer, offset, (int)dataSize);
					offset += bytesRead;
					Console.WriteLine("GetItemsEnumerator() : loop read="+bytesRead);
				}
				using (dataMs = new MemoryStream()){
					dataMs.Write(buffer, 0, bytesRead);
					//dataMs.Flush();
					dataMs.Position = 0;
					using(gz = new System.IO.Compression.GZipStream(dataMs, System.IO.Compression.CompressionMode.Decompress, true)){
						IFSEntry item = (IFSEntry)dataFormatter.Deserialize(gz);
						if(item.OriginalFullPath == itemToSearch.OriginalFullPath)
							return item;
					}
				}
			}
			return null;
		}
		
		internal long GetMaxId(string rootDriveName){
			if(indexDbConn == null) // error : to be called only after Open()
				throw new Exception("Not available in this context. Must be called after Open()");
			string query = "SELECT MAX(i.id) FROM items i, bchunks b, rootdrives r WHERE "
				+" i.bchunk = b.id AND b.rootdrive = r.id AND r.name = '"+rootDriveName+"'";
			System.Data.IDbCommand itemC =  indexDbConn.CreateCommand();
			itemC.CommandText = query;
			long maxid = -1;
			try{
				maxid = (long)itemC.ExecuteScalar();
			}
			catch{}
			return maxid;
			
			
		}
		
		internal bool AddChunk(BChunk chunk){
			if(addChunkC == null)
				throw new Exception("This index is not in 'create' mode");
			
			foreach(IFSEntry item in chunk.Files){
				//prootdrive.Value = chunk.RootDriveName;
				pid.Value = item.ID;
				pparentid.Value = item.ParentID;
				//pname.Value = item.OriginalFullPath;
				pname.Value = item.Name;
				pchunk.Value = chunk.Order;
				
				try{
					using (dataMs = new MemoryStream()){
					using(gz = new System.IO.Compression.GZipStream(dataMs, System.IO.Compression.CompressionMode.Compress, true)){
						dataFormatter.Serialize(gz, item);
						//gz.Flush();
						pdata.Value = dataMs.ToArray();
					}
					}
					addChunkC.ExecuteNonQuery();
				}
				catch(Exception e){
					Logger.Append(Severity.ERROR, "Could not save item '"+item.Name+"' to index : "+e.Message+" ---- "+e.StackTrace);	
					return false;
				}
				//dataMs.SetLength(0);
			}
			// this query is only called once per chunk, leave it here for now, if necessary we will optimize it later by
			// preparing it at the Create() stage
			string cq = "INSERT INTO bchunks ( rootdrive, id, name, taskid, stor1, stor2, stor3 ) "
				+"VALUES ( @rootdrive, @id, @name, @taskid, @stor1, @stor2, @stor3 )";
			System.Data.IDbCommand chunkC =  indexDbConn.CreateCommand();
			chunkC.CommandText = cq;
			SqliteParameter crootdrive = new SqliteParameter();
			crootdrive.ParameterName = "@rootdrive";
			crootdrive.DbType = System.Data.DbType.Int64;
			chunkC.Parameters.Add(crootdrive);
			SqliteParameter cid = new SqliteParameter();
			cid.ParameterName = "@id";
			cid.DbType = System.Data.DbType.Int64;
			chunkC.Parameters.Add(cid);
			SqliteParameter cname = new SqliteParameter();
			cname.ParameterName = "@name";
			cname.DbType = System.Data.DbType.String;
			chunkC.Parameters.Add(cname);
			SqliteParameter tid = new SqliteParameter();
			tid.ParameterName = "@taskid";
			tid.DbType = System.Data.DbType.Int64;
			chunkC.Parameters.Add(tid);
			
			crootdrive.Value = chunk.RootDriveId;
			cid.Value = chunk.Order;
			cname.Value = chunk.Name;
			tid.Value = chunk.TaskId;
			
			
			SqliteParameter stor1 = new SqliteParameter();
			stor1.ParameterName = "@stor1";
			stor1.DbType = System.Data.DbType.Int16;
			chunkC.Parameters.Add(stor1);
			
			SqliteParameter stor2 = new SqliteParameter();
			stor2.ParameterName = "@stor2";
			stor2.DbType = System.Data.DbType.Int16;
			chunkC.Parameters.Add(stor2);
			
			SqliteParameter stor3 = new SqliteParameter();
			stor3.ParameterName = "@stor3";
			stor3.DbType = System.Data.DbType.Int16;
			chunkC.Parameters.Add(stor3);
			
			stor1.Value = chunk.StorageDestinations[0];
			if(chunk.StorageDestinations.Count >1)
				stor2.Value = chunk.StorageDestinations[1];
			if(chunk.StorageDestinations.Count >2)
				stor3.Value = chunk.StorageDestinations[2];
			try{
				chunkC.ExecuteNonQuery();
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "Could not save item BCHUNK '"+chunk.Name+"' to index : "+e.Message+" ---- "+e.StackTrace);	
				return false;
			}
			return true;
		}
		
		internal List<string> GetrootDrives(){
			string q = "SELECT mountpath FROM rootdrives";
			System.Data.IDbCommand rdc =  indexDbConn.CreateCommand();
			rdc.CommandText = q;
			System.Data.IDataReader reader = rdc.ExecuteReader();
			List<string> rootDrives = new List<string>();
			while(reader.Read()){
				rootDrives.Add(reader.GetString(0));
			}
			return rootDrives;
		}
		
		/// <summary>
		/// Merges the indexes. All the "synthetic index" magic happens here through SQL queries.
		/// </summary>
		/// <param name='refTask'>
		/// Reference task.
		/// </param>
		/// <param name='curTask'>
		/// Current task.
		/// </param>
		/// <exception cref='Exception'>
		/// throws Exception if not called after a Create().
		/// </exception>
		internal void MergeIndexes (long refTask, long curTask){
			if(indexDbConn == null)
				throw new Exception("Must call Create() prior to MergeIndexes()");
			string refIndexName = Path.Combine(Utilities.ConfigManager.GetValue("Backups.IndexFolder"), "t"+refTask+".idx");
			string rcurIndexName = Path.Combine(Utilities.ConfigManager.GetValue("Backups.IndexFolder"), "t"+curTask+".idx");
			string attachRefQuery = "ATTACH DATABASE \""+refIndexName+"\" AS reft";
			string attachCurQuery = "ATTACH DATABASE \""+rcurIndexName+"\" AS curt";
			System.Data.Common.DbCommand comm = indexDbConn.CreateCommand();
			foreach(string curRootDrive in GetrootDrives()){
				
				comm.CommandText = attachRefQuery;
				comm.ExecuteNonQuery();
				comm.CommandText = attachCurQuery;
				comm.ExecuteNonQuery();
				
				// step1 : merge unchanged items (only existing in reference backup)
				/*string mergeUnchangedQ = "INSERT INTO items"
					+" SELECT ri.id, ri.parentid, ri.name, ri.chunk, ri.data"
					+" FROM reft.items ri, reft.bchunks rc, reft.rootdrives rrd WHERE ri.chunk = rc.id"
					+" AND rc.rootdrive = rrd.id AND rrd.mountpath='"+curRootDrive+"'"
					+" AND ri.id NOT IN"
						+" (SELECT ci.id FROM curt.items ci, curt.bchunks cc, curt.rootdrives crd WHERE ci.chunk = cc.id"
						+" AND cc.rootdrive = crd.id AND crd.mountpath='"+curRootDrive+"')";*/
				string mergeUnchangedQ = "INSERT INTO items"
					+" SELECT ri.id, ri.parentid, ri.name, ri.chunk, ri.data"
					+" FROM reft.items ri, reft.bchunks rc, reft.rootdrives rrd WHERE ri.chunk = rc.id"
					+" AND rc.rootdrive = rrd.id AND rrd.mountpath='"+curRootDrive+"'"
					+" AND NOT EXISTS"
						+" (SELECT ci.id FROM curt.items ci, curt.bchunks cc, curt.rootdrives crd WHERE ci.chunk = cc.id"
						+" AND cc.rootdrive = crd.id AND crd.mountpath='"+curRootDrive+"' AND ci.id = ri.id)";
				comm.CommandText = mergeUnchangedQ;
				int res = comm.ExecuteNonQuery();
				Logger.Append(Severity.DEBUG, "MergeIndexes : mergequery1 = "+mergeUnchangedQ+", inserted "+res+" unchanged items.");
				// step2 : merge chunks corresponding to these untouched items
				string mergeOldChunks = "INSERT INTO bchunks SELECT rc.rootdrive , rc.id , rc.taskid , rc.name , rc.stor1 , rc.stor2 , rc.stor3"
					+" FROM reft.bchunks rc, reft.rootdrives rrd"
					+" WHERE rc.rootdrive = rrd.id AND rrd.mountpath='"+curRootDrive+"'";
				comm.CommandText = mergeOldChunks;
				comm.ExecuteNonQuery();
				
				// step3 : merge totally new items (non existent in previous backups so non-existent in merge index)
				//string mergeNewQ = "insert into items select * from curt.items";
				/*string mergeNewQ = "INSERT INTO items"
					+" SELECT ri.id, ri.parentid, ri.name, ri.chunk, ri.data"
					+" FROM reft.items ri, reft.bchunks rc, reft.rootdrives rrd WHERE ri.chunk = rc.id"
					+" AND rc.rootdrive = rrd.id AND rrd.mountpath='"+curRootDrive+"'"
					+" AND ri.id NOT IN"
						+" (SELECT ci.id FROM items ci, bchunks cc, rootdrives crd WHERE ci.chunk = cc.id"
						+" AND cc.rootdrive = crd.id AND crd.mountpath='"+curRootDrive+"')";*/
				string mergeNewQ = "INSERT INTO items"
					+" SELECT ri.id, ri.parentid, ri.name, ri.chunk, ri.data"
					+" FROM curt.items ri, curt.bchunks rc, curt.rootdrives rrd WHERE ri.chunk = rc.id"
					+" AND rc.rootdrive = rrd.id AND rrd.mountpath='"+curRootDrive+"'"
					+" AND ri.id NOT IN"
						+" (SELECT ci.id FROM items ci, bchunks cc, rootdrives crd WHERE ci.chunk = cc.id"
						+" AND cc.rootdrive = crd.id AND crd.mountpath='"+curRootDrive+"')";
				
				comm.CommandText = mergeNewQ;
				res = comm.ExecuteNonQuery();
				Logger.Append(Severity.DEBUG, "MergeIndexes : mergequery2 (newq) = "+mergeNewQ+", inserted "+res+" new items.");
				//step 4 : merge modified items (oresent in both ref and cur indexes). This is the most complicated/evoluted step as we need to merge the old metadata 
				// with the new one for partial files (changed blocks tracking)
				string getModifiedItemsQ = "SELECT i.id, i.parentid, i.name, i.chunk, i.data"
					+" FROM reft.items i"//, reft.items ri, curt.items ci"
					+" WHERE i.id IN (SELECT id from curt.items)";
					//+" AND i.id IN (SELECT id from reft.items)";
				comm.CommandText = getModifiedItemsQ;
				System.Data.Common.DbDataReader reader = comm.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);
				while(reader.Read()){
					
					
				
				}
				Logger.Append(Severity.DEBUG, "MergeIndexes : mergequery3 (MODq) = "+getModifiedItemsQ);
				
				
			}
				
			//string mergeNewChunks = "INSERT INTO bchunks SELECT * FROM curt.bchunks";
				//string cleanupChunks = "DELETE FROM bchunks where id not in (SELECT distinct(bchunk) from 
				
			//comm.CommandText = mergeNewChunks;
			//comm.ExecuteNonQuery();
			
		}
		
		internal void WriteHeaders(){
			System.Data.Common.DbCommand comm = indexDbConn.CreateCommand();
			foreach (BackupRootDrive rd in this.Header.RootDrives){
				string query = "";
				try{
					query = "INSERT INTO rootdrives(id, mountpath, snapshottedpath, maxitemid)"
						+" VALUES( "+rd.ID+", '"+rd.SystemDrive.MountPoint+"', '"+rd.Snapshot.Path+"', 0 )";
					comm.CommandText = query;
					comm.ExecuteNonQuery();
				}
				catch(Exception e){
					Logger.Append(Severity.ERROR, "Could not save rootdrive information : "+e.Message+" (query was: "+query+")");	
				}
			}
			string headerQ = "INSERT INTO header (taskid, data) VALUES ( @taskid , @data )";
			//dataMs = new MemoryStream();
			//dataMs.Position = 0;
			
			
			comm.CommandText = headerQ;
			
			SqliteParameter taskid = new SqliteParameter();
			taskid.ParameterName = "@taskid";
			taskid.DbType = System.Data.DbType.Int64;
			taskid.Value = this.Header.TaskId;
			comm.Parameters.Add(taskid);
			
			SqliteParameter hData = new SqliteParameter();
			hData.ParameterName = "@data";
			hData.DbType = System.Data.DbType.Binary;
			using (gz = new System.IO.Compression.GZipStream(dataMs, System.IO.Compression.CompressionMode.Compress, true)){
				dataFormatter.Serialize(gz, Header);
			}
			dataMs.Position = 0;
			hData.Value = dataMs.ToArray();
			comm.Parameters.Add(hData);
			Console.WriteLine ("  - -- - -- -writeheaders() : header data len="+dataMs.ToArray().Length);
			try{
				comm.ExecuteNonQuery();
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "Could not save header information : "+e.Message);
			}
			dataMs.SetLength(0);
		}
		
		internal void  Terminate(){
			string idxQuery1 = "CREATE INDEX idx_item_id ON items(id)";
			string idxQuery2 = "CREATE INDEX idx_item_parentid ON items(parentid)";
			string indexCompleteQ = "INSERT INTO config(complete) VALUES(1)";
			//string idxQuery3 = "CREATE INDEX idx_item_rootdrive ON items(rootdrive)";
			System.Data.Common.DbCommand comm = indexDbConn.CreateCommand();
			if(addChunkC != null){
				comm.CommandText = idxQuery1;
				comm.ExecuteNonQuery();
				comm.CommandText = idxQuery2;
				comm.ExecuteNonQuery();
				comm.CommandText = indexCompleteQ;
				comm.ExecuteNonQuery();
			}
			//comm.CommandText = idxQuery3;
			//comm.ExecuteNonQuery();
			indexDbConn.Close();
			dataMs.Dispose();
		}
		
		private void CreateTables(){
			string query1 = "CREATE TABLE items ( id INTEGER, parentid INTEGER, name TEXT, chunk INTEGER, data BLOB )";
			string query2 = "CREATE TABLE header ( taskid INTEGER, data BLOB )";
			string query3 = "CREATE TABLE bchunks ( rootdrive INTEGER, id INTEGER, taskid INTEGER, name TEXT, stor1 INTEGER, stor2 INTEGER, stor3 INTEGER )";
			string query4 = "CREATE TABLE rootdrives ( id INTEGER, mountpath TEXT, snapshottedpath TEXT, maxitemid INTEGER )";
			// version : client node version that creates the index. complete : after all inserts have been done ; our way
			// to ensure index is not corrupt at software level (ie software didn't crash before committing all inserts)
			string query5 = "CREATE TABLE config(version TEXT, complete INTEGER)";
			System.Data.Common.DbCommand comm = indexDbConn.CreateCommand();
			comm.CommandText = query1;
			comm.ExecuteNonQuery();
			comm.CommandText = query2;
			comm.ExecuteNonQuery();
			comm.CommandText = query3;
			comm.ExecuteNonQuery();
			comm.CommandText = query4;
			comm.ExecuteNonQuery();
			comm.CommandText = query5;
			comm.ExecuteNonQuery();
			comm.Dispose();
		}
		
		/// <summary>
		/// Prepares and pre-compile files/items insert statements.
		/// </summary>
		private void PrepareStatements(){
			
			string q = "INSERT INTO items ( id, parentid, name, chunk, data ) "
			+"VALUES ( @id, @parentid, @name, @chunk, @data )";
			
			addChunkC = indexDbConn.CreateCommand();
			addChunkC.CommandText = q;
			
			//prootdrive.ParameterName = "@rootdrive";
			//prootdrive.DbType = System.Data.DbType.String;
			//addChunkC.Parameters.Add(prootdrive);
			
			pid.ParameterName = "@id";
			pid.DbType = System.Data.DbType.Int64;
			addChunkC.Parameters.Add(pid);
			
			pparentid.ParameterName = "@parentid";
			pparentid.DbType = System.Data.DbType.Int64;
			addChunkC.Parameters.Add(pparentid);
			
			pname.ParameterName = "@name";
			pname.DbType = System.Data.DbType.String;
			addChunkC.Parameters.Add(pname);
			
			/*SqliteParameter pchunk = new SqliteParameter();
			pchunk.ParameterName = "@chunk";
			pchunk.DbType = System.Data.DbType.String;
			addChunkC.Parameters.Add(pchunk);*/
			
			pchunk.ParameterName = "@chunk";
			pchunk.DbType = System.Data.DbType.Int64;
			addChunkC.Parameters.Add(pchunk);
			
			pdata.ParameterName = "@data";
			pdata.DbType = System.Data.DbType.Binary;
			addChunkC.Parameters.Add(pdata);
			
			addChunkC.Prepare();	
		}
	}
}

