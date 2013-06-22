using System;
using System.IO;
using System.Collections.Generic;
using P2PBackup.Common;

namespace Node.DataProcessing{
	
	//increments task processed % by 1
	public delegate void SubCompletionDelegate(string path);

	/// <summary>
	/// An incremental backup provider has a priority to distinguish itself from other enabled providers
	/// Usually one wants to get the provider with the highest priority ( high priority means fast incremental capabilities)
	/// </summary>
	internal interface IIncrementalProvider{
		string Name{get;}
		short Priority{get;}
		bool IsEnabled{get;set;}
		bool ReturnsOnlyChangedEntries{get;}
		/// <summary>
		/// Checks,  if the provider can be used on this system with the provided metadata
		/// To be called AFTER SignalFullBackup() and SetReferenceMetadata()
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		bool CheckCapability();

		//SubCompletionDelegate SubCompletionHandler{get;set;}

		IEnumerable<IFSEntry> GetNextEntry(BasePath path, string snapshottedPath);
		/// <summary>
		/// When taking a Full backup, keep incremental providers informed, in case they need to.
		/// For example, Us-n provider will need to update its data about usn transaction number.
		/// </summary>
		void SignalBackup();

		byte[] GetMetadata();

		void SetReferenceMetadata(byte[] metadata);
	}
}

