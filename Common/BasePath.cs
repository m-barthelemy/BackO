using System;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;

namespace P2PBackup.Common{
	
	
	/// <summary>
	/// A BackupPath can be one of : Filesystem (directory/file), Volume (raw block device), Object (VSS writer and components), 
	/// or VM (a virtual machine file drive).
	/// Include and exclude policies only applies to Filesystem entries or "ALL" wildcard results.
	/// </summary>
	[Serializable]
	public class BasePath{

		//public enum PathType{FS,VOLUME,OBJECT,VM,FILE}

		
		public string SnapshotType {get;set;}

		public string Path{get;set;}

		//public string ProxiedPath{get;set;}
		
		public List<string> IncludePolicy{get;set;}
		
		public List<string> ExcludePolicy{get;set;}

		//public BasePathConfig Config{get;set;}

		public List<string> ExcludedPaths{get;set;}

		// if a basepath has been added since last backup, override to perform a full backup
		// of this path (the previously existing paths can keep the default level)
		public BackupLevel OverridenLevel{get;set;}
		
		public bool Recursive{get;set;}

		/// <summary>
		/// Gets or sets the type. Syntax TYPE:PROVIDER
		/// Example: FS:LOCAL, FS:VMWARE, OBJECT:VSS, OBJECT:StorageLayout, STREAM 
		/// </summary>
		/// <value>
		/// The type.
		/// </value>
		public string Type{get;set;}

		public BasePath (){
			this.ExcludedPaths = new List<string>();
			this.IncludePolicy = new List<string>();
			this.ExcludePolicy = new List<string>();
			this.OverridenLevel = BackupLevel.Default;
			//this.Config = new BasePathConfig();
			// TODO : move this somewhere else and don't set it on unix
			/*this.ExcludePolicy.Add("pagefile.sys");
			this.ExcludePolicy.Add("hiberfil.sys");
			this.ExcludePolicy.Add("PAGEFILE.SYS");
			this.ExcludePolicy.Add("HIBERFIL.SYS");*/
			//this.ExcludePolicy.Add("$*");
		}
		
		internal BasePath(string basePath, List<string> includepolicy, List<string> excludepolicy){
			this.Path = basePath;	
			this.IncludePolicy = includepolicy;
			this.ExcludePolicy = excludepolicy;
			this.ExcludedPaths = new List<string>();
			this.OverridenLevel = BackupLevel.Default;
			/*this.ExcludePolicy.Add("pagefile.sys");
			this.ExcludePolicy.Add("hiberfil.sys");
			this.ExcludePolicy.Add("PAGEFILE.SYS");
			this.ExcludePolicy.Add("HIBERFIL.SYS");*/
			//this.ExcludePolicy.Add("$*");

		}

		/// <summary>
		/// Determines whether this instance is parent of (== can contain and replace) the specified other.
		/// </summary>
		/// <returns>
		/// <c>true</c> if this instance is parent of the specified other; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='other'>
		/// If set to <c>true</c> other.
		/// </param>
		public bool CanSwallow(BasePath other, StringComparison comp){
			// avoid a path to swallow itself; duplicated will have to be removes using Distinct() select
			if(string.Equals(this.Path, other.Path, comp)) return false;
			if(this.Path == "/" || this.Path == "") return false;
			if(this.Path == "/*") return true;
			if(/*string.Equals(this.Path, other.Path, comp) ||*/
			   other.Path.IndexOf(this.Path, comp) == 0 /*&&  this.Recursive*/
			   ){
				Console.WriteLine ("==> "+this.Path+" can swallow "+other.Path);
					/*bs.BasePaths[i-1] = MergeRules(bs.BasePaths[i-1], bs.BasePaths[i]);
					bs.BasePaths[i-1].Path = ExpandFilesystemPath(bs.BasePaths[i-1].Path);
					bs.BasePaths.RemoveAt(i);*/
				//canSwallow = true;
				//this.IncludePolicy.AddRange(other.IncludePolicy);
				for(int j=this.ExcludedPaths.Count-1; j>=0; j--){
					if(this.ExcludedPaths[j].IndexOf(other.Path, comp) == 0)
						this.ExcludedPaths.RemoveAt(j);
				}	                                       
				return true;
			}
			//Console.WriteLine ("<== "+this.Path+" cannot swallow "+other.Path);
			return false;
		}

		public override string ToString(){
			return this.Type+":"+ Path;
		}
		
		
	}

	/*      Table ½ shbackup.specialobjects ╗
 Colonne  |       Type        | Modificateurs
----------+-------------------+---------------
 bsid     | integer           |
 spopath  | character varying |
 config   | character varying |
 password | integer           |*/
	[Serializable]
	public struct BasePathConfig{
		public Password Password{get;set;}
		// error : Tuple is not serializable
		public List<Tuple<string, string>> ConfigPairs{get;set;}

	}
}

