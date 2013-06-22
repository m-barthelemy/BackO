using System;
using System.IO;
using System.Collections.Generic;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;
using Node.Utilities;

namespace Node.StorageLayer {


	/// <summary>
	/// Node storage manager. Build a representation of the storage layout of the system.
	/// </summary>
	//[Serializable]
	internal class StorageLayoutManager:IDisposable {

		private StorageLayout layout;
		private List<string> rootStoragePaths;
		public delegate void LogHandler(int code, Severity severity, string message);
		public event EventHandler<LogEventArgs> LogEvent;
		// if the constructor has non-null parameters, we assume that it means that we
		// have to mount the provided BlockDevices paths
		//private bool mount;
		IStorageDiscoverer discoverer;

		// in case disks are proxied devices to treat as loop (linux VM for example)
		LinuxLoopDeviceHelper loopH;
		LinuxStorageDiscoverer lsd;

		public StorageLayoutManager(List<string> rootStoragePaths){
			/*this.rootStoragePaths = rootStoragePaths;
			if(this.rootStoragePaths != null && this.rootStoragePaths.Count>0)
				mount = true;*/
			layout = new StorageLayout();
		}
	
		/// <summary>
		/// Gets the storage layout. If rootStoragePaths is NULL, returns layout of the local system. Else, treat 
		/// rootStoragePaths as locally accessible loop disks and scan them
		/// </summary>
		/// <returns>
		/// The storage layout.
		/// </returns>
		/// <param name='rootStoragePaths'>
		/// Root storage paths.
		/// </param>
		internal StorageLayout BuildStorageLayout(string physicalDisksProviderName, ProxyTaskInfo proxyingInfo){
			discoverer = StorageLayoutFactory.Create(physicalDisksProviderName);

			if(discoverer == null || !discoverer.Initialize(proxyingInfo)){
				//Logger.Append(Severity.ERROR, "Could not initialize storage layout manager '"+physicalDisksProviderName+"'");
				throw new Exception ("Could not initialize storage layout manager '"+physicalDisksProviderName+"'");
			}
			discoverer.LogEvent += this.LogReceivedEvent;
			Logger.Append (Severity.INFO, "Building storage layout using provider '"+discoverer.Name+"'...");

			layout = (discoverer.BuildStorageLayout());
			Console.WriteLine(" BuildStorageLayout() ---- is layout complete?  "+layout.IsComplete());

			Logger.Append(Severity.INFO, "Got "+layout.Entries.Count+" disks from storage layout provider");
			FinishBuildDisksLinux();

			return layout;
		}

		// bubble up logs
		private void LogReceivedEvent(object sender, LogEventArgs args){
			/*Logger.Append(args.Severity, args.Message);
			if(args.Code >0){

			}*/
			if(LogEvent != null)LogEvent(sender, args);
		}

		//Mounts "loop" devices, try to mount system registry to guess drives names
		private void FinishBuildDisksNT(){

		}

