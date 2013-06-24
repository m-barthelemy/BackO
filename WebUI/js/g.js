 i18n = Ext.create('Ext.i18n.Bundle',{
		bundle: 'wui',
		lang: Ext.util.Cookies.get('lang'),
		path: '/i18n',
		noCache: true
	});
	
Ext.Loader.setConfig({
    enabled: true,
    disableCaching: false,
    paths: {
        'Extensible': '/Extensible/src',
        'Extensible.example': '/Extensible/examples',
        'Ext.ux':'/ext4/ux',
        'Ext.i18n':'/i18n/',
        'backo':'/js/backo'
    }
});

Ext.onReady(function () {
 	Ext.util.Observable.observe(Ext.data.Connection);

	Ext.data.Connection.on('requestexception', function(conn, response, options, eOpts) {
	    // Authomatically redirect to login page if server replies 401 (unauthorized)
	    if(response.status == 401)
	    	window.location = '/html/Auth.html?redir='+window.location.pathname+window.location.search;
	});

    
  
	
    /*if(lang != null){
		var url = '/ext4/locale/ext-lang-' + lang + '.js';
	    Ext.Ajax.request({
	        url : url,
	        success : function(response) {
	                window.eval(response.responseText);
	        },
	        failure : function() {
	                Ext.Ajax.request({ // don't know for now a better way to handle locales without '-SUFFIX' part
				        url : '/ext4/locale/ext-lang-' + lang.substring(0,2) + '.js',
				        success : function(response) {
				                window.eval(response.responseText);
				        },
				    });
	        },
	        scope : this
	     });
     }*/
     
    /*var cp = Ext.create('Ext.state.CookieProvider', {
	    path: "/",
	    expires: new Date(new Date().getTime()+(1000*60*60*24*30)), //30 days
	    domain: "localhost"
	});
	Ext.state.Manager.setProvider(cp);*/


i18n.onReady(function(){

// write local TZ
function calculate_time_zone() {
	var rightNow = new Date();
	var jan1 = new Date(rightNow.getFullYear(), 0, 1, 0, 0, 0, 0);  // jan 1st
	var june1 = new Date(rightNow.getFullYear(), 6, 1, 0, 0, 0, 0); // june 1st
	var temp = jan1.toGMTString();
	var jan2 = new Date(temp.substring(0, temp.lastIndexOf(" ")-1));
	temp = june1.toGMTString();
	var june2 = new Date(temp.substring(0, temp.lastIndexOf(" ")-1));
	var std_time_offset = (jan1 - jan2) / (1000 * 60 * 60);
	var daylight_time_offset = (june1 - june2) / (1000 * 60 * 60);
	var dst;
	if (std_time_offset == daylight_time_offset) {
		dst = "0"; // daylight savings time is NOT observed
	} else {
		// positive is southern, negative is northern hemisphere
		var hemisphere = std_time_offset - daylight_time_offset;
		if (hemisphere >= 0)
			std_time_offset = daylight_time_offset;
		dst = "1"; // daylight savings time is observed
	}
	var i;
	// check just to avoid error messages
	if (document.getElementById('timezone')) {
		for (i = 0; i < document.getElementById('timezone').options.length; i++) {
			if (document.getElementById('timezone').options[i].value == convert(std_time_offset)+","+dst) {
				document.getElementById('timezone').selectedIndex = i;
				break;
			}
		}
	}
	
}

function convert(value) {
	var hours = parseInt(value);
   	value -= parseInt(value);
	value *= 60;
	var mins = parseInt(value);
   	value -= parseInt(value);
	value *= 60;
	var secs = parseInt(value);
	var display_hours = hours;
	// handle GMT case (00:00)
	if (hours == 0) {
		display_hours = "00";
	} else if (hours > 0) {
		// add a plus sign and perhaps an extra 0
		display_hours = (hours < 10) ? "+0"+hours : "+"+hours;
	} else {
		// add an extra 0 if needed 
		display_hours = (hours > -10) ? "-0"+Math.abs(hours) : hours;
	}
	
	mins = (mins < 10) ? "0"+mins : mins;
	return display_hours+":"+mins;
}

calculate_time_zone();

Date.prototype.getMonday=function(){
  return new Date(this.getFullYear(), this.getMonth(),(1-this.getDay())+this.getDate());
}


	var viewMenu = new Ext.menu.Menu({
        id: 'viewMenu',
       /*  overflow: 'visible'     // For the Combo popup
        },*/
        //renderTo:Ext.get('menu'),
        height:190,
        floating:true,
        autoShow:false,
        items: [
        	{
            	id:'w',
                text: i18n.getMsg('menu.view.welcome'), //'Welcome page',
                icon: '/images/w-i.png',
               	iconCls: 'calendar',
               	handler: onItemClick
            },'-',
             {
             	id:'bp',
                 text: i18n.getMsg('menu.view.backupsplan'), //'Backups plan',
                 iconCls: 'calendar',
                 icon: '/images/bp.png',
                 handler: onItemClick
               
            },{
            	id:'bs',
                text: i18n.getMsg('menu.view.backupSets'), //'Backup sets',
                icon: '/images/bs.png',
               	iconCls: 'calendar',
               	handler: onItemClick
            },{
            	id:'bh',
                text: i18n.getMsg('menu.view.backupHistory'), //'Backup sets',
                icon: '/images/histcal.png',
               	iconCls: 'calendar',
               	handler: onItemClick
            },'-',{
             	 id:'clients',
                 text: i18n.getMsg('menu.view.clientNodes'), //'Client nodes',
                 icon: '/images/cl-i.png',
                 iconCls: 'calendar',
                 handler: onItemClick
               
            },{
            	id:'sn',
                text: i18n.getMsg('menu.view.storageNodes'), //'Storage nodes',
                icon: '/images/sg-i.png',
                iconCls: 'calendar',
                handler: onItemClick
            },
        ]
    });
    
    var hubMenu = new Ext.menu.Menu({
        id: 'hubMenu',
        height:140,
        items: [
             {
            	id:'hubLogs',
                text: i18n.getMsg('menu.hub.logs'), //'View logs',
                icon:'/images/logs-i.png',
                iconCls: 'calendar',
                handler: onItemClick
                
            },'-',{
            	id:'hubConf',
                text: i18n.getMsg('menu.hub.configuration'), //'Configuration',
               	iconCls: 'calendar',
               	icon:'/images/logout.png',
               	handler: onItemClick
            }
        ]
    });
    
    var addMenu = new Ext.menu.Menu({
        id: 'addMenu',
        height:190,
        enableOverflow:false,
        items: [
             {
            	id:'addBs',
                text: i18n.getMsg('menu.add.backupset'), //'New Backup set',
                icon:'/images/bs.png',
                iconCls: 'calendar',
                handler: onItemClick
                
            },{
            	id:'addTpl',
                text: i18n.getMsg('menu.add.bsTemplate'), //'New Backup template',
               	iconCls: 'calendar',
               	icon:'/images/sg-i.png',
               	handler: onItemClick
            
            },{
            	id:'addSg',
                text: i18n.getMsg('menu.add.storageGroup'), //'new Storage group',
               	iconCls: 'calendar',
               	icon:'/images/sg-i.png',
               	handler: onItemClick
            },{
            	id:'addHv',
                text: i18n.getMsg('menu.add.hypervisor'), //'new Storage group',
               	iconCls: 'calendar',
               	icon:'/images/sg-i.png',
               	handler: onItemClick
            }
        ]
    });
    
    var sysMenu = new Ext.menu.Menu({
        id: 'sysMenu',
        height:150,
        items: [
             {
             	// id:'Hub',
                 text: i18n.getMsg('menu.sys.hub'), //'Hub...',
                 icon:'/images/hub-i.png',
                 iconCls: 'calendar',
                 menu:hubMenu
            }
            ,{
            	id:'Users',
                text: i18n.getMsg('menu.sys.users'), //'Users...',
                icon:'/images/users.png',
                iconCls: 'calendar',
                handler: onItemClick
            },{
            	id:'Stats',
                text: i18n.getMsg('menu.sys.stats'), //'View statistics',
               	iconCls: 'calendar',
               	handler: onItemClick
            }
        ]
    });
    
    var meMenu = new Ext.menu.Menu({
        id: 'meMenu',
        height:150,
        floating:true,
        autoShow:false,
        items: [
             {
            	id: 'Preferences',
                text: i18n.getMsg('menu.me.prefs'),
                icon:'/images/prefs-i.png',
                iconCls: 'calendar',
                handler: onItemClick
                
            },'-',{
            	id:'Logout',
                text: i18n.getMsg('menu.me.logout'),
               	iconCls: 'calendar',
               	icon:'/images/logout.png',
               	handler: onItemClick
            }
        ]
    });
    
    
   function onItemClick(item){
        if(item.id == 'clients')
        	url = '/html/Clients4.html';
        else if(item.id == 'sn')
        	url = '/html/StorageNodes4.html';
        else if(item.id == 'bs')
        	url = '/html/Tasks.html';
        else if(item.id == 'w')
        	url = '/html/Welcome.html';
        else if(item.id == 'bp')
        	url = '/html/TimeLine4.html';
        else if(item.id == 'bh')
        	url = '/html/BackupHistory.html';
        else if(item.id == 'logout')
        	url = '/html/Default.aspx?action=logout';
        else if(item.id == 'hubConf')
        	url = '/html/HubConf4.html';
        else if(item.id == 'hubLogs')
        	url = '/html/HubLogs.html';
        else if(item.id == 'addBs')
        	url = '/html/AddBackupSet4.html';
        else if(item.id == 'addSg')
        	url = '/html/AddStorageGroup.html';
        else if(item.id == 'addHv')
        	url = '/html/AddHypervisor.html';
        else if(item.id == 'restore')
        	url = '/html/Restore.html';
        else if(item.id == 'Users')
        	url = '/html/Users.html';
        else if(item.id == 'Stats')
        	url = '/GlobalStats.html';
        else if(item.id == 'Logout'){
        	url = '/html/Auth.html?logout=true'; 
        	window.location.href = url;
        }
        var urlParam = Ext.urlDecode(window.location.search.substring(1));
		var language;
	    if(urlParam.lang)
	        language = urlParam.lang;
        window.location.href = url+"?lang="+language;
    }
    
    
   var makeSearchTypes = new Ext.data.ArrayStore({
        fields: ['stype'],
        data : ['Nodes','Backup sets','Storage nodes', 'IP']
    });
    
    
	var tb = new Ext.Toolbar({
	renderTo: Ext.get('menu'),
	floating:false,
	enableOverflow:false,
	layout:'hbox',
	//border:2,
 	items: [
		{
			text: i18n.getMsg('menu.tb.view'), //'&nbsp;&nbsp;View',
			iconCls: 'icon-chart',
			icon:'/images/view.png',
			layout:'fit',
			menu:viewMenu 
		}, '-',
		{
			text: i18n.getMsg('menu.tb.manage'), //'&nbsp;&nbsp;Add',
			iconCls: 'icon-chart',
			icon:'/images/add.png',
			layout:'fit',
			menu:addMenu
		}, '-',
		{	text: '<b>'+i18n.getMsg('menu.tb.restore')+'</b>', //'&nbsp;&nbsp;<b>Restore</b>',
			iconCls: 'icon-chart',
			icon:'/images/restore.png',
			id: 'restore',
			handler: onItemClick
		}, '-',
		{
			text: i18n.getMsg('menu.tb.system'), //'&nbsp;&nbsp;System',
			iconCls: 'icon-chart',
			icon:'/images/system.png',
			menu:sysMenu 
		}, '-',
		{
			text: i18n.getMsg('generic.search'), //'&nbsp;&nbsp;Search :',
			iconCls: 'icon-chart',
			icon:'/images/search.png',
		}, '',
		{
			xtype: 'combo',
			store: makeSearchTypes,
			displayField: 'stype',
			typeAhead: true,
			mode: 'local',
			triggerAction: 'all',
			emptyText: 'enter any search',
			selectOnFocus: true,
			width: 135
		}, '-',
		{
			text: i18n.getMsg('menu.tb.me'), //'&nbsp;&nbsp;Me',
			iconCls: 'icon-chart',
			icon:'/images/me.png',
			menu:meMenu 
		}
	]
	});

	tb.doLayout();

    
   
	Ext.state.Manager.setProvider(new Ext.state.CookieProvider({
	    expires: new Date(new Date().getTime()+(1000*60*60*24*60)), //2 months from now
	}));


  FormatSize = function(val){
	if(val == null || val == '' || isNaN(val)) return '';
	if(val >= 1024 * 1024 *1024 * 1024)
		return Math.round(val/1024/1024/1024/1024*10)/10 +' TB';
	else if (val > 1024 * 1024 *1024)
		return Math.round(val/1024/1024/1024*10)/10 +' GB';
	else if (val > 1024 * 1024)
		return Math.round(val/1024/1024*10)/10 +' MB';
	else
		return Math.round(val/1024*10)/10+' KB';
  };

  
  
  // ***models definitions***
   	
  StorageGroup = Ext.define('StorageGroup', {
    extend: 'Ext.data.Model',
    idProperty: 'Id',
    fields: [
    	{name: 'Id',     		type: 'int'},
    	{name: 'Name',     		type: 'string'},
    	{name: 'Description',   type: 'string'},
    	{name: 'Storage',     	type: 'number'},
    	{name: 'OnlineStorage',	type: 'number'},
    	{name: 'OfflineStorage',type: 'number'},
    	{name: 'Capabilities',  type: 'int'},
    ],
    proxy: {
        type: 'rest',
        url : '/api/StorageGroups/',
        reader:{
        	type:'json',
        	applyDefaults: true
        }
    }
  });
  
  NodeConfig = Ext.define('NodeConfig', {
	   	extend: 'Ext.data.Model',
	   	fields:[
	   		{name: 'LogLevel',     		type: 'string'},
	   		{name: 'LogToSyslog',     	type: 'boolean'},
	   		{name: 'LogFile',     		type: 'string'},
	   		{name: 'StoragePath',     	type: 'string'},
	   		{name: 'StorageSize',     	type: 'number'},
	   		{name: 'IndexPath',     	type: 'string'},
	   		{name: 'ListenIP',     		type: 'string'},
	   		{name: 'ListenPort',     	type: 'number'}
	   	]
  });
   
  Plugin = Ext.define('Plugin', {
  	extend: 'Ext.data.Model',
    idProperty: 'Name',
    fields: [
        //{name: 'Id',     		type: 'int'},
        {name: 'Name',     		type: 'string'},
        {name: 'Version',     	type: 'string'},
        {name: 'Enabled',     	type: 'boolean'},
        {name: 'Category',    	type: 'string'},
        {name: 'IsProxyingPlugin',type: 'boolean'},
  	]
  });
  				
  Node = Ext.define('Node', {
    extend: 'Ext.data.Model',
    idProperty: 'Id',
    fields: [
        {name: 'Id',     		type: 'int'},
        {name: 'Name',     		type: 'string'},
        {name: 'Description',  	type: 'string'},
        {name: 'HostName',     	type: 'string'},
        {name: 'Kind',     		type: 'string'},
        {name: 'Hypervisor',    type: 'int'},
        {name: 'CertCN',     	type: 'string', defaultValue:''},
        {name: 'IP', 			type: 'string'},
        {name: 'StorageUsed', 	type: 'number'},
        {name: 'Version', 		type: 'string'},
        {name: 'OS', 			type: 'string'},
        {name: 'Quota', 		type: 'number'},
        {name: 'UsedQuota', 	type: 'number'},
        {name: 'Port', 			type: 'string'},
        {name: 'BackupSets', 	type: 'string'},
        {name: 'Group', 		type: 'number'},
        {name: 'StorageGroup', 	type: 'number'},
        {name: 'StoragePriority',type: 'int'},
        {name: 'CertificateStatus',type: 'string'},
        {name: 'Description', 	type: 'string'},
        {name: 'Locked', 		type: 'boolean'},
        {name: 'Status', 		type: 'string'},
        {name: 'CreationDate',	type: 'date'},
        {name: 'LastConnection',type: 'date'},
        {name: 'Configuration',	persist: true},
        // Node's Client-side configuration:
        {name: 'LogLevel',     	type: 'string', mapping: 'Configuration.LogLevel'},
        {name: 'LogFile',    	type: 'string', mapping: 'Configuration.LogFile'},
        {name: 'LogToSyslog',   type: 'boolean',mapping: 'Configuration.LogToSyslog'},
   		{name: 'IndexPath',     type: 'string', mapping: 'Configuration.IndexPath'},
   		{name: 'StoragePath',   type: 'string', mapping: 'Configuration.StoragePath'},
   		{name: 'StorageSize',   type: 'number', mapping: 'Configuration.StorageSize'},
   		{name: 'ListenIP',     	type: 'string', mapping: 'Configuration.ListenIP'},
   		{name: 'ListenPort',    type: 'number', mapping: 'Configuration.ListenPort', maxValue: 65535}
    ],
    hasOne: [{ 
    	model			: 'NodeConfig', 
    	name			: 'Configuration', 
    	associationKey	: 'Configuration' ,
    	getterName		: 'getConfiguration', // avoid dots in function name
        setterName		: 'setConfiguration' // avoid dots in function name
    }],
    hasMany: [
 		{ 
	    	model: 'Plugin', 
	    	name: 'Plugins', 
	    	associationKey: 'Plugins' ,
	    	//foreignKey    : 'bsid',
    	}
    ],
    save: function() { 
        var me = this; 
       // if (me.persistAssociations) { 
            //me.set( me.getAssociatedData() ); 
            console.log('associated data : '+me.get('LogLevel')+' --- '+me.getConfiguration().get('LogLevel'));
            me.getConfiguration().set('LogLevel', me.get('LogLevel'));
            me.getConfiguration().set('LogFile', me.get('LogFile'));
            me.getConfiguration().set('LogToSyslog', me.get('LogToSyslog'));
            me.getConfiguration().set('IndexPath', me.get('IndexPath'));
            me.getConfiguration().set('StoragePath', me.get('StoragePath'));
            me.getConfiguration().set('StorageSize', me.get('StorageSize'));
            me.getConfiguration().set('ListenIP', me.get('ListenIP'));
            me.getConfiguration().set('ListenPort', me.get('ListenPort'));
        //} 
        me.callParent(arguments); 
    } ,
   	proxy: {
        type: 'rest',
        url : '/api/Node/'
    }
     
 });
 
NodeGroup = Ext.define('NodeGroup', {
    extend: 'Ext.data.Model',
    idProperty: 'Id',
    fields: [
    	{name: 'Id',     		type: 'int'},
    	{name: 'Name',     		type: 'string'},
    	{name: 'Description',   type: 'string'},
    	
    ],
    proxy: {
        type: 'rest',
        url : '/api/NodeGroups/',
        reader:{
        	type:'json',
        	applyDefaults: true
        }
    }
 });
  
 Ext.define('custom.writer.JsonExtended', {
    extend: 'Ext.data.writer.Json',
    alias: 'writer.json-custom-writer-extended',
    constructor: function(config) {
        this.callParent(arguments);
    },
    getRecordData: function (record, operation) {
        record.data = this.callParent(arguments);
        Ext.apply(record.data, record.getAssociatedData());
        return record.data;
    }
});
    
 BasePath = Ext.define('BasePath', {
    extend: 'Ext.data.Model',
    idProperty: 'Path',
    fields: [
    	{name: 'Id',     		type: 'number'},
        {name: 'Path',     		type: 'string'},
        {name: 'IncludePolicy',	type: 'string'},
        {name: 'ExcludePolicy',	type: 'string'},
        {name: 'Recursive',		type: 'boolean'},
        {name: 'Type',			type: 'string'},
        {name: 'bsid', 			type: 'int'} // not real field, fake pk
 	]
  });
 
 ScheduleTime = Ext.define('ScheduleTime', {
    extend: 'Ext.data.Model',
    //idProperty: 'Id',
    fields: [
    	{name: 'Day',     		type: 'string'},
        {name: 'Level',     	type: 'string'},
        {name: 'BeginHour',		type: 'int'},
        {name: 'BeginMinute',	type: 'int'},
        {name: 'EndHour',		type: 'int'},
        {name: 'EndMinute',		type: 'int'},
 	]
 });
 
 Hypervisor = Ext.define('Hypervisor', {
    extend: 'Ext.data.Model',
    idProperty: 'Id',
    fields: [
    	{name: 'Id',     		type: 'int'},
        {name: 'Name',     		type: 'string'},
        {name: 'Kind',			type: 'string'},
        {name: 'Url',			type: 'string'},
        {name: 'UserName',		type: 'string'},
        {name: 'Password',		type: 'string'},
        {name: 'PasswordId',	type: 'int'},
        {name: 'LastDiscover',	type: 'date'},
 	],
 	proxy: {
        type: 'rest',
        url : '/api/Hypervisors/',
        reader:{
        	type:'json',
        	applyDefaults: true
        }
    }
 });
 
 
 //Data processing flags enum
 dataFlags = {
 	None:0,
 	CCompress:1,
 	CEncrypt:2,
 	CDedup:4,
 	CReplicate:8,
 	CChecksum:16,
 	SCompress:512,
 	SEncrypt:1024,
 	SDedup:2048,
 	SReplicate:4096,
 	HybridDedup:16384
 };

 Ext.define('User', {
    extend: 'Ext.data.Model',
    idProperty: 'Id',
    fields: [
        {name: 'Id',     		type: 'int'},
        {name: 'Login',     	type: 'string'},
        {name: 'Name',     		type: 'string'},
        {name: 'Password', 		type: 'string'},
        {name: 'PasswordId', 	type: 'int'},
        {name: 'Email', 		type: 'string'},
        {name: 'Culture', 		type: 'string'},
        {name: 'Language', 		type: 'string'},
        {name: 'IsEnabled', 	type: 'boolean'},
        {name: 'LastLoginDate', type: 'date', defaultValue:'Never'},
        {name: 'Roles', 		persist: true},
    ],
    validations:[
 		{type: 'length', field: 'Login', min:3, message: 'Login must be at least 3 characters'}
 	],
 });
	
 BackupSet = Ext.define('BackupSet', {
    extend: 'Ext.data.Model',
    idProperty: 'Id',
    fields: [
        {name: 'Id',     			type: 'int'},
        {name: 'Name',     			type: 'string'},
        {name: 'Inherits',     		type: 'int'},
        {name: 'Enabled',  	   		type: 'boolean'},
        {name: 'IsTemplate',	    type: 'boolean'},
        {name: 'DataFlags',     	type: 'int'},
        {name: 'StorageLayoutProvider',type: 'string'},
        {name: 'HandledBy',     	type: 'int'},
        {name: 'MaxChunkFiles',		type: 'numeric'},
        {name: 'MaxChunkSize', 		type: 'numeric'},
        {name: 'MaxPackSize',  		type: 'numeric'},
        {name: 'NodeId',     		type: 'int'},
        {name: 'Operation',    		type: 'string'},
        {name: 'Preop',     		type: 'string'},
        {name: 'Postop',     		type: 'string'},
        {name: 'RetentionDays', 	type: 'int'},
        {name: 'SnapshotRetention',	type: 'int'},
        {name: 'Parallelism',		type: 'string'},
        {name: 'StorageGroup',  	type: 'int'},
        {name: 'BasePaths', 		persist: true},// declare associated basepath as json string (extjs4 unable to save nested record, shame on it)
        {name: 'ScheduleTimes',		persist: true}
 	],
 	hasMany: [
 		{ 
	    	model: 'BasePath', 
	    	name: 'BasePaths', 
	    	associationKey: 'BasePaths' ,
	    	foreignKey    : 'bsid',
    	},{ 
	    	model: 'ScheduleTime', 
	    	name: 'ScheduleTimes', 
	    	associationKey: 'ScheduleTimes' ,
	    	//getterName: 'getConfiguration', // avoid dots in function name
	        //setterName: 'setConfiguration' // avoid dots in function name
    	}
    ],
    proxy: {
        type: 'rest',
        url: '/api/BackupSet/',
        extraParams: {format: 'json'},
        reader:{
        	type:'json',
        	applyDefaults: true
        },
	    writer: { 
	    	type: 'json',
            //type: 'json-custom-writer-extended',
            writeAllFields: true,
        }
    },
 });
 
 LogEntry = Ext.define('LogEntry',{
 	extend: 'Ext.data.Model',
    fields: [
        {name: 'Date',     		type: 'date'},
        {name: 'Code',     		type: 'int'},
        {name: 'Message1',     	type: 'string'},
        {name: 'Message2',     	type: 'string'},
    ]
 });
 
 Task = Ext.define('Task', {
    extend: 'Ext.data.Model',
    fields: [
        {name: 'Id',     		type: 'number'},
        {name: 'Type',     		type: 'string'},
        {name: 'UserId',     	type: 'number'},
        {name: 'Operation', 	type: 'string'},
        {name: 'RunStatus', 	type: 'string'}, //started, pendingstart, done...
        {name: 'Status', 		type: 'string'}, // ok, warning, error...
        {name: 'Priority', 		type: 'string'},
        {name: 'BackupSetId', 	type: 'int'},
        {name: 'BsName', 		type: 'string'},
        {name: 'Level', 		type: 'string'},
        {name: 'Flags', 		type: 'string'},
        {name: 'Parallelism', 	type: 'number'},
        {name: 'NodeId', 		type: 'number'},
        {name: 'StartDate', 	type: 'date'},
        {name: 'ElapsedTime', 	type: 'number'},
        {name: 'EndDate', 		type: 'date'},
       	{name: 'Percent', 		type: 'number'},
       	{name: 'OriginalSize', 	type: 'number'},
       	{name: 'FinalSize', 	type: 'number'},
       	{name: 'TotalItems', 	type: 'number'},
       	{name: 'CurrentAction', type: 'string'},
       	{name: 'LogEntries'/*, type: 'string'*/},
       //	{name: 'cls', type: 'string', defaultValue:'gridCell'},
    ],
    //hasMany:{model:'LogEntry', name:'LogEntries', associationKey:'LogEntries'}
  });
 
	BrowseNode = Ext.define('BrowseNode', {
	    extend: 'Ext.data.Model',
	    idProperty:'CPath',
	    fields: [
    		{name: 'Name', 		type: 'string'},
        	{name: 'Type', 		type: 'string'},
        	{name: 'Label', 	type: 'string'},
        	{name: 'FS', 		type: 'string'},
        	{name: 'Snap', 		type: 'string'},
        	{name: 'Size', 		type: 'number'},
        	{name: 'Avail', 	type: 'number'},
        	
        	{name: 'CPath', 	type: 'string'}, // dynamically built, not retrieved from server
    	],
    });
    
    BrowseIdx = Ext.define('BrowseIdx', {
	    extend: 'Ext.data.Model',
	    idProperty:'Id',
	    fields: [
	    	{name: 'Id', 		type: 'number'},
    		{name: 'Name', 		type: 'string'},
        	{name: 'Type', 		type: 'string'},
        	{name: 'Label', 	type: 'string'},
        	{name: 'FS', 		type: 'string'},
        	{name: 'Snap', 		type: 'string'},
        	{name: 'Size', 		type: 'number'},
        	{name: 'Avail', 	type: 'number'},
        	
        	{name: 'CPath', 	type: 'string'}, // dynamically built, not retrieved from server
    	],
    });

});
});