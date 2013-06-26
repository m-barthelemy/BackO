Ext.onReady(function () {
	Ext.Loader.setConfig({
        enabled: true,
        disableCaching: false,
        paths: {
            'Extensible': '/Extensible/src',
        }
    });
    Ext.require([
	 	'Ext.data.proxy.Rest',
	    'Ext.data.*',
	    'Ext.grid.*',
	    'Ext.tree.*',
	    'Ext.form.*',
	    'Ext.window.*',
	    'Extensible.calendar.data.MemoryCalendarStore',
	    'Extensible.calendar.data.EventStore',
	    'Extensible.calendar.CalendarPanel',
	    'Extensible.calendar.data.*',
	    'Extensible.calendar.*',
	    'backo.ClearableBox',
	    'backo.TaskCalMapping'
	]);
	

i18n.onReady(function(){

	Ext.tip.QuickTipManager.init(true, {maxWidth: 450,minWidth: 150, width:350 });
	Ext.get('restoreTitle').dom.innerText = i18n.getMsg('restore.title');
	var restoreNode = null;
	var restoreDestNode = null;
	var restoreBS = '';
	function GetRestoreBS(){
		return restoreBS;
	}
	            	
	var nStore = new Ext.data.TreeStore( {
		model: 'Node',
		storeId:'nStore',
		proxy: {
		    type: 'ajax',
		    url: '/api/Nodes',
		    extraParams: {format: 'json'}
		},
		root:{expanded: false },
		folderSort: true,
		listeners:{
			load:function( thisObj, node, records, successful, eOpts ){
				Ext.each(records, function (rec){
					rec.set('leaf', rec.get('Group') != -1);
					if(rec.get('Group') != -1){
						rec.set('checked', false );
						// set online/offline status icon
						if(rec.get('Status') == 'Idle' || rec.get('Status') == 'Backuping' || rec.get('Status') == 'Restoring')
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
 
         	
  var dnStore = new Ext.data.TreeStore( {
  		storeId:'dnStore',
        model: 'Node',
        proxy: {
	        type: 'ajax',
	        url: '/api/Nodes/Online',
	        extraParams: {format: 'json'}
	    },
        root:{expanded: false},
        listeners:{
		    load:function( thisObj, node, records, successful, eOpts ){
		    	console.debug('destination nodes tree loaded');
	    		Ext.each(records, function (rec){
					rec.set('leaf', rec.get('Group') != -1);
					if(rec.get('Group') > -1)
						rec.set('checked', false );
					//if(rec.get('Group') == -1)
					//	rec.set('Uid', 'g-'+rec.get('Uid') );
				});
	    	}
	    }
  });
  
  var tasksEvtStore = new Extensible.calendar.data.MemoryEventStore();
  
  var clientNodesTree = Ext.create('backo.NodesTree',{
  		id:'clientNodesTree',
  		shown: ['IP', 'Name', 'Version', 'OS'],
        height:350,
        width:475,
        store: nStore,
        padding:'10px 20px 0px 10px',
        listeners:{
        	'checkchange': function(node, checked){        	
		       	if (Ext.getCmp('clientNodesTree').getChecked().length == 1 && checked){
		       		restoreNode = node.data['Id'];
		       		var firstLevelChild = clientNodesTree.getRootNode().childNodes;
		       		Ext.each(firstLevelChild, function(child, index){
		       			if(child != null){
			       			if((child.hasChildNodes() && !child.contains(node))){
			       				child.remove(false);
			       			}
			       			else{
			       				var childNodes = [].concat(child.childNodes);
								Ext.each(childNodes, function(leafChild){
				       				if (leafChild != null && !leafChild.isRoot() && leafChild.isLeaf() && (leafChild.get('Id') != node.get('Id'))) {
		        							leafChild.remove(false);
		        					}
			       				});
			       			}
			       		}
        			});
        			destNodesTree.enable();
        			//destNodesTree.getStore().load();
        			var destNodes = Ext.getCmp('destNodesTree').getRootNode().childNodes;
        			Ext.each(destNodes, function(child, index){
		       			if(child.hasChildNodes()){
		       				var childNodes = [].concat(child.childNodes);
							Ext.each(childNodes, function(leafChild){
			       				if (leafChild != null && !leafChild.isRoot() && leafChild.isLeaf() && (leafChild.data['Id'] == restoreNode)) {
	        							//leafChild.data['checked'] = true;
	        							//leafChild.checked = true;
	        							leafChild.parentNode.expand();
	        					}
		       				});
		       			}
		       			else{
		       				if (child != null && !child.isRoot() && child.isLeaf() && (child.data['Id'] == restoreNode)) {
	        							//child.data['checked'] = true;
	        							child.parentNode.expand();
	        				}
		       			}
        			});
        			
		       	}
		       	else{
		       		clientNodesTree.getStore().load(); //setProxy(ajaxNodesProxy);
		       	}
		     }
        }
  });

  var destNodesTree = Ext.create('backo.NodesTree',{
  		id:'destNodesTree',
  		shown: ['IP', 'Name', 'Version', 'OS'],
        height:350,
        width:475,
        store: dnStore,
        disabled:true,
        style:'margin-top:15px;',
        listeners:{
        	'checkchange': function(node, checked){        	
		       	if (Ext.getCmp('destNodesTree').getChecked().length == 1 && checked){
		       		restoreDestNode = node.data['Id'];
		       		/*var firstLevelChild = destNodesTree.getRootNode().childNodes;
		       		Ext.each(firstLevelChild, function(child, index){
		       			if((!child.contains(node)) && child.hasChildNodes()){
		       				child.remove(false);
		       			}
		       			else{
		       				var childNodes = [].concat(child.childNodes);
							Ext.each(childNodes, function(leafChild){
			       				if (leafChild != null && !leafChild.isRoot() && leafChild.isLeaf() && (leafChild.id != node.id)) {
	        							leafChild.remove(false);
	        					}
		       				});
		       			}
        			});*/
        			/*bsStore.getProxy().url = '/api/BackupSets/'+node.data['Id'];
        			bsStore.load();*//*{
		  				params:{
		  					nodeId:restoreNode,
		  				}	
		       		});*/
        			restoreTypePanel.enable();
		       		restoreTypePanel.expand();
		       	}
		       	else{
		       		destNodesTree.getStore().load(); //setProxy(ajaxNodesProxy);
		       	}
		     }
        }
    });
        
 
  var nodesTreesFieldSet = new Ext.form.Panel({
        id:'nodesTreesFieldSet',
        title: '<img src="/images/1.png" class="gIcon"/>'+i18n.getMsg('restore.step1.title'), //Select a node',
        layout: {
            type: 'table',
            columns: 2
        },
        defaultType: 'textfield',
        border: true,
        hidden:false,
        height:365,
        fieldDefaults: {
            labelAlign: 'left',
            hideLabel: true
        },
        defaults:{
        	padding:0,
        	margins:0,
        },
        items: [clientNodesTree, destNodesTree]
 });		
 	                  
		
	var bsStore = new Ext.data.TreeStore({
		autoLoad:null,
	    model: 'BackupSet',
	    //root:{expanded:false},
	    root: {
		    children : []
		},
	   /* proxy:{
	    	type: 'ajax',
	        url : '/api/Backupsets',
	        reader: {
	            type: 'json',
	            //root: 'children'
	        },
	        extraParams:{format:'json'}
	    },*/
	    listeners:{
	    	load:function( thisObj, node, records, successful, eOpts ){
	    		Ext.each(records, function (rec){
					rec.set('checked', false);
					if(rec.getDepth() == 1){
						rec.set('iconCls', 'icon-bs');
					}
					
				});
	    	}
	    }
	});
		
	Extensible.calendar.data.EventModel.reconfigure();
	var bsHistoryStore = new Extensible.calendar.data.EventStore({
        autoLoad: false,
        remoteFilter:true,
        proxy: {
            type: 'ajax',
            url: '/api/Tasks/QueryHistory/',
            noCache: false,
            extraParams:{format:'json'},
            reader: {
                type: 'json',
                root: 'Items'
            },
        },
        listeners:{
        	beforeload:function(){
        		console.debug('bsHistoryStore : about to load');
        		Ext.Msg.show({
        				title:'Information',
        				msg:i18n.getMsg('restore.step3.waitMsg'),
        				buttons:false,
        				icon:'icon-loading',
        			});
        	}	
        }
    });
	    
	
		var bSetTree = new Ext.tree.Panel( {
			id:'bSetTree',
	        store: bsStore,
	        //autoRender: false,
	        autoShow: false,
	        hideHeaders: false,
	        rootVisible: false,
	        useArrows: true,
	        collapsible: false,
	        multiSelect: false,
	        singleExpand: false,
	        overlapHeader:true,
	        scroll:false,
	        height:300,
			width:400,
			lines:false,
			frame:false,
			padding:0,
			bodyPadding:'0px 0px 0px 0px',
			border:false,
			listeners:{
	        	'checkchange': function(node, checked){        	
			       	if (checked){
			       		restoreBS = node.data['Id'];
			       		var firstLevelChild = bSetTree.getRootNode().childNodes;
			       		Ext.each(firstLevelChild, function(child, index){
			       			if((!child.contains(node)) && !child.hasChildNodes()){
			       				child.data['checked']=false;;
			       			}
			       			else{
			       				var childNodes = [].concat(child.childNodes);
								Ext.each(childNodes, function(leafChild){
				       				if (leafChild != null && !leafChild.isRoot() && leafChild.isLeaf() && (leafChild.data['id'] != node.data['id'])) {
		        							child.data['checked']=false;
		        					}
			       				});
			       			}
	        			}); 
	        			Ext.Msg.show({
	        				title:'Information',
	        				msg:i18n.getMsg('restore.step3.waitMsg'),
	        				buttons:false,
	        				icon:'icon-loading',
	        			});
			       		bsHistoryStore.load({
			  				params:{
			  					from	: Ext.getCmp('restoreDate').getActiveView().getViewBounds().start, //''+Ext.Date.format(Ext.getCmp('restoreDate').getActiveView().getViewBounds().start, 'Y-m-d'),
			  					to		: Ext.getCmp('restoreDate').getActiveView().getViewBounds().end, //''+Ext.Date.format(Ext.getCmp('restoreDate').getActiveView().getViewBounds().end, 'Y-m-d'),
			  					bs		: restoreBS,
			  					statuses: 'Done',
			  					sizeOperator:'>',
			  					size:0,
			  					limit: 150
			  				}	
			       		});
			       		Ext.Msg.close();
			       	}
			       	else{
			       		//clientNodesTree.getStore().load(); //setProxy(ajaxNodesProxy);
			       	}
			     }
        	},
	        columns: [
	        {
	            xtype: 'treecolumn', 
	            text: i18n.getMsg('restore.step2.backupset'), //'path',
	           	flex: 1,
	           	//width:130,
	            sortable: true,
	            dataIndex: 'Name',
	            renderer: function(value, metaData, record, colIndex, store, view){
					if(record.get('leaf') != null && record.get('leaf') == true){
						return value; //record.get('path');
						//return bsStore.getNodeById(record.get('id')).data['path'];
					}
					else
						return '#'+record.get('Id')+' '+record.get('Name');
					//if(value != null)('Id')+' '
					//	
	            }
	        }/*,{
	            text: i18n.getMsg('restore.step2.path'), //'path',
	           	flex: 1,
	           	//width:150,
	            sortable: true,
	           	dataIndex: 'Path',
	            checked:false
	        },*/
	       /* {
	            text: i18n.getMsg('restore.step2.includeRule'), //'path',
	           	flex: 0,
	            sortable: true,
	            dataIndex: 'includerule',
	            checked:false
	        },
	        {
	            text: i18n.getMsg('restore.step2.excludeRule'), //'path',
	           	flex: 0,
	            sortable: true,
	            dataIndex: 'excluderule',
	            checked:false
	            //iconCls:iconCls
	        },*/
	    ]
	});
	
	var calStore = new Extensible.calendar.data.MemoryCalendarStore({
            data:[
	            { id:'Fulls',			title:"Fulls", 			color:2},
	            { id:'Refresh', 		title:"Refresh", 		color:8},
	            { id:'Differentials',	title:"Differentials",	color:10},
	            { id:'Incrementals',	title:"Incrementals",	color:31},
	            { id:'TransactionLogs',	title:"TransactionLogs",color:31}
            ]
	}); 

 var selectedTask = 0; // user-selected task to browse or restore
 
 var tasksCalendar = function(){
 	var cal = new Extensible.calendar.CalendarPanel( {
	    id:'restoreDate',
	    calendarStore : calStore,
	    eventStore:bsHistoryStore, //tasksEvtStore,
	    title: '', //<img src="/images/3.png" class="gIcon"/>Choose a version',
	    height:400,
	    width:'550',
	    padding:'0px 0px 10px 5px',
	    showDayView:false,
	    showWeekView :true,
	    showHeader:true,
	    weekViewCfg:{
	    	ddIncrement:60,
	    	scrollStartHour:0,
	    	hideBorders:false,
	    	showHeader:true,
	    	showTime:false,
	    	showTodayText:false,
	    },
	    showMonthView :true,
	    monthViewCfg:{
	    	moreText:'{0} backups',
	    	defaultEventTitleText:'',
	    	showHeader:true,
			showWeekLinks:true,
	    },
	    showMultiWeekView:false,
	    showNavJump:false, 
	    enableEditDetails:false,
	    showWeekLinks:true,
	    weekText:i18n.getMsg('generic.week'),
		monthText:i18n.getMsg('generic.month'),
	    //todayText:Ext.Date.format(new Date(), 'M Y'),
	    todayText:new Date().toLocaleString(), //Ext.Date.format(this.activeView.getViewBounds().start, 'M Y'),
	    listeners:{
	    	datechange:function(thisCal, startDate, viewStart, viewEnd){
	    		Ext.Msg.show({
				     title:'Information',
				     id:'searchRestoreWaitMsg',
				     msg: i18n.getMsg('restore.step3.waitMsg'), //'Searching backups which match your request... <br/> This could be a long operation.',
				     buttons: false,
				     icon: 'icon-loading',
				     renderTo:Ext.getBody(),
				});
	       		bsHistoryStore.getProxy().setExtraParam('from', viewStart);
				bsHistoryStore.getProxy().setExtraParam('to', viewEnd);
				bsHistoryStore.getProxy().setExtraParam('bs', restoreBS);
				bsHistoryStore.getProxy().setExtraParam('statuses', 'Done');
				bsHistoryStore.getProxy().setExtraParam('sizeOperator','>');
				bsHistoryStore.getProxy().setExtraParam('size',0);
				bsHistoryStore.getProxy().setExtraParam('limit',150);
	       		return false;
		    },
		    eventsrendered:function(){
		    	if(Ext.Msg.isVisible()) Ext.Msg.close();
		    },
		    eventclick:function(thisPanel, eventModelRec, htmlNode){
		    	var backupTimeSelected = document.getElementById(htmlNode.id).innerText;
		    	selectedTask = eventModelRec.data['EventId'];
		    	console.log('Selected Task #'+selectedTask);
		    	document.getElementById(htmlNode.id).style.background = 'green';
		    	document.getElementById(htmlNode.id).style.fontWeight = 'bold';
		    	document.getElementById(htmlNode.id).innerHTML = '<b>'+backupTimeSelected+'</b>(<i>#'+backupIdSelected+'</i>)';
		    	//alert('choose '+eventModelRec.data['startDate']+', id='+eventModelRec.data['EventId']);
		    	//Ext.getCmp('restoreDate').disable();
		    	restoreOptionsPanel.enable();
		    	restoreOptionsPanel.expand();
		    	return false;
		    }
		}
  	});
  	return cal;
  };
	
   var pathsStore = new Ext.data.Store( {
     	model:'BasePath',
     	clearOnLoad:false,
     	proxy: {type: 'memory'}
    });
    
	function setPaths(valuez){		
    	for(var i=0; i<valuez.length; i++){
    		console.log('add value to paths store : path='+JSON.stringify(valuez[i]));
    		pathsStore.add(Ext.create('BasePath',{
    			Path: valuez[i].data.CPath,
    			Id: valuez[i].data.Id,
    			Recursive: true,
    		}));
    	}
	}
	
  var makePathsGrid = function(){
		var pGrid = new Ext.grid.Panel( {
			id:'pGrid',
		    store: pathsStore,
		    height: 400,
		    width: '450',
		    margin:'0 0 0 10',
		    padding:'0 0 10 0',
		    scroll:'vertical',
		    viewConfig:{ markDirty:false },
		    plugins: [Ext.create('Ext.grid.plugin.CellEditing', {pluginId:'pathEdit', clicksToEdit: 1}) ],
		    columns: [
		        { 
		        	width: 30, flex:0,
		        	renderer:function(value, metaData, record, colIndex, store, view){
		        		if(record.get('Type') == "f")
		        			return '<img class="i" src="/images/f.png"/>';
		        		else if(record.get('Type') == "l")
		        			return '<img class="i" src="/images/l.png"/>';
		        		else
		        			return '<img class="i" src="/images/f-g.png"/>';
		        	}
		        },{ 
		        	text: i18n.getMsg('addbs.whatToBackup.path'), dataIndex: 'Path', width:300, flex: 2,
		        	renderer:function(value){
		        		return '<span data-qwidth="400" data-qtip="'+value+'">'+value+'</span>';
		        	},
		        	editor: {
		                xtype:'textfield',
		                allowBlank:false
		            }
		        },{ 
		        	text: i18n.getMsg('addbs.whatToBackup.recursive'), dataIndex: 'Recursive', width:70, flex: 0,
		        	xtype:'checkcolumn'
		        },{ 
		        	text: i18n.getMsg('addbs.whatToBackup.includeRule'), dataIndex: 'IncludePolicy', flex: 1,
		        	editor: {
		                xtype:'textfield',
		                allowBlank:true
		            }
		        },{ 
		        	text: i18n.getMsg('addbs.whatToBackup.excludeRule'),dataIndex: 'ExcludePolicy', flex: 1,
		        	editor: {
		                xtype:'textfield',
		                allowBlank:true
		            }
		        },
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
			                 text: '<img src="/images/browse.png" height="20" valign="middle"/> '+i18n.getMsg('addbs.whatToBackup.browse'), 
			                 handler: function(){
			                 	var nodeId = Ext.getCmp('clientNodesTree').getChecked()[0].get('Id');
				                 browseIndex('/', nodeId, selectedTask, '', 0, setPaths);
			                 }
						}),'-',
						new Ext.Button( {
			                 id: 'custAddBtn',
			                 text: '<img src="/images/add.png" height="20" valign="middle"/> '+i18n.getMsg('addbs.whatToBackup.manuallyAdd'), 
			                 handler: function(){
			                 	 var r = Ext.ModelManager.create({
				                    CPath: '',
				                    Recursive: true,
				                    IncludePolicy: '*',
				                    ExcludePolicy: '',
				                	}, 'BasePath');
				                pathsStore.insert(0, r);
				                pGrid.getPlugin('pathEdit')/*cellEditing*/.startEditByPosition({row: 0, column: 1});
			                 }
						}),
						new Ext.Button( {
			                 id: 'deleteBtn',
			                 disabled:true,
			                 text: '<img src="/images/delete.png" height="20" valign="middle"/> '+i18n.getMsg('generic.delete'), 
			                 handler: function(){
			                 	pathsStore.remove(pGrid.getSelectionModel().getSelection());
			                 }
						})
				    ]
				}
			]// end docked items/toolbar
		});// end basepaths grid
  		return pGrid;										
  };
  													
 var restoreTypePanel = new Ext.widget('form', {
        id:'restoreTypePanel',
        title:'<img src="/images/2.png" class="gIcon"/>'+i18n.getMsg('restore.step2.title'), //Choose what to restore',
        monitorValid:true,
        border: false,
        bodyPadding: 5,
		width:'620',
		height:'120',
		disabled:true,
        fieldDefaults: {labelAlign: 'left'},
        defaults: {
            margins: '0 0 0 0',
            padding: '0px 0px 5px 0px',
        },
        items: [
        	{
            	xtype: 'fieldcontainer',
            	fieldLabel: i18n.getMsg('restore.step2.type'),
            	layout:'hbox',
            	height:40,
            	border:true,
            	items:[
                    {
			            xtype: 'radiogroup',
			            hidden:false,
			            height:30,
			            border:true,
			            frame:true,
			            layout:'hbox',
			            items: [
			            	{
				                boxLabel: '<img style="vertical-align:middle;border:0;" src="/images/computer.png"/>&nbsp;'+i18n.getMsg('restore.step2.wholeSystem'),
				                name: 'rt',
				                inputValue: '0',
				                width:310,
				            },{
				                boxLabel: '<img style="vertical-align:middle;border:0;" src="/images/bs.png"/>&nbsp;'+i18n.getMsg('restore.step2.wholeBs'),
				                name: 'rt',
				                inputValue: '1',
				                width:230,
				            }, {
				                boxLabel: '<img style="vertical-align:middle;border:0;" height="18px" src="/images/browse.png"/>&nbsp;'+i18n.getMsg('restore.step2.customPath'),
				                name: 'rt',
				                inputValue: '2',
				                width:230,
				            },
				            
						 ],
						 listeners:{
	                    	change:function(thiscombo, newValue, oldValue, options){
	                    		if(newValue['rt'] == 1){
	                    			console.debug('restoreNode='+restoreNode);
	                    			Ext.getCmp('restoreTypeDetails').setTitle(i18n.getMsg('restore.step2.chooseBsTitle'));
	                    			Ext.getCmp('restoreTypeDetails').removeAll();
	                    			Ext.getCmp('restoreTypeDetails').add(bSetTree);
	                    			Ext.getCmp('restoreTypeDetails').add(tasksCalendar());
	                    			Ext.getCmp('restoreTypeDetails').setVisible(true);
	                    			console.debug('complete bs restore mode selected, restoreNode='+restoreNode);
	                    			console.debug('complete bs restore mode selected, before load()');
	                    			var bSetProxy = new Ext.data.Proxy({
								        type: 'ajax',
								        url : '/api/BackupSets/'+restoreNode,
								        reader: {type: 'json'},
								        extraParams: {format: 'json'}
								    });
								    bsStore.setProxy(bSetProxy);
	                    			//bSetTree.getStore().setProxy(bSetProxy);
	                    			bsStore.load({proxy : bSetProxy});
	                    			console.debug('complete bs restore mode selected, after load');
	                    			bSetTree.show();
	                    			
                    			}
                    			else if(newValue['rt'] == 2){
	                    			Ext.getCmp('restoreTypeDetails').setTitle(i18n.getMsg('restore.step2.chooseBsTitle'));
	                    			Ext.getCmp('restoreTypeDetails').setVisible(true);
	                    			Ext.getCmp('restoreTypeDetails').removeAll();
	                    			Ext.getCmp('restoreTypeDetails').add(tasksCalendar());
	                    			Ext.getCmp('restoreTypeDetails').add(makePathsGrid());
	                    			bsHistoryStore.load({
						  				params:{
						  					from	: Ext.getCmp('restoreDate').getActiveView().getViewBounds().start, //''+Ext.Date.format(Ext.getCmp('restoreDate').getActiveView().getViewBounds().start, 'Y-m-d'),
						  					to		: Ext.getCmp('restoreDate').getActiveView().getViewBounds().end, //''+Ext.Date.format(Ext.getCmp('restoreDate').getActiveView().getViewBounds().end, 'Y-m-d'),
						  					bs		: '32',
						  					statuses: 'Done',
						  					sizeOperator:'>',
						  					size:0,
						  					limit: 150
						  				}	
						       		});
	                    			/*var bSetProxy = new Ext.data.Proxy({
								        type: 'ajax',
								        url : '/api/BackupSets/'+restoreNode,
								        reader: {type: 'json'},
								        extraParams: {format: 'json'}
								    });
	                    			bSetTree.getStore().setProxy(bSetProxy);
	                    			bSetTree.getStore().load();*/
                    			}
                    		}
                    	},
		             }, // end radiogroup
		           ],
                }, // end 1st fieldset
                {
                    xtype: 'fieldset',
                    id:'restoreTypeDetails',
                    layout: {type: 'table', columns: 2},
                    defaultType: 'textfield',
                    border: true,
                    hidden:true,
                    height:420,
					width:'95%',
					padding:5,
					margin:5,
                    fieldDefaults: {
                        labelAlign: 'left',
                        hideLabel: true
                    },
                    defaults:{
                    	padding:0,
                    	margins:0,
                    },
                    items: [], // end restoreTypeDetails fields
        		}
    		]
 });
 
 var destStore = new Ext.data.TreeStore( {
        model:'Node',
        proxy:{
            type:'ajax',
            url:'/api/Nodes',
            extraParams:{
            	format: 'json',
            	online:true
            }
        },
        folderSort: true,
        root:{expanded: false},
	    load:function( thisObj, node, records, successful, eOpts ){
    		Ext.each(records, function (rec){
				rec.set('leaf', rec.get('Group') != -1);
				if(rec.get('Group') > -1)
					rec.set('checked', false );
			});
    	}
        
 });
 
 function setRestoreLocation(val){
 	Ext.getCmp('restoreLocation').setValue(val);
 
 }
 
 var restoreOptionsPanel = new Ext.widget('form', {
        id:'restoreDestPanel',
        title:'<img src="/images/3.png" class="gIcon"/>'+i18n.getMsg('restore.step4.title'), //Restored data destination',
        monitorValid:true,
        border: true,
        bodyPadding: 5,
		width:580,
		height:280,
		disabled:true,
        fieldDefaults: {labelAlign:'left', labelWidth:120,},
        items:[
        	{
				xtype:'fieldset',
				layout:'hbox',
				border:true,
				title:i18n.getMsg('restore.step4.title1'), //'Select the destination path for restored data :',
				items:[
					{
		        		xtype:'radiogroup',
			            width:380,
			            height:30,
			            border:false,
			            frame:false,
			            layout:'hbox',
			            items: [
			                {
				                boxLabel:i18n.getMsg('restore.step4.originalLocation'),
				                inputValue: '0',
				                name:'restoreLoc',
				                width:200,
				            }, {
				                boxLabel:i18n.getMsg('restore.step4.customLocation'),
				                inputValue: '1',
				                name:'restoreLoc',
				                width:150,
				            }
						 ],
						 listeners:{
		                	change:function(thiscombo, newValue, oldValue, options){
		                		if(newValue['restoreLoc'] == 1){
		                			Ext.getCmp('restoreLocation').enable();
		                			Ext.getCmp('restoreLocationBrowseBtn').enable();
		                		}
		                		else{
		                			Ext.getCmp('restoreLocation').disable();
		                			Ext.getCmp('restoreLocationBrowseBtn').disable();
		                		}
		            		}
		            	},
		             }, /* end radiogroup*/ {
		             	xtype:'textfield',
		             	id:'restoreLocation',
		             	width:250,
		             	disabled:true
		             
		             },{
		             	xtype:'button',
		             	id:'restoreLocationBrowseBtn',
		             	icon:'/images/browse.png',
		             	width:25,
		             	disabled:true,
		             	handler:function(){
		             		var destPath = (handleBrowse(restoreDestNode, setRestoreLocation, false));
		             		
		             	}
		             },
        		]
        	},
            {
				xtype:'fieldset',
				//layout:'vbox',
				border:true,
				title:i18n.getMsg('restore.step4.title2'), 
				items:[
					{
		             	xtype:'checkbox',
		             	id:'overWrite',
		             	boxLabel:i18n.getMsg('restore.step4.overwrite'),
		            },{
		             	xtype:'checkbox',
		             	id:'restorePermissions',
		             	boxLabel:i18n.getMsg('restore.step4.permissions'),
		            },
				]
			}
        ],
        buttons:[
        	{
     			id:'restoreBtn',
     			text:i18n.getMsg('restore.step4.launch'), //'Restore!',
     			icon:'/images/restore.png',
     			handler:function(){
     			
     			}
        	}
        ]
 });
 
 var viewport = new Ext.Panel({
    	renderTo: Ext.get('panel'),
        layout: 'accordion',
        layoutConfig:{animate:true},
        height:'100%',
	    layoutConfig: {
	        titleCollapse: false,
	        animate: true,
	        activeOnTop: false,
	        multi:true,
	        fill:false
	    },
        items: [nodesTreesFieldSet,	restoreTypePanel, restoreOptionsPanel]
 });


});
});