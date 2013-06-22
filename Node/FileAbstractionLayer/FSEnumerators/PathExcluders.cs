

namespace Node.DataProcessing{
	using System;
	using System.Collections.Generic;

	// Returns special paths to always exclude according to the specific OS
	internal interface IPathExcluder {
		 List<string> GetPathsToExclude();
	}
}

namespace Node.DataProcessing{
	using System;
	using System.Collections.Generic;
	internal class PathExcluderFactory {
		internal static IPathExcluder GetPathsExcluder(){
			IPathExcluder excluder = null;


			// Add generic exclusions (storage dir if storage node, and indexes dir)
			List<string> excludes = new List<string>();
			if(!string.IsNullOrEmpty(Utilities.ConfigManager.GetValue("Backups.IndexFolder")))
				excludes.Add(Utilities.ConfigManager.GetValue("Backups.IndexFolder"));

			if(!string.IsNullOrEmpty(Utilities.ConfigManager.GetValue("Storage.Directory")))
				excludes.Add(Utilities.ConfigManager.GetValue("Storage.Directory"));

			if(Node.Utilities.PlatForm.IsUnixClient())
				excluder = new UnixPathExcluder(excludes);
			else
				excluder = new NTPathExcluder(excludes);
			return excluder;
		}

			
	}
}


namespace Node.DataProcessing{
	using System;
	using System.Collections.Generic;
	internal class UnixPathExcluder: IPathExcluder{
		List<string> excludes = new List<string>();

		public UnixPathExcluder(List<string> initialData){
			excludes.AddRange(initialData);

		}

		public List<string> GetPathsToExclude(){

			excludes.Add("/proc");
			excludes.Add("/sys");
			excludes.Add("/dev");
			excludes.Add("/debug");
			excludes.Add("/run");
			excludes.Add("/tmp");
			return excludes;
		}
	}
}

namespace Node.DataProcessing{
	using System;
	using System.Collections.Generic;
	using Microsoft.Win32;

	internal class NTPathExcluder: IPathExcluder{

		List<string> excludes = new List<string>();

		internal NTPathExcluder(List<string> initialData){
			excludes.AddRange(initialData);

		}

		public List<string> GetPathsToExclude(){
			RegistryKey key = Registry.LocalMachine;
			// every subkey inside this key points to paths to exclude from backups
			// note that users can also define additional keys here, so it makes sense to parse it dynamically
			RegistryKey subKey = key.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\BackupRestore\FilesNotToBackup");
			foreach(string keyName in subKey.GetValueNames()){
				if(subKey.GetValueKind(keyName) != RegistryValueKind.MultiString)
					continue;
				excludes.AddRange((string[])subKey.GetValue(keyName));
			}
			// registry entries sometimes return 'incomplete' paths (\Pagefile.sys instead of c:\pagefile.sys or %systemdrive%\pagefile.sys).
			// expand those wrong paths. Also sanitize them (remove "\*" and "/s" occurrences)
			for(int i=0; i<excludes.Count; i++){
				if(excludes[i].StartsWith("\\"))
					excludes[i] = "%systemdrive%"+excludes[i];
				excludes[i] = excludes[i].Replace(" /s","");
				excludes[i] = excludes[i].Replace("*.*","");
				excludes[i] = excludes[i].Replace("\\*","");
			}
			subKey.Close();
			key.Close();
			return excludes;
		}
	}

}

