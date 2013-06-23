Ext.onReady(function () {	

	/*Ext.Loader.setConfig({
        enabled: true,
        disableCaching: false,
        paths: {
            'Extensible': '/Extensible/src',
            'Ext.ux':'/js/ext4/ux'
        }
    });*/
    Ext.require([
	 	'Ext.data.proxy.Rest',
	    'Ext.data.*',
	    'Ext.grid.*',
	    'Ext.tree.*',
	    'Ext.form.*',
	    'Ext.window.*',
	    // 'Ext.ux.BoxSelect',
	    'Ext.ux.CheckColumn',
	   // 'Ext.ux.RowExpander',
	    'Extensible.calendar.data.MemoryCalendarStore',
	    'Extensible.calendar.data.EventModel',
	    'Extensible.calendar.data.EventStore',
	    'Extensible.calendar.CalendarPanel',
	    'Extensible.calendar.data.*',
	    'Extensible.calendar.*'
	]);
	
	
		
	var params = Ext.urlDecode(window.location.search.substring(1));
	var bs, paramNode;
    if(params.bs)
    	bs = params.bs;
    if(params.node)
    	paramNode = params.node;
    	
    
    console.debug('ready before i18n');
	var i18n = Ext.create('Ext.i18n.Bundle',{
		bundle: 'wui',
		lang: Ext.util.Cookies.get('lang'),
		path: '/i18n',
		noCache: false
	});
	
i18n.onReady(function(){

	var schedStore = new Extensible.calendar.data.MemoryEventStore();
    
	console.debug('ready');
	Ext.get('addTitle').dom.innerText = i18n.getMsg('addbs.title');
	Ext.QuickTips.init();
	Ext.tip.QuickTipManager.init(true, {maxWidth: 450,minWidth: 150, width:350 });
	Ext.apply(Ext.tip.QuickTipManager.getQuickTip(), {maxWidth: 450, minWidth: 150});
    Ext.form.Field.prototype.msgTarget = 'side';
    
    var nodesChecked = [], pathsChecked = [];
    var preCheckedNode = -1000;
    var preCheckedProxy = 0;
    
    var pluginsStore = Ext.create('Ext.data.Store', {
    	storeId:'pluginsStore',
    	autoLoad:true,
        model: 'Plugin',
        //fields: ['Key','Value'],
        proxy: {
            type: 'ajax',
            url: '/api/Misc/Plugins/IStorageDiscoverer/',
            extraParams: {format: 'json'},
            reader:{
	        	type:'json',
	        	applyDefaults: true
	        }
        }
    });
       
	
    var nStore = new Ext.data.TreeStore( {
        model: 'Node',
        autoLoad: true,
        proxy: {
            type: 'ajax',
            url: '/api/Nodes',
            extraParams: {format: 'json'},
            reader:{
	        	type:'json',
	        	applyDefaults: true
	        }
        },
        root:{expanded: false},
        listeners:{
	    	load:function( thisObj, node, records, successful, eOpts ){
	    		Ext.each(records, function (rec){
					rec.set('leaf', rec.get('Group') != -1);
					if(rec.get('Group') != -1){
						rec.set('checked', preCheckedNode == rec.get('Id') );
						// set online/offline status icon
						if(rec.get('Status') == 'Idle' || rec.get('Status') == 'Backuping' || rec.get('Status') == 'Restoring')
							rec.set('iconCls','node-on');
						else if(rec.get('Status') == 'Error')
							rec.set('iconCls','node-err');
						else
							rec.set('iconCls','node-off');
					}
					else{
						rec.set('Status','');
						rec.set('LastConnection','');
					}
				});
	    	}
   	 	}
    });
    
	var proxiesStore = new Ext.data.TreeStore( {
        model: 'Node',
        //autoLoad: false,
        proxy: {
            type: 'ajax',
            url: '/api/Nodes/Plugin/local',
            extraParams: {format: 'json'},
            reader:{
	        	type:'json',
	        	applyDefaults: true
	        }
        },
        root:{expanded: false},
        listeners:{
	    	load:function( thisObj, node, records, successful, eOpts ){
	    		Ext.each(records, function (rec){
					rec.set('leaf', rec.get('Group') != -1);
					if(rec.get('Group') != -1){
						rec.set('checked', preCheckedProxy == rec.get('Id') );
						// set online/offline status icon
						if(rec.get('Status') == 'Idle' || rec.get('Status') == 'Backuping' || rec.get('Status') == 'Restoring')
							rec.set('iconCls','node-on');
						else if(rec.get('Status') == 'Error')
							rec.set('iconCls','node-err');
						else
							rec.set('iconCls','node-off');
					}
					else{
						rec.set('Status','');
						rec.set('LastConnection','');
					}
				});
	    	}
   	 	}
    });
    
	var sgStore = Ext.create('Ext.data.Store', {
    	storeId:'sgStore',
    	autoLoad:true,
        model: 'StorageGroup',
        proxy: {
            type: 'ajax',
            url: '/api/StorageGroups',
            extraParams: {format: 'json'},
			reader:{
	        	type:'json',
	        	applyDefaults: true
	        }
        }
    });
    
     var prepareBsEdit = function(bsid){
    
    	var bsMgr = Ext.ModelManager.getModel('BackupSet');
		
		BackupSet.load(bsid, {
			scope: this, 
	    	success: function(backupset) {
	    		currentBackupSet = backupset;
	    		console.debug('loaded BackupSet #'+backupset.get('Id'));
				targetPanel.getForm().loadRecord(backupset);
                fsPanel.getForm().loadRecord(backupset);
                opsPanel.getForm().loadRecord(backupset);
                stoPanel.getForm().loadRecord(backupset);
                pathsStore.add(backupset.get('BasePaths'));
                preCheckedNode = backupset.get('NodeId');
                preCheckedProxy = backupset.get('HandledBy');
                // extract data flags
                var flags = backupset.get('DataFlags');
            	Ext.getCmp('fcc').setValue((flags & dataFlags.CCompress) == dataFlags.CCompress);
            	Ext.getCmp('fcs').setValue((flags & dataFlags.SCompress) == dataFlags.SCompress);
            	Ext.getCmp('fec').setValue((flags & dataFlags.CEncrypt) == dataFlags.CEncrypt);
            	Ext.getCmp('fes').setValue((flags & dataFlags.SEncrypt) == dataFlags.SEncrypt);
            	Ext.getCmp('fdc').setValue((flags & dataFlags.CDedup) == dataFlags.CDedup);
            	Ext.getCmp('fds').setValue((flags & dataFlags.SDedup) == dataFlags.SDedup);
                	
                // extract scheduling info and convert it to calendar entries
                //Extjs4 is total crap at managing child/nested records 
                var data = backupset.data;
                for (i = 0; i < backupset.associations.length; i++) {
		            var association = backupset.associations.get(i);
		            if(association.name != 'ScheduleTimes')
		            	continue;
		            data[association.name] = null;
		            childStore = backupset[association.storeName];

		            childStore.each(function(childRecord) {
						console.debug('loaded association "'+association.name+'" : '+childRecord.get('Day'));
						var newId = Math.floor(Math.random()*1001);
						var daysToAdd = 0;
						switch(childRecord.get('Day')){
							case 'Monday':
								daysToAdd = 0;
								break;
							case 'Tuesday':
								daysToAdd = 1;
								break;
							case 'Wednesday':
								daysToAdd = 2;
								break;
							case 'Thursday':
								daysToAdd = 3;
								break;
							case 'Friday':
								daysToAdd = 4;
								break;
							case 'Saturday':
								daysToAdd = 5;
								break;
							case 'Sunday':
								daysToAdd = 6;
								break;
							
							default: break;
						}
						var startDate = new Date();
						var monDate = new Date().getMonday();
						
						monDate.setDate(monDate.getDate()+daysToAdd);
						monDate.setHours(childRecord.get('BeginHour'));
						monDate.setMinutes(childRecord.get('BeginMinute'));
						
						var endDate = new Date().getMonday();
						if(childRecord.get('EndHour') == -1){ // if end date (end backup window) has not been set
							endDate = startDate;
						}
						else {
							if(childRecord.get('EndHour') < childRecord.get('EndHour'))
								daysToAdd++;
							endDate.setDate(endDate.getDate()+daysToAdd);
							endDate.setHours(childRecord.get('EndHour'));
							endDate.setMinutes(childRecord.get('EndMinute'));
						}
						console.debug('Monday='+new Date().getMonday()+', startDate='+monDate+', endDate= '+endDate);
						schedStore.add({
							EventId:newId, 
							CalendarId:1, 
							StartDate:monDate, 
							EndDate:endDate, 
							Title:childRecord.get('Level')
						});
					});
				}	
               	return backupset;
			},
			failure: function(response) {
				alert('error loading backupset for editing: '+response);
				return null;
			}
		 });
		
    }
    
    var nodesTree = new Ext.tree.Panel({
        id:'nodesTree',
        margin:'0 15 10 10',
        padding: '0 0 10 0',
        height: 340,
        folderSort: true,
        width:500,
        collapsible: false,
        useArrows: true,
        rootVisible: false,
        store: nStore,
        multiSelect: true,
        singleExpand: false,
        draggable:false,    
        stateful:false,   
        scroll: 'vertical',
        columns: [{
            xtype: 'treecolumn',
            text: i18n.getMsg('nodestree.node'),
            flex: 1,
            //locked: true, // getChecked() doesn't work anymore with locked column (????)
            dataIndex: 'Name',
            renderer: function(value, metaData, record, colIndex, store, view){
	            if(record.get('CertCN').length > 1)
	            	return value+" (<i>"+record.get('CertCN')+"</i>)";
	            else
	            	return value;
            }
        },{
            text: i18n.getMsg('nodestree.currentIP'),
            flex: 0,
            width:90,
            dataIndex: 'IP'
        },{
            text: i18n.getMsg('nodestree.os'),
            flex: 0,
            dataIndex: 'OS',
            width:40,
            renderer:function(value){
            	if(value.toLowerCase() == 'linux')
            		return '<img src="/images/Linux-xs.png" title="'+value+'"/>';
            	else if(value.substr(0,2) == 'NT')
            		return '<img src="/images/Windows-xs.png" title="'+value+'"/>';
            	else if(value.toLowerCase() == 'freebsd')
            		return '<img src="/images/Freebsd-xs.jpg" title="'+value+'"/>';
            	else if(value.toLowerCase() == 'darwin')
            		return '<img src="/images/Apple-xs.png" title="'+value+'"/>';
            	else if(value.toLowerCase() == 'sunos')
            		return '<img src="/images/Sunos-xs.jpg" title="'+value+'"/>';
            	else if(value.length > 1)
            		return '<img src="/images/Unknown-xs.png" title="Unknown os : '+value+'"/>';
            }
        },{
            text: i18n.getMsg('generic.kind'),
            flex: 0,
            dataIndex: 'Kind',
            width:70,
            renderer: function(value, metaData, record, colIndex, store, view){
            	if(record.get('Group') != -1)
            		return i18n.getMsg('generic.kind.'+value);
            }
        },{
            text: i18n.getMsg('nodestree.quota'),
            flex: 0,
            dataIndex: 'Quota',
            width:60,
            sortable: true,
            renderer:function(value){return FormatSize(value);}
        },{
            text: i18n.getMsg('nodestree.usedQuota'),
            flex: 0,
            width:70,
            dataIndex: 'UsedQuota',
            renderer:function(value){return FormatSize(value);}
        }
        ],
        listeners:{
        	'checkchange': function(node, checked){        	
		       	if (nodesTree.getChecked().length == 1 && nodesTree.getChecked()[0].get('Status') != 'Offline')
		       		Ext.getCmp('browseBtn').enable();
	    		else
	    			Ext.getCmp('browseBtn').disable();
          	}
        }
    });				 
    
    var proxyNodesTree = new Ext.tree.Panel({
        id:'proxyNodesTree',
        margin:'0 15 10 10',
        padding: '0 0 10 0',
        height: 340,
        folderSort: true,
        width:500,
        collapsible: false,
        collapsed: true,
        useArrows: true,
        rootVisible: false,
        store: proxiesStore,
        multiSelect: true,
        singleExpand: false,
        draggable:false,    
        stateful:false,   
        scroll: 'vertical',
        disabled: true,
        columns: [{
            xtype: 'treecolumn',
            text: i18n.getMsg('nodestree.node'),
            flex: 1,
            dataIndex: 'Name',
            renderer: function(value, metaData, record, colIndex, store, view){
	            if(record.get('CertCN').length > 1)
	            	return value+" (<i>"+record.get('CertCN')+"</i>)";
	            else
	            	return value;
            }
        },{
            text: i18n.getMsg('nodestree.currentIP'),
            flex: 0,
            width:90,
            dataIndex: 'IP'
        },{
            text: i18n.getMsg('nodestree.os'),
            flex: 0,
            dataIndex: 'OS',
            width:40,
            renderer:function(value){
            	if(value.toLowerCase() == 'linux')
            		return '<img src="/images/Linux-xs.png" title="'+value+'"/>';
            	else if(value.substr(0,2) == 'NT')
            		return '<img src="/images/Windows-xs.png" title="'+value+'"/>';
            	else if(value.toLowerCase() == 'freebsd')
            		return '<img src="/images/Freebsd-xs.jpg" title="'+value+'"/>';
            	else if(value.toLowerCase() == 'darwin')
            		return '<img src="/images/Apple-xs.png" title="'+value+'"/>';
            	else if(value.toLowerCase() == 'sunos')
            		return '<img src="/images/Sunos-xs.jpg" title="'+value+'"/>';
            	else if(value.length > 1)
            		return '<img src="/images/Unknown-xs.png" title="Unknown os : '+value+'"/>';
            }
        },{
            text: i18n.getMsg('nodestree.kind'),
            flex: 0,
            dataIndex: 'Kind',
            width:70,
            renderer: function(value, metaData, record, colIndex, store, view){
            	if(record.get('Group') != -1)
            		return i18n.getMsg('nodestree.kind.'+value);
            }
        },{
            text: i18n.getMsg('nodestree.quota'),
            flex: 0,
            dataIndex: 'Quota',
            width:60,
            sortable: true,
            renderer:function(value){return FormatSize(value);}
        },{
            text: i18n.getMsg('nodestree.usedQuota'),
            flex: 0,
            width:70,
            dataIndex: 'UsedQuota',
            renderer:function(value){return FormatSize(value);}
        }
        ],
        listeners:{
        	'checkchange': function(node, checked){        	
		       	if (nodesTree.getChecked().length == 1 && nodesTree.getChecked()[0].get('Status') != 'Offline')
		       		Ext.getCmp('browseBtn').enable();
	    		else
	    			Ext.getCmp('browseBtn').disable();
          	}
        }
    });	
    			 
    var targetPanel = new Ext.widget('form', { //new Ext.form.Panel({
    	id:'targetPanel',
    	model: 'BackupSet',
        title: '<img src="/images/1.png" class="gIcon"/>'+i18n.getMsg('addbs.nameAndTargets'),
        bodyStyle: 'padding:5px;',
        waitMsgTarget: true,
		fieldDefaults: {labelWidth:140},
	    defaults: {
	        labelAlign: 'left',
	        hideLabel: false
	    },
        items: [
        	{
        		xtype:'textfield',
        		fieldLabel:i18n.getMsg('addbs.newBSName'),
                name: 'Name',
                labelWidth: 220,
                width:450
            },{
            	xtype:'checkbox',
            	fieldLabel : i18n.getMsg('addbs.isTemplate'),
            	name:'IsTemplate',
            	labelWidth: 215,
            	listeners:{
    				change:function(thisCheckbox, newValue, oldValue, options){
    					if(newValue == true){
    						nodesTree.disable();
    						nodesTree.collapse();
    					}
    					else{
    						nodesTree.enable();
    						nodesTree.expand();
    					}
    				}
    			}
            },{
            	xtype: 'fieldset',
            	layout: 'hbox',
            	border: false,
            	padding: 0,
            	margins: 0,
            	items:[
	            	{
		            	xtype:'checkbox',
		            	fieldLabel : i18n.getMsg('addbs.isEnabled'),
		            	name:'Enabled',
		            	labelWidth: 225,
		            	checked:true,
		            	width: 530
		            },{
		            	xtype:'combo',
						align:'left',
		                id: 'StorageLayoutProvider',
		                name : 'StorageLayoutProvider',
		                store: pluginsStore,
		                fieldLabel: i18n.getMsg('addbs.storageProxying'),
		                labelWidth: 200,
		                valueField:'Name',
		                displayField:'Name',
		                value: 'local',
		                typeAhead: true,
		                allowBlank:true,
		                emptyText:'Proxying...',
		                queryMode: 'remote',
		                triggerAction: 'all',
		                forceSelection:true,
		                selectOnFocus:true,
		                width:490,
		                listeners:{
		                	change:function( thisObj, newValue, oldValue, eOpts ){
		                		if(newValue != 'local'){
		                			proxyNodesTree.enable();
		                			proxyNodesTree.expand();
		                			proxyNodesTree.getStore().getProxy().url = '/api/Nodes/Plugin/'+newValue+'/';
		                			proxyNodesTree.getStore().load();
		                			proxyNodesTree.getView().refresh();
		                		}
		                	}
		                }
		            }
		           
	          	]
            },{
            	xtype: 'fieldset',
            	layout: 'hbox',
            	border: false,
            	padding: 0,
            	margins: 0,
            	items:[
            		nodesTree,
            		proxyNodesTree
            	]
            
            }
         ]  
    });
    
    var tplStore = Ext.create('Ext.data.Store', {
    	storeId:'tplStore',
    	autoLoad:true,
        model: 'BackupSet',
        proxy: {
            type: 'ajax',
            url: '/api/BackupSets/Templates/',
            extraParams: {format: 'json'},
            reader:{
	        	type:'json',
	        	applyDefaults: true
	        }
        }
    });
    
    
    var p = new Ext.data.proxy.Client({ });
	
    var pathsStore = new Ext.data.Store( {
     	model:'BasePath',
     	clearOnLoad:false,
     	proxy: {type: 'memory'}
    });
    
	function setPaths(valuez){		
    	for(var i=0; i<valuez.length; i++){
    		console.log('add value to paths store : path='+JSON.stringify(valuez[i]));
    		pathsStore.add(valuez[i].data);
    	}
	}
	
	// Prevent incompatible data processing flags to be selected
   var validateDataFlags = function(){
		if(Ext.getCmp('enableEncrypt').getValue() == true && Ext.getCmp('enableCompress').getValue() == true ){
			// prevent from selecting client encrypt and server compress( non sense as encrypted data is not compressible)
			if(Ext.getCmp('fcs').getValue() == true && Ext.getCmp('fec').getValue() == true){
				Ext.getCmp('fcs').setValue(false);
				Ext.getCmp('fcs').markInvalid('Cannot compress on server side after encrypting on client side');
				Ext.getCmp('fcc').setValue(true);
			}
		}
		else{
		
		}
	}	
	    
	var fsPanel = new Ext.widget('form', { //new Ext.form.Panel({
    	id:'fs',
    	model: 'BackupSet',
        title:'<img src="/images/2.png" class="gIcon"/>'+i18n.getMsg('addbs.whatToBackup.title'), // Configure what to backup and how'
        bodyStyle: 'padding:5px;',
        height:450,
        waitMsgTarget: true,
		fieldDefaults: {
	        labelAlign: 'left',
	        labelWidth:140,
	        hideLabel: false
	    },
        items: [
        	{
				xtype:'combo',
				align:'left',
                id: 'Inherits',
                name : 'Inherits',
                store: tplStore,
                fieldLabel: i18n.getMsg('addbs.inheritTemplate'),
                labelWidth: 250,
                valueField:'Id',
                displayField:'Name',
                typeAhead: true,
                allowBlank:true,
                //blankText:'A value is required.',
                queryMode: 'remote',
                triggerAction: 'all',
                forceSelection:true,
                selectOnFocus:true,
                width:490
				
			},{
        		xtype: 'fieldset',
                title: i18n.getMsg('addbs.whatToBackup.title1'), //'Directories and files to backup',
                border: true,
	            height:350,
	            width:'80%',
				anchor: '100%',
				padding: '10',
                layout: 'anchor', 
                items: [
                	{
                	xtype: 'fieldcontainer',
                	layout: 'hbox',
            		items:[
							new Ext.grid.Panel( {
								id:'pathsGrid',
							    store: pathsStore,
							    height: 250,
							    width: 650,
							    scroll:'vertical',
							    viewConfig:{ markDirty:false },
							    plugins: [Ext.create('Ext.grid.plugin.CellEditing', {clicksToEdit: 1}) ],
							    columns: [
							        { 
							        	width: 20, flex:0,
							        	renderer:function(value, metaData, record, colIndex, store, view){
							        		if(record.get('Type').substring(0,2) == "FS")
							        			return '<img src="/images/f-g.png"/>';
							        	}
							        },
							        { text: i18n.getMsg('addbs.whatToBackup.path'), dataIndex: 'Path', width:300, flex: 2,
							        	renderer:function(value){
							        		return '<span data-qwidth="400" data-qtip="'+value+'">'+value+'</span>';
							        	},
							        	editor: {
							                xtype:'textfield',
							                allowBlank:false
							            }
							        },
							        { text: i18n.getMsg('addbs.whatToBackup.recursive'), dataIndex: 'Recursive', width:70, flex: 0,
							        	xtype:'checkcolumn'
							        },
							        { text: i18n.getMsg('addbs.whatToBackup.includeRule'), dataIndex: 'IncludePolicy', flex: 1,
							        	editor: {
							                xtype:'textfield',
							                allowBlank:true
							            }
							        },
							        { text: i18n.getMsg('addbs.whatToBackup.excludeRule'),dataIndex: 'ExcludePolicy', flex: 1,
							        	editor: {
							                xtype:'textfield',
							                allowBlank:true
							            }
							        },
							        { text: 'Snapshot',		dataIndex: 'Snapshot', width: 70, flex: 0 },
							        { text: 'Type',			dataIndex: 'Type', width: 80, flex: 0 }
							    ],
							    listeners:{// enable 'Delete' butoon if paths are selected
							    	selectionchange: function(thisObj, selected, eOpts){
										if(selected.length >=1)
		       								Ext.getCmp('deleteBtn').enable();
		       							else
		       								Ext.getCmp('deleteBtn').disable();
		       						}
							    },
							    dockedItems: [
								    {
									    xtype: 'toolbar',
									    dock: 'bottom',
									    padding:0, margins:0, height:27,
									    items: [
									    	new Ext.Button( {
								                 id: 'browseBtn',
								                 disabled:true,
								                 text: '<img src="/images/browse.png" height="20" valign="middle"/> '+i18n.getMsg('addbs.whatToBackup.browse'), 
								                 handler: function(){
								                 	var selectedNode = nodesTree.getChecked()[0].data['Id'];
								                 	var pathSeparator = '/';
								                 	if(nodesTree.getChecked()[0].data['OS'].substr(0,2) == "NT")
								                 		pathSeparator = '\\';
								                 	handleBrowse(selectedNode, pathSeparator, setPaths, true);
								                 }
											}),'-',
											new Ext.Button( {
								                 id: 'custAddBtn',
								                 text: '<img src="/images/add.png" height="20" valign="middle"/> '+i18n.getMsg('addbs.whatToBackup.manuallyAdd'), 
								                 handler: function(){
								                 	 var r = Ext.ModelManager.create({
									                    Path: '',
									                    Recursive: true,
									                    IncludePolicy: '*',
									                    ExcludePolicy: '',
									                	}, 'BasePath');
									                pathsStore.insert(0, r);
									                cellEditing.startEditByPosition({row: 0, column: 1});
								                 },
											}),
											new Ext.Button( {
								                 id: 'deleteBtn',
								                 disabled:true,
								                 text: '<img src="/images/delete.png" height="20" valign="middle"/> '+i18n.getMsg('generic.delete'), 
								                 handler: function(){
								                 	pathsStore.remove(Ext.getCmp('pathsGrid').getSelectionModel().getSelection());
								                 	
								                 }
								               
											})
									    ]
									}
								]// end docked items/toolbar
							}),// end basepaths grid
		                    {
			               		xtype:'fieldset',
			               		layout:'vbox',
			               		border:true,
			               		anchor:'right',
			               		collapsible:true,
			               		//collapsed: true,
			               		title:'Advanced options',
			               		defaults:{labelWidth:240},
			               		margin: '0 0 0 15',
			               		items:[
			               			{
					               		xtype:'checkbox',
					               		id:'backupPermissions',
					               		fieldLabel:i18n.getMsg('addbs.whatToBackup.permissions'),
					               		checked:true,
					               		width:320
					               	},{
					               		xtype:'checkbox',
					               		id:'DontTrustMtime',
					               		fieldLabel:i18n.getMsg('addbs.whatToBackup.nomtime'),
					               		checked:false,
					               		width:320
					               	},{
					               		xtype:'numberfield',
					               		id:'MaxChunkFiles',
					               		name:'MaxChunkFiles',
					               		fieldLabel:i18n.getMsg('addbs.whatToBackup.maxChunkFiles'),
					               		value:2000,
					               		minValue: 0,
					               		width:320
					               	},{
					               		xtype:'numberfield',
					               		id:'MaxChunkSize',
					               		name:'MaxChunkSize',
					               		fieldLabel:i18n.getMsg('addbs.whatToBackup.maxChunkSize'),
					               		value:100*1024*1024,
					               		minValue: 0,
					               		width:320
					               	},{
					               		xtype:'numberfield',
					               		id:'MaxPackSize',
					               		name:'MaxPackSize',
					               		fieldLabel:i18n.getMsg('addbs.whatToBackup.maxPackSize'),
					               		value:100*1024*1024,
					               		minValue: 0,
					               		width:320
					               	}
					               	
					            ]
							}
						]
						}
																								
					]		
                 //}),
               	},
           
          ]
          
         });   // end fsPanel
         
        /*Ext.tip.QuickTipManager.register({
		    target: 'fcs',
		    title: 'My Tooltip',
		    text: 'This tooltip was added in code',
		    width: 300,
		    dismissDelay: 10000 // Hide after 10 seconds hover
		});
*/

        // 3 - preops and postops
        var opsPanel = new Ext.form.Panel({
    	id:'opsPanel',
        frame: false,
        title:'<img src="/images/3.png" class="gIcon"/> '+i18n.getMsg('addbs.ops.title'), //Configure pre and post-backup custom operations',
        labelAlign: 'right',
        labelWidth: 85,
        autoWidth:true,
        autoHeight:true,
        bodyStyle: 'padding:5px 5px 5px 5px;',
        waitMsgTarget: true,
        items: [
        	   new Ext.form.FieldSet({
                title: i18n.getMsg('addbs.ops.title1'), //'Commands to execute on client node',
                autoHeight: true,
                layout: "column", 
                height: 190,
                defaultType: 'textfield',
                items: [
                	{
                            xtype: 'label',
                            text: i18n.getMsg('addbs.ops.pre'), //'Pre-backup operations',
                            columnWidth: .10
                    },{
                		xtype:'textarea',
                        fieldLabel: '',
                        label: '',
                        textLabel: '',
                        emptyText: 'put script or commands to execute',
                        name: 'Preop',
                        columnWidth: .40,
                        height:150,
                        align:'left',
                        autoCreate: {
							tag: "textarea",
							rows:15,
							height: 80,
							columns:10,
							autocomplete: "off",
							wrap: "off"
						},
                    },{
                            xtype: 'label',
                            text: i18n.getMsg('addbs.ops.post'), //'Post-backup operations',
                            columnWidth: .10
                    },{
                		xtype:'textarea',
                        fieldLabel: '',
                        label: '',
                        textLabel: '',
                        emptyText: 'put script or commands to execute',
                        name: 'Postop',
                        columnWidth: .40,
                        height:150,
                        align:'left',
                        autoCreate: {
							tag: "textarea",
							rows:15,
							height: 80,
							columns:10,
							autocomplete: "off",
							wrap: "off"
						},
                    },
                ]
                }),
                {
                        xtype: 'checkbox',
                        id:'keepShellOpen',
                        boxLabel: i18n.getMsg('addbs.ops.keepShellOpen'), 
                },
                {
                        xtype: 'checkbox',
                        id:'noBackupIfShellError',
                        boxLabel: i18n.getMsg('addbs.ops.noBackupIfShellError'), 
                },
           ]
    });
    
    // 4 - scheduling
    var schedP = new Ext.data.proxy.Client({  });
    /*var schedStore = new Extensible.calendar.data.MemoryEventStore({
	       
	});*/
   
	var calStore = new Extensible.calendar.data.MemoryCalendarStore({
            data:[
	            { id:0,	title:"Fulls", 			color:2},
	            { id:1, title:"Refresh", 		color:8},
	            { id:2,	title:"Differentials",	color:10},
	            { id:3,	title:"Incrementals",	color:31},
	            { id:4,	title:"TransactionLogs",color:31}
            ]
	}); 
	    
     var schedPanel = new Ext.form.Panel({
    	id:'schedPanel',
        frame: false,
        title:'<img src="/images/4.png" class="gIcon"/>'+i18n.getMsg('addbs.sched.title'), // Choose when and how to backup',
        bodyStyle: 'padding:5px;',
        waitMsgTarget: true,
     	items:[
     		/*{
	            xtype: 'radiogroup',
	            title:' ',
	            hidden:false,
	            height:25,
	            border:true,
	            frame:true,
	            layout:'hbox',
	            margins:{top:0},
	            items: [
	                {
		                boxLabel: i18n.getMsg('addbs.sched.useSchedulingPeriodic'),
		                name: 'schedType',
		                inputValue: '0',
		                width:680,
		                checked:true,
		            }, {
		                boxLabel: i18n.getMsg('addbs.sched.useSchedulingCDP'),
		                name: 'schedType',
		                inputValue: '1',
		                width:350,
		            }
				 ],
				 listeners:{
                	change:function(thiscombo, newValue, oldValue, options){
                		if(newValue['schedType'] == 0){
                			Ext.getCmp('backupScheduleCalendar').enable();
                			Ext.getCmp('schedCDPFields').disable();
            			}
            			else{
            				Ext.getCmp('backupScheduleCalendar').disable();
            				Ext.getCmp('schedCDPFields').enable();
            			}
            		}
            	},
             },*/ // end radiogroup
             {
             xtype:'fieldcontainer',
             layout:'table',
             columns:2,
             //height:460,
             items:[
           
	     		new Extensible.calendar.CalendarPanel( {
	     			calendarStore:calStore,
			        eventStore: schedStore,
			        id:'backupScheduleCalendar',
			       // autoRender:true,
			        height:500,
			        width:600,
			        showDayView:false,
			        showWeekView :true,
			        showMultiWeekView : false,
			        showMonthView :false,
			        showHeader:true,
			        showNavNextPrev:true,
			        showTodayText:false,
			        todayText:i18n.getMsg('addbs.sched.recurrence'),//'Recurrence :',
			        //dayText:i18n.getMsg('addbs.sched.everyDay'),
			        weekText:i18n.getMsg('addbs.sched.everyWeek'),
			        //monthText:i18n.getMsg('addbs.sched.everyMonth'),
			        showTime:false,
			        enableEditDetails: false,
			        editViewCfg:{
			        	enableRecurrence:false,
			        	enableEditDetails: false,
			        },
			        weekViewCfg:{
			        	dayCount:7,
			        	ddIncrement:15,
			        	scrollStartHour:8,
			        	hideBorders:false,
			        	showHeader:true,
			        	//showWeekLinks:true,
			        	hourHeight:15,
			        	startDay:1, //start on monday
			        	startDayIsStatic: true,
			        	showTime:false,
			        	enableContextMenus:false,
			        },
			       /* monthViewCfg:{
			        	defaultEventTitleText:'',
			        	showHeader:true,
			        	showWeekLinks:true,
			        	prevMonthCls:'cal-disabled-day',
			        	nextMonthCls:'cal-disabled-day',
			        	startDay:1, //start on monday
			        	startDayIsStatic: true,
			        	enableContextMenus:false
            
			        },*/
			        /*viewCfg:{
			        	showHeader:true,
			        	showWeekLinks:true,
			        	startDay:1, //start on monday
			        	startDayIsStatic: true,
			        	enableContextMenus:false,
			        },*/
			        showNavJump:false, 
			       // enableEditDetails:false,
			        listeners:{
					    dayclick:function(thisCal, clickedDate, isAallday, extElement){
					    	
					    	backupTimeConf(clickedDate, null);
					    	return false;
					    },
					    rangeselect: function(thisCal, datesObject, callbackFn){
					    	backupTimeConf(datesObject.StartDate, datesObject.EndDate);
					    	return false;
					    },
					    eventClick:function(thisCal, eventRecord, htmlEl){
					    	return false;
					    },
					    eventresize:function(thisCal, eventRecord){
					    	//alert('resize: rec '+eventRecord.get('StartDate')+' , '+eventRecord.get('EndDate'));
					    	//eventRecord.set(EndDate,);
					    	schedStore.sync();
					    	thisCal.refresh(true);
					    	return false;
					   	},
					   	eventmove:function(thisCal, eventRecord){
					    	//alert('resize: rec '+eventRecord.get('StartDate')+' , '+eventRecord.get('EndDate'));
					    	//eventRecord.set(EndDate,);
					    	schedStore.sync();
					    	thisCal.refresh(true);
					   	},
					}
				}),
				{
					xtype:'panel',
					id:'schedCDPFields',
					margins:{left:35, top:0},
					hideLabels:true,
					border:false,
					frame:false,
					layout:'table',
					rowspan:2,
					columns:2,
					disabled:true,
					items:
					[
						{
							xtype:'displayfield',
							value:'maximum backup frequency',
							width:180,
							padding:{left:30},
						},{
							xtype:'combo',
							label:'maximum backup frequency',
							mode:           'local',
                            value:          '15',
                            triggerAction:  'all',
                            forceSelection: true,
                            allowBlank:false,
                            editable:false,
                            fieldLabel:'',
                            id:           'cdpFrequency',
                            displayField:   'name',
                            valueField:     'value',
                            queryMode: 'local',
                            labelWidth:250,
                            store:          Ext.create('Ext.data.Store', {
                              fields : ['name', 'value'],
                                data   : [
                                	{name : '5 mn',   value: '5'},
                                    {name : '10 mn',   value: '10'},
                                    {name : '15 mn',  value: '15'},
                                    {name : '20 mn', value: '20'},
                                    {name : '30 mn', value: '30'},
                                    {name : '45 mn', value: '45'},
                                    {name : '60 mn', value: '60'},
                                    {name : '2 h', value: '120'},
                                    {name : '4 h', value: '240'},
                                ]
                            })
						}
					]
				}
			
			
			]
			}
			
     	]
    });
    	
    	
    var stoPanel = new Ext.form.Panel({
    	id:'stoPanel',
        frame: false,
        title:'<img src="/images/5.png" class="gIcon"/> '+i18n.getMsg('addbs.ret.title'),
        labelAlign: 'left',
        defaults:{padding:5},
        items: [
        	{
        		xtype: 'fieldset',
        		layout: 'hbox',
        		border: false,
        		items:[
		        	 new Ext.form.ComboBox({
		                id:'StorageGroup',
		                name:'StorageGroup',
		                fieldLabel: i18n.getMsg('generic.storageGroup'),
		                store: sgStore,
		                valueField:'Id',
		                displayField:'Name',
						listConfig: {
					        getInnerTpl: function(){
					            var tpl = '<tpl for=".">'
					            			+'<div class="x-combo-list-item">{Name} <i>({Description})&nbsp;</i>'
					            			+'<tpl if="(Capabilities & dataFlags.SCompress) == dataFlags.SCompress">'
					            				+'<img src="/images/compress.png">'
					            			+'</tpl>'
					            			+'<tpl if="(Capabilities & dataFlags.SEncrypt) == dataFlags.SEncrypt">'
					            				+'<img src="/images/encrypt.png">'
					            			+'</tpl>'
					            			+'<tpl if="(Capabilities & dataFlags.SDedup) == dataFlags.SDedup">'
					            				+'<img src="/images/dedup.png">'
					            			+'</tpl>'
					            			+'</div>'
					            		+'</tpl>';
					            return tpl;
					        }
					    },
		                typeAhead: true,
		                triggerAction: 'all',
		                forceSelection: true,
		                allowBlank:		false,
		                selectOnFocus:true,
		                width:400,
		                listeners:{
		                	change:function( thisObj, newValue, oldValue, eOpts ){
		                		if(newValue != null && newValue >0)
		                			Ext.getCmp('dataprocessing').enable();
		                		var record = sgStore.getById(newValue);
		                		if((record.get('Capabilities') & dataFlags.SCompress) == dataFlags.SCompress)
		                			Ext.getCmp('fcs').enable();
		                		else
		                			Ext.getCmp('fcs').disable();
		                		if((record.get('Capabilities') & dataFlags.SEncrypt) == dataFlags.SEncrypt)
		                			Ext.getCmp('fes').enable();
		                		else
		                			Ext.getCmp('fes').disable();
		                		if((record.get('Capabilities') & dataFlags.SDedup) == dataFlags.SDedup)
		                			Ext.getCmp('fds').enable();
		                		else
		                			Ext.getCmp('fds').disable();
		                	}
		                }
		        	}),
		        	{
						xtype: 'hidden',
						id:'DataFlags',
						name: 'DataFlags'
					},{ // Select data processing flags ensuring that mutually exclusive options are not selectable
				        xtype: 'fieldset',
				        id: 'dataprocessing',
				        title: i18n.getMsg('addbs.whatToBackup.dataFlags'),
				        labelWidth:130,
				        width:550,
				        layout:'vbox',
				        border:true,
				        margin:'0 0 0 20',
				        disabled: true,
				        items: [
				        	{
				        		xtype:	'fieldset',
				        		layout:	'hbox',
				        		defaults:{width:100,margin:0, padding:0},
				        		border:	false,
					        	items:[
						        	{ xtype:'displayfield', value: ' '},
						        	{ xtype:'displayfield', value: ' ', width:30},
						            { xtype:'displayfield', value: i18n.getMsg('addbs.whatToBackup.dataFlags.byclient')},
						            { xtype:'displayfield', value: i18n.getMsg('addbs.whatToBackup.dataFlags.bysn')},
						         ]
				          	},{
				        		xtype:	'fieldset',
				        		layout:	'hbox',
				        		defaults:{width:110, height:20, margin:0, padding:0},
				        		border:	false,
					        	items:[
					        		{ xtype:'checkbox', fieldLabel: '', id: 'enableCompress', width:30, checked: true,
					        		listeners:{
					        			change: function(thisObj, newValue, oldValue, eOpts){
					        				if(newValue == false){
					        					Ext.getCmp('fcc').disable();
					        					Ext.getCmp('fcs').disable();
					        				}
					        				if(newValue == true){
					        					Ext.getCmp('fcc').enable();
					        					Ext.getCmp('fcs').enable();
					        				}
					        			}
					        		 } },
						        	{ xtype:'displayfield', value:'<img src="/images/compress.png"/>&nbsp;'+i18n.getMsg('addbs.whatToBackup.dataFlags.compress')},
						            { xtype:'radio', 		fieldLabel: '', id:'fcc',	name: 'flagCompress', inputValue: '1' , checked: true},
						            { xtype:'radio', 		fieldLabel: '', id:'fcs',	name: 'flagCompress', 
						            	/*msgTarget:'side', 
						            	width:30,
						            	autoFitErrors: true,*/
						            	errorMsgCls: 'errorQtip',
							            listeners:{
						        			change: validateDataFlags
						        		}
					        		}
						         ]
				          	},{
				        		xtype:	'fieldset',
				        		layout:	'hbox',
				        		defaults:{width:110, height:20, margin:0, padding:0},
				        		border:	false,
					        	items:[
					        		{ xtype:'checkbox', 	fieldLabel: '', 	id: 'enableEncrypt', width:30, checked: false,
					        		listeners:{
					        			change: function(thisObj, newValue, oldValue, eOpts){
					        				if(newValue == false){
					        					Ext.getCmp('fec').disable();
					        					Ext.getCmp('fes').disable();
					        				}
					        				if(newValue == true){
					        					Ext.getCmp('fec').enable();
					        					Ext.getCmp('fes').enable();
					        				}
					        			}
					        		 } },
						        	{ xtype:'displayfield', value:'<img src="/images/encrypt.png"/>&nbsp;'+i18n.getMsg('addbs.whatToBackup.dataFlags.encrypt')},
						            { xtype:'radio', 		fieldLabel: '', id:'fec',	name: 'flagEncrypt', disabled:true, inputValue: '1',
							            listeners:{
						        			change: validateDataFlags
						        		}
					        		},{ xtype:'radio', 		fieldLabel: '', id:'fes',	name: 'flagEncrypt', disabled:true, inputValue: '2' }
						         ]
				          	},{
				        		xtype:	'fieldset',
				        		layout:	'hbox',
				        		defaults:{width:110, height:20, margin:0, padding:0},
				        		border:	false,
					        	items:[
					        		{ xtype:'checkbox', fieldLabel: '', id: 'enableDedupe', width:30, checked: true,
					        		listeners:{
					        			change: function(thisObj, newValue, oldValue, eOpts){
					        				if(newValue == false){
					        					Ext.getCmp('fdc').disable();
					        					Ext.getCmp('fds').disable();
					        				}
					        				if(newValue == true){
					        					Ext.getCmp('fdc').enable();
					        					Ext.getCmp('fds').enable();
					        				}
					        			}
					        		 } },
						        	{ xtype:'displayfield', value:'<img src="/images/dedup.png"/>&nbsp;'+i18n.getMsg('addbs.whatToBackup.dataFlags.dedupe')},
						            { xtype:'checkbox', id:'fdc',	name: 'flagDedup', inputValue: '1', checked: true},
						            { xtype:'checkbox', id:'fds',	name: 'flagDedup', inputValue: '2', checked: true },
						            { xtype:'checkbox', boxLabel: i18n.getMsg('addbs.whatToBackup.dataFlags.useDedicatedDdb'), labelWidth: 175, width:200, id:'UseDedicatedDdb',	name: 'UseDedicatedDdb', checked: false }
						         ]
				          	}
				        ]
				    },
				]
			},	    
        	new Ext.form.FieldSet({
                title: i18n.getMsg('addbs.ret.redundancy'),
                autoHeight: true,
                layout: 'hbox', 
                fieldDefaults:{labelWidth:200},
                items: [
                    new Ext.form.ComboBox({
		                height:35,
		                align:'left',
		                id:'redundancy',
		                fieldLabel:  i18n.getMsg('addbs.ret.nbcopies'),
		                store: new Ext.data.ArrayStore({
		                    fields: ['bType'],
		                    data : [['1'],['2'],['3']]
		                }),
		                valueField:'bType',
		                displayField:'bType',
		                typeAhead: true,
		                mode: 'local',
		                triggerAction: 'all',
		                emptyText:'1',
		                selectOnFocus:true,
		                width:250
		        	}),
                    {
                        xtype: 'displayfield',
                        text: 'copies. ',
                        width:70
                    },
                   
                ]
                }),
        	   new Ext.form.FieldSet({
                title: i18n.getMsg('addbs.ret.retention'),
                autoHeight: true,
                layout: "vbox", 
                items: [
                	{
                		xtype: 'fieldset',
                		layout:'hbox',
                		border:false,
                		defaults:{labelWidth:200},
                		items:[
		                    new Ext.form.ComboBox({
				                id:'RetentionDays',
				                name:'RetentionDays',
				                fieldLabel:i18n.getMsg('addbs.ret.keepsets'),
				                store: new Ext.data.ArrayStore({
				                    fields: ['bType', 'daysV'],
				                    data : [['1 week (7 days)',7],['2 weeks (14 days)',14],['3 weeks (21 days)',21],['1 month (31 days)',31],['2 months (62 days)',61], ['6 months (183 days)',183], ['1 year (365 days)',365], ['2 years (731 days)',731]]
				                }),
				                valueField:'daysV',
				                displayField:'bType',
				                typeAhead: true,
				                mode: 'local',
				                triggerAction: 'all',
				                value:31,
				                selectOnFocus:true,
				                width:350,
				               	labelWidth:200
				        	}),
				        	new Ext.form.ComboBox({
				                id:'ArchiveAction',
				                name:'ArchiveAction',
				                fieldLabel:i18n.getMsg('addbs.ret.after'),
				                store: new Ext.data.ArrayStore({
				                    fields: ['bType', 'daysV'],
				                    data : [['Delete','Action.Delete'],['Move to...','Action.Move'],['Only keep...','21']]
				                }),
				                valueField:'daysV',
				                displayField:'bType',
				                typeAhead: true,
				                mode: 'local',
				                triggerAction: 'all',
				                value:'Action.Delete',
				                selectOnFocus:true,
				                width:350
				        	}),
				        	
				        ]
				    },{
				    	xtype: 'fieldset',
                		layout:'hbox',
                		border:false,
                		defaults:{labelWidth:200},
                		items:[
                			new Ext.form.ComboBox({
				                id:'SnapshotRetention',
				                name:'SnapshotRetention',
				                fieldLabel:i18n.getMsg('addbs.ret.keepsnaps'),
				                labelWidth:200,
				                store: new Ext.data.ArrayStore({
				                    fields: ['name', 'value'],
				                    data : [['Until next backup',-1], ['Don\'t keep',0],['1 day',1], ['2 days',2], ['1 week (7 days)',7],['2 weeks (14 days)',14],['3 weeks (21 days)',21],['1 month (31 days)',31],['2 months (62 days)',61]]
				                }),
				                valueField:'value',
				                displayField:'name',
				                typeAhead: true,
				                mode: 'local',
				                triggerAction: 'all',
				                value:0,
				                selectOnFocus:true,
				                width:350
				        	}),
	                     ]
                    },
                ]
                }),
                
          ],
          buttons:[
     			{
     			id:'create',
     			text:i18n.getMsg('generic.ok'),
     			//formBind:true,
     			handler:function(){
				    var basePathz = [];
				    var scheduling = [];
                	pathsStore.each(function(record){
                		console.log('adding pathStore path to Backupset record : '+JSON.stringify(record.data));
                		basePathz.push(record.data);
                	});
                	// concert calendar events to ScheduleTime objects
                	schedStore.each(function(record){
                		console.log('adding pathStore path to Backupset record : '+JSON.stringify(record.data));
                		console.debug('adding new scheduletime with day='+record.get('StartDate')+' => mapped to "'+Ext.Date.format(record.get('StartDate'), 'l')+'"');
                		
                		var endHour ;
                		// if end date == start date, no backup end window has been set
                		if(record.get('EndDate') == record.get('StartDate')){
                			endHour = -1;
                		}
                		else{
                		 	endHour = record.get('EndDate').getHours();
                		}
                		var schedule = Ext.create('ScheduleTime',{
                			Day:		Ext.Date.format(record.get('StartDate'), 'l'),
                			Level:		record.get('Title'),
                			BeginHour:	record.get('StartDate').getHours(),
                			BeginMinute:record.get('StartDate').getMinutes(),
                			EndHour:	endHour,
                			EndMinute:	record.get('EndDate').getMinutes()
                		});
                		scheduling.push(schedule.data);
                	});
                	// Create base BackupSet object if needed
                	if(currentBackupSet == null || currentBackupSet === undefined){
					    currentBackupSet = Ext.create('BackupSet',{
	                		Operation: 'Backup'
	                	});
                	}
                	// ...Now set dataflags
                    var  flags = dataFlags.Node;
                    if(Ext.getCmp('enableCompress').getValue() == true)
                    	if(Ext.getCmp('fcc').getValue() == true)
                    		flags |= dataFlags.CCompress;
                    	else if (Ext.getCmp('fcs').getValue() == true)
                    		flags |= dataFlags.SCompress;
                    if(Ext.getCmp('enableEncrypt').getValue() == true)
                    	if(Ext.getCmp('fec').getValue() == true)
                    		flags |= dataFlags.CEncrypt;
                    	else if (Ext.getCmp('fes').getValue() == true)
                    		flags |= dataFlags.SEncrypt;	
                    if(Ext.getCmp('enableDedupe').getValue() == true)
                    	if(Ext.getCmp('fdc').getValue() == true)
                    		flags |= dataFlags.CDedup;
                    	else if (Ext.getCmp('fds').getValue() == true)
                    		flags |= dataFlags.SDedup;	
                    Ext.getCmp('DataFlags').setValue(flags);
                	
                	// ...ready to set new BackupSet params using form items : apply form panels one by one
                	targetPanel.getForm().updateRecord(currentBackupSet);
                	fsPanel.getForm().updateRecord(currentBackupSet);
                	opsPanel.getForm().updateRecord(currentBackupSet);
                	stoPanel.getForm().updateRecord(currentBackupSet);
                	currentBackupSet.set('BasePaths', basePathz);
                	currentBackupSet.set('ScheduleTimes', scheduling);
                	
                	//var method = 'create';
					//if(bs >0) method = 'update';       
					//console.debug('action = '+method);
					currentBackupSet.setProxy({
				        type: 'rest',
				        url: '/api/BackupSet/',
				        extraParams: {format: 'json'}
	   				 });
	   				 
	   				 //set proxying info if relevant:
	   				 if(proxyNodesTree.getChecked().length == 0 && preCheckedProxy >0 ){
	   				 	currentBackupSet.set('HandledBy', preCheckedProxy);
	   				 	
					 }
					 Ext.each(proxyNodesTree.getChecked(), function (node){
						currentBackupSet.set('HandledBy', node.get('Id'));
					 });
						
	   				 
                	if(currentBackupSet.get('IsTemplate') == false){
                		// case where editing a backupset : nodes tree might not have been expanded 
						// if the user didn't change anything there. So apply existing NodeId.
						if(nodesTree.getChecked().length == 0 && preCheckedNode >0){
							currentBackupSet.set('NodeId', preCheckedNode);
							currentBackupSet.save();
						}
	                	Ext.each(nodesTree.getChecked(), function (node){
							currentBackupSet.set('NodeId', node.get('Id'));
							currentBackupSet.save();
						});
						
					}
					else{ // this BackupSet is not directly applied to a node, it's a template.
						currentBackupSet.save();
					}	
                	//Ext.getCmp('create').disable();
				}	
 			},
 			{text:i18n.getMsg('generic.cancel'),}
 		]
    });
    
    var notifyPanel = new Ext.form.Panel({
    	id:'notifyPanel',
        frame: false,
        title:'<img src="/images/6.png" class="gIcon"/> Notifications and actions',
        labelAlign: 'left',
        items: [
			{
	    		xtype:	'fieldset',
	    		layout:	'hbox',
	    		defaults:{width:130,margin:5, padding:0},
	    		border:	true,
	        	items:[
		        	new Ext.form.ComboBox({
		                id:'Notifications1',
		                name:'Notifications',
		                fieldLabel:i18n.getMsg('addbs.actions.on'),
		                labelWidth:200,
		                store: new Ext.data.Store({
		                    fields: ['name', 'value'],
		                    data : [
		                    	{name:'Unknown', 		value:'Unknown'},
								{name:'PendingStart', 	value:'PendingStart'},
								{name:'PreProcessing', 	value:'PreProcessing'},
								{name:'Started', 		value:'Started'},
								{name:'WindowExceeded', value:'WindowExceeded'},
								{name:'Paused', 		value:'Paused'}, 
								{name:'PostProcessing', value:'PostProcessing'}, 
								{name:'Stopped', 		value:'Stopped'}, 
								{name:'Error', 			value:'Error'}, 
								{name:'Cancelling', 	value:'Cancelling'}, 
								{name:'Cancelled', 		value:'Cancelled'}, 
								{name:'Done', 			value:'Done'}, 
								{name:'Expiring', 		value:'Expiring'}, 
								{name:'Expired', 		value:'Expired'}
		                    
		                    ]
		                }),
		                valueField:'value',
		                displayField:'name',
		                typeAhead: false,
		                mode: 'local',
		                triggerAction: 'all',
		                value:'',
		                selectOnFocus:true,
		                width:350
		        	}),
		        	new Ext.form.ComboBox({
		                id:'Actions1',
		                name:'Notifications',
		                fieldLabel:i18n.getMsg('addbs.actions.do'),
		                labelWidth:200,
		                store: new Ext.data.Store({
		                    fields: ['name', 'value'],
		                    data : [
		                    	{value:'Mail', 		name:'Send mail'},
								{value:'StopTask', 	name:'Stop task'},
								{value:'Script', 	name:'Execute script'},
								
		                    
		                    ]
		                }),
		                valueField:'value',
		                displayField:'name',
		                typeAhead: true,
		                mode: 'local',
		                triggerAction: 'all',
		                value:'',
		                selectOnFocus:true,
		                width:350
		        	}),
        		]
        	}
        	
        ]
    });
    
    var viewport = new Ext.form.Panel({
    	renderTo: Ext.get('panel'),
        layout: 'accordion',
        layoutConfig:{animate:true},
        fieldDefaults: {
            labelAlign: 'left',
            hideLabel: false
        },
        height:615,
        items: [targetPanel, fsPanel, opsPanel, schedPanel, stoPanel, notifyPanel]
	});


function backupTimeConf(startDate, endDate){

	/*var timeDayNumbers = new Ext.form.FieldContainer({
     	id:'timeDayNumbers',
     	layout: {type: 'table', columns: 4},
     	items:[
     		{
        		xtype:'displayfield',
        		id:'startDayLabel',
        		width:90,
        		align:'left'
        	},{
                xtype: 'numberfield',
                id:'startDay',
                allowDecimals:false,
                allowBlank:false,
                align:'left',
                minValue:1,
                maxValue:31,
                width:40,
     		},{
        		xtype:'displayfield',
        		id:'endDayLabel',
        		value:i18n.getMsg('addbs.sched.toDay'), //'to day',
        		width:70,
        	},{
                xtype: 'numberfield',
                id:'endDay',
                allowDecimals:false,
                allowBlank:false,
                minValue:1,
                maxValue:31,
                width:40
     		},
     	]
     });*/
     
     var timeDaysOfWeek = new Ext.form.FieldContainer({
     	id:'timeDaysOfWeek',
     	//layout: {type: 'table', columns: 4},
     	defaults:{labelWidth:150},
     	items:[
     		{
                xtype			: 'combo',
                id				: 'weekDayName',
                fieldLabel		: i18n.getMsg('addbs.sched.every'),
                width			: 250,
				mode			: 'local',
                triggerAction	: 'all',
                forceSelection	: true,
                allowBlank		: false,
                editable		: false,
                displayField	: 'value',
                valueField		: 'name',
                queryMode		: 'local',
                store			: Ext.create('Ext.data.Store', {
                  	fields : ['name', 'value'],
                    data   : [
                        {name : '1',  value: i18n.getMsg('generic.monday')},
                        {name : '2',  value: i18n.getMsg('generic.tuesday')},
                        {name : '3',  value: i18n.getMsg('generic.wednesday')},
                        {name : '4',  value: i18n.getMsg('generic.thursday')},
                        {name : '5',  value: i18n.getMsg('generic.friday')},
                        {name : '6',  value: i18n.getMsg('generic.saturday')},
                        {name : '7',  value: i18n.getMsg('generic.sunday')}
                    ]
                }),
     		},
     	]
     });
     
     var timeGenericPart = new Ext.form.FieldContainer({
     	id:'timeGenericPart',
     	layout: 'vbox', //{type: 'table', columns: 4},
     	defaults:{labelWidth: 150},
     	items:[
     		{
     			xtype:'combo',
                height:24,
                align:'left',
                id:'bType',
                fieldLabel: i18n.getMsg('addbs.sched.backupType'),
                store: new Ext.data.ArrayStore({
                    fields: ['bType'],
                    data : [['Full'],['Refresh'],['Differential'],['Incremental'],['TransactionLogs']]
                }),
                valueField:'bType',
                displayField:'bType',
                typeAhead: true,
                mode: 'local',
                triggerAction: 'all',
                value:'Refresh',
                selectOnFocus:true,
                width:240,
                colspan:3,
        	},{
        		xtype:'timefield',
        		id:'startAt',
        		fieldLabel:i18n.getMsg('addbs.sched.startAt'),
        		format:'H:i',
        		increment: 30,
        		width:240,
        		colspan:3
        	},{
        		xtype:'timefield',
        		id:'endAt',
        		fieldLabel:i18n.getMsg('addbs.sched.endAt'),
        		format:'H:i',
        		increment: 30,
        		width:240,
        	},
     	]
     });
	var bTimeWin = new Ext.Window({ 
 		id:'bTimeWin',
        width:470,
       	height:200,
        plain: true,
        title:i18n.getMsg('addbs.sched.title1'), 
		scroll:false,
        modal:true,
        autoDestroy :true,
        monitorValid:true,
        resizable:true,
		items: [],
        buttons: [{
                text:'Ok',
                disabled:false,
                handler:function(){
                	var dt = new Date();
                	var theStartDay = Ext.Date.format(startDate, 'd'); //Ext.getCmp('startDay').getRawValue();
                	//if(theStartDay > 0 && theStartDay < 10) theStartDay = '0'+ theStartDay;
                	var theEndDay = Ext.Date.format(startDate, 'd');; //Ext.getCmp('endDay').getRawValue();
                	//if(theEndDay > 0 && theEndDay < 10) theEndDay = '0'+ theEndDay;
                	var sDate = /*new Date(*/Ext.Date.format(dt, 'Y-m')+'-'+theStartDay+' '+Ext.getCmp('startAt').getRawValue();//);
                	var eDate = null;
                	//if(theEndDay.length <1) 	// 1-day backup
                		eDate = /*new Date(*/Ext.Date.format(dt, 'Y-m')+'-'+theStartDay+' '+Ext.getCmp('endAt').getRawValue(); //);
                	//else if(Ext.getCmp('weekDayName').getValue() == null) 				//backup is selected for several days
                	//	eDate = /*new Date(*/Ext.Date.format(dt, 'Y-m')+'-'+theEndDay+' '+Ext.getCmp('endAt').getRawValue();//);
                	//else { 											//backup is recurring the 'd' day every week
                	//	alert('pipule');
                	//}
                	var newId = Math.floor(Math.random()*1001); //Ext.getCmp('schedStore').getTotalCount()+1;
                	var typeTitle = Ext.getCmp('bType').getRawValue();
                	var t1combobox = Ext.getCmp('bType');
	                var t1v = t1combobox.getValue();
					var t1record = t1combobox.findRecord(t1combobox.valueField || t1combobox.displayField, t1v);
					var t1index = t1combobox.store.indexOf(t1record);
					console.debug('new ScheduleTime : EventId:'+newId+', CalendarId:'+t1index+', StartDate:'+sDate+', EndDate:'+eDate+', Title:'+typeTitle);
                	schedStore.add({EventId:newId, CalendarId:t1index, StartDate:''+sDate, EndDate:''+eDate, Title:typeTitle});
                	bTimeWin.close();
                }
            },{
                text: 'Close',
                handler: function(){
                    //winBrowse.hide();
                    bTimeWin.close();
                }
            }
        ] 
     }); // end bTimeWin
     
    /* var timeWinPanel =  new Ext.form.Panel({
		id:'schedCalPanel',
        monitorValid:true,
        border: false,
        autoScroll:false,
        bodyPadding: 10,
        bodyStyle:'font-size:0.9em !important;',
		height:'180',
        items: []
	});*/
	console.debug('new schedule start date : '+Ext.Date.format(startDate, 'd'));
     /*if(endDate == null || Ext.Date.format(startDate, 'd') != Ext.Date.format(endDate, 'd')){
     	bTimeWin.add(timeDayNumbers);
     	if(endDate == null){
     		Ext.getCmp('endDayLabel').hide();
     		Ext.getCmp('endDay').hide();
     		Ext.getCmp('startDayLabel').setValue(i18n.getMsg('addbs.sched.day'));
     		Ext.getCmp('startDayLabel').show();
     	}
     	else{
     		Ext.getCmp('endDay').setValue(Ext.Date.format(endDate, 'd'));
     		//if(Ext.getCmp('endDay').setValue(Ext.Date.format(endDate, 'd'))
     		Ext.getCmp('endAt').setValue(endDate);
     		Ext.getCmp('startDayLabel').setValue(i18n.getMsg('addbs.sched.fromDay')); 
     	}
     }
     else if(Ext.Date.format(startDate, 'd') == Ext.Date.format(endDate, 'd')){*/
     	console.debug('selected a schedule with both start and end time');
     	//Ext.getCmp('startDayLabel').hide();
     	bTimeWin.add(timeDaysOfWeek);
     	Ext.getCmp('endAt').setValue(endDate);
		Ext.getCmp('weekDayName').setValue(Ext.Date.format(startDate, 'N'));
    // }
     //Ext.getCmp('startDay').setValue(Ext.Date.format(startDate, 'd'));
     Ext.getCmp('startAt').setValue(startDate);
     bTimeWin.add(timeGenericPart);
     //bTimeWin.add(timeWinPanel);
     
	 bTimeWin.show();
 } // end backupTimeConf()


	// now that everything is loaded, check if we have to be in 'edit' mode 
	var currentBackupSet = undefined;
 	if(bs >0){
    	prepareBsEdit(bs);
    	console.debug('loaded backupset #'+currentBackupSet+' for editing');
    }
    if(paramNode > 0){
    	preCheckedNode = paramNode;
    }

}); //end i18n
});


