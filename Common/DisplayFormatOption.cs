using System;

namespace P2PBackup.Common{
	
	/// <summary>
	/// Misc Common namespace attributes, used for console display formatting
	/// </summary>
	public enum DisplayFormat{Raw,Size,Time};

	[System.AttributeUsage(System.AttributeTargets.Class |
                       System.AttributeTargets.Struct|
	                   System.AttributeTargets.Property,
                       AllowMultiple = false)]
	public class DisplayFormatOption:Attribute{


		public DisplayFormatOption (){
			Display = true;
			FormatAs = DisplayFormat.Raw;
		}

		public bool Display{get;set;}
		public int Size{get;set;}
		public string DisplayAs{get;set;}
		public DisplayFormat FormatAs{get;set;}
	}
}

