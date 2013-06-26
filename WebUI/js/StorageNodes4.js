Ext.require([
    'Ext.data.*',
    'Ext.grid.*',
    'Ext.tree.*',
    'Ext.form.*',
    'Ext.window.*',
    'Ext.chart.*',
    'Ext.fx.target.Sprite',
]);

Ext.onReady(function () {
	Ext.Loader.setConfig({enabled:true});
     
i18n.onReady(function(){

	Ext.tip.QuickTipManager.init(true, {maxWidth: 450,minWidth: 150, width:350 });
	
	var groupsStore = new Ext.data.JsonStore( {
        model : 'StorageGroup',
        autoLoad:true,
        proxy: {
            type: 'rest',
            url: '/api/StorageGroups/',
            extraParams: {format: 'json'},
            reader:{
	        	type:'json',
	        	applyDefaults: true
	        }
        }
    });
        
	var toBeExpanded = true;
    var fieldzWithoutDate = [];
	var fieldz=[];
	fieldz.push('date');
	
    var sgStore = new Ext.data.TreeStore( {
        model : 'Node',
        storeId:'sgStore',
        autoLoad:true,
        proxy: {
            type: 'ajax',
            url: '/api/StorageNodes',
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
					if(rec.get('Group') > -1){
						//rec.set('checked', false );
						if(rec.get('Status') == 'Idle')
							rec.set('iconCls','node-idle');
						else if(rec.get('Status') == 'Online' || rec.get('Status') == 'Backuping' || rec.get('Status') == 'Restoring')
							rec.set('iconCls','node-on');
						else if(rec.get('Status') == 'Error')
							rec.set('iconCls','node-err');
						else
							rec.set('iconCls','node-off');
					}
					else if(rec.get('Group') == -1)
						rec.set('icon', '/images/sg-i.png');
					
				});
	    	}
	    }
    });
	
	
    var tree = new Ext.tree.Panel({
        id:'clientNodesTree',
        layout:'fit',
        height: '100%',
        width:900,
        overlapHeader:true,
        collapsible: false,
        useArrows: true,
        rootVisible: false,
        store: sgStore,
        multiSelect: true,
        singleExpand: false,
        draggable:false,  
        folderSort: true,     
        columns:[{
        	xtype: 'treecolumn',
            text: i18n.getMsg('nodestree.node'), //'Storage group / Nodes',
            width:190,
            flex:2,
            dataIndex:'Name',
            renderer: function(value, metaData, record, colIndex, store, view){
            	value = (record.get('Name') == '') ? record.get('HostName') : record.get('Name');
		    	value = (value == '') ? record.get('IP') : value;
		    	if(record.get('Group') == -1)
		    		value = '<b>'+value+'</b>';
		    	return '<span data-qtip="#'+record.get('Id')+'<br/>'+record.get('Description')+'">'+value+'</span>';
	        }
        },{
            header:i18n.getMsg('nodestree.listenIP'), //'Hostname / IP',
            width:95,
            flex:0,
            dataIndex:'ListenIP',
            padding:'0',
            renderer: function(value, metaData, record, colIndex, store, view){
            	if(record.get('Group') == -1) return '';
            	if(record.get('leaf') == true)
            		value = "&nbsp;&nbsp;"+value;
            	
            	if(record.get('cls').length > 1)
	            	return '<span class="'+record.get('cls')+'">'+value+"</span>";
	            else
	            	return ' '+value;
	         }
        },{
            header:i18n.getMsg('generic.description'), //'Hostname / IP',
            width:170,
            dataIndex:'Description',
            padding:'0',
            hidden: true,
        },{
            header:i18n.getMsg('nodestree.currentIP'), //'Hostname / IP',
            width:95,
            flex:0,
            dataIndex:'IP',
            padding:'0',
            hidden: true,
            renderer: function(value, metaData, record, colIndex, store, view){
            	if(record.get('Group') == -1) return '';
            	else return value;
	         }
        },{
            text: i18n.getMsg('nodestree.listenPort'),
            flex: 0,
            width:50,
            dataIndex: 'ListenPort',
            renderer: function(value, metaData, record, colIndex, store, view){
            	if(record.get('Group') == -1) return '';
            	else return value;
            }
        },{
        	header: i18n.getMsg('nodestree.priority'), //'Priority',
            width:45,
            flex:0,
            dataIndex:'StoragePriority',
            renderer: function(value, metaData, record, colIndex, store, view){
            	if(record.get('Group') == -1) return '';
            	if(record.get('leaf') == true)
            		value = "&nbsp;&nbsp;"+value;
            	if(record.get('cls').length > 1)
	            	return '<span class="'+record.get('cls')+'">'+value+"</span>";
	            else
	            	return ' '+value;
	         }
        },{
        	header:i18n.getMsg('nodestree.storageSize'), //'Storage space',
            width:85,
            flex:0,
            dataIndex:'StorageSize',
            renderer: function(value, metaData, record, colIndex, store, view){
            	if(record.get('cls').length > 1)
	            	value = '<span class="'+record.get('cls')+'">'+FormatSize(value)+"</span>";
	            else
	            	value = '  '+FormatSize(value);
	            if(record.get('leaf') == true)
            		value = "&nbsp;"+value;
	            return value;
	         }
        },{
        	header:i18n.getMsg('nodestree.storageUsed'), //'Available',
            width:75,
            flex:0,
            dataIndex:'StorageUsed',
            renderer: function(value, metaData, record, colIndex, store, view){
            	
            	if(record.get('cls').length > 1)
	            	value = '<span class="'+record.get('cls')+'">'+FormatSize(value)+"</span>";
	            else
	            	value = '  '+FormatSize(value);
	            if(record.get('leaf') == true)
            		value = "&nbsp;"+value;
	            return value;
	         }
        },{
        	header:i18n.getMsg('nodestree.percentUsed'), //'% used',
            width:70,
            flex:0,
            renderer: function(value, metaData, record, colIndex, store, view){
            	value = Math.round(record.get('StorageUsed')/record.get('StorageSize')*1000)/10;
            	var iconStyle="";
            	if(value > 90)
                	iconStyle="sq_re.gif";
                else if(value < 80)
                	iconStyle="sq_gr.gif";
                else
                	iconStyle="sq_ye.gif";
              	value = '<img style="vertical-align:middle;border:0;" src="/images/'+iconStyle+'">'+value+' %';
            	if(record.get('cls').length > 1)
	            	value= '<span class="'+record.get('cls')+'">'+value+"</span>";
	            else
	            	value= '  '+value;
	            if(record.get('leaf') == true)
            		value = "&nbsp;"+value;
	            return value;
	         }
         },{
         	header: i18n.getMsg('nodestree.version'), //'Version',
            width:55,
            flex:0,
            dataIndex:'Version',
            hidden: true,
         },{
            text: i18n.getMsg('nodestree.os'),
            flex: 0,
            dataIndex: 'OS',
            width:35,
            renderer:function(value, metaData, record, colIndex, store, view){
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
        	header:i18n.getMsg('nodestree.storagePath'), //'Storage path',
            width:210,
            dataIndex:'StoragePath',
            flex:1,
        },{
        	header:i18n.getMsg('nodestree.status'), //'Status',
            width:80,
            dataIndex:'Status',
            hidden: true,
            renderer:function(value, metaData, record, colIndex, store, view){
            	if(record.get('Group') == -1) return '';
            	return i18n.getMsg('nodestree.status.'+value);
            }
        }/*,{
            text: i18n.getMsg('nodestree.capabilities'),
            flex: 0,
            width:100,
            sortable: true,
            dataIndex: 'Capabilities',
            renderer: function(value, metaData, record, colIndex, store, view){
            	var formattedCaps="";
            	if(value == '') return '';
            	var capsList = value.toLowerCase().split(", ");
            	for (var i=0; i < capsList.length; i++) 
            		formattedCaps += '<img src="/images/'+capsList[i].substring(1)+'.png" title="'+capsList[i]+'"/>&nbsp;';
        		
            	return formattedCaps;
            }
        
        }*/],
        dockedItems: [{
		    xtype: 'toolbar',
		    dock: 'bottom',
		    style:'bottom:0px;',
		    padding:1,
		    margins:0,
		    height:25,
		    items: [
		        {
		        	xtype:'button',
	        		text:i18n.getMsg('generic.add'), //'&nbsp;&nbsp;Configure',
	        		id: 'addBtn',
	               	iconCls:'icon-btn',
	                icon:'/images/add.png',
	                width:80,
	                height:22,	  
	                border:1,             
	               	handler:function(){
	               		getSnConfig(Ext.create('StorageGroup'));
	               	}
	        	},{
		        	xtype:'button',
	        		text:i18n.getMsg('nodestree.configureBtn'), //'&nbsp;&nbsp;Configure',
	        		id: 'configureBtn',
	               	iconCls:'icon-btn',
	                icon:'/images/conf.png',
	                width:80,
	                height:22,	  
	                border:1,             
	               	handler:function(){
	               		getSnConfig(groupsStore.getById(tree.getSelectionModel().getSelection()[0].get('Id')));
	               	},
	               	disabled:true,
	        	},{
		        	xtype :'button',
	        		text: i18n.getMsg('generic.delete'), 
	        		id: 'deleteBtn',
	               	iconCls:'icon-btn',
	                icon:'/images/delete.png',
	                width:80,
	                height:22,	  
	                border:1,             
	               	handler:function(){
	               		var toBeDeleted = groupsStore.getById(tree.getSelectionModel().getSelection()[0].get('Id'));
	               		console.log('asked to delete sg #'+toBeDeleted.get('Id'));
	               		groupsStore.remove(toBeDeleted);
	               		groupsStore.sync();
	               		sgStore.reload();
	               	},
	               	disabled:true,
	        	},'-',
	        	{
	          		text:''+i18n.getMsg('nodestree.expand'),
		            icon:'/images/plus.gif',
		            handler : function(){
	                	Ext.getCmp('clientNodesTree').getRootNode().cascadeBy(function(r) {  
	                		if(toBeExpanded == true)
	                			r.expand();  
	                		else
	                			r.collapse();
	                	})
	                	if(toBeExpanded == true){
                			toBeExpanded = false;
                		}
                		else{
                			toBeExpanded = true;
                			Ext.getCmp('clientNodesTree').getRootNode().expand();
                		}
                		
	            	}
	          	}
	        ]
	      }],
    	listeners: {
          	selectionchange:function(thisObj, selected, opts){
          		if(selected.length == 1 && selected[0].get('Group') == -1){
          			Ext.getCmp('configureBtn').enable();
          			Ext.getCmp('deleteBtn').enable();
          		}
          		else{
          			Ext.getCmp('configureBtn').disable();
          			Ext.getCmp('deleteBtn').disable();
          		}
          	}
		},
    });
    
   /* Ext.define('sgM2', {
        extend: 'Ext.data.Model',
        fields: [
            
            {name: 'userName',     type: 'string'},
            
            {name: 'percent', type: 'string'},
           
        ]
    });*/
    
    /*var sgStore =  Ext.create('Ext.data.JsonStore', { //new Ext.data.JsonStore( {
    	autoLoad:true,
        model: 'sgM',
        proxy: {
            type: 'ajax',
            url: '/api/StorageNodes',
            extraParams: {
		        format	: 'json'
		    }
        },
       // reader:{
       // 	type:'json',
       // },
       
    });*/
      
        
    var globalG = Ext.create('widget.panel', {
        width: 280,
       	height: 170,
        //title: i18n.getMsg('welcome.sgChart.title'),
        renderTo: Ext.get('sgChart'), //Ext.getBody(),
        layout: 'fit',
       	padding:'10 0 0 0',
       	align:'right',
       	margins:5,
       	border:false,
       	bodyStyle:'background:transparent;',
       	iconCls:'task-sg',
       	align:'top',
        items: {
            xtype: 'chart',
            id: 'chartCmp',
            animate: true,
            store: groupsStore,
            field:'Name',
            shadow: true,
            title: i18n.getMsg('welcome.sgChart.title'),
            legend: {
                position: 'right',
                itemSpacing:2,
                padding:2,
                labelFont:'9px Arial',
                zIndex:0,
                field:'Name',
            },
            insetPadding: 5,
            theme: 'Base:gradients',
            series: [{
            	//title:'Storage groups space',
                type: 'pie',
                field: 'Storage',
                showInLegend: true,
                legendItem:'Name',
                //donut: 25,
                style: {
                	//opacity: 0.9,
            	},
            	mask: 'horizontal',
		        listeners: {
		            select: {
		                fn: function(me, selection) {
		                    me.setZoom(selection);
		                    me.mask.hide();
		                }
		            }
		        },
                tips: {
                  trackMouse: true,
                  width: 150,
                  height: 28,
                  anchor:'left',
                  renderer: function(storeItem, item) {
                    //calculate percentage.
                    var total = 0;
                    sgStore.each(function(rec) {
                        total += rec.get('Storage');
                    });
                    var iconStyle = "";
                    var percentFree =  (storeItem.get('OnlineStorage')+storeItem.get('OfflineStorage'))/storeItem.get('Storage')*100;
                    if(percentFree < 10)
                    	iconStyle="sq_re.gif";
                    else if(percentFree > 800)
                    	iconStyle="sq_gr.gif";
                    else
                    	iconStyle="sq_ye.gif";
                    this.setTitle('<img style="vertical-align:middle;border:0;" src="/images/'+iconStyle+'">'+storeItem.get('userName') + ': ' +storeItem.get('percent')+ '%');
                  }
                },
                highlight: {
                  segment: {
                    margin: 5
                  }
                },
                label: {
                    field: 'Storage',
                    display: 'rotate',
                    //display: 'insideEnd',
                    contrast: true,
                    font: '12px Arial',
                    minMargin:0,
                    renderer:function(value){
                    	return FormatSize(value);
                    }
                }
            }]
        }
    });
  
  var radarChart = Ext.create('Ext.chart.Chart', {
      //  margin: '0 0 0 0',
      	xtype:'chart',
        width: 700,
       	height: 380,
       	//layout:'fit',
        insetPadding: 20,
        flex:1,
        animate: true,
        store: groupsStore,
        //shadow:true,
        id:'radarChart',
        //theme:'Category2',
       /* legend: {
                position: "bottom"
                //ADDING LISTENERS HERE DOESN'T WORK
            },*/
       /* legend: {
                position: 'right',
                itemSpacing:2,
                padding:2,
                labelFont:'9px Arial',
                zIndex:1000,
                field:'percent',
            },*/
        axes: [{
            steps: 5,
            type: 'Radial',
            position: 'radial',
            maximum: 100,
            label:{
	            display:true,
	            
	            
	        }
        }],
        series: [{
            type: 'radar',
            xField:'Name',
            yField:'percent',
           // labelField: 'userName',
            //labelOrientation :'horizontal',
            showInLegend:true,
            showMarkers:true,
            markerConfig: {
                radius: 3,
                size: 4
            },
            style: {
                fill: '#797D8B',
                opacity: 0.5,
                'stroke-width': 2
            }
        }
        ]
    });
    
    
	Ext.define('BPM', {
        extend: 'Ext.data.Model',
        fields: [fieldz],
    });
    
    window.generateData = function(n, floor){
        var data = [],
            p = (Math.random() *  11) + 1,
            i;
            
        floor = (!floor && floor !== 0)? 20 : floor;
        
        for (i = 0; i < (n || 12); i++) {
            data.push({
                date: Ext.Date.monthNames[i % 12].substring(0,3),
                'Storage pool 1': Math.floor(Math.max((Math.random() * 100), floor)),
                'Unreliable pool': Math.floor(Math.max((Math.random() * 100), floor)),
                'ClientNodes': Math.floor(Math.max((Math.random() * 100), floor)),
               
            });
        }
        return data;
    };
    
    window.sgHistoryStore = Ext.create('Ext.data.JsonStore', {
        /*model:
        autoLoad: false,
		proxy: {
	        type: 'ajax',
	        url:'/Get.aspx?w=sgHistory',
	        //root: 'backupSets',
    	},*/
    	//autoLoad:false,
    	fields:fieldz,
    	data:generateData(),
    	
    });
    
       
    var seriez = [];
    
    var spaceHistory = Ext.create('Ext.chart.Chart', {
     	id:'spaceHistory',
       	layout:'fit',
        insetPadding: 20,
        flex:1,
        animate: true,
        store: sgHistoryStore,
        shadow:true,
        theme:'Category2',
       	width:260,
       	height:260,
        axes: [{
	                type: 'Numeric',
	                position: 'left',
	                title:'Space',
	                //fields: ['Unreliable pool' , 'Storage pool 1'],
	                fields:fieldzWithoutDate,
	                title: false,
	                grid: false
	            }, {
	                type: 'Category',
	                position: 'bottom',
	                title:'Date',
	                fields: ['date'],
	                title: true
	            }
	    ],
	    //series:seriez,
        series: [{
            type: 'line',
            xField:'date',
            yField:fieldzWithoutDate,
           
            showInLegend:true,
            showMarkers: true,
            markerConfig: {
                radius: 3,
                size: 3
            },
            style: {
                fill: '#797D8B',
                opacity: 0.5,
                'stroke-width': 0.5
            }
        }
        ]
    });
    
    
    var snPanel =  new Ext.Panel({
		renderTo: Ext.get('panel'),
        monitorValid:true,
        border: false,
        autoScroll:false,
        bodyPadding: 0,
		height:'100%',
        fieldDefaults: {
            labelAlign: 'left',
            labelWidth: 90,
            labelStyle: 'font-size:0.9em !important;'
        },
        defaults: {
            padding:0,
            margins:0
        },
        layout:'hbox',
        align:'right',
        items: [
        	tree, 
	        {
		        xtype:'fieldset',
		        layout: {
                    type: 'vbox',
                    align:'stretch'
                },
		        margin: '0 0 0 5',
		        width:280,
		        height:500,
		        title: i18n.getMsg('welcome.sgChart.title'),
		        items: [radarChart, spaceHistory]
	        }
        ]
	});
	
	var getSnConfig = function(sg){
		var context = 'create';
		if(sg.get('Id') > 0)
			context = 'edit';
			
		var form = new Ext.form.Panel({/*Ext.widget('form', {*/
                id:'configFormPanel',
                model: 'StorageGroup',
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
	                    	},{
			                    xtype: 'checkboxgroup',
			                    id:'Capabilities',
			                    name:'Capabilities',
			                    fieldLabel: 'Allowed data processing options',
			                    layout: 'hbox',
			                    border: true,
			                    width: 550,
			                    items: [
			                    	{
			                    		xtype: 'checkbox',
			                    		padding:'0 20 0 10',
			                    		inputValue: 512,
			                    		id:'compress',
			                    		boxLabel: '<img class="gIcon" src="/images/compress.png">'+i18n.getMsg('addbs.whatToBackup.dataFlags.compress'),
			                    	},{
			                    		xtype: 'checkbox',
			                    		padding:'0 20 0 0',
			                    		inputValue: 1024,
			                    		id:'encrypt',
			                    		boxLabel: '<img class="gIcon" src="/images/encrypt.png">'+i18n.getMsg('addbs.whatToBackup.dataFlags.encrypt'),
			                    	},{
			                    		xtype: 'checkbox',
			                    		inputValue: 2048,
			                    		id:'dedup',
			                    		boxLabel: '<img class="gIcon" src="/images/dedup.png">'+i18n.getMsg('addbs.whatToBackup.dataFlags.dedupe'),
			                    	}
			                    ]
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
	            		form.getForm().updateRecord(sg);
	            		var cap = 0;
	            		for(var rec in Ext.getCmp('Capabilities').getValue()){
	            			cap += Ext.getCmp('Capabilities').getValue()[rec];
	            		}
	            		sg.set('Capabilities', cap);
		    			sg.save();
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
    	form.loadRecord(sg);
    	var caps = sg.get('Capabilities');
    	if((caps & dataFlags.SCompress) == dataFlags.SCompress)
    		Ext.getCmp('compress').setValue(true);
    	if((caps & dataFlags.SEncrypt) == dataFlags.SEncrypt)
    		Ext.getCmp('encrypt').setValue(true);
    	if((caps & dataFlags.SDedup) == dataFlags.SDedup)
    		Ext.getCmp('dedup').setValue(true);
    	sgConf.show();
	
	}
	
});
});