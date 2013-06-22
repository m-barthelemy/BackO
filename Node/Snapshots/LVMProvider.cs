using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using Node.Utilities;
using Node.StorageLayer;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;

namespace Node.Snapshots{

	public class LVMProvider:ISnapshotProvider{

		public string Name{get{return "LVM";}}
		public SPOMetadata Metadata{get;set;}
		public event EventHandler<LogEventArgs> LogEvent;

		public List<ISnapshot> ListSnapshottable(string path){
			return new List<ISnapshot>();	
		}
		
		public List<ISnapshot> ListSpecialObjects(){
			return null;
		}
		
		public LVMProvider (){
		}
		
		public Snapshot Create(SnapshotSupportedLevel level){
			
			
			return new Snapshot();
		}
		
		public Snapshot CreateWriterSnapshot(List<string> writerPaths){
			return new Snapshot();
		}
		
		public ISnapshot[] CreateVolumeSnapShot(List<FileSystem> volumes, string[] specialObjects, SnapshotSupportedLevel level){
			ArrayList snapshots = new ArrayList();
			// We need the lvm block device path, so let's use VolumeManager
			FileSystem[] lvmVolumes = FilesystemManager.GetLVMDrives();
			foreach(FileSystem volume in volumes){
				/*string blockDevice = String.Empty;
				foreach(SpecialDrive sd in lvmVolumes){
					if(volumeName == sd.MountPoint)
						blockDevice = sd.BlockDevice;
				}*/
				Snapshot sn = new Snapshot();
				sn.Type = this.Name;
				Logger.Append(Severity.DEBUG, "1/3 Snapshotting volume "+volume.MountPoint+" ("+volume.Path+")");
				string snapshotName = volume.MountPoint.Substring(1)+"__backup_DONOTREMOVE_"+DateTime.Now.ToString("yyyyMMdd-hh-mm-ss");
				string lvmParams = "lvcreate --size 1G --permission r --snapshot --name "+snapshotName+" "+volume.Path;
				ProcessStartInfo pi = new ProcessStartInfo("lvm", lvmParams);
				pi.RedirectStandardOutput = true;
				pi.RedirectStandardError = true;
				pi.UseShellExecute = false;
				sn.TimeStamp = Utilities.Utils.GetUtcUnixTime(DateTime.UtcNow);
				Process p = Process.Start(pi);
				p.WaitForExit();
				// we don't check stdout nor stderr, due to how crappy is lvm can be in its output 
				//(reports errors due to bad scanning which didn't prevent cnapshot from being created)
				//string stdOut = p.StandardOutput.ReadToEnd();
				string stdErr = p.StandardError.ReadToEnd();
				/*if(stdErr.Length >5) // something went wrong
					throw new Exception("Unable to snapshot LVM volume '"+volumeName+"'. Error : '"+stdErr.Replace(Environment.NewLine, " - ")
						+"' (std output was: '"+stdOut.Replace(Environment.NewLine, " - ")+"'). command was : 'lvm "+lvmParams+"'");
				else*/
					Logger.Append(Severity.DEBUG, "2/3 Successfully snapshotted "+volume.MountPoint+" to "+snapshotName);
				string mountPoint = ConfigManager.GetValue("Backups.TempFolder")+"/"+snapshotName+"/";
				Directory.CreateDirectory(mountPoint);
				// now mount it, the good old way
				string mountCommand = volume.Path.Substring(0,volume.Path.LastIndexOf("/")+1)+snapshotName+" "+mountPoint;
				ProcessStartInfo mountInfo = new ProcessStartInfo("mount", mountCommand);
				mountInfo.RedirectStandardOutput = true;
				mountInfo.RedirectStandardError = true;
				mountInfo.UseShellExecute = false;
				Process mount = Process.Start(mountInfo);
				mount.WaitForExit();
				string mountOut = mount.StandardOutput.ReadToEnd();
				string mountErr = mount.StandardError.ReadToEnd();
				//if(mountErr != String.Empty) // something went wrong, on success the command returns nothing
				//	throw new Exception("Unable to mount snapshot for '"+volumeName+"'. Error : '"+mountErr+"' (std output was: '"+mountOut+"', command was 'mount "+mountCommand+"')");
				//else
				FileSystem[] lvmWithSnap = FilesystemManager.GetLVMDrives();
				bool found = false;
				foreach(FileSystem theVol in lvmWithSnap){
					if(snapshotName == theVol.MountPoint){
						Logger.Append(Severity.DEBUG, "3/3 Successfully mounted "+snapshotName+" to "+mountPoint);
						found = true;
					}
				}
				if(found == false)
					throw new Exception("Unable to snapshot or mount for '"+volume.MountPoint+"'. mount Error : '"+mountErr+"', snapshot Error :"+stdErr);
				sn.Path = volume.MountPoint;
				sn.Id = Guid.Empty;
				sn.MountPoint = mountPoint;
				snapshots.Add(sn);
			}
			return (ISnapshot[])snapshots.ToArray(typeof(ISnapshot));
		}
		
		public bool IsVolumeSnapshottable(string volumeName){
			return true;
		}
		
		public void Delete(ISnapshot sn){
		}
		
		public SnapshotType Type{
			get{return SnapshotType.LVM;}
		}
		
		public SnapshotCapabilities Capabilities{
			get{return SnapshotCapabilities.Volume;}
		}
		
		public SnapshotSupportedLevel Levels{
			get{return SnapshotSupportedLevel.Full|SnapshotSupportedLevel.Incremental;}
		}

		public void Dispose(){

		}
	}
}

