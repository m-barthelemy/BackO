using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using P2PBackup.Common;
using Node.Utilities;
//using Community.CsharpSqlite.SQLiteClient;
using Mono.Data.Sqlite;

namespace Node.DataProcessing{

	/// <summary>
	/// Backup Index. We chose to use an embedded, 100% managed Sqlite implementation to store items.
	/// With the proper tuning options, write performance is really good, and we benefit from all the advantages
	/// a database can bring : data organisation and values indexing, fast searches (for incrementals and restores)
	/// This may also have even more benefit in the future : eg allow some 'data-mining' like features to bring deep
	/// knowledge about the backuped data.
	/// The only drawback so far has been memory usage, we still have to figure out how to keep it low during searches
	/// (most searches operation are done during incremental/synthfull by sequentially scanning the DB without any sort, so memory usage should be kept low)
	/// </summary>
	internal class Index{
		
		internal string FullName{get;private set;}
		internal string Name{get; private set;}
		internal IndexHeader Header{get;set;}
		// [OBSOLETE]rowid is used during incrementals to calculate an offset between ref index position and ref entry position
		// as an heuristic to avoid having too many non-ordered enumerations of the ref index
		internal long RowId{get; private set;}
		private List<BackupRootDrive> rootDrives;
		SqliteConnection indexDbConn;
		MemoryStream dataMs;
		BinaryFormatter dataFormatter;
		System.IO.Compression.GZipStream gz;
		IFileProvider fsEntryProv;

		System.Data.IDbCommand[] addChunkC;
		SqliteParameter[] pid;
		SqliteParameter[] pparentid;
		//SqliteParameter[] taskid;
		SqliteParameter[] pname;
		SqliteParameter[] ptarget;
		SqliteParameter[] psize;
		SqliteParameter[] pkind;
		SqliteParameter[] pcsp;
		SqliteParameter[] pfsp;
		SqliteParameter[] pattrs;
		SqliteParameter[] pcrtime;
		SqliteParameter[] pogroup;
		SqliteParameter[] pouser;
		SqliteParameter[] pperms;
		SqliteParameter[] psattrs;
		SqliteParameter[] pdedup;
		SqliteParameter[] pxattrs;
		SqliteParameter[] plastmetadatamodtime;
		SqliteParameter[] plastmodtime;
		SqliteParameter[] pchunk;
		SqliteParameter[] pchangestatus;
		private long TaskId;

		internal Index(long taskId, bool isPartial){
			this.Header = new IndexHeader();
			this.TaskId = taskId;
			if(TaskId <=0)
				throw new Exception("Task ID is invalid (got "+TaskId+")");
			if(isPartial)
				this.Name = "p"+TaskId+".idx";
			else
				this.Name = "t"+TaskId+".idx";
			this.FullName = Path.Combine(Utilities.ConfigManager.GetValue("Storage.IndexPath"), this.Name);
			fsEntryProv = ItemProvider.GetProvider();
		}

		internal bool Exists(){
			if(File.Exists(this.FullName)) return true;
			return false;
		}

		internal void Create(List<BackupRootDrive> rootDrives){

			this.rootDrives = rootDrives;
			string indexFolder = Utilities.ConfigManager.GetValue("Storage.IndexPath");
			if(!string.IsNullOrEmpty(indexFolder)){
				if(!Directory.Exists(indexFolder))
					Directory.CreateDirectory(indexFolder);
				this.FullName = Path.Combine(indexFolder, this.Name);
			}
			else // if config doesn't tell where to put indexes, create them in the current (./bin/) directory
				this.FullName = this.Name; 

			Logger.Append(Severity.DEBUG, "About to create index as '"+this.FullName+"'");
			if(File.Exists(this.FullName))
				throw new Exception("Index file already exists! ("+this.FullName+")");
			indexDbConn = new SqliteConnection();
			//indexDbConn.ConnectionString = "Version=3,Synchronous=off,data source=file:"+this.FullName+"";
			indexDbConn.ConnectionString = "Version=3,URI=file:"+this.FullName+"";
			indexDbConn.Open();

			// Disable journaling for faster inserts.
			string disableJournal = "PRAGMA journal_mode=off";
			// Disable synchronous (again, for speed)
			string disablesynchronous = "PRAGMA synchronous=off";
			// tune page_size to reduce index size
			string pageSize = "PRAGMA page_size=4096";

			using(System.Data.IDbCommand initC =  indexDbConn.CreateCommand()){
				initC.CommandText = disableJournal;
				initC.ExecuteNonQuery();
				initC.CommandText = disablesynchronous;
				initC.ExecuteNonQuery();
				initC.CommandText = pageSize;
				initC.ExecuteNonQuery();
			}
			CreateTables();
			using(System.Data.IDbCommand configC =  indexDbConn.CreateCommand()){
				// store encoding to correctly retrieve filenames later
				string encodingConfig = "INSERT INTO config(key, value) VALUES('encoding', @encoding)";
				configC.CommandText = encodingConfig;
				SqliteParameter encParam = new SqliteParameter("@encoding", System.Text.Encoding.Default.EncodingName);
				configC.Parameters.Add(encParam);
				configC.ExecuteNonQuery();
			}

			addChunkC = new SqliteCommand[rootDrives.Count];
			pid  = new SqliteParameter[rootDrives.Count];
		 	pparentid = new SqliteParameter[rootDrives.Count];
			//taskid = new SqliteParameter[rootDrives.Count];
			pname = new SqliteParameter[rootDrives.Count];
			ptarget = new SqliteParameter[rootDrives.Count];
			psize = new SqliteParameter[rootDrives.Count];
			pkind = new SqliteParameter[rootDrives.Count];
			pcsp = new SqliteParameter[rootDrives.Count];
			pfsp = new SqliteParameter[rootDrives.Count];
			pattrs = new SqliteParameter[rootDrives.Count];
			pcrtime = new SqliteParameter[rootDrives.Count];
			plastmetadatamodtime = new SqliteParameter[rootDrives.Count];
			plastmodtime = new SqliteParameter[rootDrives.Count];
			pogroup = new SqliteParameter[rootDrives.Count];
			pouser = new SqliteParameter[rootDrives.Count];
			pperms = new SqliteParameter[rootDrives.Count];
			psattrs = new SqliteParameter[rootDrives.Count];
			pxattrs = new SqliteParameter[rootDrives.Count];
			pdedup = new SqliteParameter[rootDrives.Count];
			pchunk = new SqliteParameter[rootDrives.Count];
			pchangestatus = new SqliteParameter[rootDrives.Count];

			PrepareStatements();
			dataFormatter = new BinaryFormatter();
			dataMs = new MemoryStream();
			gz = new System.IO.Compression.GZipStream(dataMs, System.IO.Compression.CompressionMode.Compress, true);
			
		}


