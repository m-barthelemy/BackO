Ext.require([
    'Ext.data.*',
    'Ext.grid.*',
    'Ext.tree.*',
    'Ext.form.*',
    'Ext.chart.*',
]);

Ext.onReady(function () {
	Ext.Loader.setConfig({enabled:true});
	
    var i18n = Ext.create('Ext.i18n.Bundle',{
		bundle: 'wui',
		lang: Ext.util.Cookies.get('lang'),
		path: '/i18n',
		noCache: false
	});
	
i18n.onReady(function(){

	Ext.QuickTips.init();
	Ext.tip.QuickTipManager.init();
	Ext.apply(Ext.tip.QuickTipManager.getQuickTip(), {
    	showDelay: 500,      // Show 500ms after entering target
    	hideDelay: 1000,
    	dismissDelay: 30000,
    	autoHide:true,
		closable:false,
	});
	
	interval = 4; // by default show the plan for the next 4 hours
	
	var BackupSetSchedule = Ext.define('BackupSetSchedule', {
	    extend: 'Ext.data.Model',
	    fields: [
	    	{name: 'Id',     		type: 'int'},
	        {name: 'Name',     		type: 'string'},
	        {name: 'Inherits',     	type: 'int'},
	        {name: 'Enabled',     	type: 'boolean'},
	        {name: 'IsTemplate',    type: 'boolean'},
	        {name: 'DataFlags',     type: 'int'},
	        {name: 'HandledBy',     type: 'int'},
	        {name: 'MaxChunkFiles',	type: 'numeric'},
	        {name: 'MaxChunkSize', 	type: 'numeric'},
	        {name: 'MaxPackSize',  	type: 'numeric'},
	        {name: 'NodeId',     	type: 'int'},
	        {name: 'Operation',    	type: 'string'},
	        {name: 'Preop',     	type: 'string'},
	        {name: 'Postop',     	type: 'string'},
	        {name: 'RetentionDays', type: 'int'},
	        {name: 'StorageGroup',  type: 'int'},
	        {name: 'BasePaths', 	persist: true},
	    	
	        {name: 'Day',     		type: 'string'},
	        {name: 'BeginHour',     type: 'int'},
	        {name: 'EndHour',     	type: 'int'},
	        {name: 'BeginMinute',   type: 'int'},
	        {name: 'EndMinute',     type: 'int'},
	        {name: 'Level',     	type: 'string'},
	    ],
	    hasMany: [
	 		{ 
		    	model: 'BasePath', 
		    	name: 'BasePaths', 
		    	associationKey: 'BasePaths' ,
	    	}
    	]
	});
    	
    
	var schedStore = new Ext.data.JsonStore({
		model:'BackupSetSchedule',
		autoLoad: false,
		proxy: {
	        type: 'ajax',
	        url:'/api/Plan',
	        extraParams: {
		        format	: 'json',
		        interval: interval
		    },
		    reader:{
	        	type:'json',
	        	applyDefaults: true
	        }
	        //root: 'backupSets',
    	},
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
	    },
	    listeners:{
	    	load:function(){
	    		console.debug('loaded online client nodes store');
	    		schedStore.load({
				    callback: function(records, operation, success) {
				        //bPlanGrid.reconfigure(schedStore, columns);
				        bPlanGrid.getView().refresh();
				    }
				});
	    	}
	    }
	 });
 
 	
	//adapt grid size
	var gridWidth = document.getElementById('panel').offsetWidth; //ok with Chrome & Firefox
	var hoursWidth = gridWidth-(170/*+90*/) ;// substract 'node' column
	
	
	function GenerateColumns(nbDisplayedHours){
		var d = new Date();
		var columns = []
		var halfHourWidth = Math.round(hoursWidth/(nbDisplayedHours*2));
		// by default, we display a 12 hours timeline
		for(i=d.getHours()-1; i<d.getHours()+(nbDisplayedHours-1);i++){
		for(j=0;j<2;j++){ // resolution is : every 1/2h. Does it really make sense to be more precise here?
			var colId = '';
			var colTime="";
			var colText="";
			if(i<0){
				colId = "0_"+(i+24);
				colTime = "0_"+(i+24);
			}
			else if(i == 0){
				colId = "1_0";
				colTime = "1_0";
			}
			else if(i<24){
				colId = "0_"+i;
				colTime = "0_"+i;
			}
			else{
				colId = "1_"+(i-24);
				colTime = "1_"+(i-24);
			}
			if(j==0) colTime = colTime+":00";
			else {
				colTime = colTime+":30";
			}
			
			var colText = colTime.substring(2);
			
			if(d.getHours() == i && j==0 && d.getMinutes() < 30)
				colText = "<b>"+colText+"</b>";
			if(d.getHours() == i && j==1 && d.getMinutes() >= 30)
				colText = "<b>"+colText+"</b>";
				
			var this_column = new Ext.grid.column.Column({
				name: colTime,
				sortable	: false,
				resizable	: false, 
				menuDisabled: true,
				unlockable	: false,
				text		: colText+'&nbsp;', // put empty content, else column won't show
				margins		: 0,
				padding		: '0 -2 0 -2',
				flex		: 0,
				width		: halfHourWidth,
				//locked		: true,
				renderer	: function(value, metaData, taskSet, rowIndex, colIndex, store) {
					var colRawName = Ext.getCmp('bPlanGrid').headerCt.getGridColumns()[colIndex].name;
					//metaData.tdAttr += ' style="margin-left:4px !important; padding:-4px !important;"';
					if(colRawName == null)
						return '';
					var colCurTime = colRawName.substring(2);
					var content = '';
					/*console.debug('colindex='+colIndex+', rowindex='+rowIndex+', got new scheduled backupset : '
						+taskSet.get('Id')+', name='+taskSet.get('Name')+', starthour='+taskSet.get('BeginHour')  
						+', colcurtime substring='+colCurTime.substring(0, colCurTime.indexOf(':')));
					*/

					var cellClass="";
					var colHour = parseInt(colCurTime.substring(0, colCurTime.indexOf(':')));
					var colMinute = parseInt(colCurTime.substring(colCurTime.indexOf(':')+1));
					
					if(taskSet.get('BeginHour') == colHour
						&& (
							colMinute ==0 && taskSet.get('BeginMinute') < 30  
							|| colMinute == 30 && taskSet.get('BeginMinute') >= colMinute
						)
					){
						//console.warn('found backupset scheduled @'+taskSet.get('BeginHour')+':'+taskSet.get('BeginMinute')+', colCurTime='+colCurTime);
						
						if(taskSet.get('Operation') == "Backup"){
							if(taskSet.get('Level') == 'Full')
			                	cellClass += " bFull";
			                else if(taskSet.get('Level') == 'Differential')
			                	cellClass += " bDiff";	
			                else if(taskSet.get('Level') == 'Incremental')
			                	cellClass += " bIncr";	
			                else if(taskSet.get('Level') == 'Refresh')
			                	cellClass += " bRefresh";	
		                }
		                else if(taskSet.get('Operation') == "HouseKeeping"){
		                	if(taskSet.get('type') == 'Full')
			                	cellClass += " bSpecialFull";
		                	else if(taskSet.get('Level') == 'Differential')
		                		cellClass += " bSpecialDiff";
		                	else if(taskSet.get('Level') == 'Incremental')
		                		cellClass += " bSpecialIncr";
		                }
		                
		                // adjust task duration
		                var duration = 0;
		                
		                // handle tasks with a defined end window
		                if(taskSet.get('EndHour') != -1){
		                	cellClass += " cell_bb";
			                // task ends same day as its start day
			                if(taskSet.get('EndHour') > taskSet.get('BeginHour'))
			                	duration = taskSet.get('EndHour') - taskSet.get('BeginHour');
			                else
			                	duration = 24- (taskSet.get('BeginHour') - taskSet.get('EndHour')) ;
			                
			                metaData.tdAttr += ' colspan="'+(duration*2)+'"';
			            }
			            else{ // backup with no defined end window
			            	cellClass += " cell_bu";
			            	metaData.tdAttr += ' colspan="5"'; // default to 2.5 hours
			            }
		                content = '#'+taskSet.get('Id')+' <i>('+taskSet.get('Name')+')</i>';
					}
						//console.debug('got new scheduled backupset : '+taskSet.get('Id')+', name='+taskSet.get('Name'));
						
					//qtip with details about backupset, on hover. todo : restore them
					metaData.tdAttr += ' data-qtip="<center><b><i>'+taskSet.get('Name')+'</i></b></center><hr><br/><table>' ;
					metaData.tdAttr += '<tr><td><b>Id</b>:</td><td>#'+taskSet.get('Id')+'</td></tr>';
					
					metaData.tdAttr += '<tr><td><b>Level</b>:</td><td>'+taskSet.get('Level')+'</td></tr>';
					metaData.tdAttr += '<tr><td><b>Begin</b>:</td><td>'+taskSet.get('BeginHour')+':'+taskSet.get('BeginMinute')+'</td></tr>';
					var endValue = 'undefined';
					if(taskSet.get('EndHour') > -1)
						endValue = taskSet.get('EndHour')+':'+taskSet.get('EndMinute');
					metaData.tdAttr += '<tr><td><b>Max. end:&nbsp;</b></td><td>'+endValue+'</td></tr>';
					metaData.tdAttr += '<tr><td><b>Options:&nbsp;</b></td><td>';
        			metaData.tdAttr += '</td><tr/><tr><td><b>Items: </b></td><td><ul style=\'list-style-type: square;\'>';
        			taskSet.BasePaths().each(function(basePath) {
        				metaData.tdAttr += '<li><i>'+basePath.get('Path')+'</i></li>';
        			});
        			metaData.tdAttr += '</ul></td></tr>';
        			metaData.tdAttr += '</table>" ';
					
		           return '<div class="'+cellClass+'" onclick="">&nbsp;'+content+'</div>';
		         }
	        });
			columns.push(this_column); 
		}	
		}
		
		// optional (hidden by default) column to show nodes IPs
		var secondCol = new Ext.grid.column.Column({
			text		: i18n.getMsg('nodestree.currentIP'),
			flex		: 0,
			width		: 90,
			sortable	: true,
			resizable	: true,
			locked		: true,
			hidden		: true,
			renderer:function(value) {
				nStore.each(function(node) {
					if(node.get('Id') == value){
						return node.get('IP');
					}
				});
				//metaData.style +='border-right:1px solid lightgrey;';
				return '';
		
			}
		});
		columns.unshift(secondCol);
		
		var firstCol = new Ext.grid.column.Column({
			//id:0,
			text		: i18n.getMsg('nodestree.node'),
			flex		: 0,
			width		: 170,
			sortable	: true,
			resizable	: true,
			locked		: true,
			//groupable : true,
			menuDisabled: true,
			dataIndex	: 'NodeId',
			renderer:function(value, metaData, record, rowIndex, colIndex, store) {
				nStore.each(function(node) {
					if(node.get('Id') == value){
						value = '<div class="node-on" style="padding-left:15px;background-position:top left;">'
							+node.get('NodeName')+'</div>';
						//break;
					}
				});
				//metaData.style +='border-right:1px solid lightgrey;';
				return value;
			}
		});
		columns.unshift(firstCol);
		
		return columns;
		} // enf GenerateColumns
		
		
	
	var combo = new Ext.form.field.ComboBox({
	  name : 'perpage',
	  width: 40,
	  store: new Ext.data.ArrayStore({
	    fields: ['id'],
	    data  : [ ['15'], ['20'], ['30'], ['40'] ]
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

	var bbar = new Ext.toolbar.Paging({
	  store: schedStore,
	  displayInfo: true,
	  items   :    [
	    '-',
	    i18n.getMsg('generic.perPage'),
	    combo,
	    '-',
	    {
    		xtype:'displayfield',
    		value:i18n.getMsg('runningTasks.groupBy'),
    	},
	    {
    		xtype			: 'combo',
    		id				: 'groupBy',
    		mode			: 'local',
            value			: 'none',
            triggerAction	: 'all',
            forceSelection	: true,
            allowBlank		: true,
            editable		: false,
            displayField	: 'value',
            valueField		: 'name',
            queryMode		: 'local',
            width			: 150,
            store			: Ext.create('Ext.data.Store', {
              fields : ['name', 'value'],
                data   : [
                    {name : 'client',   value: 'client node'},
                    {name : '', value: 'don\'t group'},
                ]
            }),
            listeners:{
            	'change': function( thisCombo, newValue, oldValue, options ){
            		schedStore.group(newValue);
            	}
            }
    	},
	  ],
	  doRefresh : function(){
         var me = this;
         me.store.getProxy().extraParams.interval = interval;
         me.store.load();
      }
	});

	combo.on('select', function(combo, record) {
	  bbar.pageSize = parseInt(record.get('id'), 10);
	  bbar.doLoad(bbar.cursor);
	}, this);

	var groupingFeature = Ext.create('Ext.grid.feature.Grouping',{
        groupHeaderTpl: '{name} ({rows.length} Item{[values.rows.length > 1 ? "s" : ""]})',
        depthToIndent:15,
  	});
  
  	
  	
	var bPlanGrid = new Ext.grid.Panel({
	    id:'bPlanGrid', 
	    store: schedStore,
	    renderTo:Ext.get('panel'),
	    width: gridWidth,
	    height: '100%',
	    frame: false,
	    enableCtxMenu: true, 
	    enableColumnResize:false,
	    enableHdMenu:false,
	    autoShow:false,
	    viewConfig: {loadMask: false},
	    features: [groupingFeature],
	    //groupField:'client',
	    columns: GenerateColumns(interval), //columns,
		tbar:{
			height:27,
			items:[
			  {
                text:' ',
                icon:'/images/prev.png',
                width:45,
                height:22,
                listeners:{
                     scope:this
               	}
               },{
                text:'',
                width:230,
                disabled:true
               },{
               	text:'Period :',
               	disabled:true,
               	locked:true,
               	align:'center'
               },{
		    		xtype		: 'combo',
		    		id			: 'period',
		    		mode		: 'local',
		            value		: interval,
		            triggerAction:  'all',
		            forceSelection: true,
		            allowBlank	: true,
		            editable	: false,
		            displayField: 'value',
		            valueField	: 'name',
		            queryMode	: 'local',
		            width:150,
		            store:          Ext.create('Ext.data.Store', {
		              fields : ['name', 'value'],
		                data   : [
		                    {name : '4',   	value: '4 hours'},
		                    {name : '8', 	value: '8 hours'},
		                    {name : '12', 	value: '12 hours'}, 
		                    {name : '24', 	value: '24 hours'},
		                    {name : 'w', 	value: 'week'},
		                ]
		            }),
		            listeners:{
		            	'change': function( thisCombo, newValue, oldValue, options ){
		            		if(newValue != null){
		            			interval = newValue;
                				schedStore.load({params:{interval:interval}});
                				var grid = Ext.getCmp('bPlanGrid');
                				grid.reconfigure(schedStore, GenerateColumns(interval));
                			}
		            	}
		            }
		    	},{
	                text:'',
	                width:240,
	                disabled:true
	            }, {
	                text:' ',
	                icon:'/images/next.png',
	                width:45,
	                align:'right',
	                listeners:{
	                     scope:this
	                    //,click:{fn:this.addRecord,buffer:200}
               		}
            	}
        	]
        },
	    bbar: bbar
	});
	
	//automatic refresh
 	setInterval(function(){
 			schedStore.load({params:{interval:interval}});
 			bPlanGrid.reconfigure(schedStore, GenerateColumns(interval));
 		},60000);
	});
});