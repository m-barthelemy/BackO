using System;
using System.Collections.Generic;

namespace P2PBackup.Common.Volumes {

	/// <summary>
	/// Reprensent a host's storage media topology. This is the root node of the tree 
	/// containing IDiskElements.
	/// Current Storagelayout limitations, as of 2012-09 :
	/// - no support for disk containg FS but wihtout partitioning
	/// - no support for logical volumes (MD raid, LVM...) !! FIXME
	/// </summary>
	public class StorageLayout {


		public List<IDiskElement> Entries{get;set;}

		public StorageLayout ()	{
			this.Entries = new List<IDiskElement>();
		}

		/// <summary>
		/// Gets all present and mounted FileSystems as a flat List
		/// </summary>
		/// <returns>
		/// The filesystems.
		/// </returns>
		/// <param name='element'>
		/// Element.
		/// </param>
		//private List<IDiskElement> current = null; // not thread safe...

		/*public IEnumerable<FileSystem> GetAllFileSystems(List<IDiskElement> current){
			//if(current == null) current = this.Entries;
			foreach(IDiskElement element in current){
				Console.WriteLine (" #GetAllFileSystems : scanning  "+element.ToString());
				if(element is FileSystem){
					Console.WriteLine ("GetAllFileSystems : got fs "+element.Path);
					yield return((FileSystem)element);
				}
				else{
					if(element.Children == null) {
						Console.WriteLine ("  #No child ");
						Console.WriteLine ("  #No child ("+element.Children.Count+")");
						continue;
					}
					//current = element.Children;
					GetAllFileSystems(element.Children);
				}

			}
			//Logger.Append(Severity.DEBUG2, "Found "+filesystems.Count+" mounted filesystems.");

		}*/

		/*public List<FileSystem> GetAllFileSystems(){
			List<FileSystem> fsl = new List<FileSystem>();
			foreach(IDiskElement de in this.Entries)
				foreach(var elt in  this.GetChildrenFlat())
					if(elt is FileSystem)
					fsl.Add((FileSystem)elt);
			return fsl;
		}*/

		/*public List<FileSystem> GetAllFileSystems(){
>>>>>>> .r813
			List<FileSystem> fsl = new List<FileSystem>();
			foreach(IDiskElement de in this.Entries){
				Console.WriteLine ("   ->"+de.ToString());
				if(de is FileSystem) fsl.Add((FileSystem)de);
				foreach(var elt in  de.Children){
					Console.WriteLine ("   ------>"+elt.ToString());
					if(elt.GetType() == typeof(FileSystem)){ fsl.Add((FileSystem)elt);}
					foreach(var elt2 in elt.Children){
						Console.WriteLine ("   ---------->"+elt2.ToString());
						if(elt2 is FileSystem) fsl.Add((FileSystem)elt2);
					}
				}
			}
			return fsl;
		}*/

		public List<FileSystem> GetAllFileSystems(IDiskElement root){
			List<FileSystem> fsl = new List<FileSystem>();
			/*List<IDiskElement> */System.Collections.ObjectModel.ReadOnlyCollection<IDiskElement>rootElts = null;
			if(root == null)
				rootElts = this.Entries.AsReadOnly();
			else{
				rootElts = root.Children;
				if(root is FileSystem)
					fsl.Add((FileSystem)root);
			}
			foreach(IDiskElement de in rootElts){
				Console.WriteLine ("   ->"+de.ToString());
				fsl.AddRange(GetAllFileSystems(de));
			}
			return fsl;
		}


		public List<Partition> GetAllPartitions(IDiskElement root){
			List<Partition> partitions = new List<Partition>();
			/*List<IDiskElement>*/ System.Collections.ObjectModel.ReadOnlyCollection<IDiskElement> rootElts = null;
			if(root == null)
				rootElts = this.Entries.AsReadOnly();
			else{
				rootElts = root.Children;
				if(root is Partition)
					partitions.Add((Partition)root);
			}
			foreach(IDiskElement de in rootElts){
				Console.WriteLine ("   ->"+de.ToString());
				//if(de is FileSystem) fsl.Add((FileSystem)de);
				partitions.AddRange(GetAllPartitions(de));
			}
			return partitions;
		}

		// returns all partitions, and also disks that don't have partitions (might be a non-partitioned disk with FS on it)
		/*public List<Partition> GetAllPartitions(){
>>>>>>> .r813
			List<Partition> parts = new List<Partition>();
			foreach(IDiskElement de in this.Entries){
				Console.WriteLine ("   ->"+de.ToString());
				if(de is Partition) parts.Add((Partition)de);
				//if(de is Disk && (de.Children == null || de.Children.Count == 0))  parts.Add((Partition)de);
				foreach(var elt in  de.Children){
					Console.WriteLine ("   ------>"+elt.ToString());
					if(elt.GetType() == typeof(Partition)){ Console.WriteLine ("##########################lululululululte"); 
						parts.Add((Partition)elt);}
					foreach(var elt2 in elt.Children){
						Console.WriteLine ("   ---------->"+elt2.ToString());
						if(elt2 is Partition) parts.Add((Partition)elt2);
					}
				}
			}
			return parts;
		}*/

		public bool IsComplete(){
			/*if(this.Entries == null || this.Entries.Count == 0)
				return false;

			foreach(IDiskElement d in this.Entries){
				Console.WriteLine ("   ->"+d.ToString());
				foreach(IDiskElement elt in GetChildrenFlat()){
					if(elt == null)continue;
					Console.WriteLine ("   ---->  "+elt.ToString());
					if(!d.IsComplete){
						Console.WriteLine ("Layout not complete because element "+d.Path+", type '"+d.GetType()+"' is not.");
						return false;
					}
				}
			}*/
			return true;
		}


		/*private List<IDiskElement> current ;
		private IEnumerable<IDiskElement> GetChildrenFlat(){
			if(current == null) current = this.Entries;
			foreach(IDiskElement elt in current){

				if(elt.Children != null){
					current = elt.Children;
					GetChildrenFlat();
				}
				yield return elt;
			}
			yield return current;
		}*/


	}
}

