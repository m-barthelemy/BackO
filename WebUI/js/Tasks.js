Ext.require([
    'Ext.data.*',
    'Ext.grid.*',
    'Ext.tree.*',
    'Ext.form.*',
    'Ext.window.*',
    //'Ext.ux.RowExpander',
    'Ext.grid.plugin.RowExpander'
]);
	
Ext.onReady(function () {
	Ext.Loader.setConfig({enabled:true});
	Ext.Loader.setPath('Ext.ux', '/js/ext4/ux');
	
    i18n = Ext.create('Ext.i18n.Bundle',{
		bundle: 'wui',
		lang: Ext.util.Cookies.get('lang'),
		path: '/i18n',
		noCache: false
	});
	
	i18nTask = Ext.create('Ext.i18n.Bundle',{//2nd i18n for tasks log entries
		bundle: 'taskmsg',
		lang: Ext.util.Cookies.get('lang'),
		path: '/i18n',
		noCache: false
	});
	
i18n.onReady(function(){
//i18nTask.onReady(function(){

 Ext.get('runningTasksTitle').dom.innerText = i18n.getMsg('runningTasks.title');
 var refreshInterval; //periodically reload the running Tasks store
 var detailsToggled = false; 
 
  
  var taskStore = new Ext.data.JsonStore( {
        model: 'Task',
        autoLoad:true,
        proxy: {
            type: 'ajax',
            url: '/api/Tasks/Running',
            extraParams: {format: 'json'},
            reader:{
	        	type:'json',
	        	/*root:'items',
	        	totalProperty: 'totalCount',*/
	        	applyDefaults: true,
	        }
        },
        listeners:{
        	load:function(){
        		Ext.get('runningTasksTitle').dom.innerText = i18n.getMsg('runningTasks.title')+' ('+taskStore.getTotalCount()+')';
        	}
        }
  });
  

 var nStore = new Ext.data.JsonStore( {
    model: 'Node',
    autoLoad:true,
    proxy: {
        type: 'ajax',
        url: '/api/Nodes/Online',
        //root:'children'
        extraParams: {format: 'json'},
	    reader:{
        	type:'json',
        	applyDefaults: true,
        }
    }
 });
 //nStore.load();
	
    var usersStore = new Ext.data.JsonStore( {
        model: 'User',
        autoLoad: true,
        proxy: {
            type: 'ajax',
            url: '/api/Users',
            extraParams: {format: 'json' },
		    reader:{
	        	type:'json',
	        	applyDefaults: true
	        }
        }
    });
   //usersStore.load();
    
  var groupingFeature = Ext.create('Ext.grid.feature.Grouping',{
        //groupHeaderTpl: '{[i18n.getMsg("runningTasks."+name )]} ({rows.length} Item{[values.rows.length > 1 ? "s" : ""]})',
        groupHeaderTpl: ' {[i18n.getMsg("runningTasks."+Ext.getCmp("groupBy").getValue())]} : {name} ({rows.length})',
        depthToIndent:15,
        
  });
  
  
  /*var logEntryExpander = new Ext.ux.RowExpander({
  	rowBodyTpl:['<div style="overflow-y:auto;overflow-x:none" class="logEntries">'],
    listeners:{
	  	expand: function(ex, record, body, rowIndex){
	  		ProcessExpander(record, body, rowIndex);
	  	}
	  }
  });*/
  
  function FormatTime(val){
  	
  	var seconds = Math.round(val%60);
  	if(seconds <10) seconds = "0"+seconds;
  	
  	var hours = Math.floor(val/3600);
  	var minutes = Math.floor((val/60)%60);
  	if(minutes <10) minutes = "0"+minutes;
   	return hours+":"+minutes+":"+seconds;
  }
  /*
  var expander = new Ext.ux.RowExpander({
  	id:'expander',
  	
              rowBodyTpl:[
	              '<table><tr><td><div class="gridCell" style="margin-left:75px;max-height:100;width:550;overflow-x:none;overflow-y:auto;"><table>',
	              '<tpl for=".">',
	              '<tr>',
	              '<td>',
	              '<tpl if="code &gt;= 800 && code &lt; 900"><img class="x-tree-node-icon" src="/images/sq_re.gif"/></tpl>',
	              '<tpl if="code &gt;= 900"><img class="x-tree-node-icon" src="/images/sq_ye.gif"/></tpl>',
	              //'</td>',
	              '{date}</td>',
	              '<td>{message}</td>',
	              '</tr>',
	              '</tpl>',
	              '</table></div></td>',
	              '<td width="50">&nbsp;</td>',
	              '<td><div class="gridCell" style="max-height:100;width:150;overflow-x:none;overflow-y:auto;"><table>',
	              'sessions:<br/><br/><br/><br/><br/>',
	              '</div></td></tr></table>'
              ],
              urlTpl: '/Get.aspx?w=TaskLogEntries&trackingId={id}',
              stateful:true,
              listeners:{
              	onexpand: function(ex, record, body, rowIndex){
              		ProcessExpander(record, body, rowIndex);
              	}
              }
  });
  */
  var combo = new Ext.form.field.ComboBox({
	  name : 'perpage',
	  width: 40,
	  store: new Ext.data.ArrayStore({
	    fields: ['id'],
	    data  : [
	      ['15'],
	      ['20'],
	      ['30'],
	      ['40'],
	    ]
	  }),
	  mode : 'local',
	  value: '20',
	  listWidth     : 40,
	  triggerAction : 'all',
	  displayField  : 'id',
	  valueField    : 'id',
	  editable      : false,
	  forceSelection: true
	});
	
	var selModel = Ext.create('Ext.selection.CheckboxModel', {
        listeners: {
            selectionchange: function(sm, selections) {
                if(selections.length > 0){
                	Ext.getCmp('cancelTaskBtn').enable();
                	clearInterval(refreshInterval);
                }
                else{
                	Ext.getCmp('cancelTaskBtn').disable();
                	SetAutoRefresh();
                }
            }
        }
  });
  
   function toggleDetails(){
    	var nRows=taskStore.getCount();
    	var theGrid = Ext.getCmp('grid');
    	var exp = theGrid.getPlugin('expander');
    	theGrid.invalidateScroller();
	    for(i=0;i< nRows;i++){
	        exp.toggleRow(i);
	        //grid.plugins.each(function(plugin){	alert(plugin.id)}) ;
	        
	    //theGrid.invalidateScroller();
	    }
	    if(detailsToggled == true) detailsToggled = false;
	    else detailsToggled = true;
	    
	    theGrid.invalidateScroller();
	    theGrid.doLayout();
	}
        	
	var bbar = new Ext.toolbar.Paging({
	  store: taskStore,
	  displayInfo: true,
	  dock: 'bottom',
	  height:28,
	  items   :    [
	    '-',
	    i18n.getMsg('generic.perPage'),
	    combo,
	    '-',
	   /* {
    		xtype:'displayfield',
    		value:i18n.getMsg('runningTasks.groupBy'),
    	},
	    '-',*/
	   	{
    		text:'&nbsp;&nbsp;'+i18n.getMsg('runningTasks.start'),
    		icon:'/images/start.png',
    		scale:'medium',
    	},{
    		id:'cancelTaskBtn',
    		text:'&nbsp;&nbsp;'+i18n.getMsg('runningTasks.stop'),
    		icon:'/images/stop.png',
    		scale:'medium',
    		disabled:true,
    		handler:function(){
    			/*for(var record in selModel.getSelection()){
                		alert(record.get('id'));
				}*/
				var items = "";
				Ext.each(grid.getSelectionModel().getSelection(), function (item) {
				  items += item.data.id+",";
				});
				Ext.Msg.show({
				     title:i18n.getMsg('runningTasks.confirmStop.title'),
				     msg: i18n.getMsg('runningTasks.confirmStop.message', items),
				     buttons: Ext.Msg.YESNO,
				     icon:Ext.Msg.WARNING,
				     fn:function(btn){
				     	if(btn == "yes"){
				     		var conn = new Ext.data.Connection(); 
						    conn.request({ 
						            url: '/api/Tasks/Cancel', 
						            method: 'GET', 
						            scope: this, 
						            //params: items, 
						            params:{format:'json'},
						            /*success: function(responseObject) { 
						             var okMsg = Ext.Msg.alert('Status', responseObject.responseText); 
						             //Ext.getCmp('create').disable();
						            }, */
						             failure: function(responseObject) { 
						                 Ext.Msg.alert('Status', 'Unable to apply changes. Error:'+responseObject.responseText); 
						            } 
						    }); 
						    RefreshStores();
				     	}
				     	else{
				     		this.close();
				     	}
				     }
				});
    		}
    	},'-',{
    		xtype:'displayfield',
    		value:i18n.getMsg('runningTasks.groupBy'),
    	},{
    		xtype:'combo',
    		id:'groupBy',
    		stateful:true,
    		stateId:'tasksGroupByState',
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
    		store:Ext.create('Ext.data.Store', {
                  fields : ['name', 'value'],
                    data   : [
                        {name : 'bsName',   	value: 'backup set name'},
                        {name : 'Operation',  	value: i18n.getMsg('runningTasks.operation')},
                        {name : 'Status', 		value: i18n.getMsg('runningTasks.status')},
                        {name : 'RunStatus',value: i18n.getMsg('runningTasks.runningStatus')},
                        {name : 'Type', 		value: i18n.getMsg('runningTasks.type')},
                        {name : 'Priority', 	value: i18n.getMsg('runningTasks.priority')},
                        //{name : 'percent', value: 'completion %'},
                        {name : 'ClientId', 	value: i18n.getMsg('runningTasks.client')},
                       // {name : 'rate', value: i18n.getMsg('runningTasks.rate')},
                        {name : 'StartDate', 	value: i18n.getMsg('runningTasks.startDate')},
                       // {name : 'elapsedTime', value: i18n.getMsg('runningTasks.duration')},
                        {name : 'FinalSize', 	value: i18n.getMsg('runningTasks.size.final')},
                    ]
	        }),
            listeners:{
            	'change': function( thisCombo, newValue, oldValue, options ){
            		if(newValue == '')  taskStore.ungroup();
            		else       			taskStore.group(newValue);
            	}
            }
    	},{
            text:'&nbsp;&nbsp;'+i18n.getMsg('runningTasks.clearGroupBy'),
            icon:'/images/clearfield.png',
            handler : function(){
            	taskStore.clearGrouping();
        	}
      	},'-',{
      		text:'&nbsp;'+i18n.getMsg('runningTasks.details'),
            icon:'/images/plus.gif',
            handler : toggleDetails
      	},'-',
	  ],
      toggleHandler: function(btn, pressed){
        var view = grid.getView();
        view.showPreview = pressed;
        view.refresh();
        if(detailsToggled) {
        	toggleDetails();
        	toggleDetails();
        }
       }
	});

	combo.on('select', function(combo, record) {
	  bbar.pageSize = parseInt(record.get('id'), 10);
	  bbar.doLoad(bbar.cursor);
	}, this);
	
    
  var grid = Ext.create('Ext.grid.Panel', {
  		id:'grid',
        renderTo: Ext.get('panel'),
        collapsible: false,
        frame: false,
        stateful:false,
        //stateId:'grid1',
        store: taskStore,
        selModel: selModel,
        //selModel: Ext.create("Ext.selection.CheckboxModel", { checkOnly : true }),
        height: '100%',
        border:false,
        scroll:'vertical',
        frameHeader:false,
        multiSelect: true,
        features: [groupingFeature],
       	viewConfig: {
	    	enableSelection: true,
	    	disableSelection: true,
	    	stripeRows: true
	    },
        plugins: [
        	{
              ptype: 'rowexpander',
              pluginId: 'expander',
              id:'expander',
              enableCaching:false,
              rowBodyTpl:[
	              '<table><tr><td><div class="gridCell" style="margin-left:75px;max-height:100;width:600;overflow-x:none;overflow-y:auto;">',
	              '<table style="height:100%;">',
	              '<tr><th colspan="3">Task log</th></tr>',
	              '<tpl for="values.LogEntries">',
	              '<tr>',
	              '<td>',
	              '<tpl if="Code &gt;= 900"><img class="x-tree-node-icon" src="/images/sq_ye.gif"/></tpl>',
	              '<tpl if="Code &gt;= 800 && Code &lt; 900"><img class="x-tree-node-icon" src="/images/sq_re.gif"/></tpl>',
	              '<tpl if="Code &gt;= 700 && Code &lt; 800"><img class="x-tree-node-icon" src="/images/sq_gr.gif"/></tpl>',
	              '{[ Ext.Date.format(new Date(values.Date), "h:i:s") ]}</td>',
	              '<td>({Code}) {[i18nTask.getMsg("task."+values.Code, values.Message1, values.Message2)]}</td>',
	              '</tr>',
	              '</tpl>',
	              '</table></div></td>',
	              '<td width="50">&nbsp;</td>',
	              '<td><div class="gridCell" style="max-height:100;width:150;overflow-x:none;overflow-y:auto;">',
	              '<table style="height:100%;">',
	              '<tr><th>Sessions</th></tr><tr><td><br/><br/><br/>',
	              //'{[ taskStore.getById('24481').sessions().each(function(session){	alert(session.id)}) ]}',
	              '</td></tr></table>',
	              '</div></td></tr></table>',
	             
              ],
              urlTpl: '/api/Tasks/{Id}/Log?format=json',
              //stateful:true,
              //stateId:'expanderState',
		    },
        ],
        columns: [
	        {
	        	text: '%',
	        	width:70,
	        	dataIndex:'Status',
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
	            	//value='<img src="/images/compe_'+record.get('status').substr(0,1).toLowerCase()+"_"+compA[index]+'.png"/>'+compA[index]+'%';
	            	value='<img src="/images/c_'+record.get('Status').substr(0,1).toLowerCase()+"_"+roundedPercent+'.png" title="'
	            		+i18n.getMsg('runningTasks.status.'+record.get('Status'))+'"/><div class="greyV" style="horizontal-align:right;"><b>'+record.get('Percent')+'%</b></div>';
	            	return value;
	            }
	        },
	        {
	            text: i18n.getMsg('runningTasks.task'),
	            //flex: 2,
	            dataIndex: 'BsName',
	            width:300,
	            renderer: function(value, metaData, record, colIndex, store, view){
	            	value = '<div class="taskBox"><b>#'+record.get('Id')+' <i>('+record.get('BsName')+')</i></b>'
	            		+'<span><ul><li class="gStatus">'+record.get('CurrentAction')+'</li></ul></span></div>';
		            return value;
	            }
	        },{
	            text: i18n.getMsg('runningTasks.client'),
	            //flex: 0,
	            width:140,
	            dataIndex: 'NodeId',
	            renderer: function(value){
	            	nStore.each( function(node){
							if(node.get('Id') == value){
								value = '<div class="'+node.get('iconCls')+'" style="padding-left:15px;background-position:top left;">'
									+node.get('Name')+'<br/> <span class="greyV">'+node.get('IP')+'</span></div>';
								//break;
							}
					});
					return value;
	            }
	        },{
	            text: i18n.getMsg('runningTasks.operation')+'<br>'+i18n.getMsg('runningTasks.priority'),
	            width:90,
	            dataIndex: 'Operation',
	            renderer: function(value, metaData, record, colIndex, store, view){
	            	if(value == 'Backup'){
	            		value += " ("+record.get('Level')+")";
	            		/*if(record.get('compress') == 'True')
	            			value += '<img src="/images/compress.png" title="compress"/>&nbsp;';
	            		if(record.get('encrypt') == 'True')
	            			value += '<img src="/images/encrypt.png" title="encrypt"/>&nbsp;';
	            		if(record.get('clientdedup') == 'True')
	            			value += '<img src="/images/clientdedup.png" title="client deduplication"/>&nbsp;';*/
		            	if(value == '') return '';
		            	/*var capsList = record.get('Flags').toLowerCase().split(", ");
		            	for (var i=0; i < capsList.length; i++) 
		            		if(capsList[i] != 'none')
		            			value += '<img src="/images/'+capsList[i].substring(1)+'.png" title="'+capsList[i]+'"/>&nbsp;';
		            	*/
	            	}
	            	value += '<br/><i>'+ i18n.getMsg('runningTasks.priority.'+record.get('Priority'))+'</i>';
	            	return value;
	            }
	        },{
	            text: i18n.getMsg('runningTasks.type'),
	            //flex: 1,
	            witdh:100,
	            dataIndex: 'Priority',
	             renderer: function(value, metaData, record, colIndex, store, view){
	             	var userName = '';
	             	if(record.get('Type') == 'Manual'){
	             		usersStore.each( function(trecord){
								if(trecord.get('Id') == record.get('UserId')){
									userName = ' <img src="/images/me-xs.png" border="0" align="top"/><i>'+trecord.get('Name')+'</i>';
								}
						});
	             	}
        			return ''+i18n.getMsg('runningTasks.type.'+record.get('Type'))+'<br/> '+userName;
        			
        		}
	       },{
	            text: i18n.getMsg('runningTasks.status'),
	            //flex: 1,
	            width:90,
	            dataIndex: 'RunStatus',
	            renderer: function(value, metaData, record, colIndex, store, view){
	            	return i18n.getMsg('runningTasks.status.'+value)+"<br/>"+FormatTime(record.get('ElapsedTime'))+"";
	            }
	        },{
	            text: i18n.getMsg('generic.size'),
	            columns:[
	            	{
	            		text: '<i>'+i18n.getMsg('runningTasks.size.original')+'</i>',
	            		//flex: 0,
	            		width:65,
	            		margins:'-4 0 1 0',
	            		dataIndex:'OriginalSize',
	            		renderer: function(value, metaData, record, colIndex, store, view){
			            	return FormatSize(value)+'<br/><div align="right"><i>'+record.get('TotalItems')+'</i></div>'; //+' items)</i>';
			            }
	            	},
	            	{
	            		text: '<i>'+i18n.getMsg('runningTasks.size.final')+'</i>',
	            		//flex: 0,
	            		width:65,
	            		margins:'-4 0 1 0',
	            		dataIndex:'FinalSize',
	            		renderer: function(value){
			            	return FormatSize(value)+'<br/><div align="left"><i> '+i18n.getMsg('runningTasks.items')+'</i></div>';
			            }
	            	},
	            	
	            ]
	        },{
	            text: i18n.getMsg('runningTasks.rate'),
	            //flex: 0,
	            width:85,
	            renderer: function(value, metaData, record, colIndex, store, view){
	            	var content =  FormatSize(record.get('OriginalSize')/ ( (new Date() - record.get('StartDate'))/1000 ))+"/s"
	            	var sessCount = 0;
	            	/*record.sessions().each(function(session) {
                		//console.log(comment.get('message')); 
                		sessCount++;
            		});
            		content += "<i>("+sessCount+" "+i18n.getMsg('runningTasks.sessions')+"/"+record.get('Parallelism')+")</i>";
            		*/
            		return content;
	            }
	        },{
	            text: i18n.getMsg('runningTasks.startDate')
	            	+'<br/>'+i18n.getMsg('runningTasks.endDate'),
	            width:130,
	            align:'left',
	            dataIndex: 'StartDate',
	            renderer: function(value, metaData, record, colIndex, store, view){
	            	return ''+record.get('StartDate').toLocaleString()
	            		+'<br/>'+ record.get('EndDate').toLocaleString();
	            }
	        },
        ],
        dockedItems: [bbar]
    });
 	/*grid.store.addListener('load', function() {
	  var expander = grid.getPlugin('expander');
	  for(i = 0; i < grid.getStore().getCount(); i++) {
	    expander.toggleRow(i);
	  }
	});*/
 	//automatic refresh
 	function SetAutoRefresh(){
 		refreshInterval = setInterval(RefreshStores,30000)
 	}
 	
 	function RefreshStores(){
 		nStore.load();
 		taskStore.load();
 		
 		if(detailsToggled) {
        	toggleDetails();
        }
        /*	setTimeout(null, 1000);
        	toggleDetails();
        }*/
        grid.view.refresh();
 	}
 	SetAutoRefresh();
 });
  
 });
 //});