		internal void Open(/*string name*/){
			Logger.Append(Severity.TRIVIA, "About to open index '"+this.FullName+"' for reading");
			if(!File.Exists(FullName)){
				Logger.Append(Severity.TRIVIA, "Wanted index File '"+this.FullName+"' doesn't exist");
				throw new FileNotFoundException("This index ("+this.FullName+") doesn't exists or doesn't have a local copy");
			}
			indexDbConn = new SqliteConnection();
			//indexDbConn.ConnectionString = "Version=3,Synchronous=off,data source=file:"+this.FullName+"";
			indexDbConn.ConnectionString = "Version=3,URI=file:"+this.FullName+"";
			indexDbConn.Open();
			Logger.Append (Severity.TRIVIA, "Opened index connection to "+this.Name);
			dataFormatter = new BinaryFormatter();
			dataMs = new MemoryStream();

			// try to limit SELECTs memory usage (keep 500 pages of cache instead of default 2000)
			string memQ = "PRAGMA cache_size=1000";
			using(System.Data.IDbCommand memC =  indexDbConn.CreateCommand()){
				memC.CommandText = memQ;
				memC.ExecuteNonQuery();
				string tmpQ = "PRAGMA temp_store=1";
				memC.CommandText = tmpQ;
				memC.ExecuteNonQuery();
			}
			//get Header
			string headerQ = "SELECT data FROM header";
			dataMs = new MemoryStream();
			using(System.Data.IDbCommand headerC =  indexDbConn.CreateCommand()){
				headerC.CommandText = headerQ;
				System.Data.IDataReader hReader = headerC.ExecuteReader();
				hReader.Read();

				long dataSize = hReader.GetBytes(0, 0, null, 0, 0);
				int bytesRead=0; 
	    		byte[] buffer = new byte[dataSize];
			
				int curPos = 0;
				while (bytesRead < dataSize){
	   				bytesRead += (int)hReader.GetBytes(0, curPos, buffer, curPos, (int)dataSize);
					curPos += bytesRead;
				}
				dataMs.Write(buffer, 0, bytesRead);
			}

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
			Logger.Append (Severity.TRIVIA, "Successfully opened and initialized index "+this.Name);
			dataMs.SetLength(0);
		}

		/// <summary>
		/// Enumerates all drive entries, retrieving only what's abolutely necessary to perform incremental comparisons
		/// </summary>
		/// <returns>
		/// The base items enumerator.
		/// </returns>
		/// <param name='rootDriveName'>
		/// Root drive name.
		/// </param>
		internal IEnumerable<IFSEntry> GetBaseItemsEnumerator(string rootDriveName, long startIndex){
			string query = "SELECT rowid, id, parentid, lastmodtime, lastmetadatamodtime, name, size, kind FROM \""+rootDriveName+"\""
				+" WHERE rowid >="+startIndex;
			using(System.Data.IDbCommand itemC =  indexDbConn.CreateCommand()){
				itemC.CommandText = query;
				System.Data.IDataReader reader = itemC.ExecuteReader();
				IFileProvider prov = ItemProvider.GetProvider();
				while(reader.Read()){
					IFSEntry item = prov.GetEmptyItem();
					this.RowId = reader.GetInt64(0);
					item.ID = reader.GetInt64(1);
					item.ParentID = reader.GetInt64(2);
					item.LastModifiedTime = reader.GetInt64(3);
					item.LastMetadataModifiedTime = reader.GetInt64(4);
					item.Name = (string)reader[5];
					item.FileSize = reader.GetInt64(6);
					item.Kind = (Node.FileType)reader.GetInt32(7);
					yield return item;
				}
			}
		}

		/*internal IEnumerable<IFSEntry> GetReducedItemsEnumerator(string rootDriveName){
			string query = "SELECT id, name FROM \""+rootDriveName+"\"";
			using(System.Data.IDbCommand itemC =  indexDbConn.CreateCommand()){
				itemC.CommandText = query;
				System.Data.IDataReader reader = itemC.ExecuteReader();
				IFSEntry item = ItemProvider.GetProvider().GetItemByPath
				while(reader.Read()){

					item.ID = (long)reader.GetInt64(0);
					item.Name = (string)reader[1];
					yield return item;
						
				}
			}
		}*/
		

		internal IFSEntry SearchItem(IFSEntry itemToSearch, string rootDriveName, out long rowid){
			return SearchItem(itemToSearch.ID, rootDriveName, out rowid);
		}

		internal IFSEntry SearchItem(long itemIdToSearch, string rootDriveName, out long rowid){
			rowid=0;
			string query = "SELECT * FROM \""+rootDriveName+"\" WHERE id="+itemIdToSearch;
			using(System.Data.Common.DbCommand itemC =  indexDbConn.CreateCommand()){
				itemC.CommandText = query;
				System.Data.Common.DbDataReader reader = itemC.ExecuteReader();
				while(reader.Read()){
					return GetItemFromReader(reader);
				}
			}
			return null;
		}

