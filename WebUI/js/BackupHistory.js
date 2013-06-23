Ext.Loader.setConfig({
        enabled: true,
        disableCaching: false,
        paths: {
            'Extensible': '/Extensible/src',
            //'Extensible.example': '/Extensible/examples',
            'Ext.ux':'/js/ux',
            'Ext.i18n':'/i18n/'
        }
 });

Ext.example = function(){
    var msgCt;
    function createBox(t, s){
       return '<div class="msg"><h3>' + t + '</h3><p>' + s + '</p></div>';
    }
    return {
        msg : function(title, format){
            if(!msgCt){
                msgCt = Ext.core.DomHelper.insertFirst(document.body, {id:'msg-div'}, true);
            }
            var s = Ext.String.format.apply(String, Array.prototype.slice.call(arguments, 1));
            var m = Ext.core.DomHelper.append(msgCt, createBox(title, s), true);
            m.hide();
            m.slideIn('t').ghost("t", { delay: 5000, remove: true});
        },

        init : function(){
        }
    };
}();

	
Ext.onReady(function () {
	/*Ext.Loader.setConfig({
        enabled: true,
        disableCaching: false,
        paths: {
            'Extensible': '/Extensible/src',
            'Extensible.example': '/Extensible/examples',
            'Ext.ux':'/ext4/ux',
            'Ext.i18n':'/i18n/'
        }
     });*/
    Ext.require([
	 	'Ext.data.proxy.Rest',
	    'Ext.data.*',
	    'Ext.grid.*',
	    'Ext.tree.*',
	    'Ext.form.*',
	    'Ext.window.*',
	    //'Ext.ux.RowExpander',
	    'Ext.grid.plugin.RowExpander',
	    'Ext.ux.BoxSelect',
	    
	]);
	var params = Ext.urlDecode(window.location.search.substring(1));
	
    i18n = Ext.create('Ext.i18n.Bundle',{
		bundle: 'wui',
		lang: Ext.util.Cookies.get('lang'),
		path: '/i18n',
		noCache: true
	});
	
	i18nTask = Ext.create('Ext.i18n.Bundle',{
		bundle: 'taskmsg',
		lang: Ext.util.Cookies.get('lang'),
		path: '/i18n',
		noCache: true
	});

i18n.onReady(function(){
//i18nTask.onReady(function(){
	Ext.QuickTips.init();
	Ext.tip.QuickTipManager.init(true, {maxWidth: 450,minWidth: 150, width:350 });
	
 	Ext.get('histTitle').dom.innerText = i18n.getMsg('history.title');
 
	 function FormatTime(val){
	  	
	  	var seconds = Math.round(val%60);
	  	if(seconds <10) seconds = "0"+seconds;
	  	
	  	var hours = Math.floor(val/3600);
	  	var minutes = Math.round((val/60)%60);
	  	if(minutes <10) minutes = "0"+minutes;
	   	return hours+":"+minutes+":"+seconds;
	  }
	  
	  
	  function toggleDetails(){
	    	var nRows=taskStore.getCount();
	    	var theGrid = Ext.getCmp('grid');
	    	var exp = theGrid.getPlugin('expander');
		    for(i=0;i< nRows;i++){
		        exp.toggleRow(i);
		        //grid.plugins.each(function(plugin){	alert(plugin.id)}) ;
		    }
		    if(detailsToggled == true) detailsToggled = false;
		    else detailsToggled = true;
		    //theGrid.refresh();
	 }
 
 /*Ext.define('BasePath', {
    extend: 'Ext.data.Model',
    fields: [
    	{name: 'path',     		type: 'string'},
    ]
  });*/  
 
	  Ext.define('BackupM', {
	    extend: 'Ext.data.Model',
	    fields: [
	        {name: 'id',     		type: 'string'},
	        {name: 'name',     	type: 'string'},
	        {name: 'paths',     	type: 'string'},
	        {name: 'checked', type: 'boolean', defaultValue:false},
	        {name: 'leaf', type: 'boolean', defaultValue:true},
	    ],
	 });


 
 var nStore = new Ext.data.TreeStore( {
 	autoLoad:true,
    model: 'Node',
    proxy: {
        type: 'ajax',
        url: '/api/Nodes',
        extraParams: {format: 'json'},
	    reader:{
        	type:'json',
        	applyDefaults: true,
        }
    },
    root:{
    	expanded: false
    },
    folderSort: true,
    listeners:{
    	load:function( thisObj, node, records, successful, eOpts ){
    		Ext.each(records, function (rec){
				rec.set('leaf', false/*rec.get('Group') != -1*/);
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
				else{
					rec.set('Status','');
					rec.set('LastConnection','');
				}
				
			});
    	}
 	}
 });
 
 var bs,from,to,sizeOperator,size,statuses;
 //nStore.load();
 var taskStore = new Ext.data.JsonStore( {
        model: 'Task',
        //autoload:{params:{start: 0, limit: 13}},
        autoLoad:false,
        pageSize: 15,
        loadMask:false,
        //groupField:'bsName',
        buffered:false,
        proxy: {
            type: 'ajax',
            url: '/api/Tasks/QueryHistory/',
            
            reader:{
	        	type:'json',
	        	root:'Items',
	        	totalProperty: 'TotalCount',
	        },
	        extraParams : {
            	bs:bs,
				from:from,
				to:to,
				sizeOperator:sizeOperator,
				size:size,
				statuses:statuses,
        	}
        },
        
       /* listeners:{
        	load:function(){
        		Ext.getCmp('grid').setTitle(i18n.getMsg('runningTasks.title')+' ('+taskStore.getTotalCount()+')');
        		Ext.getCmp('grid').invalidateScroller();
        		Ext.getCmp('grid').doLayout();
        	}
        }*/
  });
  
  var bsStore = new Ext.data.JsonStore( {
    model: 'BackupSet',
    autoLoad:false,
    storeId:'bsStore',
    id:'bsStore',
    proxy: {
        type: 'ajax',
        url: '/api/Backupsets/',
        reader:{
	    	type:'json',
	    },
    },
  });
 
  var groupingFeature = Ext.create('Ext.grid.feature.Grouping',{
        groupHeaderTpl: '{name} ({rows.length} Item{[values.rows.length > 1 ? "s" : ""]})',
        depthToIndent:15,
  });
  
  
  var tree = new Ext.tree.Panel({
        id:'clientNodesTree',
        title: i18n.getMsg('nodestree.node'),
        height: 450,
        layout:'fit',
        collapsible: false,
        collapsed:false,
        useArrows: true,
        rootVisible: true,
        store: nStore, //Ext.data.StoreManager.lookup('nStore'),
        multiSelect: true,
        singleExpand: false,
        draggable:false,    
        stateful:false,   
        hideHeaders:true,
        stripeRows:true,
        padding:0,
        margins:0,
        border: false,
        root: {
	        text: "*",
	        expanded: true,
	        iconCls:'',
	        icon:'',
	        checked: false,
	    },
        columns: [{
            xtype: 'treecolumn', //this is so we know which column will show the tree
            id:Ext.id(),
            flex: 2,
            width:200,
            sortable: false,
            dataIndex: 'Name',
           /* renderer: function(value, metaData, record, colIndex, store, view){
	            if(record.get('certCN').length > 1)
	            	return value+" (<i>"+record.get('certCN')+"</i>)";
	            
	            else
	            	return value;
            }*/
        },
        ],
    	listeners: {
        	'checkchange': function(node, checked){        	
	       		//if(checked){
	       			if(node.getDepth() == 2 ){
	       				console.debug('selected node #'+node.get('Id'));
	       					       			//alert(checked);
						if(node.get('expanded') == false && checked){
	       				bsStore.on('load', function(){
				       		var theNode = node;
				       		var nodez = [];
				       		bsStore.each( function(taskSet){
				       			
								nodez.push({
			                    	id:'bs'+taskSet.get('Id'),
			                    	internalId:'bs'+taskSet.get('Id'),
			                    	'Name': taskSet.get('Name'),
			                    	//text:'<span '+nodeTip+'>'+taskSet.get('Id')+taskSet.get('Name')+'</span>',
			                    	//id:'bs'+trecord.get('id'),
			                    	//'parentId':node.get('id'),
			                    	leaf:true,
			                    	checked:true,
			                    	icon:'/images/bs.png',
			                    });
			                });
			                node.appendChild(nodez);
			                var fakeChild = node.getChildAt(0);
			                node.removeChild(fakeChild);
			                //node.updateInfo();
			                
		                }); // end onload
		                bsStore.load({
		                	scope   : this,
		                	url: '/api/BackupSets/'+node.get('Id'),
			  				params:{
			  					format:'json'
			  				},
		       			});
		       			} // end if expanded
		       			else{
		       				node.eachChild(function (n) {
			                	n.set('checked', checked);
			            	});
		       			}
		       			
	       			}
	       		//}
          	},
          	'itemclick': function(view, record) {
			    //var selModel = tree.getSelectionModel();
			    record.cascadeBy(function(r) {
			    	var checkstatus = record.get('checked');
			        //selModel.select(r, checkstatus);
			        r.set('checked', checkstatus);
			    })
			},
			'beforeitemexpand':function(nodeW, obj){
				console.debug('expanding node #'+nodeW.get('Id')+', name='+nodeW.get('Name')+', depth='+nodeW.getDepth());
				if(nodeW.getDepth() == 2 /*&& nodeW.childNodes.length <= 1*/){
					console.debug('getting node #'+nodeW.get('Id')+' backupsets...');
		       		bsStore.on('load', function(){
			       		var theNode = nodeW;
			       		var nodez = [];
			       		bsStore.each( function(trecord){
							var nodeTip = '';
			       			nodeTip += ' data-qtip="<center><b><i>'+trecord.get('Name')+'</i></b></center><hr><br/><table>' ;
							nodeTip += '<tr><td><b>Id</b>:</td><td>#'+trecord.get('Id')+'</td></tr>';
							nodeTip += '<tr><td><b>Options:&nbsp;</b></td><td>';
		        			nodeTip += '</td><tr/><tr><td><b>Items: </b></td><td><ul style=\'list-style-type: square;\'>';
		        			trecord.BasePaths().each(function(basePath) {
		        				nodeTip += '<li><i>'+basePath.get('Path')+'</i></li>';
		        			});
		        			nodeTip += '</ul></td></tr>';
		        			nodeTip += '</table>" ';
		        			
		                    nodeW.appendChild({
								Id:trecord.get('Id'),
								'Id':trecord.get('Id'),
								'NodeKind':'BackupSet',
		                    	leaf: true,
		                    	'Name': '<span '+nodeTip+'>#'+trecord.get('Id')+'&nbsp;'+trecord.get('Name')+'</span>', //trecord.get('Name'),
		                    	text:trecord.get('Id')+' : '+trecord.get('Name'),
		                    	//'id':'bs'+trecord.get('id'),
		                    	
		                    	parentId:nodeW.get('Id'),
		                    	'parentId':nodeW.get('Id'),
		                    	
		                    	icon:'/images/bs.png',
		                    	checked:false,//trecord.get('checked')
		                    });
		                });
		                
	                }); // end onload
	                bsStore.load({
	                	url: '/api/BackupSets/'+nodeW.get('Id'),
		  				params:{format:'json'},
	       			});
	       		
				}
			}
	 	},
    });
    var gridWidth = document.getElementById('panel').offsetWidth - 454;
    
    var nStore2 = new Ext.data.JsonStore( {
	 	autoLoad:true,
	    model: 'Node',
	    proxy: {
	            type: 'ajax',
	            url: '/api/Nodes/Online',
	            extraParams: {
			        format	: 'json'
			    },
	        },
	        root:{
		    	expanded: true
		    },
	        folderSort: true,
	        listeners:{
		    	/*load:function( thisObj, node, records, successful, eOpts ){
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
						else{
							rec.set('Status','');
							rec.set('LastConnection','');
						}
						
					});
		    	}*/
	   	 	}
	 });
 	/*var nStore2 = new Ext.data.JsonStore( {
 		autoLoad:true,
	    model: 'NodeM',
	    proxy: {
	        type: 'ajax',
	        url: '/Get.aspx?w=Clients',
	        //root:'children'
	    },
	    listeners:{
        	load:function(){
        		//Ext.getCmp('grid').getView().refresh();
        	}
        }
    });*/
    //nStore2.sync();
    
    var grid = Ext.create('Ext.grid.Panel', {
    	id:'grid',
        collapsible: false,
        frame: false,
        stateful:false,
        store: taskStore,
        minHeight:'550',
        height: '100%',
        width:gridWidth,
        layout:'fit',
        align:'left',
        autoScroll:true,
        scroll:'vertical',
        overlapHeader:true,
        multiSelect: true,
        features: [/*groupingFeature, */{ftype: 'groupingsummary'}],
        viewConfig: {
        	loadMask: true,
    		onStoreLoad: Ext.emptyFn,
    	},
    	/*verticalScroller: {
    		xtype: 'paginggridscroller'
  		},
  		invalidateScrollerOnRefresh: false,
    	// infinite scrolling does not support selection
    	disableSelection: true,*/
        plugins: [
        	{
              ptype: 'rowexpander',
              id:'expander',
              pluginId:'expander',
              rowBodyTpl:[
	              '<table>',
	              	'<tr>',
	              	'<td><div class="gridCell" style="margin-left:75px;max-height:100;width:550px;overflow-x:none;overflow-y:auto;">',
		              '<table>',
			              '<tpl for=".">',
				              '<tr>',
					              '<td>',
					              '<tpl if="code &gt;= 900"><img class="x-tree-node-icon" src="/images/sq_ye.gif"/></tpl>',
					              '<tpl if="code &gt;= 800 && code &lt; 900"><img class="x-tree-node-icon" src="/images/sq_re.gif"/></tpl>',
					              '<tpl if="code &gt;= 700 && code &lt; 800"><img class="x-tree-node-icon" src="/images/sq_gr.gif"/></tpl>',
					              //'</td>',
					              '{date}</td>',
					              //'<td>{message}</td>',
					             // '<td>{[this.getTaskMsg(values.code)]}</td>', //{i18nTask.get("
					             '<td>({code}) {[i18nTask.getMsg("task."+values.code, values.message1, values.message2)]}</td>',
				              '</tr>',
			              '</tpl>',
		              '</table>',
		              '</div></td>',
		         '</tr>',
		         '</table>'
	              /*'<td width="50">&nbsp;</td>',
	              '<td><div class="gridCell" style="max-height:100;width:150;overflow-x:none;overflow-y:auto;"><table>',
	              'sessions:<br/><br/><br/><br/><br/>',
	              '</div></td></tr></table>'*/
              ],
              urlTpl: '/api/Tasks/{id}/Log',
              stateful:true,
              listeners:{
              	onexpand: function(ex, record, body, rowIndex){
              		ProcessExpander(record, body, rowIndex);
              	}
              }
		    },
        ],
        columns: [
	        {
	        	text: '%',
	        	width:65,
	        	dataIndex:'Percent',
	        	tdCls:'gridRow',
	        	renderer: function(value, metaData, record, colIndex, store, view){
	        		var roundedPercent=0;
	        		var rawPercent = record.get('Percent');
	        		if(rawPercent < 40)
	        			roundedPercent = 20;
	        		else if(rawPercent < 60)
	        			roundedPercent = 40;
	        		else if(rawPercent < 80)
	        			roundedPercent = 60;
	        		else 
	        			roundedPercent = 100;
	            	//value='<img src="/images/compe_'+record.get('status').substr(0,1).toLowerCase()+"_"+compA[index]+'.png"/>'+/*record.get('percent')*/compA[index]+'%';
	            	value='<img src="/images/c_'+record.get('Status').substr(0,1).toLowerCase()+"_"+roundedPercent+'.png"/><div class="greyV" style="horizontal-align:right;"><b>'+record.get('Percent')+'%</b></div>';
	            	return value;
	            }
	        },{
	            text: i18n.getMsg('runningTasks.task'),
	            flex: 1,
	            dataIndex: 'BsName',
	            tdCls:'gridRow',
	            width:160,
	            //tpl:'#{id} : {bsName}',
	            renderer: function(value, metaData, record, colIndex, store, view){
	            	value = '<div class="taskBox"><b>#'+record.get('Id')+' <i>('+record.get('BsName')+')</i></b>'
	            		+'<span><ul><li class="gStatus">'+i18n.getMsg('runningTasks.status.'+record.get('RunStatus'))+'</li></ul></span></div>';
		            return value;
	            }
	        },{
	            text: i18n.getMsg('runningTasks.client'),
	            flex: 1,
	            width:100,
	            id:'tClient',
	            dataIndex: 'NodeId',
	            renderer: function(value){
	            	//Ext.data.StoreManager.lookup('nStore').each( function(trecord){
	            	nStore2.each( function(node){
							if(node.get('Id') == value){
								value = '<div>'
									+node.get('Name')+'<br/> <span class="greyV">'+node.get('IP')+'</span></div>';
							}
					});
					return value;
	            },
	           /* summaryType: 'count',
		        summaryRenderer: function(value){
		            return Ext.String.format('{0} '+i18n.getMsg('runningTasks.client'), value);
		        }*/
	        },{
	            text: i18n.getMsg('runningTasks.operation'),
	            flex: 0,
	            width:100,
	            dataIndex: 'Operation',
	            renderer: function(value, metaData, record, colIndex, store, view){
	            	if(value == 'Backup'){
	            		value += " ("+i18n.getMsg('runningTasks.type.'+record.get('Type'))+")"
	            			+"<br/>"+record.get('Level')+"&nbsp;&nbsp;";
	            		
	            		/*var capsList = record.get('Flags').toLowerCase().split(", ");
		            	for (var i=0; i < capsList.length; i++) 
		            		if(capsList[i] != 'none')
		            			value += '<img src="/images/'+capsList[i].substring(1)+'.png" title="'+capsList[i]+'"/>&nbsp;';
		            	*/
	            	}
	            	
	            	return value;
	            },
	            summaryType: 'count',
		        summaryRenderer: function(value){
		            return Ext.String.format('{0} ', value);
		        }
	        },{
	            text: i18n.getMsg('generic.size'),
	            flex: 0,
	            //width:195,
	           // height:35,
	            padding:'-10 -10 -10 0',
	            //margins:'-4 0 -4 0',
	            style:'vertical-align:top;',
	            columns:[
	            	{
	            		text: '<i>'+i18n.getMsg('runningTasks.size.original')+'</i>',
	            		flex: 0,
	            		width:65,
	            		//height:15,
	            		//padding:'-10 0 0 0',
	            		margins:'-4 0 1 0',
	            		dataIndex:'OriginalSize',
	            		sortable:true,
	            		renderer: function(value, metaData, record, colIndex, store, view){
			            	return FormatSize(value)+'<br/><div align="right"><i>'+record.get('TotalItems')+'</i></div>'; 
			            },
			            summaryType: 'sum',
				        summaryRenderer: function(value){
				            return FormatSize(value);
				        }
	            	},
	            	{
	            		text: '<i>'+i18n.getMsg('runningTasks.size.final')+'</i>',
	            		flex: 0,
	            		width:65,
	            		//height:15,
	            		//padding:'-10 0 0 0',
	            		margins:'-4 0 1 0',
	            		dataIndex:'FinalSize',
	            		sortable:true,
	            		renderer: function(value, metaData, record, colIndex, store, view){
			            	return FormatSize(value)+'<br/><div align="left"><i> items</i></div>';
			            },
			            summaryType: 'sum',
				        summaryRenderer: function(value){
				            return FormatSize(value);
				        }
	            	},
	            	/*{
			            text: '<i>'+i18n.getMsg('runningTasks.transferredSize')+'</i>',
			            flex: 0,
			            width:70,
			            height:15,
			            //padding:'-10 0 0 0',
			            margins:'-4 0 1 0',
			            dataIndex: 'transferredSize',
			            renderer: function(value){
			            	return FormatSize(value);
			            }
			        }*/
	            ]
	        },{
	            text: i18n.getMsg('runningTasks.duration')+"<br/>"+i18n.getMsg('runningTasks.rate'),
	            flex: 0,
	            width:60,
	            sortable:true,
	            renderer: function(value, metaData, record, colIndex, store, view){
	            	return  FormatSize(record.get('OriginalSize')/ ( (record.get('EndDate') - record.get('StartDate'))/1000 ))+"/s"
	            }
	            
	        },{
	        	//xtype:'datecolumn',
	        	//format: 'Y-m-d H:i:s',
	        	
	        	//margins:'-10 0 0 0',
	        	//padding:'0 0 0 0',
	            text: i18n.getMsg('runningTasks.startDate')
	            	+'<br/>'+i18n.getMsg('runningTasks.endDate')+'',
	            //format:'mm H:i:s',
	            //style:'line-height:0 !important;',
	            flex: 1,
	            width:155,
	            align:'left',
	            sortable:false,
	            dataIndex: 'StartDate',
	            renderer: function(value, metaData, record, colIndex, store, view){
	            
	            	return ''+/*new Date(record.get('StartDate')*/record.get('StartDate').toLocaleString()
	            		+'<br/>'+ record.get('EndDate').toLocaleString()+'';
	            }
	        },
        ],
        dockedItems: [{
		    xtype: 'pagingtoolbar',
		    pageSize:15,
		    store:taskStore,
		    dock: 'bottom',
		    ui: 'footer',
		    height:25,
		    //defaults: {align:'right'},
		    align:'right',
		    items: [
			        	{
			        		xtype:'displayfield',
			        		value:i18n.getMsg('runningTasks.groupBy'),
			        		width:80,
			        	},
			        	{
			        		xtype:'combo',
			        		id:'groupBy',
			        		mode:           'local',
			                value:          'none',
			                triggerAction:  'all',
			                forceSelection: true,
			                allowBlank:true,
			                editable:       false,
			                fieldLabel:     '',
			                displayField:   'value',
			                valueField:     'name',
			                queryMode: 'local',
			                width:150,
			                store:          Ext.create('Ext.data.Store', {
			                  fields : ['name', 'value'],
			                    data   : [
			                        {name : 'bsName',   value: 'backup set name'},
			                        {name : 'operation',  value: i18n.getMsg('runningTasks.operation')},
			                        {name : 'status', value: i18n.getMsg('runningTasks.status')},
			                        {name : 'runningStatus', value: i18n.getMsg('runningTasks.runningStatus')},
			                        //{name : 'percent', value: 'completion %'},
			                        {name : 'clientId', value: i18n.getMsg('runningTasks.client')},
			                       // {name : 'rate', value: i18n.getMsg('runningTasks.rate')},
			                        {name : 'startDate', value: i18n.getMsg('runningTasks.startDate')},
			                       // {name : 'elapsedTime', value: i18n.getMsg('runningTasks.duration')},
			                        {name : 'originalSize', value: i18n.getMsg('runningTasks.size.original')},
			                        //{name : 'finalSize', value: i18n.getMsg('runningTasks.size.final')},
			                        //{name : '', value: 'don\'t group'},
			                    ]
			                }),
			                listeners:{
			                	'change': function( thisCombo, newValue, oldValue, options ){
			                		if(newValue == '')
			                			taskStore.ungroup();
			                		else
			                			taskStore.group(newValue);
			                	}
			                }
			        	},
			        	{
				            text:'&nbsp;&nbsp;'+i18n.getMsg('runningTasks.clearGroupBy'),
				            icon:'/images/clearfield.png',
				            handler : function(){
			                	taskStore.clearGrouping();
			            	}
			          	},
			          	'-',
			          	{
				      		text:'&nbsp;'+i18n.getMsg('runningTasks.details'),
				            icon:'/images/plus.gif',
				            handler : toggleDetails
				      	},
			          	{
			          		xtype:'button',
						    icon:'/images/excel.png',
						    handler:function(button){
						        var gridPanel=button.up('grid');
						        var dataURL='data:application/vnd.ms-excel;base64,'+Ext.ux.exporter.Exporter.exportAny(taskStore, 'excel');
						        window.location.href=dataURL;
						    }
						},
					  
            	]
            }]
    });
    
    
     window.generateData = function(n, floor){
        var data = [],
            p = (Math.random() *  11) + 1,
            i;
            
        floor = (!floor && floor !== 0)? 20 : floor;
        
        for (i = 0; i < (n || 12); i++) {
            data.push({
                name: Ext.Date.monthNames[i % 12].substring(0,3),
                data1: Math.floor(Math.max((Math.random() * 100), floor)),
                data2: Math.floor(Math.max((Math.random() * 100), floor)),
                data3: Math.floor(Math.max((Math.random() * 100), floor)),
                data4: Math.floor(Math.max((Math.random() * 100), floor)),
                data5: Math.floor(Math.max((Math.random() * 100), floor)),
                data6: Math.floor(Math.max((Math.random() * 100), floor)),
                data7: Math.floor(Math.max((Math.random() * 100), floor)),
                data8: Math.floor(Math.max((Math.random() * 100), floor)),
                data9: Math.floor(Math.max((Math.random() * 100), floor))
            });
        }
        return data;
    };
    window.store1 = Ext.create('Ext.data.JsonStore', {
        fields: ['name', 'data1', 'data2', 'data3', 'data4', 'data5', 'data6', 'data7', 'data9', 'data9'],
        data: generateData()
    });
    store1.loadData(generateData(12));
    
    
 var dateNow = new Date();
 dateNow = Ext.Date.add(dateNow, Ext.Date.DAY, 1);
 
 var viewport = Ext.create('Ext.form.Panel', {
            layout:'border',
            renderTo:Ext.get('panel'),
            height:'100%',
            items:[{
            	xtype:'container',
                region:'west',
                id:'west-panel',
                //split:true,
                width: 200,
                height:570,
                frame:false,
                collapsible: false,
                //margins:'35 0 5 5',
               // cmargins:'35 5 5 5',
                layout:'accordion',
                layoutConfig:{
                    animate:true
                },
                items: [
                tree,
                {
                	//xtype:'container',
                    title: i18n.getMsg('history.dateRange'),
                    border:false,
                    height:290,
                    autoScroll:true,
                    items:[
                    	{
                    		xtype:'label',
                    		forId:'dateFrom',
                    		align:'left',
                    		text:i18n.getMsg('history.from'),
                    		width:50,
                    		id:'fromlabel',
                    	},
                    	 {
		                	//fieldLabel:i18n.getMsg('history.from'),
		                	labelWidth:50,
		                	labelAlign:'left',
		                	//anchor: '100%',
		                	id:'dateFrom',
		                	xtype:'datefield',
		                	width:140,
		                	value: new Date(Date.now() -7),
		                	maxValue: new Date(),
		                	//margins:'5 5 5 5',
		                	//format:'l, M d',
		                },
		                {
                    		xtype:'label',
                    		forId:'dateTo',
                    		align:'left',
                    		text:i18n.getMsg('history.to'),
                    		width:50,
                    		id:'tolabel',
                    		//margins:'5 5 5 5',
                    	},
		                {
		                	//fieldLabel:i18n.getMsg('history.to'),
		                	labelWidth:50,
		                	labelAlign:'left',
		                	//margins:'5 15 0 0',
		                	//anchor: '100%',
		                	id:'dateTo',
		                	xtype:'datefield',
		                	width:140,
		                	value: dateNow,
		                	maxValue: dateNow,
		                	
		                	//value: new Date(Date.now()).add('d',1),
		                	//format:'l, M d',
		                },
		                {
                    		xtype:'label',
                    		forId:'statuses',
                    		align:'left',
                    		text:i18n.getMsg('runningTasks.status'),
                    		width:50,
                    		id:'statusLabel',
                    	},
                    	{
			        		//xtype:'combo',
			        		xtype:'boxselect',
			        		id:'statuses',
			        		resizable: false,
			        		stacked:true,
							autoScroll:false,
							hideTrigger:true,
			        		mode:     'local',
			                value:[
			                	'Done', 'Error', 'Cancelled', 'Expiring'
			                ],
			                triggerAction:  'all',
			                forceSelection: true,
			                allowBlank: 	false,
			                editable:       false,
			                fieldLabel:     '',
			                displayField:   'value',
			                valueField:     'name',
			                queryMode: 		'local',
			                width:180,
			                labelWidth:0,
			                height:110,
			                store:          Ext.create('Ext.data.Store', {
			                  fields : ['name', 'value'],
			                    data   : [
			                        {name : 'Started',   value: i18n.getMsg('runningTasks.status.Started')},
			                        {name : 'Done',  value: i18n.getMsg('runningTasks.status.Done')},
			                        {name : 'Cancelling', value: i18n.getMsg('runningTasks.status.Cancelling')},
			                        {name : 'Cancelled', value: i18n.getMsg('runningTasks.status.Cancelled')},
			                        {name : 'Paused', value: i18n.getMsg('runningTasks.status.Paused')},
			                        {name : 'Expiring', value: i18n.getMsg('runningTasks.status.Expiring')},
			                        {name : 'Expired', value: i18n.getMsg('runningTasks.status.Expired')},
			                        {name : 'Error', value: i18n.getMsg('runningTasks.status.Error')},
			                        {name : 'PendingStart', value: i18n.getMsg('runningTasks.status.PendingStart')},
			                        
			                    ]
			                }),
			                
			        	},
			        	{
                    		xtype:'label',
                    		forId:'sizeType',
                    		align:'left',
                    		text:i18n.getMsg('generic.size'),
                    		width:40,
                    		id:'sizelabel',
                    	},
			        	{
			        		xtype:'fieldset',
			        		layout:'hbox',
			        		border:0,
			        		overlapHeader:true,
			        		frame:false,
			        		//borders:'0 0 0 0',
			        		padding:5,
			        		//margins:'0 0 0 0',
			        		items:[
			        	
					        	{
					        		xtype:'combo',
					        		id:'sizeType',
					        		mode:           'local',
					                value:          'originalSize', //i18n.getMsg('runningTasks.size.original'),
					                triggerAction:  'all',
					                forceSelection: true,
					                allowBlank:		false,
					                editable:       false,
					                //fieldLabel:     'size',
					                displayField:   'value',
					                valueField:     'name',
					                queryMode: 'local',
					                width:70,
					                store:          Ext.create('Ext.data.Store', {
					                  fields : ['name', 'value'],
					                    data   : [
					                        {name : 'originalSize',   value: i18n.getMsg('runningTasks.size.original')},
					                        {name : 'finalSize',  value: i18n.getMsg('runningTasks.size.final')},
					                    ]
					                })
					            },
					            {
					        		xtype:'combo',
					        		id:'sizeOperator',
					        		mode:           'local',
					                value:          '>',
					                triggerAction:  'all',
					                forceSelection: true,
					                allowBlank:false,
					                editable:       false,
					                displayField:   'value',
					                valueField:     'name',
					                queryMode: 'local',
					                width:33,
					                store:          Ext.create('Ext.data.Store', {
					                  fields : ['name', 'value'],
					                    data   : [
					                        {name : '>',   value: '>'},
					                        {name : '<',  value: '<'},
					                    ]
					                })
					            },
					            {
					            	xtype:'numberfield',
		                			id: 'size',
		                			width:48,
		                			value:0,
		                			minValue:0,
					            },
					            {
					        		xtype:'combo',
					        		id:'sizeUnit',
					        		mode:           'local',
					                value:          'MB',
					                triggerAction:  'all',
					                forceSelection: true,
					                allowBlank:false,
					                editable:       false,
					                displayField:   'value',
					                valueField:     'name',
					                queryMode: 'local',
					                width:45,
					                store:          Ext.create('Ext.data.Store', {
					                  fields : ['name', 'value'],
					                    data   : [
					                        {name : 'KB',   value: 'KB'},
					                        {name : 'MB',  value: 'MB'},
					                        {name : 'GB',  value: 'GB'},
					                        {name : 'TB',  value: 'TB'},
					                    ]
					                })
					            },
			            ]
			            },
			            
		                {
		                	//margins:'5 0 20 0',
		                	margins:'20 20 20 20',
		                	//anchor: '0%',
		                	id:'go',
		                	xtype:'button',
		                	align:'right',
		                	text:'Generate',
		                	icon:'/images/view.png',
		                	width:80,
		                	scale:'small',
		                	//columnSpan:2,
		                	handler:function(){
		                		Ext.Msg.show({
			        				title:'Information',
			        				msg:i18n.getMsg('restore.step3.waitMsg'),
			        				buttons:false,
			        				icon:'icon-loading',
			        			});	
			        			var multiplier = 1024*1024;
								if(Ext.getCmp('sizeUnit').getValue() == 'GB')
									multiplier *= 1024;
								if(Ext.getCmp('sizeUnit').getValue() == 'TB')
									multiplier *= 1024*1024;	
								size = Ext.getCmp('size').getValue() * multiplier;
		                		var tasksRaw = tree.getChecked();
		                		var tasks = [];
		                		for(var i=0; i< tasksRaw.length; i++){
		                			console.debug('Selected backupset #'+tasksRaw[i].raw.Id);
		                			//if(tasksRaw[i].get('Id').substring(0,2) == "bs")
		                			if( (tasksRaw[i].raw.NodeKind != undefined) && tasksRaw[i].raw.NodeKind == "BackupSet"){
		                				console.log('catched bs!');
		                				tasks.push(tasksRaw[i].raw.Id/*tasksRaw[i].get('Id').substring(2)*/);
		                			}
		                		}
		                		bs=tasks.join(',');
					  			from=Ext.getCmp('dateFrom').getValue();
					  			to=Ext.getCmp('dateTo').getValue();
					  			sizeOperator=Ext.getCmp('sizeOperator').getValue();
					  			statuses=Ext.getCmp('statuses').getValue();
					  			
		                		taskStore.on('load', function(){
				       				Ext.Msg.close();
				       			});
				       			taskStore.getProxy().extraParams.bs = bs;
				       			taskStore.getProxy().extraParams.from = from;
				       			taskStore.getProxy().extraParams.to = to;
				       			taskStore.getProxy().extraParams.sizeOperator = sizeOperator;
				       			taskStore.getProxy().extraParams.size = size;
				       			taskStore.getProxy().extraParams.statuses = statuses;
		                		taskStore.load({
				                	scope   : this,
					  				params:{
					  					/*bs:''+tasks.join(','),
					  					from:Ext.getCmp('dateFrom').getValue(),
					  					to:Ext.getCmp('dateTo').getValue(),
					  					sizeOperator:Ext.getCmp('sizeOperator').getValue(),
					  					size:size,
					  					statuses:Ext.getCmp('statuses').getValue(),*/
					  					bs:bs,
					  					from:from,
					  					to:to,
					  					sizeOperator:sizeOperator,
					  					size:size,
					  					statuses:statuses,
					  					start:0,
					  					limit:15,
					  					format:'json'
					  				},
				       			});
		                	}
		                },
                    ]
                }
               
                ]
            },{
                region:'center',
                //margins:'35 5 5 0',
                layout:'column',
                height:500,
                //width:500,
                //title:'History',
                autoScroll:false,
                overlapHeaders:true,
                items: [grid]
            },{
                region:'east',
                //margins:'35 5 5 0',
                layout:'column',
                height:570,
                width:250,
                //title:'History',
                autoScroll:true,
                /*defaults: {
                    layout: 'anchor',
                    defaults: {
                        anchor: '100%'
                    }
                },*/
               // xtype:'fieldset',
                
                items: [
                	{
				        xtype:'fieldset',
				        layout: {
		                    type: 'vbox',
		                    align:'stretch'
		                },
				        margin: '0 0 0 5',
				        width:240,
				        height:185,
				        title: i18n.getMsg('welcome.sgChart.title'),
				        items: [
				        	{
					            xtype: 'chart',
					            title:'backup size',
					            animate: true,
					            shadow: true,
					            theme: 'Green',
					            /*background: {
								    image: '/images/graphbg.png'
								},*/
								insetPadding:5,
					            width:230,
					            height:160,
					            store: store1,
					            /*mask: 'horizontal',*/
						        listeners: {
						            select: {
						                fn: function(me, selection) {
						                    me.setZoom(selection);
						                    me.mask.hide();
						                }
						            }
						        },
					            axes: [{
					                type: 'Numeric',
					                position: 'left',
					                title:'Space',
					                fields: ['data1'],
					                title: false,
					                grid: true
					            }, {
					                type: 'Category',
					                position: 'bottom',
					                title:'Date',
					                fields: ['name'],
					                title: false
					            }],
					            series: [
						            {
						                type: 'line',
						                axis: 'left',
						                gutter: 80,
						                smooth: true,
						                xField: 'name',
						                yField: ['data1'],
						                listeners: {
						                  itemmouseup: function(item) {
						                      Ext.example.msg('Item Selected', item.value[1] + ' visits on ' + Ext.Date.monthNames[item.value[0]]);
						                  }  
						                },
						               tips: {
						                    trackMouse: true,
						                    width: 80,
						                    height: 40,
						                    renderer: function(storeItem, item) {
						                        this.setTitle(storeItem.get('name') + '<br />' + storeItem.get('data1'));
						                    }
						                },
						               
						            }
				        
				        	]
				    	},
                ]
			        },
			        {
				        xtype:'fieldset',
				        layout: {
		                    type: 'vbox',
		                    align:'stretch'
		                },
				        margin: '0 0 0 5',
				        width:240,
				        height:185,
				        title: i18n.getMsg('welcome.sgChart.title'),
				        items: [
				        	{
					            xtype: 'chart',
					            title:'backup size',
					            animate: true,
					            shadow: true,
					            theme: 'Green',
					            /*background: {
								    image: '/images/graphbg.png'
								},*/
								insetPadding:5,
					            width:230,
					            height:160,
					            store: store1,
					            /*mask: 'horizontal',
						        listeners: {
						            select: {
						                fn: function(me, selection) {
						                    me.setZoom(selection);
						                    me.mask.hide();
						                }
						            }
						        },*/
					            axes: [{
					                type: 'Numeric',
					                position: 'left',
					                title:'Space',
					                fields: ['data1'],
					                title: false,
					                grid: true
					            }, {
					                type: 'Category',
					                position: 'bottom',
					                title:'Date',
					                fields: ['name'],
					                title: false
					            }],
					            series: [
						            {
						                type: 'line',
						                axis: 'left',
						                gutter: 80,
						                smooth: true,
						                xField: 'name',
						                yField: ['data1'],
						                listeners: {
						                  itemmouseup: function(item) {
						                      Ext.example.msg('Item Selected', item.value[1] + ' visits on ' + Ext.Date.monthNames[item.value[0]]);
						                  }  
						                },
						               tips: {
						                    trackMouse: true,
						                    width: 80,
						                    height: 40,
						                    renderer: function(storeItem, item) {
						                        this.setTitle(storeItem.get('name') + '<br />' + storeItem.get('data1'));
						                    }
						                },
						               
						            }
				        
				        	]
				    	},
                ]
			        },
			        
			        {
				        xtype:'fieldset',
				        layout: {
		                    type: 'vbox',
		                    align:'stretch'
		                },
				        margin: '0 0 0 5',
				        width:240,
				        height:185,
				        title: i18n.getMsg('welcome.sgChart.title'),
				        items: [
				        	{
					            xtype: 'chart',
					            title:'backup size',
					            animate: true,
					            shadow: true,
					            theme: 'Green',
					            /*background: {
								    image: '/images/graphbg.png'
								},*/
								insetPadding:5,
					            width:230,
					            height:160,
					            store: store1,
					            /*mask: 'horizontal',
						        listeners: {
						            select: {
						                fn: function(me, selection) {
						                    me.setZoom(selection);
						                    me.mask.hide();
						                }
						            }
						        },*/
					            axes: [{
					                type: 'Numeric',
					                position: 'left',
					                title:'Space',
					                fields: ['data1'],
					                title: false,
					                grid: true
					            }, {
					                type: 'Category',
					                position: 'bottom',
					                title:'Date',
					                fields: ['name'],
					                title: false
					            }],
					            series: [
						            {
						                type: 'line',
						                axis: 'left',
						                gutter: 80,
						                smooth: true,
						                xField: 'name',
						                yField: ['data1'],
						                listeners: {
						                  itemmouseup: function(item) {
						                      Ext.example.msg('Item Selected', item.value[1] + ' visits on ' + Ext.Date.monthNames[item.value[0]]);
						                  }  
						                },
						               tips: {
						                    trackMouse: true,
						                    width: 80,
						                    height: 40,
						                    renderer: function(storeItem, item) {
						                        this.setTitle(storeItem.get('name') + '<br />' + storeItem.get('data1'));
						                    }
						                },
						               
						            }
				        
				        	]
				    	},
                ]
			        }
        
                ] // end eastpanel items
            }]
        });
    //});
 
 });
 });
 //});