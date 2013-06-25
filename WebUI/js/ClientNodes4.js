Ext.Loader.setConfig({ enabled: true});
 
 var nodesChecked = [];
 var pathsChecked = [];
 var toBeExpanded = true;
 
 Ext.require([
    'Ext.data.*',
    'Ext.grid.*',
    'Ext.tree.*',
    'Ext.form.*',
    'Ext.window.*',
    'Ext.fx.target.Sprite',
    //'backo.NodesTree'
 ]);
	

  function ManageLockStatus(areLocked){
  	var checkedNodes = Ext.getCmp('clientNodesTree').getSelectionModel().getSelection();
	var checkedList = '';
	Ext.each(checkedNodes, function (node){
		var conn = new Ext.data.Connection(); 
	    conn.request({ 
    		url: '/api/Node/'+node.get('Id')+'/Lock/'+areLocked+'?format=json', 
            method: 'POST', 
            scope: this, 
            params: {format:'json'}, 
            //success: function(responseObject){ Ext.Msg.alert('Status', responseObject.responseText);  }, 
            failure: function(responseObject) { 
                 Ext.Msg.alert('Status', 'Unable to save changes. Error:'+responseObject.responseText); 
            } 
	    }); 
	});
	
    Ext.getCmp('clientNodesTree').getStore().load();
    Ext.getCmp('clientNodesTree').applyState();
  }
  
  function setTempPathFromBrowser(value){
  	Ext.getCmp('Backups.TempFolder').setValue(value[0].get('Path'));
  }
  function setIndexesPathFromBrowser(value){
  	Ext.getCmp('IndexPath').setValue(value[0].get('Path'));
  }
  function setStorageFolderFromBrowser(value){
  	Ext.getCmp('StoragePath').setValue(value[0].get('Path'));
  }
  
 Ext.onReady(function () {
  	Ext.Loader.setConfig({enabled:true});

i18n.onReady(function(){

	Ext.tip.QuickTipManager.init(true, {maxWidth: 450,minWidth: 150, width:350 });
	
	var nStore = new Ext.data.TreeStore( {
		model: 'Node',
		autoLoad: true,
		//autoSync: true,
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
		folderSort: true,
		listeners:{
			load:function( thisObj, node, records, successful, eOpts ){
				Ext.each(records, function (rec){
					rec.set('leaf', rec.get('Group') != -1);
					if(rec.get('Group') != -1){
						// set online/offline status icon
						if(rec.get('Status') == 'Idle')
							rec.set('iconCls','node-idle');
						else if(rec.get('Status') == 'Online' || rec.get('Status') == 'Backuping' || rec.get('Status') == 'Restoring')
							rec.set('iconCls','node-on');
						else if(rec.get('Status') == 'Error')
							rec.set('iconCls','node-err');
						else
							rec.set('iconCls','node-off');
					}
					
				});
			}
		}
	});
    //nStore.getProxy().extraParams.node = 'root';
    
  Ext.define('NG', {
        extend: 'Ext.data.Model',
        idProperty: 'Id',
        fields: [/*'id', 'name', 'description'*/
        	 {name: 'Id',     type: 'int'},
        	 {name: 'Name',     type: 'string'},
        	 {name: 'Description',     type: 'string'},
        ]
    });

    var ngStore = Ext.create('Ext.data.JsonStore', {
    	autoLoad:false,
    	storeId:'ngStore',
        model: 'NG',
        proxy: {
            type: 'ajax',
            url: '/api/Nodes',
            extraParams: {
		        format	: 'json',
		    },
        },
        folderSort: true,
		//groupField: 'user'
    });
    
    
 function GetConfigWindow(){
 	
 	// for nox we only allow to configure 1 node at a time.
 	//var checkedNodes = Ext.getCmp('clientNodesTree').getChecked();
 	var checkedNodes = Ext.getCmp('clientNodesTree').getSelectionModel().getSelection();
	var checkedId = null;
	Ext.each(checkedNodes, function (nodes){
		//checkedId = nodes.internalId;
		checkedId = nodes.get('Id');
	});
 
	Ext.define('SG', {
        extend: 'Ext.data.Model',
        fields: ['Name', 'Id']
    });

    var sgStore = Ext.create('Ext.data.Store', {
    	storeId:'sgStore',
    	autoLoad:false,
        model: 'StorageGroup',
        proxy: {
            type: 'ajax',
            url: '/api/StorageGroups',
            extraParams: {format: 'json'},
		    reader:{
	        	type:'json',
	        	applyDefaults: true
	        }
        },
        
    });
    
	var configFormPanel = new Ext.form.Panel({/*Ext.widget('form', {*/
        title: i18n.getMsg('nodeconf.confTab'),
        id:'configFormPanel',
        model: 'Node',
        url : '/api/Node/'+checkedId+'/Configuration/',
        monitorValid:true,
        border: false,
        autoScroll:true,
        bodyPadding: 10,
		height:'460',
        items: [
       	{
            xtype: 'fieldset',
            title: i18n.getMsg('nodeconf.useTemplate'), //'Use template(s)',
            border: true,
            collapsible:true,
            items: [
            	{
            	xtype: 'fieldcontainer',
            	layout:'hbox',
            	items:[
            		{
                        xtype:          'combo',
                        mode:           'local',
                        value:          '<none>',
                        triggerAction:  'all',
                        forceSelection: true,
                        allowBlank:		true,
                        editable:       false,
                        fieldLabel:     i18n.getMsg('nodeconf.selectTemplate'),
                        labelWidth:		130,
                        id:           	'template1',
                        displayField:   'value',
                        valueField:     'name',
                        queryMode: 'local',
                        width:400,
                        store:          Ext.create('Ext.data.Store', {
                          fields : ['name', 'value'],
                            data   : [
                                {name : '0',   value: 'none'},
                                {name : '1',  value: 'linux'},
                                {name : '2', value: 'windows'}
                            ]
                        })
                    },{
                        xtype:          'combo',
                        mode:           'local',
                        value:          '<none>',
                        triggerAction:  'all',
                        forceSelection: true,
                        editable:       false,
                        id:           	'template2',
                        displayField:   'value',
                        valueField:     'name',
                        queryMode: 		'local',
                        width:210,
                        margins: {left: 15},
                        store:          Ext.create('Ext.data.Store', {
                          fields : ['name', 'value'],
                            data   : [
                                {name : '0', value: '<none>'},
                                {name : '1', value: 'linux'},
                                {name : '2', value: 'windows'}
                            ]
                        })
                    },
                   	]// end template                            
                   },
                   ]
        	}, 
        	{
                xtype: 'fieldset',
                title: i18n.getMsg('nodeconf.generalConf'), //'General configuration',
                defaultType: 'textfield',
                border: true,
                collapsible:true,
				anchor: '100%',
                items: [
                	{
                		xtype: 'fieldcontainer',
                    	layout: 'hbox',
                    	defaults: {labelWidth:180},
                    	items:[
                    		{
                				xtype:'textfield',
		                        fieldLabel: i18n.getMsg('generic.description'),
		                        id: 'Description',
		                        name: 'Description',
		                        width:570,
		                        allowBlank:true,
                			}
                		]
	                },{
                    	xtype: 'fieldcontainer',
                    	layout: 'hbox',
                    	defaults: {labelWidth:180},
                    	items:[
                    		{
						    	xtype:'combo',
				                align:'left',
				                id:'Group',
				                name: 'Group',
				                fieldLabel: i18n.getMsg('nodeconf.group'),
				                store:ngStore,
				                valueField:'Id',
				                displayField:'Name',
				                typeAhead: false,
				                allowBlank:true,
				                queryMode:'remote',
				                forceSelection:true,
				                selectOnFocus:true,
				                triggerAction: 'all',
				                width:400,
				            },{
	                    		xtype:'combo',
				                align:'left',
				                id:'LogLevel',
				                name: 'LogLevel',
				                fieldLabel: i18n.getMsg('nodeconf.logLevel'),
				                store: new Ext.data.ArrayStore({
				                    fields: ['bType'],
				                    data : [['TRIVIA'],['DEBUG'],['INFO'],['WARNING'], ['ERROR']]
				                }),
				                valueField:'bType',
				                displayField:'bType',
				                typeAhead: true,
				                allowBlank:false,
				                mode: 'local',
				                triggerAction: 'all',
				                value:'INFO',
				                width:280,
						      },
                    	]
	                },
                	{
                    	xtype: 'fieldcontainer',
                    	layout: 'hbox',
                    	defaults: {labelWidth:180},
                    	items:[		
                    		{
                				xtype:'textfield',
		                        fieldLabel: i18n.getMsg('nodeconf.logFile'),
		                        id: 'LogFile',
		                        name: 'LogFile',
		                        align:'left',
		                        width:400,
		                        allowBlank:false,
		                        blankText:'A value is required.'
                			},{
							    xtype:'checkbox',
							    id: 'LogToSyslog',
							    name: 'LogToSyslog',
							    fieldLabel: i18n.getMsg('nodeconf.syslog'),
							    width:400
							},
                		]
            		},
						
                ]               
        	}, // end field set general conf
        	{
                xtype: 'fieldset',
                id:'backupContainer',
                title: i18n.getMsg('nodeconf.backupConf'), //'Storage configuration',
                defaultType: 'textfield',
                border: true,
                collapsible:true,
				anchor: '100%',
                items:[
                	{
	                    	xtype: 'fieldcontainer',
	                    	layout: 'hbox',
	                    	defaults: {labelWidth:180},
	                    	items:[
								{
						            xtype:'textfield',
                        			id: 'IndexPath',
                        			name: 'IndexPath',
                        			fieldLabel: i18n.getMsg('nodeconf.indexesFolder'),
                        			width:376,
						        },
						        new Ext.Button( { 
					                 width:24,
					                 id: 'browseButtonIndexFolder',
					                 icon:'/images/browse.png', 
					                 flex:0,
					                 handler: function(){
					                 	var pathSeparator = '/';
					                 	if(checkedNodes[0].data['OS'].substr(0,2) == "NT")
	                 						pathSeparator = '\\';
					                 	handleBrowse(checkedId, pathSeparator, setIndexesPathFromBrowser, false);
					                 },
					                 allowBlank:false,
			                         blankText:'A value is required.',
					                 initComponent: function() {
										   if(checkedNodes.length > 1) // if several nodes are selectionned, we disable browsing as it makes no sense.
												this.disable();
									 }
								}),
								new Ext.form.ComboBox({
					                id:'IndexCacheRetention',
					                name:'IndexCacheRetention',
					                fieldLabel:i18n.getMsg('nodeconf.indexRetention'),
					                store: new Ext.data.ArrayStore({
					                    fields: ['bType', 'daysV'],
					                    data : [['1 day',1],['2 days',2],['1 week',7],['2 weeks',14],['3 weeks',21],['1 month',31],['2 months',61]]
					                }),
					                valueField:'daysV',
					                displayField:'bType',
					                typeAhead: true,
					                mode: 'local',
					                triggerAction: 'all',
					                value:7,
					                selectOnFocus:true,
					                width:280,
					                labelWidth: 180
					        	}),
	                    	]
	                  },
	                   {
	                    	xtype: 'fieldcontainer',
	                    	layout: 'hbox',
	                    	defaults: {labelWidth: 180},
	                    	items:[
						        {
						            xtype:'checkbox',
                        			id: 'hasQuota',
                        			fieldLabel: i18n.getMsg('nodeconf.hasQuota'), 
                        			width:400,
                        			allowBlank:false,
                        			listeners:{
                        				change:function(thisCheckbox, newValue, oldValue, options){
                        					if(newValue == true){
                        						Ext.getCmp('Quota').enable();
                        						Ext.getCmp('quotaUnit').enable();
                        					}
                        					else{
                        						Ext.getCmp('Quota').disable();
                        						Ext.getCmp('quotaUnit').disable();
                        					}
                        				}
                        			}
						        },{
						            xtype:'numberfield',
                        			id: 'Quota',
                        			fieldLabel:i18n.getMsg('nodeconf.quota'),
                        			width:235,
                        			value:0,
                        			minValue:0,
                        			disabled:true,
						        },{
						       		xtype:'combo',
					                height:24,
					                align:'left',
					                id:'quotaUnit',
					                store: new Ext.data.ArrayStore({
					                    fields: ['bType'], data : [['MB'],['GB'],['TB']]
					                }),
					                valueField:'bType',
					                displayField:'bType',
					                typeAhead: false,
					                mode: 'local',
					                triggerAction: 'all',
					                value:'GB',
					                selectOnFocus:true,
					                forceSelection:true,
					                width:45,
					                disabled:true							               
					      		}
					      	]
					}
                ]
            },
        	{
                xtype: 'fieldset',
                id:'storageContainer',
                title: i18n.getMsg('nodeconf.storageConf'), //'Storage configuration',
                defaultType: 'textfield',
                border: true,
                collapsible:true,
                collapsed:false,
                height:100,
                items:[
                		{ // storage section
		        				xtype: 'fieldcontainer',
		                    	layout: 'hbox',
		                    	defaults: {labelWidth:180},
		                    	items:[
	                    			{
	                    				xtype:'checkbox',
	                    				id:'isStorageNode',
	                    				fieldLabel: i18n.getMsg('nodeconf.storageNode'),
	                    				width:230,
	                    				checked:false,
	                    				 handler: function() {
			                    			if(this.getValue() == false){
			                    				Ext.getCmp('storageContainer').disable();
			                    				Ext.getCmp('isStorageNode').enable();
			                    			}
			                    			else{
			                    				Ext.getCmp('storageContainer').enable();
			                    			}
			                			}
	                    			},{
					        			xtype:'numberfield',
					        			name:'StoragePriority',
					        			width:170,
					        			fieldLabel: i18n.getMsg('nodeconf.priority'),
					        			value: 2, minValue: 0, maxValue: 99,
					        			labelWidth:90,
					        		},{
	                    				xtype:'combo',
						                id: 'StorageGroup',
						                name : 'StorageGroup',
						                store: sgStore,
						                fieldLabel: i18n.getMsg('generic.storageGroup'),
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
						                typeAhead: false,
						                allowBlank:true,
				                        blankText:'A value is required.',
						                queryMode: 'remote',
						                triggerAction: 'all',
						                forceSelection:true,
						                selectOnFocus:true,
						                width:420
					        		}
	                    		]
	                    },
	                    {
		                    	xtype: 'fieldcontainer',
		                    	layout: 'hbox',
		                    	width: '50%',
		                    	defaults: {labelWidth:180},
		                    	items:[
	                    			{
				                        xtype:'textfield',
				                        labelAlign:'left',
				                        id: 'ListenIP',
				                        name: 'ListenIP',
				                        fieldLabel: i18n.getMsg('nodeconf.listenIP'),
				                        width:400,
				                        //regex: new RegExp('(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)', null),
				                        //regexText: 'Invalid IP address'
			                   	 	},{
							            xtype: 'numberfield',
							            id: 'ListenPort',
							            name: 'ListenPort',
							            fieldLabel: i18n.getMsg('nodeconf.listenPort'),
							            allowBlank:false,
				                        blankText:'A value is required.',
							            value: 52562,
							            minValue: 0,
							            maxValue: 65535,
							            width:280
							        }
			                   	 ]
	                    },{
		                    	xtype: 'fieldcontainer',
		                    	layout: 'hbox',
		                    	width:'50%',
		                    	defaults: {labelWidth:180},
		                    	items:[
							        {
							            xtype:'textfield',
	                        			id: 'StoragePath',
	                        			name: 'StoragePath',
	                        			fieldLabel:i18n.getMsg('nodeconf.storageFolder'),
	                        			width:376,
	                        			allowBlank:false,
				                        blankText:'A value is required.',
							        },
							        new Ext.Button( {
						                 width:24,
						                 id: 'browseButtonStorageDir',
						                 icon:'/images/browse.png', 
						                 flex:0,
						                 handler: function(){
						                 	handleBrowse(checkedId, setStorageFolderFromBrowser, false);
						                 },
						                 descriptionText:'' ,
						                 initComponent: function() {
											   if(checkedNodes.length > 1) // if several nodes are selectionned, we disable browsing as it makes no sense.
													this.disable();
										 }
									}),
									{
							            xtype:'numberfield',
	                        			id: 'formattedStorageSize',
	                        			name: 'formattedStorageSize',
	                        			fieldLabel: i18n.getMsg('nodeconf.storageSize'),
	                        			width:235,
	                        			value:0,
	                        			minValue:0
							        },{
							       		xtype:'combo',
						                height:24,
						                align:'left',
						                id:'unit',
						                store: new Ext.data.ArrayStore({
						                    fields: ['bType'], data : [['MB'],['GB'],['TB']]
						                }),
						                valueField:'bType',
						                displayField:'bType',
						                typeAhead: false,
						                mode: 'local',
						                triggerAction: 'all',
						                value:'GB',
						                selectOnFocus:true,
						                forceSelection:true,
						                width:45								               
						      		},{
							        	xtype:'hidden',
							        	name: 'StorageSize',
							        	id: 'StorageSize'
							        },
								]
							},
                ]
            },
            {
                xtype: 'fieldset',
                id:'delegationContainer',
                title: i18n.getMsg('nodeconf.delegationConf'), //'Storage configuration',
                defaultType: 'textfield',
                border: true,
                collapsible:true,
				anchor: '100%',
                items:[]
            }      			
        ],
	});
	
	var pluginsFormPanel = new Ext.form.Panel({/*Ext.widget('form', {*/
               
        id:'pluginsFormPanel',
        title: i18n.getMsg('nodeconf.pluginsTab'),
        //model: 'Node',
        //url : '/api/Node/'+checkedId+'/Configuration/',
        monitorValid:true,
        border: false,
        scroll: 'vertical',
        bodyPadding: 10,
		height: 460,
        items: []
    });
	
	var permissionsFormPanel = new Ext.form.Panel({/*Ext.widget('form', {*/
               
        id:'permissionsFormPanel',
        title: i18n.getMsg('nodeconf.permissionsTab'),
        //model: 'Node',
        //url : '/api/Node/'+checkedId+'/Configuration/',
        monitorValid:true,
        border: false,
        scroll: 'vertical',
        bodyPadding: 10,
		height: 460,
        items: []
    });
 	var tabz = new Ext.tab.Panel({
	    activeTab: 0,
	    items:[
	    	configFormPanel,
	    	pluginsFormPanel,
	    	permissionsFormPanel
	    ] , 
	})
            
 var nodeConf =  new Ext.Window({
 		id:'nodeConf',
        width:900,
       	height:500,
        plain: true,
        title:'', 
		autoScroll:false,
        modal:true,
        autoDestroy :true,
        monitorValid:true,
        resizable:true,
		items:tabz,
		dockedItems: [{
		    xtype: 'toolbar',
		    dock: 'bottom',
		    ui: 'footer',
		    align:'right',
		    items: [
        	 {
                text: i18n.getMsg('generic.ok'), //'Apply',
                formBind:true,
                handler: function() {
            		
            		Ext.define('custom.writer.Json', {
				            extend: 'Ext.data.writer.Json',
				            getRecordData: function(record) {
				                Ext.apply(record.data, record.getAssociatedData());
				                return record.data;
				            }
					});
            		var customWriter = new custom.writer.Json({writeAllFields: true});
            		
            		var record = configFormPanel.getForm().getRecord();
            		// set size values properly
            		var multiplier = 1024*1024;
					if(Ext.getCmp('unit').getValue() == 'GB')
						multiplier *= 1024;
					if(Ext.getCmp('unit').getValue() == 'TB')
						multiplier *= 1024*1024;	
					console.log('storage size : unit='+Ext.getCmp('unit').getValue()+', value='+Ext.getCmp('StorageSize').getValue() * multiplier);
					
					multiplier = 1024*1024;
					if(Ext.getCmp('quotaUnit').getValue() == 'GB')
						multiplier *= 1024;
					if(Ext.getCmp('quotaUnit').getValue() == 'TB')
						multiplier *= 1024*1024;	
					
            		record.getProxy().setWriter(customWriter);
	            	console.log('saving personal settings (record #'+record.get('Id')+' )... ');
	    			configFormPanel.getForm().updateRecord(record);
	    			record.set('StorageSize',  Ext.getCmp('formattedStorageSize').getValue() * multiplier);
	    			record.set('Quota', Ext.getCmp('Quota').getValue() * multiplier);
	    			record.save();
	    			this.up('window').close();
            	}
        	},{
                text: i18n.getMsg('generic.cancel'), 
                handler: function() {
                	//this.up('form').getForm().reset();
                	this.up('window').close();
            	}
        	},{
                text: i18n.getMsg('nodeconf.addBackupSet'), 
                handler: function() {
                	window.location = 'AddBackupSet4.html?node='+checkedId;
            	}
        	}
        ]
       }] //end dockedItems
    });
	
	
		var nodeMgr = Ext.ModelManager.getModel('Node');
		nodeMgr.setProxy({
	        type: 'ajax',
	        url: '/api/Node/'+checkedId,
	        extraParams: {format: 'json'}
	    });
		nodeMgr.load(checkedId, {
			scope: this, 
	    	success: function(theNode) {
	    		console.log('loaded currently logged user :'+theNode.get('Name'));
				configFormPanel.getForm().loadRecord(theNode);
				// let's display the node configuration (hub-side)
				var currentNode = Ext.getCmp('clientNodesTree').getStore().getNodeById(checkedId); //.findRecord('id', checkedId, 0, false, true, true);
				var storageSize = currentNode.get('StorageSize');
				var formatedSize = 0;
				var unit = "";
				if(storageSize >= 1024 * 1024 *1024 * 1024){
					formatedSize = Math.round(storageSize/1024/1024/1024/1024);
					unit = 'TB';
				}
				else if (storageSize >= 1024 * 1024 *1024){
					formatedSize =  Math.round(storageSize/1024/1024/1024);
					unit = 'GB';
				}
				else{
					formatedSize =  Math.round(storageSize/1024/1024);
					unit = 'MB';
				}
				if(Ext.getCmp('StorageSize') != undefined){
					Ext.getCmp('formattedStorageSize').setValue(formatedSize);
					Ext.getCmp('unit').setValue(unit);
				}
				if(storageSize >0 && currentNode.get('StoragePriority') >0 && currentNode.get('StoragePath') != '')
					Ext.getCmp('isStorageNode').setValue(true);
				
				var quota = currentNode.get('Quota');
				var formatedQuota = 0;
				var quotaUnit = "";
				if(quota >= 1024 * 1024 *1024 * 1024){
					formatedQuota = Math.round(quota/1024/1024/1024/1024);
					quotaUnit = 'TB';
				}
				else if (storageSize > 1024 * 1024 *1024){
					formatedQuota =  Math.round(quota/1024/1024/1024);
					quotaUnit = 'GB';
				}
				else{
					formatedQuota =  Math.round(quota/1024/1024);
					quotaUnit = 'MB';
				}
				Ext.getCmp('Quota').setValue(formatedQuota);
				Ext.getCmp('quotaUnit').setValue(quotaUnit);
				if(quota >0)
					Ext.getCmp('hasQuota').setValue(true);
				
				ngStore.load(
					{callback:function(){
						var gcombobox = Ext.getCmp('Group');
						var grecord = gcombobox.store.findRecord('Id', currentNode.get('Group'));
						gcombobox.setValue(grecord.get('Id'));
					}}
				);
				sgStore.load(
					{callback:function(){
						var sgcombobox = Ext.getCmp('StorageGroup');
						var sgrecord = sgcombobox.store.findRecord('Id', currentNode.get('StorageGroup'));
						sgcombobox.setValue(sgrecord.get('Id'));
					}}
				);
				
				// Now load node's plugins
				var data = theNode.data;
			    for (i = 0; i < theNode.associations.length; i++) {
			        var association = theNode.associations.get(i);
			        if(association.name != 'Plugins')
			        	continue;
			        data[association.name] = null;
			        childStore = theNode[association.storeName];

			        childStore.each(function(childRecord) {
			        	pluginsFormPanel.add({
			        		xtype: 		'fieldset',
			        		border: 	true,
			        		title: 		childRecord.get('Name'),
			        		collapsible:true,
			        		collapsed: 	true,
			        		labelAlign: 'left',
			        		items:[
			        			{xtype:'displayfield', value: '<b>'+i18n.getMsg('generic.name')+'</b> : '+childRecord.get('Name')},
			        			{xtype:'displayfield', value: '<b>'+i18n.getMsg('generic.version')+'</b> : '+childRecord.get('Version')},
			        			{xtype:'displayfield', value: '<b>'+i18n.getMsg('generic.version')+'</b> : '+childRecord.get('Category')},
			        			{
			        				xtype:'displayfield',
			        				value: '<b>'+i18n.getMsg('generic.kind')+'</b> : '
			        					+(childRecord.get('IsProxyingPlugin') ? 'Proxy' : 'Local client')
			        			},
			        			{xtype:'checkbox', boxLabel: 'Enabled', value:childRecord.get('Enabled')}
			        		]
			        	});
			        });
			    }
		
				nodeConf.setTitle(i18n.getMsg('nodeconf.title')+' #'+theNode.get('Id')+' '+theNode.get('Name')+' ('+theNode.get('IP')+')');
			},
			failure: function(response) {
				console.log('error : '+response);
			}
		 });
		
		
	
		
		//gcombobox.setValue(3);
    //}
	//});
	nodeConf.show();
//}); / end i18n
} // end GetConfigWindow	 
	

	function expandAll(){
		Ext.getCmp('tree').getRootNode().cascadeBy(function(r) {  r.expand();   })
	}
	
	Ext.get('clTitle').dom.innerText = i18n.getMsg('clientnodes.title');
	
	var groupingFeature = Ext.create('Ext.grid.feature.Grouping',{
        groupHeaderTpl: '{name} ({rows.length})',
        depthToIndent:15,
    });
  
  	var tree = Ext.create('backo.NodesTree',{
  		id:'clientNodesTree',
  		shown: ['IP', 'Name', 'Version', 'LastConnection', 'OS', 'Certificate', 'Quota'],
        height: '100%',
        width:'60%',
        align: 'left',
        layout:'fit',
        anchor:'100%',
        store: nStore,
        dockedItems: [{
		    xtype: 'toolbar',
		    dock: 'bottom',
		    style:'bottom:0px;',
		    padding:0,
		    margins:1,
		    items: [
		    	{
		        	xtype:'button',
	        		text:i18n.getMsg('nodegroup.add'), //'&nbsp;&nbsp;Configure',
	        		id: 'addBtn',
	               	iconCls:'icon-btn',
	                icon:'/images/add.png',
	                width:90,
	                height:22,	  
	                border:1,             
	               	handler:function(){
	               		getGroupConfig(Ext.create('NodeGroup'));
	               	}
	        	},{
		        	xtype :'button',
	        		text: i18n.getMsg('nodegroup.delete'), 
	        		id: 'deleteBtn',
	               	iconCls:'icon-btn',
	                icon:'/images/delete.png',
	                width:90,
	                height:22,	  
	                border:1,             
	               	handler:function(){
	               		//var toBeDeleted = groupsStore.getById(tree.getSelectionModel().getSelection()[0].get('Id'));
	               		//console.log('asked to delete sg #'+toBeDeleted.get('Id'));
	               		//groupsStore.remove(toBeDeleted);
	               		//groupsStore.sync();
	               		//sgStore.reload();
	               		alert('not implemented');
	               	},
	               	disabled:true,
	        	},{
		        	xtype:'button',
	        		text:  i18n.getMsg('nodestree.configureBtn'),
	        		id: 'configureBtn',
	               	iconCls:'icon-btn',
	                icon:'/images/conf.png',
	                width:85,
	                height:27,	  
	                border:1,             
	               	handler:GetConfigWindow,
	               	disabled:true,
	        	},'-',
	        	{
	        		xtype:'button',
	        		text:  i18n.getMsg('nodestree.authorizeBtn'),
	        		id:'authorizeBtn',
	                iconCls:'icon-btn',
	                icon:'/images/unlocked.png',
	                width:85,
	                height:27,
	                handler:function(){ManageLockStatus('False');},
	               	disabled:true,
	        	},'-',
	        	{
	        		xtype:'button',
	        		text:  i18n.getMsg('nodestree.lockBtn'),
	        		id:'lockBtn',
	                iconCls:'icon-btn',
	                icon:'/images/locked.png',
	                width:85,
	                height:27,
	                listeners:function(){ManageLockStatus('True');},
	               	disabled:true,
	            },'-',
	          	{
	          		text:  i18n.getMsg('nodestree.expand'),
		            icon:'/images/plus.gif',
		            handler : function(){
	                	Ext.getCmp('clientNodesTree').getRootNode().cascadeBy(function(r) {  
	                		if(toBeExpanded == true){
	                			r.expand();  
	                		}
	                		else{
	                			r.collapse();
	                		}
	                		Ext.getCmp('clientNodesTree').getView().refresh();
	                	 
	                	})
	                	if(toBeExpanded == true)
                			toBeExpanded = false;
                		else{
                			toBeExpanded = true;
                			Ext.getCmp('clientNodesTree').getRootNode().expand();
                		}
                		
	            	}
	          	},{
		    		xtype			: 'combo',
		    		id				: 'groupBy',
		    		stateful		: true,
		    		stateId			: 'tasksGroupByState',
		    		mode			: 'local',
		            value			: 'none',
		            triggerAction	: 'all',
		            forceSelection	: true,
		            allowBlank		: true,
		            editable		: false,
		            fieldLabel		: i18n.getMsg('runningTasks.groupBy'),
		            displayField	: 'value',
		            valueField		: 'name',
		            queryMode		: 'local',
		            width			: 180,
		    		store:Ext.create('Ext.data.Store', {
		                  fields : ['name', 'value'],
		                    data   : [
		                        {name : 'OS',   value: i18n.getMsg('nodestree.os')},
		                        {name : 'Version',  value: i18n.getMsg('nodestree.version')},
		                        {name : 'Status', value: i18n.getMsg('runningTasks.status')},
		                        {name : 'SSL', value: i18n.getMsg('runningTasks.runningStatus')},
		                       	{name : 'Kind', value: i18n.getMsg('runningTasks.runningStatus')},
		                    ]
			        }),
		           // listeners:{
		            //	'change': function( thisCombo, newValue, oldValue, options ){
		            //		if(newValue == '')
		            //			nStore.ungroup();
		            //		else
		            //			nStore.group(newValue);
		            //	}
		           // }
		    	}
	          	
	          	
		    ]// end dockbar items
		}],
			
    	listeners: {
    		itemmove:function(thisObj, oldParent, newParent, idx, eOpts){
          		console.debug('changed node group!');
          		if(newParent.get('Group') == -1){
          			thisObj.set('Group', newParent.get('Id'));
          			thisObj.save();	
          		}
          		//nStore.sync();
          	},
	   		selectionchange: function(thisObj, selected, eOpts){
				if(selected.length >1){
		       		Ext.getCmp('authorizeBtn').enable();
		        	Ext.getCmp('lockBtn').enable();
	    			Ext.getCmp('configureBtn').disable();       			
		        }
	    		if(selected.length ==1){
	    			Ext.getCmp('configureBtn').enable();
	    			Ext.getCmp('authorizeBtn').enable();
		        	Ext.getCmp('lockBtn').enable();
		        }
		        else{
		        	Ext.getCmp('configureBtn').disable();
		        	Ext.getCmp('authorizeBtn').disable();
		        	Ext.getCmp('lockBtn').disable();
		        }
          	},
          	select: function( thisObj, record, index, eOpts ){
          		Ext.getCmp('nodeBS').getStore().getProxy().url = '/api/BackupSets/'+record.get('Id');
          		Ext.getCmp('nodeBS').getStore().load();
          	}
	 	}
  	
  	});  // end client nodes tree
    
    var nodeBSStore  = Ext.create('Ext.data.JsonStore', {
    	autoLoad:false,
        model: 'BackupSet',
        proxy: {
            type: 'ajax',
            extraParams: {format: 'json'}
        }
    });
    
    var nodeBSView = Ext.create('Ext.view.View', {
		extend: 'Ext.view.View',
		id: 'nodeBS',
		width: '40%',
		height: '100%',
		autoScroll: true,
		padding: 10,
		selModel: { /*deselectOnContainerClick: false*/},
		store: nodeBSStore,
		tpl: Ext.create('Ext.XTemplate',
		    '<tpl for=".">'+
	            '<div class="sidebar-title"><img class="i" src="/images/bs.png" title="#{Id}"/>    &nbsp;&nbsp;<b>{Name}</b></div>'+
	            '<table class="gridCell nodeBSView bsDetail" style="width:95%; max-width:95%; margin:5px;">'+
		    		'<tr>'+
			            '<td></td>'+
			            '<td><b>{[this.localize("browser.path")]} </b></td>'+
			            '<td>{[this.localize("addbs.whatToBackup.includeRule")]} </td>'+
			            '<td>{[this.localize("addbs.whatToBackup.excludeRule")]}</td>'+
			            '<td>{[this.localize("addbs.whatToBackup.recursive")]}</td></tr>'+
			            '<tpl for="BasePaths">'+
			                '<tr><td><img style="height:20px;" src="/images/f-g.png"/></td><td><b>{Path}</b></td>'+
			                '<td>{IncludePolicy}</td><td>{ExcludePolicy}</td><td>{Recursive}</td></tr>'+
			            '</tpl>'+
	            '</table>'+
	            '<table class="nodeBSView bsDetail">'+
	            	'<tr>'+
	            		'<td><b>{[this.localize("addbs.whatToBackup.dataFlags")]}	</b></td>'+
	            		'<td>:	</td>'+
	            		'<td colspan="2"> {[this.showCFlags(values.DataFlags)]} (client),	{[this.showSFlags(values.DataFlags)]} (storage)	</td>'+
	            	'</tr><tr>'+
	            		'<td><b>{[this.localize("addbs.ret.retention")]}	</b></td>'+
	            		'<td>:</td>'+
	            		'<td>{RetentionDays} {[this.localize("generic.dayz")]}	</td>'+
	            		'<td><b>Snapshots retention 			</b></td>'+
	            		'<td>:	</td>'+
	            		'<td> {SnapshotRetention} {[this.localize("generic.dayz")]}	</td>'+
	            	'</tr><tr>'+
		            	'<td><b>{[this.localize("generic.storageGroup")]} 	</b></td>'+
		            	'<td>:</td>'+
		            	'<td> {StorageGroup}	</td>'+
		            	'<td><b>{[this.localize("addbs.parallelism")]}</b></td>'+
		            	'<td>:</td><td> {Parallelism}	</td>'+
	            	'</tr>'+
	            '</table>'+
	            '<div class="actionBtns" name="{Id}"></div>'+
	            '<br/><br/><hr><br/>'+
		    '</tpl>',
		{
			showCFlags:function(val){
				var flagIcons = '';
            	if(val & dataFlags.CCompress)
            		flagIcons += '<img src="/images/compress.png"/>';
            	if(val & dataFlags.CEncrypt)
            		flagIcons += '<img src="/images/encrypt.png"/>';
            	if(val & dataFlags.CDedup)
            		flagIcons += '<img src="/images/dedup.png"/>';
            	if(flagIcons == '')
            			flagIcons = 'None';
            	return flagIcons;
            },
            showSFlags:function(val){
				var flagIcons = '';
            	if(val & dataFlags.SCompress)
            		flagIcons += '<img src="/images/compress.png"/>';
            	if(val & dataFlags.SEncrypt)
            		flagIcons += '<img src="/images/encrypt.png"/>';
            	if(val & dataFlags.SDedup)
            		flagIcons += '<img src="/images/dedup.png"/>';
            	if(flagIcons == '')
            			flagIcons = 'None';
            	return flagIcons;
            },
            localize:function(val){
            	return i18n.getMsg(val);
            }
		}),
		listeners:{
		    refresh:function(){
		        var renderSelector = Ext.query('div.actionBtns'); 
		            for(var i in renderSelector){
		                Ext.create('Ext.button.Button',{
		                    text:'Edit...',
		                    margin:5,
		                    renderTo:renderSelector[i],
		                    href: '/html/AddBackupSet4.html?bs='+renderSelector[i].getAttribute('name')
		                   /* handler:function(thisObj, evt){
		                    	window.location = '/html/AddBackupSet4.html?bs='+renderSelector[i].getAttribute('name');
		                    }*/
		                });  
		                Ext.create('Ext.button.Button',{
		                    text:'Backup now',
		                    itemId:''+renderSelector[i].getAttribute('name'),
		                    margin:5,
		                    icon:'/images/start.png',
		                    renderTo:renderSelector[i],
		                    handler:function(thisObj, evt){
		                    	console.log(' button '+renderSelector[i].getAttribute('name'));
		                    	Ext.Ajax.request({
				                   	url: '/api/BackupSet/'+this.getItemId()+'/Start/',
				                   	method:'GET',
				                	params:{format:'json'},
				                	failure: function(response, opts) {
								        alert('server-side failure :' + response.responseText);
								    }
				                });
		                    }
		                });    
		            } 
		    }
		}
	});
	
    var treesPanel = Ext.create('Ext.panel.Panel', {
    	id:'masterPanel',
    	border:true,
    	height: '100%',
	    layout: 'hbox',
	    items: [tree, nodeBSView /*, bsTree*/],
	    renderTo: Ext.get('panel')
	});
	//nStore.load({params:{node:'root'}});
	
	var getGroupConfig = function(ng){
		var context = 'create';
		if(ng.get('Id') > 0)
			context = 'edit';
			
		var form = new Ext.form.Panel({/*Ext.widget('form', {*/
                id:'ngConfigFormPanel',
                model: 'NodeGroup',
                url : '/api/StorageGroups',
                monitorValid:true,
                border: false,
                layout: 'fit',
                bodyPadding: 10,
                margin:10,
				fieldDefaults:{labelWidth:180},
                items: [
	               	{
	                    xtype: 'fieldset',
	                    border: false,
	                    items: [
	                    	{
	                    		xtype:'textfield',
	                    		name: 'Name',
	                    		fieldLabel: i18n.getMsg('generic.name'),
	                    		width:380
	                    	},{
	                    		xtype:'textfield',
	                    		name: 'Description',
	                    		fieldLabel: i18n.getMsg('generic.description'),
	                    		width:500
	                    	}
			            ]
	                }
	            ]
	    });
	    
		var sgConf =  new Ext.Window({
	 		id:'sgConf',
	        width:600,
	       	height:190,
	        plain: true,
	        title:'', 
			scroll:false,
	        modal:true,
	        autoDestroy :true,
	        monitorValid:true,
	        resizable:true,
			items:form,
			dockedItems: [{
			    xtype: 'toolbar',
			    dock: 'bottom',
			    ui: 'footer',
			    align:'right',
			    items: [
	        	 {
	                text: i18n.getMsg('generic.ok'), 
	                formBind:true,
	                handler: function() {
	            		form.getForm().updateRecord(ng);
	            		var cap = 0;
	            		
		    			ng.save();
		    			this.up('window').close();
	            	}
	        	},{
	                text: i18n.getMsg('generic.cancel'), 
	                handler: function() {
	                	this.up('window').close();
	            	}
	        	},
	        ]
	       }] //end dockedItems
    	});
    	form.loadRecord(ng);
    	
    	sgConf.show();
	
	}
	
	
});
});