using System;
using System.IO;
using System.Diagnostics;
using Node.Utilities;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using P2PBackup.Common;
using ProtoBuf;

namespace Node.DataProcessing{

	//TODO write version tag in the index db file
	/// <summary>
	/// Dedup index/database implementation. We chose for now to have the simplest implementation:
	/// -the 'db' is a flat list of checksums, ids and refcounts
	/// -operates on blocks ok 512k whenever possible
	/// -doesn't operate on blocks less than 16k. This way we can still see good benefits of dedup without having a huge dedup index.
	/// 	We try to maintain a 'currentPos' cursor for each user (if parallelism >1), assuming that most of the time, 
	/// 	data will be re-processed in the same order than in previous tasks ; this way we try to reduce complete dedup index traversals.
	/// 	!! : Over time this could become less true with often modified files
	/// -uses lightweight checksums (20 bytes): a block MD5 checksum (128 bits/ 16 bytes) and, to avoid collisions, a 4 bytes integer
	/// 	storing the deduped block size (detect collisions when source data doesn't have the same length)
	/// </summary>
	/// TODO : as the real collisions risk is unknown, add 4 more bytes to the checksum containing the 4 first or 4 last bytes 
	/// 	of the data block
	public class DedupIndex:IDisposable{

		private static /*readonly*/ DedupIndex _instance; //;
		private List<LightDedupedBlock> index;
		private ConcurrentBag<FullDedupedBlock> addPending;
		private string dedupDB;
		private string backDb;
		private int currentPos;
		private int corruptedDDBCount;
		private int pendingAddCount;
		private long maxId;
		private long oldCount;

		//private DataPipeline ddbPipeline;
		private FileStream ddbStream;

		Stopwatch mergeSw = new Stopwatch();

		// currently we only support 1 dedupdb instance loaded at a given time.
		private static int currentBackupSet;

		private bool initializing = false;
		internal string IndexDBName{
			get{return dedupDB;}
		}

		private DedupIndex (){}

		internal bool ExistsAndValid(string checksum){
			return true;
		}

		private long Initialize(bool forWriting){
			initializing = true;
			addPending = new ConcurrentBag<FullDedupedBlock>();
			//Initialize();
			Console.WriteLine (" ###Utilities.ConfigManager.GetValue(Storage.IndexPath)="+Utilities.ConfigManager.GetValue("Storage.IndexPath"));
			dedupDB = Path.Combine(Utilities.ConfigManager.GetValue("Storage.IndexPath"),"dedup_"+currentBackupSet+".idx");

			// before backup we save the database. If backup fails we invalidate the database (when Revert() is called)
			// by replacing the 'new' db with the backDb copy.
			backDb = Path.Combine(Utilities.ConfigManager.GetValue("Storage.IndexPath"),"dedup_"+currentBackupSet+"_"+Utilities.Utils.GetUtcUnixTime(DateTime.Now)+".idx");
			if(forWriting){
				Backup();
				ddbStream = new FileStream(dedupDB, FileMode.Append, FileAccess.Write, FileShare.Read, 1024*1024);
			}

			currentPos = 0;
			pendingAddCount = 0;
			index = new List<LightDedupedBlock>();
			Stopwatch stopWatch = new Stopwatch();
			lock(index){
				Logger.Append(Severity.INFO, "Reading deduplication indexes from DB "+dedupDB+"...");
	        	stopWatch.Start();
				foreach(FullDedupedBlock fddb in this.GetFullDbEnumerator())
					index.Add(new LightDedupedBlock{Checksum = fddb.Checksum, ID = fddb.ID, RefCounts = fddb.RefCounts});

				stopWatch.Stop();
			}
			TimeSpan ts = stopWatch.Elapsed;
			oldCount = index.Count;
			Logger.Append(Severity.INFO, "Got "+oldCount+" deduplication indexes" 
			              +(corruptedDDBCount > 0 ? " ( + "+corruptedDDBCount+" corrupted) " : "")+" from DB in "+ts.TotalMilliseconds+"ms.");	
			maxId = (long)index.Count;

			initializing = false;
			return oldCount;
		}
		
		public static DedupIndex Instance(int bsId, bool forWriting){
			if(currentBackupSet == 0)
				currentBackupSet = bsId;
			else if(bsId != currentBackupSet)// refuse to have multiple dedub db loaded simultaneously
				throw new Exception("A dedup DB (#"+currentBackupSet+" is already loaded.");
			if(_instance == null){
				_instance = new DedupIndex();
				long nbItems = _instance.Initialize(forWriting);
				Logger.Append(Severity.INFO, "Deduplication indexes DB ready ("+nbItems+" items)");
			}
			return _instance;
		}
		
		internal bool DeReference(byte[] checksum){
			for(int j=0; j< index.Count; j++){
				if(UnsafeCompare(index[currentPos].Checksum, checksum)){
					index[currentPos].RefCounts--;
					return true;
				}
			}
			return false;
		}
		
