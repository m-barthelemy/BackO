function handleBrowse(nodeId, pathSep, callBackFn, allowMultiple){
	var multi = allowMultiple;
    var params = Ext.urlDecode(window.location.search.substring(1));
	
	var i18n = new Ext.i18n.Bundle({bundle:'wui', path:'/i18n', lang:Ext.util.Cookies.get('lang')});

    Ext.define('VolumeModel', {
        extend: 'Ext.data.Model',        
        fields: [
            {name: 'Name', type: 'string', mapping:'@n'},
            //{name: 'text', type: 'string', mapping:'@n'},
          	{name: 'checked', type: 'string'},
          	{name: 'iconCls', type: 'string', mapping:'@type'},
          	{name: 'size', type: 'numeric', mapping:'@size'},
          	{name: 'avail', type: 'numeric', mapping:'@avail'},
          	{name: 'label', type: 'string', mapping:'@label'},
          	{name: 'fs', type: 'string', mapping:'@fs'},
          	{name: 'snapshottable', type: 'string', mapping:'@snap'},
          	{name: 'leaf', type: 'boolean', defaultValue:true},
        ]
    });
    
    Ext.define('SPOModel', {
        extend: 'Ext.data.Model',        
        fields: [
          	{name: 'name', type: 'string', mapping:'name'},
          	{name: 'n', type: 'string', mapping:'name'},
          	{name: 'path', type: 'string', mapping:'path'},
            {name: 'type', type: 'string', mapping:'type'},
          	{name: 'version', type: 'string', mapping:'version'},
          	//{name: 'checked', type: 'string'},
          	{name: 'iconCls', type: 'string', mapping:'type'},
          	{name: 'icon', type: 'string'},
          	{name: 'leaf', type: 'boolean', mapping:'leaf', defaultValue:false},
          	{name: 'child', type: 'boolean', defaultValue:false},
          	{name: 'disabled', type: 'string'},
        ],
        /*associations:[
		{type:'hasMany', model:'SPOModel1',name:'childObject'},
		]*/
    });
    
     Ext.define('VM', {
        extend: 'Ext.data.Model',          
        fields: [
            {name: 'name', type: 'string'},
            {name: 'type', type: 'string'},
          	{name: 'file', type: 'string'},
          	{name: 'leaf', type: 'boolean', defaultValue:true},
          	{name: 'checked', type: 'boolean'},
          	{name: 'iconCls', type: 'string', mapping:'type', defaultValue:"computer"}
        ],
    });
    
 i18n.onReady(function(){    
       
    var brwStore = new Ext.data.TreeStore( {
    	storeId:'brw',
     	model:'BrowseNode',
     	autoLoad:false,
        proxy: {
            type: 'ajax',
            url: '/api/Node/'+nodeId+'/Browse',
            extraParams: {format:'json'},
            reader: {
                type: 'json',
                root: 'Children',
            }
        },
       root: {text: '/', id: '/', value:'/', CPath:'/', expanded: true},
       //folderSort: true,
       nodeParam:'path',
       listeners:{
	    	load:function( thisObj, node, records, successful, eOpts ){
	    		console.debug('browse node #'+nodeId+' : fs store loaded');
	    		Ext.each(records, function (record){
	    			if(record.get('Type') == 'fs')
	    				record.set('iconCls', 'v');
	    			record.data.leaf = false;
	    			record.leaf = false;
	    			record.set('leaf',false);
	    			record.set('checked',false);
	    			record.data.checked = false;
	    			record.set('CPath', node.get('CPath')+pathSep+record.get('Name'));
	    			if( record.get('CPath').length>1 && record.get('CPath').substring(0,2) == '/\\')
	    				record.set('CPath', record.get('CPath').substring(2));
	    		});
	    		
	    		return false;
	    	}
    	}
    });
    
    var volStore = new Ext.data.TreeStore( {
     	model:'VolumeModel',
     	clearOnLoad:false,
     	autoLoad:false,
        proxy: {
            type: 'ajax',
            url: '/api/Node/'+nodeId+'/Drives',
            extraParams: {isXml: true},
            reader: {
                type: 'xml',
                root: 'root',
                record: 'd',
            }
        },
       root: {text: '/', id: '//', value:'/', expanded: true},
       listeners:{
	    	load:function( thisObj, node, records, successful, eOpts ){
	    		Ext.each(records, function (record){
	    			record.data.checked = false;
	    		});
	    		console.log('volumes store loaded');
	    	}
    	}
    });
    
    var spoStore = new Ext.data.TreeStore( {
     	model:'SPOModel',
     	clearOnLoad:false,
     	autoLoad:false,
        proxy: {
            type: 'ajax',
            url: '/api/Node/'+nodeId+'/SpecialObjects',
            timeout:90000,
            doRequest: function(operation, callback, scope) {
                var writer  = this.getWriter(),
                    request = this.buildRequest(operation, callback, scope);

                if (operation.allowWrite()) {
                    request = writer.write(request);
                }
				// raise default 30s timeout, since VSS writers can take some time to enumerate
                Ext.apply(request, {
                    headers       : this.headers,
                    timeout       : 90000,
                    scope         : this,
                    callback      : this.createRequestCallback(request, operation, callback, scope),
                    method        : this.getMethod(request),
                    disableCaching: false // explicitly set it to false, ServerProxy handles caching
                });

                Ext.Ajax.request(request);
                return request;
            }
        },
       root: {
            text: '/',
            id: '//',
            value:'/',
            name:'VSS Writers',
            expanded: false,
            checked:false
        }
    });
    
    var vmStore = new Ext.data.TreeStore( {
     	model:'VM',
     	clearOnLoad:false,
     	autoLoad:false,
        proxy: {
            type: 'ajax',
            url: '/Get.aspx?w=VM&nodeId='+nodeId
        },
        root: {
            text: '/',
            id: '//',
            value:'/',
            name:'Virtual machines',
            expanded: true
        }
    });
    
	var spoTree = new Ext.tree.Panel({
		id:'spoTree',
		title:i18n.getMsg('browser.tabs.Objects'),
		store:spoStore,
		hideHeaders: false,
        rootVisible: true,
        useArrows: true,
		collapsible: false,
        singleExpand: false,
        overlapHeader:true,
        autoScroll:false,
        selModel: {mode: 'SINGLE'},
        layout:'fit',
		frame:true,
		height:430,
		padding:0,
		bodyPadding:'0px 10px 0px 0px',
		border:0,
		columns: [
	         {
	            xtype: 'treecolumn', 
	            text: i18n.getMsg('browser.path'), //'path',
	           	flex: 2,
	            sortable: true,
	            dataIndex: 'path',
	            checked:false,
	            renderer: function(value, metaData, record, colIndex, store, view){
	            	if(record.get('icon') != ""){
	            		if(record.get('disabled') == "True")
		            		return "<strike>"+value+"</strike>";
	            	}
		            else if(record.get('disabled') == "True"){
		            	return "<strike>"+value+"</strike>";
		            	//metadata['disabled'] = true;
		            	metadata['iconCls'] = "spoDisabled";
		            	metadata['cls'] = "disabled";
		            }
		            else{
		            	if(record.get('path') == record.get('name'))
		            		return value;
		            	else
		            		return record.get('path')+'\\'+record.get('name');
		            }
	            }
	        },{
	            text: i18n.getMsg('nodestree.version'), //'size',
	           	flex: 0,
	            sortable: true,
	            dataIndex: 'version',
	            width:70
	        }
	    ],
	    listeners:{
	    	checkchange: function(node, checked, options){
	       		var firstLevelChild = node.childNodes;
	       		Ext.each(firstLevelChild, function(child, index){
	       			if(child != null){
		       			child.set('checked', checked);
		       		}
    			});
	         }
        }    
	    	
	});
	
	var vmTree = new Ext.tree.Panel({
		id:'vmTree',
		title:i18n.getMsg('browser.tabs.Virtual'),
		store:vmStore,
		hideHeaders: false,
        rootVisible: true,
        useArrows: true,
		collapsible: false,
        multiSelect: allowMultiple,
        //selModel: {mode: 'SINGLE'},
        singleExpand: false,
        overlapHeader:true,
        autoScroll:false,
        layout:'fit',
		frame:true,
		height:430,
		padding:0,
		bodyPadding:'0px 10px 0px 0px',
		border:0,
		columns: [
	         {
	            xtype: 'treecolumn', 
	            text: i18n.getMsg('browser.path'), //'path',
	           	flex: 0,
	           	width:230,
	            sortable: true,
	            dataIndex: 'name',
	            checked:false,
	            renderer: function(value, metaData, record, colIndex, store, view){
		            if(record.get('type').length <1){
		            	metaData['iconCls'] = "computer";
		            	return "<strong>"+value+"</strong>";
		            }
		            else if(record.get('type') == "cdrom"){
		            	metaData['iconCls'] = "cdrom";
		            	return value;
		            }
		            else
		            	return value;
	            }
	        },{
	            text: i18n.getMsg('browser.label'), //'size',
	           	flex: 1,
	            sortable: true,
	            dataIndex: 'file',
	            width:250,
	        },
	    ],
	    listeners:{
	    	checkchange: function(node, checked, options){
	    		//if(multi){
		       		var firstLevelChild = node.childNodes;
		       		Ext.each(firstLevelChild, function(child, index){
		       			if(child != null){
			       			child.set('checked', checked);
			       		}
	    			});
	    		//}
	    		//else{
		    		/*if(checked){// when not in 'multi" mode, uncheck other nodes to prevent multiple checking
		    			Ext.each(afTree.getChecked(), function(checkedNode){
		    				checkedNode.set('checked', false);
		    			});
		    		}*/
	    		//}
	         }
        }    
	    	
	});
	
	var volumesTree = new Ext.tree.Panel({
		id:'volumesTree',
		title:i18n.getMsg('browser.tabs.Volumes'),
		store:volStore,
		hideHeaders: false,
        rootVisible: false,
        useArrows: true,
		collapsible: false,
        singleExpand: false,
        overlapHeader:true,
        autoScroll:false,
        layout:'fit',
		frame:true,
		height:360,
		padding:0,
		bodyPadding:'0px 10px 0px 0px',
		border:0,
		columns: [
	         {
	            xtype: 'treecolumn', //this is so we know which column will show the tree
	            text: i18n.getMsg('browser.path'), //'path',
	           	flex: 2,
	            sortable: true,
	            dataIndex: 'n',
	        },{
	            text: i18n.getMsg('browser.label'), //'label',
	           	flex: 2,
	            sortable: true,
	            dataIndex: 'label',
	        },{
	            text: i18n.getMsg('browser.fs'), //'fs',
	           	flex: 0,
	           	width:40,
	            sortable: true,
	            dataIndex: 'fs',
	        },{
	            text: i18n.getMsg('browser.snapshot'), //'size',
	           	flex: 0,
	            sortable: true,
	            dataIndex: 'snapshottable',
	            width:60,
	            renderer: function(value){
	            	if(value == 'NONE') // no snapshot capability
	            		return '<i>'+i18n.getMsg('generic.no')+'</i>';
	            	else
	            		return value;
	            }
	        },{
	            text: i18n.getMsg('generic.size'), //'size',
	           	flex: 0,
	            sortable: true,
	            dataIndex: 'size',
	            width:60,
	            renderer: function(value){
	            	if (value == null)
	            		return "";
	            	if (value > 1024*1024*1024) {
			            return Math.round(value/1024/1024/1024)+" GB";
			        }
			        else if (value > 1024*1024) {
			            return Math.round(value/1024/1024)+" MB";
			        }
			        else if (value > 1024) {
			            return Math.round(value/1024)+" KB";
			        }
			        else 
			        	return value + '';
			    }
	        }
	    ]
	});
	
	var afTree = new Ext.tree.Panel( {
		id:'afTree',
		title:i18n.getMsg('browser.tabs.FS'),
        store: brwStore,
        hideHeaders: false,
        rootVisible: true,
        useArrows: true,
		collapsible: false,
        singleExpand: false,
        overlapHeader:true,
        autoScroll:false,
        layout:'fit',
		frame:true,
		height:430,
		padding:0,
		bodyPadding:'0px 10px 0px 0px',
		border:0,
        columns: [
	         {
	            xtype: 'treecolumn', //this is so we know which column will show the tree
	            text: i18n.getMsg('browser.path'), //'path',
	           	flex: 2,
	            sortable: true,
	            dataIndex: 'Name',
	        },{
	            text: i18n.getMsg('generic.size'), //'size',
	           	flex: 0,
	            sortable: true,
	            dataIndex: 'Size',
	            width:60,
	            renderer: function(value){
	            	if (value == 0)
	            		return "";
	            	if (value > 1024*1024*1024) {
			            return Math.round(value/1024/1024/1024)+" GB";
			        }
			        else if (value > 1024*1024) {
			            return Math.round(value/1024/1024)+" MB";
			        }
			        else if (value > 1024) {
			            return Math.round(value/1024)+" KB";
			        }
			        else 
			        	return value + '';
			    },
	        },{
	            text: i18n.getMsg('browser.available'), //'available',
	           	flex: 0,
	            sortable: true,
	            dataIndex: 'Avail',
	            width:60,
	            renderer: function(value){
	            	if (value == 0)
	            		return "";
	            	if (value > 1024*1024*1024) {
			            return Math.round(value/1024/1024/1024)+" GB";
			        }
			        else if (value > 1024*1024) {
			            return Math.round(value/1024/1024)+" MB";
			        }
			        else if (value > 1024) {
			            return Math.round(value/1024)+" KB";
			        }
			        else 
			        	return value + '';
			    },
	        },{
	            text: i18n.getMsg('browser.fs'), //'fs',
	           	flex: 0,
	           	width:40,
	            sortable: true,
	            dataIndex: 'FS',
	        },{
	        	//xtype:'checkbox',
	            text: i18n.getMsg('browser.snapshot'), //'size',
	           	flex: 0,
	            sortable: true,
	            dataIndex: 'Snap',
	            width:60,
	            renderer: function(value){
	            	if(value == 'NONE') // no snapshot capability
	            		return '<i>'+i18n.getMsg('generic.no')+'</i>';
	            	else
	            		return value;
	            }
	        },
	        /*{
	            text: i18n.getMsg('browser.label'), //'label',
	           	flex: 2,
	            sortable: true,
	            dataIndex: 'label',
	            checked:false
	        },*/
        
        ],
        listeners:{
	        beforeitemexpand:function(thisItem, eOpts){ //function(view, record, item, index, event) {
	           	
	            if(thisItem.get('checked') == true){
		           /* Ext.each(thisItem, function(child, index){
		       			if(child != null){
			       			child.data.checked = true;
			       			//child.set('checked', true);
			       		}
	    			});*/
	    			thisItem.cascadeBy(function(n) {
					    /*var ui = n.getUI();*/
					    n.data.checked = thisItem.get('checked');
					    n.checked = thisItem.get('checked');
					    n.set('checked', thisItem.get('checked'));
					});
	            }
	            
	         },
	         checkchange: function(node, checked, options){
	         	console.log('fs tree checkchange');
	         	console.log('fs tree checkchange : '+node.get('Name')+' is checked:'+checked);
	         	/*var hasTocheck = false;
	         	if(checked)
	         		hasTocheck = true;
	       		var firstLevelChild = node.childNodes;
	       		Ext.each(firstLevelChild, function(child, index){
	       			if(child != null){
		       			child.set('checked', hasTocheck);
		       		}
    			});
    			node.set('checked', checked);*/
    			
    			if(multi){
	    			node.cascadeBy(function(n) {
					    n.data.checked = checked;
					    n.checked = checked;
					    n.set('checked', checked);
					});
				}
				else{
					if(checked){// when not in 'multi" mode, uncheck other nodes to prevent multiple checking
		    			Ext.each(afTree.getChecked(), function(checkedNode){
		    				checkedNode.set('checked', false);
		    			});
		    			node.set('checked', true);
		    		}
		    	}
	         
	         },
	         /*itemappend: function(thisNode, insertedNode, index, options ){
	         	if(thisNode.get('checked') == true)
		            insertedNode.set('checked', true);
	         }*/
        }    
    });
    
	var winBrowse = new Ext.Window({ 
 		id:'winBrowse',
        width:650,
       	height:520,
        title:i18n.getMsg('browser.title'), //"Browse node",
		autoScroll:true,
        autoDestroy :true,
        resizable:true,
        BodyStyle:'overflow-y:auto !important; overflow-x:auto !important;',
		items: [
			new Ext.tab.Panel({
			    activeTab: 0,
			    items:[
			    	afTree,
			    	volumesTree,
			    	//vmTree,
			    	spoTree
			    ] , 
			})
		],
        buttons: [{
                text:i18n.getMsg('generic.ok'),
                disabled:false,
                handler:function(){
                	var valz = new Array();
                	Ext.each(afTree.getChecked(), function(checkedNode){
			    		var newPath = Ext.create('BasePath', {
			    			Path: checkedNode.get('CPath')/*.substring(1)*/,
			    			IncludePolicy: '*',
			    			Recursive: true,
			    			Type: 'FS:local',
			    		});
			    		valz.push(newPath);
                	});
                	Ext.each(spoTree.getChecked(), function(checkedNode){
                		var depth = checkedNode.getDepth();
	                	var fullPath = [];
	                	var value = "";
	                	if(checkedNode.get('path').length > 1)
			    			value = checkedNode.get('path');
			    		//else
			    		fullPath.push(value);
			    		//value = checkedNode.get('name');
		    			//fullPath.push(value);
			    		for(var i=0; i< depth-1; i++){ // depth -1 because we don't want root node to get VSS path
			    			checkedNode = checkedNode.parentNode;
			    			if(checkedNode.get('path').length > 1)
				    			value = checkedNode.get('path');
				    		else
				    			value += "\\"+checkedNode.get('name');
			    			fullPath.unshift(value);
			    		}
			    		var newPath = Ext.create('BasePath', {
			    			Path:fullPath.join('\\'),
			    			IncludeRule: '*',
			    		});
	                	//valz.push(fullPath.join('\\'));
	                	valz.push(newPath);
	                	
		               // }
                		/*if (checkedNode.get('name').length > 1)
			    			valz.push(checkedNode.get('name'));
			    		else*/
			    			
                	});
                	Ext.each(volumesTree.getChecked(), function(checkedNode){
                	
                		var fullPath = [];
	                	var value = "";
	                	if(checkedNode.get('Name').length > 1){
			    			value = checkedNode.get('Name');
			    			fullPath.push(value);
				    		for(var i=0; i< checkedNode.getDepth(); i++){
				    			checkedNode = checkedNode.parentNode;
				    			fullPath.unshift(checkedNode.get('Name'));
				    		}
		                	//valz.push(fullPath.join('/'));
		                	var newPath = Ext.create('BasePath', {
				    			Path:fullPath.join('/'),
				    			
				    		});
				    		valz.push(newPath);
		                }
                	});
                	winBrowse.close();
                	callBackFn(valz);
                }
            },{
                text: i18n.getMsg('generic.cancel'),
                handler: function(){
                    winBrowse.close();
                    return null;
                }
            }
        ] 
     });
	winBrowse.show();
});
}
 