		internal Dictionary<long, Pair<long, bool>> GetItemsForRefresh(string rootFS, bool dontUseMtime){
			var idsAndTime = new Dictionary<long, Pair<long, bool>>();
			string query = "";
			if(dontUseMtime)
				query = "SELECT DISTINCT(id),lastmetadatamodtime FROM \""+rootFS+"\" ";
			else
				query = "SELECT DISTINCT(id),lastmodtime FROM \""+rootFS+"\" ";
			using(System.Data.Common.DbCommand itemC =  indexDbConn.CreateCommand()){
				itemC.CommandText = query;
				System.Data.Common.DbDataReader reader = itemC.ExecuteReader();
				while(reader.Read()){
					idsAndTime.Add(reader.GetInt64(0), new Pair<long, bool>{Item1=reader.GetInt64(1), Item2=false});
				}
			}
			return idsAndTime;
		}

		private IFSEntry GetItemFromReader(System.Data.Common.DbDataReader reader){

			IFSEntry item = fsEntryProv.GetEmptyItem ();
			object xattrs = GetBlob("xattributes", reader);
			if(xattrs != null)
				item.ExtendedAttributes = (List<Tuple<string, byte[]>>)xattrs;//(List<Tuple<string, byte[]>>)dataFormatter.Deserialize(gz);;
			item.ID = reader.GetInt64(reader.GetOrdinal("id"));
			item.ParentID = reader.GetInt64(reader.GetOrdinal("parentid"));
			item.Name = reader.GetString(reader.GetOrdinal("name"));
			item.FileSize = reader.GetInt64(reader.GetOrdinal("size"));
			item.Kind = (Node.FileType)reader.GetInt32(reader.GetOrdinal("kind"));
			item.ChunkStartPos = (uint)reader.GetInt32(reader.GetOrdinal("chunkstartpos"));
			item.FileStartPos = reader.GetInt64(reader.GetOrdinal("filestartpos"));
			item.Attributes = reader.GetInt32(reader.GetOrdinal("attributes"));
			item.CreateTime = reader.GetInt64(reader.GetOrdinal("createtime"));
			item.Attributes = reader.GetInt32(reader.GetOrdinal("attributes"));
			item.BlockMetadata = new FileBlockMetadata();
			byte[] dedupedBlocks = (byte[])reader["dedup"];
			int i = 0;
			if(dedupedBlocks != null && dedupedBlocks.Length >=8)
				while(i<dedupedBlocks.Length){
					Console.WriteLine ("dedupedblocks array len="+dedupedBlocks.Length+", cur pos="+i);
					item.BlockMetadata.DedupedBlocks.Add(BitConverter.ToInt64(dedupedBlocks, i));
					i += 8;
				}
			return item;
		}
		
		private object GetBlob(string field, System.Data.Common.DbDataReader reader){
			if(reader[reader.GetOrdinal(field)] == null) return null;
			int dataSize = (int)reader.GetBytes(reader.GetOrdinal(field), 0, null, 0, 0);
			if(dataSize == 0) return null;

			int offset = 0, bytesRead=0;
			byte[] buffer = new byte[dataSize];
			while (bytesRead < dataSize){
				bytesRead += (int)reader.GetBytes(reader.GetOrdinal(field), offset, buffer, offset, (int)dataSize);
				offset += bytesRead;
				Console.WriteLine("Inedx GetBlob("+field+") - : loop read="+bytesRead);
			}
			using (dataMs = new MemoryStream()){
				dataMs.Write(buffer, 0, bytesRead);
				dataMs.Position = 0;
				using(gz = new System.IO.Compression.GZipStream(dataMs, System.IO.Compression.CompressionMode.Decompress, true)){
					return dataFormatter.Deserialize(gz);
				}
			}
		}

		internal long GetMaxId(string rootDriveName){
			if(indexDbConn == null) // error : to be called only after Open()
				throw new Exception("Not available in this context. Must be called after Open()");
			string query = "SELECT MAX(id) FROM \""+rootDriveName+"\" ";
			using(System.Data.IDbCommand itemC =  indexDbConn.CreateCommand()){
				itemC.CommandText = query;
				return (long)itemC.ExecuteScalar();
			}
		}

		internal int GetMaxChunkId(){
			if(indexDbConn == null) // error : to be called only after Open()
				throw new Exception("Not available in this context. Must be called after Open()");
			string query = "SELECT MAX(id) FROM bchunks";
			using(System.Data.IDbCommand itemC =  indexDbConn.CreateCommand()){
				itemC.CommandText = query;
				return (int)(long)itemC.ExecuteScalar();
			}
		}
		