		private void FinishBuildDisksLinux(){
			// Maybe the returned layout is partial because disks are loops (proxied Linux VM backup...)
			List<string> loopNames = new List<string>();
			foreach(IDiskElement elt in layout.Entries){
				if(elt is Disk && ((Disk)elt).Type == DiskType.Loop /*&& ((Disk)elt).Enabled*/){
					Disk disk = (Disk)elt;
					loopH = new LinuxLoopDeviceHelper();
					try{
						string loopPath = loopH.GetLoop(disk.ProxiedPath, "");
						Logger.Append(Severity.DEBUG, "Obtained loop device "+loopPath+" for fuse device "+disk.ProxiedPath); 
						disk.ProxiedPath = loopPath;
						if(loopPath != null){
							loopNames.Add(loopPath.Substring(loopPath.LastIndexOf("/")+1));
							Console.WriteLine (" #### Added loopPath  "+loopPath.Substring(loopPath.LastIndexOf("/")));
						}
					}
					catch(Exception e){
						Logger.Append(Severity.ERROR, "Couldn't create loop device for disk '"+disk.ProxiedPath+"' : "+e.Message);
					}

						/*
						Logger.Append(Severity.DEBUG, "Disk '"+disk.ProxiedPath+"' got loop entry "+loopPath);
						// update disk proxied path, since we'll have to use instead of the original proxied path
						// wich is now useless for the rest of operations
						disk.ProxiedPath = loopPath;


					}
					else{
						Logger.Append(Severity.WARNING, "Disk '"+disk.ProxiedPath+"' could'nt got a loop device entry.");
						disk.Enabled = false;
					}*/



				}
			}

			//If we are asked to manage loop devices, call Linux discoverer  to the rescue 
			// and replace actual layout by the one we got from it
			if(loopNames.Count >0){

				lsd = new LinuxStorageDiscoverer();
				lsd.LogEvent += LogReceivedEvent;
				lsd.Initialize(loopNames, Path.Combine(Utilities.ConfigManager.GetValue("Storage.IndexPath"), "tmp"));
				StorageLayout fromLinuxDiscovererLayout = lsd.BuildStorageLayout();

				foreach(IDiskElement ldlEntry in fromLinuxDiscovererLayout.Entries){
					foreach(IDiskElement elt in layout.Entries){
						if(!(elt is Disk) || !(ldlEntry is Disk)) continue;
						Console.WriteLine ("vmware disk path="+elt.Path+", proxiedpath="+((Disk)elt).ProxiedPath+", linuxdisco path="+ldlEntry.Path+", linuxdisco proxiedpath="+((Disk)ldlEntry).ProxiedPath);
						if( ((Disk)elt).ProxiedPath == ((Disk)ldlEntry).Path){
							foreach(IDiskElement newE in ldlEntry.Children)
								elt.AddChild(newE);
						}

					}
				}
			}
			//disk.Children.AddRange(lsd.GetSysfsPartitions(disk.ProxiedPath, ""));


			// Now, we should have everything that is was possible to gather about disk layout.
			// Finish it by parsing MBR/bootmanager and compare partitions layouts
			foreach(IDiskElement elt in layout.Entries){
				if(! (elt is Disk)) continue;
				Disk d = (Disk)elt;
				if(d.BlockStream == null){
					Logger.Append(Severity.ERROR, "No access to block device '"+d.Path+"' stream, cannot finish building disk layout");
					continue;
				}
				d.MbrBytes = new byte[512];
				d.BlockStream.Read(d.MbrBytes, 0, 512);
				MBR mbr = new MBR(d.MbrBytes);
				d.Signature = mbr.DiskSignature;
				/*if(d.TreatAsLoop)
					disk.Children.AddRange(lsd.GetSysfsPartitions(disk.ProxiedPath, ""));
				else
					disk.Children.AddRange(lsd.GetSysfsPartitions(disk.Path, ""));*/
				foreach(Partition p in GetPartitionsFromMBR(d)){
					bool ofound = false;
					foreach(IDiskElement dskE in d.Children){
						//Console.WriteLine ("     EEEEEEEEEEEE  current elt is "+dskE.ToString());
						if(! (dskE is Partition)) continue;

						Partition tempP = (Partition)dskE;
						//Console.WriteLine ("     EEEEEEEEEEEE MBR says : "+p.ToString());
						//Console.WriteLine ("     EEEEEEEEEEEEinside layout.children.Children : "+tempP.ToString());
						if(tempP.Offset != p.Offset) continue;
						ofound = true;
						if(dskE.Size == 0){
							Logger.Append (Severity.TRIVIA, "added 'size' information to partition with offset "+dskE.Offset);
							dskE.Size = p.Size;
						}
						if(dskE.Size != p.Size){
							Logger.Append (Severity.WARNING, "Mismatch in 'size' information between discoverer and MBR for partition with offset "+dskE.Offset
							+", trusting MBR");
							dskE.Size = p.Size;
						}
						tempP.Type = p.Type;
						break;
					}
					if(!ofound){ // MBR partition undected by storage discoverer
						Logger.Append (Severity.TRIVIA, "Adding new partition from MBR, previously undiscovered. Partition is "+p.ToString());
						elt.AddChild(p);
					}
				}
			}

		}

