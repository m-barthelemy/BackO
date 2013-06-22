using System;
using System.Collections.Generic;

namespace P2PBackup.Common.Volumes {

	public enum SnapshotType{VSS, LVM, ZFS, BTRFS, VADP, NONE}

	public interface IDiskElement:IEquatable<IDiskElement> {
		string Path{get;set;}
		string Id{get;set;}
		List<IDiskElement> Parents{get;}
		//List<IDiskElement> Children{get;}
		System.Collections.ObjectModel.ReadOnlyCollection<IDiskElement> Children{get;}
		ulong Offset{get;set;}
		long Size{get;set;}
		/// <summary>
		/// Gets or sets a value indicating whether all the informations has been gathered for this element
		/// If false when discovery provider returns, generic mecanisms will be used to complete this information.
		/// The "ultimate goal" is to have storage layout entries all Complete, which  means
		/// we know enough about the storage layout to perform a bare-metal restore (and any sub-level restore)
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is complete; otherwise, <c>false</c>.
		/// </value>
		bool IsComplete{get;set;}
		void AddChild(IDiskElement elt);

	}
}

