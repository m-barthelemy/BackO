using System;
using System.Collections.Generic;
using P2PBackup.Common;

namespace P2PBackup.Common{

	/// <summary>
	/// Restore position : restore this item before storage entries, or after
	/// Most SpecialObjects will have to be restored AFTER FS is available on restore target
	/// Special example cases having to be restores before storage may be:
	/// -Storage Layout (of course, if storage layout is required to be restored, it has to be done BEFORE we restore data)
	/// -VM configuration, if asked to restore a whole VM node
	/// </summary>
	public enum RestoreOrder{AfterStorage=0,BeforeStorage=1}

	public interface ISpecialObject : IPlugin{

		List<BasePath> BasePaths{get;}
		// list of real backuped components
		// eg : for a basepath telling VSS:SqlServerWriter/*, may give :
		//    SqlServerWriter/Mydb1
		//    SqlServerWriter/MyDb2
		//    SqlServerWriter/Master
		//      and so on
		List<string> ExplodedComponents{get;}
		void SetItems(List<string> spoPaths);
		SPOMetadata Metadata{get;set;}
		RestoreOrder RestorePosition{get;}
		event EventHandler<LogEventArgs> LogEvent;

		void Freeze();
		void Resume();
			
		void PrepareRestore(List<string> spoPaths);


		void Restore();
		
	}
}