		internal bool AddChunk(BChunk chunk){
			if(addChunkC[chunk.RootDriveId] == null)
				throw new Exception("This index is not in 'create' mode");
			foreach(IFSEntry item in chunk.Items){
				AddItem(item, chunk.Order, chunk.RootDriveId);
			}
			// this query is only called once per chunk, leave it here for now, if necessary we will optimize it later by
			// preparing it at the Create() stage
			string cq = "INSERT INTO bchunks ( rootdrive, id, taskid, name, taskid, stor1, stor2, stor3 ) "
				+"VALUES ( @rootdrive, @id, @taskid, @name, @taskid, @stor1, @stor2, @stor3 )";
			System.Data.IDbCommand chunkC =  indexDbConn.CreateCommand();
			chunkC.CommandText = cq;

			SqliteParameter crootdrive = new SqliteParameter("@rootdrive", System.Data.DbType.Int64);
			chunkC.Parameters.Add(crootdrive);

			SqliteParameter cid = new SqliteParameter("@id", System.Data.DbType.Int64);
			chunkC.Parameters.Add(cid);

			SqliteParameter cname = new SqliteParameter("@name", System.Data.DbType.String);
			chunkC.Parameters.Add(cname);

			SqliteParameter tid = new SqliteParameter("@taskid", System.Data.DbType.Int64);
			chunkC.Parameters.Add(tid);
			
			crootdrive.Value = chunk.RootDriveId;
			cid.Value = chunk.Order;
			cname.Value = chunk.Name;
			tid.Value = chunk.TaskId;
			
			SqliteParameter stor1 = new SqliteParameter("@stor1", System.Data.DbType.Int16);
			chunkC.Parameters.Add(stor1);
			
			SqliteParameter stor2 = new SqliteParameter("@stor2", System.Data.DbType.Int16);
			chunkC.Parameters.Add(stor2);
			
			SqliteParameter stor3 = new SqliteParameter("@stor3", System.Data.DbType.Int16);
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

		private void AddItem(IFSEntry item, int chunkOrder, int rootDrive){

			pid[rootDrive].Value 			= item.ID;
			pparentid[rootDrive].Value 		= item.ParentID;
			//taskid[rootDrive].Value 		= this.TaskId;
			pname[rootDrive].Value 			= item.Name;
			ptarget[rootDrive].Value 		= item.TargetName;
			psize[rootDrive].Value 			= item.FileSize;
			pkind[rootDrive].Value 			= (int)item.Kind;
			pcsp[rootDrive].Value 			= item.ChunkStartPos;
			pfsp[rootDrive].Value 			= item.FileStartPos;
			pcrtime[rootDrive].Value 		= item.CreateTime;
			pogroup[rootDrive].Value 		= item.OwnerGroup;
			pouser[rootDrive].Value 		= item.OwnerUser;
			pperms[rootDrive].Value 		= item.Permissions;
			pattrs[rootDrive].Value 		= item.Attributes;
			psattrs[rootDrive].Value 		= item.SpecialAttributes;
			plastmetadatamodtime[rootDrive].Value = item.LastMetadataModifiedTime;
			plastmodtime[rootDrive].Value 	= item.LastModifiedTime;
			pchunk[rootDrive].Value 		= chunkOrder;
			pchangestatus[rootDrive].Value 	= item.ChangeStatus;
			pxattrs[rootDrive].Value 		= null; // bin serialization 

			try{
				if(item.ExtendedAttributes != null && item.ExtendedAttributes.Count >0){
					using (dataMs = new MemoryStream()){// serialize extended attributes using system serialization 
						using(gz = new System.IO.Compression.GZipStream(dataMs, System.IO.Compression.CompressionMode.Compress, true)){
							dataFormatter.Serialize(gz, item.ExtendedAttributes);
						}
						dataMs.Flush();
						pxattrs[rootDrive].Value = dataMs.ToArray();
						dataMs.SetLength(0);
					}
				}
				if(item.BlockMetadata != null){// Null sometimes happen (deleted item...)
					byte[] byteArray = new byte[item.BlockMetadata.DedupedBlocks.Count*8];// 8=size of long
					Buffer.BlockCopy(item.BlockMetadata.DedupedBlocks.ToArray(), 0, byteArray, 0, byteArray.Length);
					pdedup[rootDrive].Value = byteArray;
				}
				addChunkC[rootDrive].ExecuteNonQuery();
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "Could not save item '"+item.Name+"' to index : "+e.Message+" ---- "+e.StackTrace);	
				throw;
			}
		}

		internal void AddProviderMetadata(string providerName, BackupRootDrive rd, byte[] metadata){
			try{
				string cq = "INSERT INTO providersmetadata ( name, rootdrive, data) "
					+"VALUES ( @name, @rd, @data )";
				System.Data.IDbCommand provC =  indexDbConn.CreateCommand();
				provC.CommandText = cq;

				SqliteParameter pName = new SqliteParameter("@name", System.Data.DbType.String);
				pName.Value = providerName;
				provC.Parameters.Add(pName);

				SqliteParameter pRd = new SqliteParameter("@rd", System.Data.DbType.Int16);
				pRd.Value = rd.ID;
				provC.Parameters.Add(pRd);

				SqliteParameter pData = new SqliteParameter("@data", System.Data.DbType.Binary);
				pData.Value = metadata;
				provC.Parameters.Add(pData);

				provC.ExecuteNonQuery();
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "Couldn't save Incremental provider '"+providerName+"' metadata : "+e.ToString());
			}
		}

		internal byte[] GetProviderMetadata(string providerName, string rootdriveName){
			string query = "SELECT data FROM providersmetadata WHERE name=@name"
				+" AND rootdrive=(SELECT id FROM rootdrives WHERE mountpath=@rd";
			System.Data.IDbCommand provC =  indexDbConn.CreateCommand();
			provC.CommandText = query;

			SqliteParameter pName = new SqliteParameter("@name", System.Data.DbType.String);
			provC.Parameters.Add(pName);

			SqliteParameter pRd = new SqliteParameter("@rd", System.Data.DbType.String);
			provC.Parameters.Add(pRd);
			return (byte[])provC.ExecuteScalar();
		}

		internal Dictionary<string, byte[]> GetProviderMetadata(string rootdriveName){
			string query = "SELECT name, data FROM providersmetadata WHERE"
				+" rootdrive=(SELECT id FROM rootdrives WHERE mountpath=@rd)";
			System.Data.IDbCommand provC =  indexDbConn.CreateCommand();
			provC.CommandText = query;

			SqliteParameter pRd = new SqliteParameter("@rd", System.Data.DbType.String);
			pRd.Value = rootdriveName;
			provC.Parameters.Add(pRd);
			System.Data.IDataReader reader = provC.ExecuteReader();
			Dictionary<string, byte[]> provMetaData = new Dictionary<string, byte[]>();
			while(reader.Read()){
				int dataSize = (int)reader.GetBytes(reader.GetOrdinal("data"), 0, null, 0, 0);
				if(dataSize == 0) return null;
				int offset = 0, bytesRead=0;
				byte[] buffer = new byte[dataSize];
				while (bytesRead < dataSize){
					bytesRead += (int)reader.GetBytes(reader.GetOrdinal("data"), offset, buffer, offset, (int)dataSize);
					offset += bytesRead;
				}
				provMetaData.Add(reader.GetString(0), buffer);
			}
			return provMetaData;
		}

		internal void AddSpecialObject(string spoName, List<string> spoComponents){

			string components = "INSERT INTO specialobjects (path, type) VALUES (@path, @type)";
			using(System.Data.IDbCommand provC =  indexDbConn.CreateCommand()){
				provC.CommandText = components;
				SqliteParameter path = new SqliteParameter();
				path.ParameterName = "@path";
				provC.Parameters.Add(path);
				SqliteParameter pType = new SqliteParameter();
				pType.ParameterName = "@type";
				provC.Parameters.Add(pType);

				foreach(string component in spoComponents){
					path.Value = component;
					pType.Value = spoName;
					provC.ExecuteNonQuery();
				}
			}
		}

