using System;
using System.Threading;

namespace Node{
	
	/// <summary>
	/// Only used to gather stats, for debuging and tuning purposes
	/// </summary>
	public  class BenchmarkStats{
		private static readonly BenchmarkStats _instance = new BenchmarkStats();
		private BenchmarkStats (){
			DedupTime = 0;
			ChecksumTime = 0;
			CompressTime = 0;
			DedupLookups = 0;
			SendTime = 0;
		}

		public double ReadTime{get;set;}
		public double DedupTime{get;set;}
		public double ChecksumTime{get;set;}
		public double CompressTime{get;set;}
		public int DedupLookups{get;set;}
		public int DedupColdFound{get;set;}
		public int DedupHotFound{get;set;}
		public int DedupAdd{get;set;}
		public double SendTime{get;set;}
		public static BenchmarkStats Instance(){
			return _instance;
		}
	}
}