		/*internal int ChunkReferences(string chunkName){
			int references = 0;
			for(int j=0; j< index.Count; j++){
				if(index[j].DataChunkName == chunkName)
					references++;
			}
			return references;
		}*/

		internal int ChunkReferences(long id){
			int references = 0;
			for(int j=0; j< index.Count; j++){
				if(index[j].ID == id)
					references++;
			}
			return references;
		}


		internal bool Contains(byte[] checksum, string chunkname, int posInChunk, int bufSize, int storageNode, ref long dedupId){
#if DEBUG
			BenchmarkStats.Instance().DedupLookups++;
#endif
			for(int j=0; j< index.Count; j++){
				if(UnsafeCompare(index[currentPos].Checksum, checksum)){
					Interlocked.Increment(ref index[currentPos].RefCounts); // in case parallelism >1 and multiple accesses to the same checksum entry
#if DEBUG
					if(j<(index.Count - currentPos)+2)
						BenchmarkStats.Instance().DedupHotFound ++;
					else
						BenchmarkStats.Instance().DedupColdFound ++;
#endif
					dedupId = index[currentPos].ID;
					j++; // move to next dedup entry, hoping that next request will match it
					return true;
				}
				if(currentPos == index.Count -1)
						currentPos = 0;
				else
						currentPos = j;
				currentPos++;
			}
			// key not found, add it
			FullDedupedBlock newDdb = new FullDedupedBlock();
			newDdb.Checksum = new byte[20];
			Array.Copy(checksum, newDdb.Checksum, checksum.Length);
			newDdb.DataChunkName = chunkname;
			newDdb.Length = bufSize;
			newDdb.StartPos = posInChunk;
			newDdb.StorageNodes[0] = storageNode;
			newDdb.RefCounts = 1;
			Interlocked.Increment(ref maxId);
			newDdb.ID = maxId;
			addPending.Add(newDdb);
			Interlocked.Increment(ref pendingAddCount);
#if DEBUG
			BenchmarkStats.Instance().DedupAdd++;
#endif			
			if(pendingAddCount > 2000)
				MergePending();
			dedupId = maxId;
			return false;
		}
		
		private void MergePending(){
			if(addPending.Count == 0) return;
			Logger.Append(Severity.INFO, "Merging "+addPending.Count+" new deduped blocks into main list");
			mergeSw.Start();
			lock(index){
				lock(addPending){
						foreach(FullDedupedBlock fdb in addPending){
							// save complete  dedupedblock to disk database...
							Serializer.SerializeWithLengthPrefix<FullDedupedBlock>(ddbStream, fdb, PrefixStyle.Base128);
							// ...and add a 'light' block to in-memory index
							index.Add(new LightDedupedBlock{Checksum = fdb.Checksum, ID = fdb.ID, RefCounts = fdb.RefCounts});
						}
					addPending = new ConcurrentBag<FullDedupedBlock>();
					pendingAddCount = 0;
				}
			}	
			mergeSw.Stop();
			Logger.Append(Severity.TRIVIA, "Merged and saved new deduped blocks in "+mergeSw.ElapsedMilliseconds+" ms");
			mergeSw.Reset();
		}

		/// <summary>
		/// When a backup task fails, we shoud call Revert() which will:
		/// -delete the newly modified dedup db
		/// -rename the backuped db.
		/// This prevents future backups to use deduped blocks belonging to an invalid task (with data chunks
		/// that might even not have been transferred at all).
		/// </summary>
		internal void Revert(){
			Logger.Append (Severity.DEBUG, "Reverting ddb...");
			File.Delete(dedupDB);
			File.Move(backDb, dedupDB);
			Logger.Append(Severity.INFO, "Successfully reverted ddb");
		}

		internal void Commit(){
			MergePending();
			//ReleaseWriters();
			ddbStream.Close();
			Update();
			Logger.Append(Severity.INFO, "Successfully committed ddb");
		}