		internal List<string> GetRootDrives(){
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

		// filter : 'onlynodes' retrieves only containers items (mounts, fodlers...)
		// 'onlyleaves' : retrieves only final items (files, links)
		internal IEnumerable<IFSEntry> BrowseChildren(string originalMountPoint, long itemId, string filter){
			string q = "SELECT * from \""+originalMountPoint+"\" WHERE parentid="+itemId;
			if(filter == "onlynodes")
				q += " AND kind NOT IN ("+(int)FileType.File+", "+(int)FileType.Fifo+", "+(int)FileType.Symlink+")";
			else if(filter == "onlyleaves")
				q += " AND kind IN ("+(int)FileType.File+", "+(int)FileType.Fifo+", "+(int)FileType.Symlink+")";
			q +=" ORDER BY name";
			System.Data.Common.DbCommand rdc = indexDbConn.CreateCommand();
			rdc.CommandText = q;
			System.Data.Common.DbDataReader reader = rdc.ExecuteReader();
			while(reader.Read()){
				yield return GetItemFromReader(reader);
			}
		}

		/// <summary>
		/// Gets the entries incremental plugin used for backup (unless backup is a full, which will return null).
		/// </summary>
		/// <returns>
		/// The entries plugin name.
		/// </returns>
		internal string GetEntriesPlugin(string originalMountPoint){
			string q = "SELECT incrementalplugin FROM rootdrives WHERE mountpath='"+originalMountPoint+"'";
			System.Data.IDbCommand rdc =  indexDbConn.CreateCommand();
			rdc.CommandText = q;
			return rdc.ExecuteScalar().ToString();
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
		/// <param name='completePartial'>
		/// if the backup was an incremental-like backup,tells if we want to complete the synthetic index
		/// with entries present in ref index and not in current index. Defaults to false.
		/// Set to true if incremental provider only return really changed entries (and not unchanged ones)
		/// </param>
		/// <exception cref='Exception'>
		/// throws Exception if not called after a Create().
		/// </exception>
		internal void MergeIndexes (long refTask, long curTask){
			if(indexDbConn == null)
				throw new Exception("Must call Create() prior to MergeIndexes()");
			string refIndexName = Path.Combine(Utilities.ConfigManager.GetValue("Storage.IndexPath"), "t"+refTask+".idx");
			string rcurIndexName = Path.Combine(Utilities.ConfigManager.GetValue("Storage.IndexPath"), "p"+curTask+".idx");
			string attachRefQuery = "ATTACH DATABASE \""+refIndexName+"\" AS reft";
			string attachCurQuery = "ATTACH DATABASE \""+rcurIndexName+"\" AS curt";
			System.Data.Common.DbCommand comm = indexDbConn.CreateCommand();
			comm.CommandText = attachRefQuery;
			comm.ExecuteNonQuery();
			comm.CommandText = attachCurQuery;
			comm.ExecuteNonQuery();

			Logger.Append (Severity.TRIVIA, "Memory usage (before merges) : "+GC.GetTotalMemory(false)/1024+"K");
			GC.Collect();
			Logger.Append (Severity.TRIVIA, "Memory usage (after collect) : "+GC.GetTotalMemory(false)/1024+"K");
			int res = 0;

			foreach(string curRootDrive in GetRootDrives()){
				string itemsIncrPlugin = GetEntriesPlugin(curRootDrive);
				if(IncrementalPluginProvider.IsPartialTypePlugin(itemsIncrPlugin)){
					// step1 : merge unchanged items (only existing in reference backup)
					string mergeUnchangedQ = "INSERT INTO \""+curRootDrive+"\""
						+" SELECT *"
						+" FROM reft.\""+curRootDrive+"\" ri"
						+" WHERE ri.id NOT IN"
						+"  (SELECT id FROM curt.\""+curRootDrive+"\" )";
					comm.CommandText = mergeUnchangedQ;
					res = comm.ExecuteNonQuery();
					Logger.Append (Severity.TRIVIA, "Memory usage (before shrink) : "+GC.GetTotalMemory(false)/1024);
					ShrinkMemory();
					Logger.Append (Severity.TRIVIA, "Memory usage (after shrink) : "+GC.GetTotalMemory(false)/1024);
					Logger.Append(Severity.DEBUG, "MergeIndexes : 1/6 "+/*mergeUnchangedQ+*/" , inserted "+res+" unchanged items.");

					// step3 : merge new or updated items (non existent in previous backups so non-existent in merge index)
					string mergeNewQ = "INSERT /*OR REPLACE*/ INTO \""+curRootDrive+"\""
						+" SELECT *"
						+" FROM curt.\""+curRootDrive+"\""
						+" WHERE changestatus ="+((int)DataLayoutInfos.HasChanges);
					comm.CommandText = mergeNewQ;
					res = comm.ExecuteNonQuery();
					Logger.Append(Severity.DEBUG, "MergeIndexes : 3/6 "+/*mergeNewQ+*/" , inserted "+res+" new items.");

				}
				// if entries provider returns a complete layout, it also returns unchanged entries flagged with Changestatus = Nochange
				// update these entries with the dedup info from ref index (since entries are unchanged, data wasn't read thus no dedup info here
				else{
					string mergeAll = "INSERT INTO \""+curRootDrive+"\""
						+" SELECT *"
						+" FROM reft.\""+curRootDrive+"\"";
					comm.CommandText = mergeAll;
					res = comm.ExecuteNonQuery();

					string mergeDedup = "UPDATE \""+curRootDrive+"\""
						+" SET dedup= (SELECT dedup from reft.\""+curRootDrive+"\" ri"
						+"     WHERE ri.id = \""+curRootDrive+"\".id "
						+"     AND ri.parentid = \""+curRootDrive+"\".parentid )";
					comm.CommandText = mergeDedup;
					res = comm.ExecuteNonQuery();
				}
				// step2 : merge chunks corresponding to these untouched items
				string mergeOldChunks = "INSERT INTO bchunks"
					+" SELECT rc.rootdrive , rc.id , rc.taskid , rc.name , rc.stor1 , rc.stor2 , rc.stor3"
					+" FROM reft.bchunks rc, reft.rootdrives rrd"
					+" WHERE rc.rootdrive = rrd.id AND rrd.mountpath='"+curRootDrive+"'";
				comm.CommandText = mergeOldChunks;
				res = comm.ExecuteNonQuery();
				Logger.Append(Severity.DEBUG, "MergeIndexes : 2/6 "+/*mergeOldChunks+*/" , inserted "+res+" reference chunks.");
				Logger.Append (Severity.TRIVIA, "Memory usage (before shrink) : "+GC.GetTotalMemory(false)/1024);
				ShrinkMemory();
				Logger.Append (Severity.TRIVIA, "Memory usage (after shrink) : "+GC.GetTotalMemory(false)/1024);

				// Now delete deleted items. Useful only with a 'partial' entries incremental provider
				string deleteDeleted = "DELETE FROM \""+curRootDrive+"\""
					+" WHERE changestatus="+(int)DataLayoutInfos.Deleted;
				comm.CommandText = deleteDeleted;
				res = comm.ExecuteNonQuery();
				Logger.Append(Severity.DEBUG, "MergeIndexes : 4/6 : deleted "+res+" items.");

				// Renamed or metadata-changes only : insert newly gathered properties, but keep old chunk number
				string metadataChangedQ = "INSERT /*OR REPLACE*/ INTO \""+curRootDrive+"\""
					+" SELECT ci.id, ci.parentid, /*ci.taskid,*/ ci.name, ci.targetname, ci.size, ci.kind, ci.chunkstartpos, ci.filestartpos, ci.attributes ,"
					+" ci.ogroup , ci.ouser , ci.permissions , ci.sattributes , ci.xattributes, ci.createtime , ci.lastmodtime , ci.lastmetadatamodtime , "
					+" ri.chunk , ci.changestatus , ri.dedup  "
					
					+" FROM curt.\""+curRootDrive+"\" ci, reft.\""+curRootDrive+"\" ri"
					+" WHERE "
						//+" (ci.changestatus IN ("+(int)DataLayoutInfos.MetadaOnly+","+(int)DataLayoutInfos.RenameOnly+")"
					+"    (ci.changestatus & "+(uint)DataLayoutInfos.MetadaOnly+" = ci.changestatus"
					+"     OR ci.changestatus & "+(uint)DataLayoutInfos.RenameOnly+" = ci.changestatus)"
					//+" AND ci.ispartial=0"
					+" AND (ci.changestatus & +"+(uint)DataLayoutInfos.PartialRangesFile+" <> ci.changestatus)"
					+" AND ci.id = ri.id";
				comm.CommandText = metadataChangedQ;
				res = comm.ExecuteNonQuery();
				Logger.Append(Severity.DEBUG, "MergeIndexes : 5/6 "+/*deleteDeleted+*/" , merged "+res+" metadata-only items.");


				// last step : merge partial items  (ref blocks ranges "+" new blocks ranges)
				// This clearly sould not be index's responsability,
				// but lacking ideas (at the moment) for a better place/design, let's handle that here
				string partialQNew = "SELECT * FROM curt.\""+curRootDrive+"\""
					//+" WHERE ispartial=1";
					+" WHERE (changestatus & +"+(uint)DataLayoutInfos.PartialRangesFile+" <> changestatus)"
						+" AND (changestatus & +"+(uint)DataLayoutInfos.Deleted+" <> changestatus)";
				comm.CommandText = partialQNew;
				System.Data.Common.DbDataReader reader = comm.ExecuteReader();
				int partialCount = 0;
				while(reader.Read()){
					IFSEntry curPartial = GetItemFromReader(reader);
					IFSEntry refPartial = null;
					long useless = 0;
					if( (refPartial = SearchItem(curPartial, curRootDrive, out useless)) != null)
						curPartial.BlockMetadata.BlockMetadata.AddRange(refPartial.BlockMetadata.BlockMetadata);
					partialCount++;
				}
				Logger.Append(Severity.DEBUG, "MergeIndexes : 6/8 "+/*deleteDeleted+*/" , merged "+res+" partial items.");
				Logger.Append (Severity.TRIVIA, "Memory usage (before shrink) : "+GC.GetTotalMemory(false)/1024);
				ShrinkMemory();
				Logger.Append (Severity.TRIVIA, "Memory usage (after shrink) : "+GC.GetTotalMemory(false)/1024);
				Logger.Append (Severity.TRIVIA, "Memory usage (before collect) : "+GC.GetTotalMemory(false)/1024);
				GC.Collect();
				Logger.Append (Severity.TRIVIA, "Memory usage (after collect) : "+GC.GetTotalMemory(false)/1024);


				//step 4 : merge modified items (oresent in both ref and cur indexes). This is the most complicated/evoluted step as we need to merge the old metadata 
				// with the new one for partial files (changed blocks tracking)
				/*string getModifiedItemsQ = "SELECT *"
					+" FROM reft.\""+curRootDrive+"\" ri"//, reft.items ri, curt.items ci"
					+" WHERE ri.id IN (SELECT id from curt.\""+curRootDrive+"\")";
				comm.CommandText = getModifiedItemsQ;
				Console.WriteLine ("modified items query:  "+getModifiedItemsQ);
				System.Data.Common.DbDataReader reader = comm.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);
				System.Data.Common.DbCommand commU = indexDbConn.CreateCommand();
				string updatedItemsQ = "SELECT * FROM curt.\""+curRootDrive+"\""
					+" WHERE id = @refid";
				commU.CommandText = updatedItemsQ;
				System.Data.Common.DbParameter refid = commU.CreateParameter();
				refid.ParameterName = "@refid";
				refid.DbType = System.Data.DbType.Int64;
				commU.Parameters.Add(refid);
				commU.Prepare();
				while(reader.Read()){
					// get new item in order to make some comparison 
					refid.Value = reader.GetInt64(0);
					System.Data.Common.DbDataReader updatedItemReader = commU.ExecuteReader();
					updatedItemReader.Read();
					// renamed files / reused inodes : ref id == cur id but names are different
					if(reader.GetString(reader.GetOrdinal("name")) == updatedItemReader.GetString(updatedItemReader.GetOrdinal("name"))){
						IFile oldItem = GetItemFromReader(reader);
						IFile newItem = GetItemFromReader(updatedItemReader);
					}
					else
						Console.WriteLine ("New item");
					// same inode id & names
				
				}
				Logger.Append(Severity.DEBUG, "MergeIndexes : mergequery3 (MODq) = "+getModifiedItemsQ);
				*/

			}
			using(System.Data.Common.DbCommand finalCommand = indexDbConn.CreateCommand()){
				string mergeNewChunks = "INSERT INTO bchunks SELECT * FROM curt.bchunks";
				//string cleanupChunks = "DELETE FROM bchunks where id not in (SELECT distinct(bchunk) from 
				finalCommand.CommandText = mergeNewChunks;
				res = finalCommand.ExecuteNonQuery();
				Logger.Append(Severity.DEBUG, "MergeIndexes : 7/8 "+mergeNewChunks+" , inserted "+res+" new chunks.");

				// add incremental providers metadata to the synthetic index
				string addProvMetadataQ = "INSERT INTO providersmetadata"
					+" SELECT * FROM curt.providersmetadata";
				finalCommand.CommandText = addProvMetadataQ;
				res = finalCommand.ExecuteNonQuery();
				Logger.Append(Severity.DEBUG, "MergeIndexes : 8/8 "+mergeNewChunks+" , inserted "+res+" incremental providers metadata entries");
			}
			comm.Dispose();
		}

		private void ShrinkMemory(){
			System.Data.IDbCommand shrink =  indexDbConn.CreateCommand();
			shrink.CommandText = "PRAGMA shrink_memory";
			shrink.ExecuteNonQuery();
		}

		internal void WriteHeaders(){
			System.Data.Common.DbCommand comm = indexDbConn.CreateCommand();
			foreach (BackupRootDrive rd in this.rootDrives){
				string query = "";
				try{
					Console.WriteLine ("rd.SystemDrive.OriginalMountPoint="+rd.SystemDrive.OriginalMountPoint);
					Console.WriteLine (" rd.Snapshot.MountPoint="+rd.Snapshot.MountPoint);
					query = "INSERT INTO rootdrives(id, mountpath, snapshotpath, snapshotid, snapshottype, maxitemid, incrementalplugin)"
						+" VALUES( "+rd.ID+", '"+rd.SystemDrive.OriginalMountPoint+"', '"+rd.Snapshot.MountPoint
						+"','"+rd.Snapshot.Id+"', '"+rd.Snapshot.Type+"',  0, '"+(rd.IncrementalPlugin == null?"":rd.IncrementalPlugin.Name)+"' )";
					comm.CommandText = query;
					comm.ExecuteNonQuery();
				}
				catch(Exception e){
					Logger.Append(Severity.ERROR, "Could not save rootdrive information : "+e.Message+" (query was: "+query+")");	
				}
			}
			string headerQ = "INSERT INTO header (taskid, data) VALUES ( @taskid , @data )";
			comm.CommandText = headerQ;
			
			SqliteParameter taskid = new SqliteParameter();
			taskid.ParameterName = "@taskid";
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
			if(addChunkC != null){
				using(System.Data.Common.DbCommand comm = indexDbConn.CreateCommand()){
					foreach (BackupRootDrive rd in this.rootDrives){
						try{
							string idxQuery1 = "CREATE INDEX idx_item_id_"+rd.ID+" ON \""+rd.SystemDrive.OriginalMountPoint+"\"(id)";
							comm.CommandText = idxQuery1;
							comm.ExecuteNonQuery();
						}
						catch(Exception e){
							Logger.Append(Severity.WARNING, "Could not index drive '"+rd.SystemDrive.OriginalMountPoint+"' : "+e.ToString());
						}
					}
					string indexCompleteQ = "INSERT INTO config(key, value) VALUES('complete','1')";
					comm.CommandText = indexCompleteQ;
					comm.ExecuteNonQuery();
				}
				for(int i=0; i< addChunkC.Length; i++)
					addChunkC[i].Dispose();
			}
			indexDbConn.Close();
			indexDbConn.Dispose();
			dataMs.Dispose();
			Logger.Append(Severity.TRIVIA, "Index Terminate("+this.Name+") called");
		}
		
		private void CreateTables(){

			List<string> queries = new List<string>();
			using(System.Data.Common.DbCommand comm = indexDbConn.CreateCommand()){
				foreach (BackupRootDrive rd in this.rootDrives){
					// Finally, don't use item id as primary key. If it saves space, it has the unwanted side-effect of sorting items by primary key instead of
					// keeping the natural, fs ordering. This fs ordering is good to keep in order to easily compare FS during incrementals
					queries.Add( "CREATE TABLE \""+rd.SystemDrive.OriginalMountPoint+"\" (id INTEGER, parentid INTEGER, /*taskid INTEGER,*/ "
						+"name TEXT, targetname TEXT, size INTEGER, kind INTEGER, chunkstartpos INTEGER, filestartpos INTEGER, attributes INTEGER, "
						+"ogroup INTEGER, ouser INTEGER, permissions INTEGER, sattributes INTEGER, xattributes blob, createtime INTEGER, " 
						+"lastmodtime INTEGER, lastmetadatamodtime INTEGER, chunk INTEGER, changestatus INTEGER, dedup BLOB )");
				}
				queries.Add("CREATE TABLE header ( taskid INTEGER, data BLOB )");
				queries.Add("CREATE TABLE bchunks ( rootdrive INTEGER, id INTEGER, taskid INTEGER, name TEXT, stor1 INTEGER, stor2 INTEGER, stor3 INTEGER )");
				queries.Add("CREATE TABLE rootdrives ( id INTEGER, mountpath TEXT, snapshotpath TEXT, snapshotid TEXT, snapshottype TEXT, maxitemid INTEGER, incrementalplugin TEXT )");
				// version : client node version that creates the index. complete : after all inserts have been done ; our way
				// to ensure index is not corrupt at software level (ie software didn't crash before committing all inserts)
				queries.Add("CREATE TABLE config(key TEXT, value INTEGER)");
				queries.Add("CREATE TABLE providersmetadata (name TEXT, rootdrive INTEGER, available TEXT, data BLOB)");
				queries.Add("CREATE TABLE specialobjects (path TEXT, type TEXT)");
				foreach(string query in queries){
					comm.CommandText = query;
					comm.ExecuteNonQuery();
				}
				queries = null;
			}
		}
		
		/// <summary>
		/// Prepares and pre-compile files/items insert statements.
		/// This way adding a chunk is a really fast operation
		/// </summary>
		private void PrepareStatements(){
			foreach (BackupRootDrive rd in this.rootDrives){
				string q = "INSERT INTO \""+rd.SystemDrive.OriginalMountPoint+"\""
					+"( id, parentid, /*taskid,*/ name, targetname, size, kind, chunkstartpos, filestartpos, attributes, "
					+"ogroup, ouser, permissions, sattributes, xattributes, "
					+"createtime, lastmodtime, lastmetadatamodtime, chunk, changestatus, dedup/*, data*/ ) "
				+"VALUES ( @id, @parentid, /*@taskid,*/ @name, @targetname, @size, @kind, @chunkstartpos, @filestartpos, @attributes, "
						+"@ogroup, @ouser, @permissions, @sattributes, @xattributes, @createtime, @lastmodtime, @lastmetadatamodtime, @chunk, @changestatus, @dedup/*, @data*/ )";

				addChunkC[rd.ID] = indexDbConn.CreateCommand();
				addChunkC[rd.ID].CommandText = q;
			
				pid[rd.ID] = new SqliteParameter("@id", System.Data.DbType.Int64);
				addChunkC[rd.ID].Parameters.Add(pid[rd.ID]);
				
				pparentid[rd.ID] = new SqliteParameter("@parentid", System.Data.DbType.Int64);
				addChunkC[rd.ID].Parameters.Add(pparentid[rd.ID]);
				
				/*taskid[rd.ID] = new SqliteParameter("@taskid", System.Data.DbType.UInt32);
				addChunkC[rd.ID].Parameters.Add(taskid[rd.ID]);*/
				
				pname[rd.ID] = new SqliteParameter("@name", System.Data.DbType.String);
				addChunkC[rd.ID].Parameters.Add(pname[rd.ID]);

				ptarget[rd.ID] = new SqliteParameter("@targetname", System.Data.DbType.String);
				addChunkC[rd.ID].Parameters.Add(ptarget[rd.ID]);

				psize[rd.ID] = new SqliteParameter("@size", System.Data.DbType.Int64);
				addChunkC[rd.ID].Parameters.Add(psize[rd.ID]);

				pkind[rd.ID] = new SqliteParameter("@kind", System.Data.DbType.UInt32);
				addChunkC[rd.ID].Parameters.Add(pkind[rd.ID]);

				pcsp[rd.ID] = new SqliteParameter("@chunkstartpos", System.Data.DbType.UInt32);
				addChunkC[rd.ID].Parameters.Add(pcsp[rd.ID]);

				pfsp[rd.ID] = new SqliteParameter("@filestartpos", System.Data.DbType.UInt32);
				addChunkC[rd.ID].Parameters.Add(pfsp[rd.ID]);

				pattrs[rd.ID] = new SqliteParameter("@attributes", System.Data.DbType.UInt32);
				addChunkC[rd.ID].Parameters.Add(pattrs[rd.ID]);

				pogroup[rd.ID] = new SqliteParameter("@ogroup", System.Data.DbType.Int32);
				addChunkC[rd.ID].Parameters.Add(pogroup[rd.ID]);

				pouser[rd.ID] = new SqliteParameter("@ouser", System.Data.DbType.Int32);
				addChunkC[rd.ID].Parameters.Add(pouser[rd.ID]);

				pperms[rd.ID] = new SqliteParameter("@permissions", System.Data.DbType.UInt32);
				addChunkC[rd.ID].Parameters.Add(pperms[rd.ID]);

				psattrs[rd.ID] = new SqliteParameter("@sattributes", System.Data.DbType.UInt32);
				addChunkC[rd.ID].Parameters.Add(psattrs[rd.ID]);

				pxattrs[rd.ID] = new SqliteParameter();
				pxattrs[rd.ID].ParameterName = "@xattributes";
				addChunkC[rd.ID].Parameters.Add(pxattrs[rd.ID]);

				pcrtime[rd.ID] = new SqliteParameter();
				pcrtime[rd.ID].ParameterName = "@createtime";
				addChunkC[rd.ID].Parameters.Add(pcrtime[rd.ID]);

				plastmetadatamodtime[rd.ID] = new SqliteParameter("@lastmetadatamodtime", System.Data.DbType.Int64);
				addChunkC[rd.ID].Parameters.Add(plastmetadatamodtime[rd.ID]);

				plastmodtime[rd.ID] = new SqliteParameter("@lastmodtime", System.Data.DbType.Int64);
				addChunkC[rd.ID].Parameters.Add(plastmodtime[rd.ID]);

				pchunk[rd.ID] = new SqliteParameter("@chunk", System.Data.DbType.Int64);
				addChunkC[rd.ID].Parameters.Add(pchunk[rd.ID]);

				pchangestatus[rd.ID] = new SqliteParameter("@changestatus", System.Data.DbType.UInt32);
				addChunkC[rd.ID].Parameters.Add(pchangestatus[rd.ID]);

				pdedup[rd.ID] = new SqliteParameter("@dedup", System.Data.DbType.Binary);
				addChunkC[rd.ID].Parameters.Add(pdedup[rd.ID]);
				addChunkC[rd.ID].Prepare();	
			}
		}
	}

	public class CorruptedItemException:Exception{

		public CorruptedItemException(){}

		public CorruptedItemException(string message)
			: base(message) { }
	}

}