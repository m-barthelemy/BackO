using System;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;

namespace P2PBackup.Common {

	[Serializable]
	public class StorageGroup {

		public StorageGroup() {
			this.Storage = 0;
			this.OnlineStorage = 0;
			this.OfflineStorage = 0;
			this.OnlineAvailStorage = 0;
		}
		
		public StorageGroup(string name, int priority){
			
			this.Name = name;
			this.Priority = priority;
		}
		
		public StorageGroup(int id, string name, int priority){
			this.Id = id;
			this.Name = name;
			this.Priority = priority;
		}

		[DisplayFormatOption(Size=7)]
		public int Id{get;set;}

		[DisplayFormatOption(Size=25)]
		public string Name{get;set;}

		[DisplayFormatOption(Size=25)]
		public string Description{get;set;}

		[DisplayFormatOption(Size=3)]
		public int Priority{get;set;}

		[DisplayFormatOption(Size=9,FormatAs=DisplayFormat.Size)]
		public long Storage{get;set;}

		[DisplayFormatOption(Size=9,FormatAs=DisplayFormat.Size)]
		[Ignore] // don't save volatile property to db
		public long OnlineStorage{get;set;}

		[DisplayFormatOption(Size=9,FormatAs=DisplayFormat.Size)]
		[Ignore]
		public long OfflineStorage{get;set;}

		[DisplayFormatOption(Size=9,FormatAs=DisplayFormat.Size)]
		[Ignore] // don't save volatile property to db
		public long OnlineAvailStorage{get;set;}

		[DisplayFormatOption(Size=45)]
		public DataProcessingFlags Capabilities{get;set;}


	}
}