		/*private List<Disk> GetDisks(List<string> rootStoragePaths){

			List<Disk> disks = new List<Disk>();
			if(rootStoragePaths == null)
				disks = GetLocalDisks();


			foreach(string loopDevice in rootStoragePaths){
				Logger.Append(Severity.DEBUG, "Discovering root block device '"+loopDevice+"'...");
				Disk disk = new Disk();
				disk.Path = loopDevice;
				MBR mbr = new MBR(disk.Path);
				if (mbr.IsValid()){
					Logger.Append(Severity.DEBUG, "Block device '"+loopDevice+"' has an MBR");

					disk.Signature = mbr.DiskSignature;
					disk.MbrSignature = mbr.MBRSignature;
					disk.MbrCode = mbr.Code;
					disk.Children.AddRange(GetPartitions(disk));
				}
				if(disk.Children == null || disk.Children.Count == 0)
					disk.Children.AddRange(GetFileSystems(disk));
				layout.Entries.Add(disk);

			}

			return disks;

		}*/



		private List<Disk> GetLocalDisks(){// separate disks enumeration (unix/win)
			return default(List<Disk>);
		}

		private static List<Partition> GetPartitionsFromMBR(IDiskElement element){
		//	foreach(IDiskElement subElts in element.Children){
				//check if there are already PartitionSchemes for this disk
				//if(
			//}
			List<Partition> partitions = new List<Partition>();
			//try to find if disk element has an MBR, and, if yes, read it and retrieve partitions
			Logger.Append(Severity.DEBUG, "Discovering block device '"+element.Path+"' partitions...");
			MBR mbr;
			if(element is Disk && ((Disk)element).MbrBytes != null){
				Logger.Append(Severity.TRIVIA, "Disc has MBR sector attached");
				mbr = new MBR(((Disk)element).MbrBytes);
			}
			else if(!string.IsNullOrEmpty(element.Path))
				mbr = new MBR(element.Path);
			else
				return null;
			
			//if (mbr.IsValid()){
				int i =0;
				foreach( IBMPartitionInformation partition in mbr.Partitions){
					if(partition.StartLBA == 0 && partition.LengthLBA == 0)
					continue; // unaffected partition
					i++;
					Partition part = new Partition();
					/*if(Node.Utilities.PlatForm.IsUnixClient())
						part.Path = "/dev/*/
					part.Bootable = partition.Bootable;
					part.Schema = PartitionSchemes.Classical;
					part.Type = partition.PartitionType;
					part.Offset = partition.StartLBA;
					part.Size = partition.LengthLBA* 512; // ????? s this correct?
					part.Parents.Add(element);
					Logger.Append(Severity.DEBUG, "Found partition, type "+part.Type.ToString()+", offset "+part.Offset+", size "+part.Size/1024/1024+"MB, bootable:"+part.Bootable);
					if(part.Type == PartitionTypes.LinuxExtended 
					   || part.Type == PartitionTypes.Microsoft_Extended
					   || part.Type == PartitionTypes.Microsoft_Extended_LBA){
						try{
							part.IsComplete = false;
							part.BlockStream = ((Disk)element).BlockStream;

							//debug

						List<Partition> extendedParts = GetExtendedPartitions(part, part.Offset, 0);
						/*foreach(Partition extP in extendedParts)
							Console.WriteLine (" EXTENDED : got "+extP.ToString());*/

							partitions.AddRange(extendedParts);
						}
						catch(Exception e){
							Logger.Append(Severity.DEBUG, "Extended partition doesn't seem to have child partitions : "+e.ToString());
						}
					}
					else if(part.Type == PartitionTypes.EFI_GPT_Disk || part.Type == PartitionTypes.EFI_System_Partition){
						Logger.Append(Severity.WARNING, "Found partitions indentifying themselves as GPT. GPT/EFI is not yet supported!");
					}
					else
						part.IsComplete = true;
					partitions.Add(part);
				}
			//}
			//else
				
			return partitions;
		}

