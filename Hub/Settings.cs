using System;
using System.Configuration;
using P2PBackupHub.Utilities;
using P2PBackup.Common;

namespace P2PBackupHub {
	public enum DBProvider{sqlite,postgres,mysql};
	sealed class Settings : ApplicationSettingsBase{
    		[UserScopedSetting] public string ConnectionString{get;set;}
		[UserScopedSetting] public DBProvider Provider{get;set;}
		[UserScopedSetting] public Severity LogLevel{get;set;}
		
		
		
	}

}