		/// <summary>
		/// Renames previous DB file and serializes current dedup blocks list.
		/// To be called after a  backup operation.
		/// Also empties the index list to allow content to be GCed.
		/// Counts & reports medium deduped block size and total deduped data
		/// </summary> 
		private void Update(){
			//TODO : check if dedup index is used and exit

			Logger.Append(Severity.DEBUG, "Saving deduplication indexes database...");
			if(initializing){
				Logger.Append(Severity.NOTICE, "Won't save deduplication DB, not initialized yet");
				return;
			}
			try{ // delete if exists (should not happen)
				File.Delete(dedupDB+".new");
			}
			catch(FileNotFoundException){}

			using(FileStream newDbFs = new FileStream(dedupDB+".new", FileMode.Create, FileAccess.Write)){
				// dedup db metrics
				long totalDataSize = 0;
				long totalRefCount = 0;
				using (IEnumerator<FullDedupedBlock> fullBlocksRef = GetFullDbEnumerator().GetEnumerator()){
					lock(index){
						foreach(LightDedupedBlock ddb in index){
							if(fullBlocksRef.MoveNext()){
								FullDedupedBlock fdb = fullBlocksRef.Current;
								fdb.RefCounts = ddb.RefCounts;
								totalRefCount += fdb.RefCounts;
								if(ddb.RefCounts == 0)// auto-clean db by pruning records not referenced anymore
									continue;
								totalDataSize += fdb.Length;
								Serializer.SerializeWithLengthPrefix<FullDedupedBlock>(newDbFs, fdb, PrefixStyle.Base128);
							}
						}
						Logger.Append(Severity.INFO, "Saved "+index.Count+" deduplication records (previously "+oldCount+") into database. "); 
						if(index.Count >0)
							Console.WriteLine ("Total data size represented by deduplication : "+totalDataSize/1024/1024+"MB, medium block size="+totalDataSize/index.Count/1024+"KB, medium refcount="+totalRefCount/index.Count);
						index = null;
					}
					currentBackupSet = 0;
				}
			}
			string ddbBackup = dedupDB.Replace(".idx", ".idx.bak");
			try{ // remove previous backup
				File.Delete(ddbBackup);
			}
			catch(FileNotFoundException){}
			try{
				File.Move(dedupDB, ddbBackup);
			}
			catch(FileNotFoundException){/*nothing wrong  :first use of the dedup db*/}
			catch(Exception _e){
				Logger.Append (Severity.ERROR, "Could not move old dedup DB to backup location: "+_e.Message);
				throw(_e);
			}
			try{
				File.Move(dedupDB+".new", dedupDB);
			}
			catch(Exception f){
				Logger.Append (Severity.ERROR, "Could not move new dedup DB to definitive location: "+f.Message);
				throw(f);
			}
		}

		private IEnumerable<FullDedupedBlock> GetFullDbEnumerator(){
			using(FileStream fs = new FileStream(dedupDB, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)){
				FullDedupedBlock curBlock = null;
				while ( (curBlock = Serializer.DeserializeWithLengthPrefix<FullDedupedBlock>(fs, PrefixStyle.Base128)) != null){
					yield return curBlock;
				}
			}
		}

		public long DebugDump(){
			int count = 0;
			foreach(FullDedupedBlock fdb in GetFullDbEnumerator()){
				Console.WriteLine(fdb);
				count++;
			}
			return count;
		}

		public  void Dispose(){
			currentBackupSet = 0;
			index = null;
			_instance = null;
		}

		private void Backup(){
			try{
				File.Copy(dedupDB, backDb);
			}
			catch(FileNotFoundException){// first use of the ThexCS ddb, doesn't exist yet
			}
		}

		internal string ChecksumDdb(){
			using(FileStream cksumFS = new FileStream(dedupDB, FileMode.Open, FileAccess.Read, FileShare.Read)){
				return BitConverter.ToString(SHA1.Create().ComputeHash(cksumFS));
			}
		}

		// FIXME !!! find method to compare arrays WITHOUT /4 divisible limitation
		static unsafe bool UnsafeCompare_old(byte[] a1, byte[] a2) {
		  if(a1==null || a2==null || a1.Length!=a2.Length)
		    return false;
		  fixed (byte* p1=a1, p2=a2) {
		    byte* x1=p1, x2=p2;
		    int l = a1.Length;
		    for (int i=0; i < l/8; i++, x1+=8, x2+=8)
		      if (*((long*)x1) != *((long*)x2)) return false;
		    if ((l & 4)!=0) { if (*((int*)x1)!=*((int*)x2)) return false; x1+=4; x2+=4; }
		    if ((l & 2)!=0) { if (*((short*)x1)!=*((short*)x2)) return false; x1+=2; x2+=2; }
		    if ((l & 1)!=0) if (*((byte*)x1) != *((byte*)x2)) return false;
		    return true;
		  }
		}
		
		static unsafe bool UnsafeCompare(byte[] sourceA, byte[] destA) {
			if (sourceA.Length != destA.Length) 
				return false;
			int len = sourceA.Length;
			unsafe {
				fixed (byte* ap = sourceA, bp = destA) {
					long* alp = (long*)ap;
					long* blp = (long*)bp;
					for (; len >= 8; len -= 8) {
						if (*alp != *blp) 
							return false;
						alp++;
						blp++;
					}
					byte* ap2 = (byte*)alp, bp2 = (byte*)blp;
					for (; len > 0; len--) {
						if (*ap2 != *bp2) 
							return false;
						ap2++;
						bp2++;
					}
				}
			}
			return true;
		}
				
	}
}