		internal static List<Partition> GetExtendedPartitions(Partition element, ulong offset, int iterNb){
			List<Partition> extParts = new List<Partition>();
			byte[] ebrData = new byte[512];
			//Console.WriteLine ("     GetExtendedPartitions(iter="+iterNb+") 1: will try to read EBR from "+element.Path+" at offset" +(element.Offset*512));
			ulong currentDiskOffset;
			/*if(firstStart)
				currentDiskOffset =( (element.Offset) + offset);
			else
				currentDiskOffset = offset - element.Offset;*/
			//element.BlockStream.Seek((long)offset*512, SeekOrigin.Begin);
			element.BlockStream.Seek((long)element.Offset*512, SeekOrigin.Begin);
			element.BlockStream.Read(ebrData, 0, 512);
			//Console.WriteLine ("     GetExtendedPartitions() 2: Read done");

			EBR ebr = new EBR(ebrData);
			//Console.WriteLine ("     GetExtendedPartitions() : found extended part EBR ");

			Partition extPart = new Partition();
			//extPart.Offset = ebr.Partitions[0].StartLBA+currentDiskOffset;
			extPart.Offset = ebr.Partitions[0].StartLBA;

			//if(iterNb==0){
				extPart.Offset += element.Offset;

			//}
			//if(iterNb == 1)
				//extPart.Offset += element.Offset;
				// note offset for relative EBRs
				//offset = extPart.Offset;
			extPart.Size = ebr.Partitions[0].LengthLBA;
			extPart.Type = ebr.Partitions[0].PartitionType;
			extPart.Schema = PartitionSchemes.Extended;
			extPart.BlockStream = element.BlockStream;
			extParts.Add(extPart);
			Console.WriteLine (" ########## raw extpart = "+extPart.ToString());

			Partition truc = new Partition();
			truc.Offset = ebr.Partitions[1].StartLBA/*-element.Offset*/;
			if(iterNb==0)
				truc.Offset += element.Offset;

			else if(iterNb >=1 ){
				truc.Offset += offset;
			}

			truc.Size = ebr.Partitions[1].LengthLBA;
			truc.BlockStream = element.BlockStream;
			Console.WriteLine (" ########## raw nextEbr = "+truc.ToString());


			if(ebr.Partitions[1].StartLBA >0 && truc.Offset > extPart.Offset){
				//Console.WriteLine ("    *     GetExtendedPartitions()² 3:pointer to next EBR : "+ebr.Partitions[1].StartLBA*512);
				//Console.WriteLine ("    *     GetExtendedPartitions()² 4: found extended part EBR for ext part "+extPart.Offset);
				//extParts.AddRange(GetExtendedPartitions(truc, currentDiskOffset, false));
				extParts.AddRange(GetExtendedPartitions(truc, offset, iterNb+1));
			}
			return extParts;
		}

		private List<FileSystem> GetFileSystems(IDiskElement element){

			return default(List<FileSystem>);
		}

		public void Dispose(){
			Logger.Append(Severity.TRIVIA, "Disposing StorageLayoutManager, removing loop devices and disposing discoverer...");
			if(loopH != null){
				lsd.Dispose();
				foreach(IDiskElement de in this.layout.Entries){
					Disk d = (Disk)de;
					if(d.Type == DiskType.Loop){
						try{
							loopH.RemoveLoop(d.ProxiedPath);
							Logger.Append(Severity.DEBUG, "Deleted loop device '"+d.ProxiedPath+"'");
						}
						catch(Exception e){
							Logger.Append(Severity.ERROR, "Could not delete loop device '"+d.ProxiedPath+"' : "+e.Message);
						}
					}
				}

			}
			if(discoverer != null)
				discoverer.Dispose();
			layout = null;
		}
	}
}

