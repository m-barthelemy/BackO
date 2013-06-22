using System;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;

namespace P2PBackup.Common{
	
	//public enum BackupType{Default=0, Full=1, Differential=2, Incremental=3, TransactionLog=4, SyntheticFull=5};
	public enum BackupLevel{Default=0, Refresh=1, Incremental=2, Differential=3, Full=4, SnapshotOnly=10, TransactionLog=11}; //, Incremental=3, TransactionLog=4, SyntheticFull=5};


	[Serializable]
	[Alias("scheduletime")]
	public class ScheduleTime : IEquatable<ScheduleTime>{

		public int BeginHour{get;set;}
		public int BeginMinute{get;set;}
		public int EndHour{get;set;}
		public int EndMinute{get;set;}

		public BackupLevel Level{get;set;}
		public DayOfWeek Day{get; set;}
			
		[Index(false)]
		public int BackupSetId{get;set;}

		public ScheduleTime (){
			this.BeginHour = -1;
			this.EndHour = -1;
		}
		
		/*public ScheduleTime(BackupLevel level, DayOfWeek day, string begin, string end){
			this.Level = level;
			this.Day = day;
			this.Begin = begin;
			this.End = end;
		}*/

		public override string ToString () {
			return string.Format ("[ScheduleTime: BeginHour={0}, BeginMinute={1}, EndHour={2}, EndMinute={3}, Level={4}, Day={5}, BackupSetId={6}]", BeginHour, BeginMinute, EndHour, EndMinute, Level, Day, BackupSetId);
		}

		public bool Equals(ScheduleTime other){
			if (this.BackupSetId == other.BackupSetId
			    && this.BeginHour == other.BeginHour
			    && this.BeginMinute == other.BeginMinute
			    && this.EndHour == other.EndHour
			    && this.EndMinute == other.EndMinute
			    && this.Day == other.Day)   
				return true;
			else
				return false;
		}
		
	}


	public class BackupSetSchedule: P2PBackup.Common.BackupSet/*, P2PBackup.Common.ScheduleTime*/{
		
		[BelongTo(typeof(ScheduleTime))]
		public int BeginHour{get;set;}
		[BelongTo(typeof(ScheduleTime))]
		public int BeginMinute{get;set;}
		[BelongTo(typeof(ScheduleTime))]
		public int EndHour{get;set;}
		[BelongTo(typeof(ScheduleTime))]
		public int EndMinute{get;set;}
		[BelongTo(typeof(ScheduleTime))]
		public BackupLevel Level{get;set;}
		[BelongTo(typeof(ScheduleTime))]
		public DayOfWeek Day{get; set;}
		
	}

}

