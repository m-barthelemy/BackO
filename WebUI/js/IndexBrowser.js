
function browseIndex(pathSep, nodeId, taskId, fs, parentId, callbackFn){
   
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
       
    var idxStore = new Ext.data.TreeStore( {
    	storeId		: 'idx',
     	model		: 'BrowseIdx',
     	autoLoad	: false,
        proxy: {
            type: 'ajax',
            url: '/api/Node/'+nodeId+'/BrowseIndex/'+taskId+'/',
            extraParams: {format:'json', path:fs, filter:'onlynodes'},
            reader: {
                type: 'json',
                root: 'Children',
            }
        },
       root: {text: '/', Id: 0, value:'/', CPath:'', r:'', expanded: true, checked: false},
       nodeParam:'parentId',
       listeners:{
            beforeload: function(store, operation, eOpts){
            	if(operation.node.get("Type") == 'fs'){
		            operation.params.parentId = 0;
		    	}
		    	operation.params.path = operation.node.get("r");
		    },
	    	load:function( thisObj, node, records, successful, eOpts ){
	    		console.debug('browse node #'+nodeId+' : fs store loaded');
	    		var browseRootId = 0;
	    		Ext.each(records, function (record){
	    			record.set('leaf',false);
	    			if(record.get('Type') == 'fs'){ // filesystems/mountpoiunts/drives
	    				// add a 'fs'/root field
	    				record.set('r', record.get('Name'));
	    				record.set('iconCls', 'v');
	    				// define a fake node Id if it's a root (by default multiple index drives will all have an Id=0, that will clash)
	    				browseRootId --;
	    				record.set('Id', browseRootId);
	    			}
	    			else if(record.get('Type') == '0'){//files
	    				record.set('iconCls', 'f');
	    				record.set('leaf', true);
	    			}
	    			else if(record.get('Type') == '2'){//links
	    				record.set('iconCls', 'l');
	    				record.set('leaf', true);
	    			}
	    			if(record.get('Type') != 'fs'){// if node not root, inherit its root ('r' attribute) from parent
	    				record.set('r', node.get('r'));
	    			}
	    			record.set('checked',false);
	    			// root Cpath is '' (empty) so don't join its value, to avoid '//' style paths
	    			// Also, don't join
	    			if(record.get('Name')!= pathSep && record.get('Name')!= '' && record.getDepth() >1)
	    				record.set('CPath', node.get('CPath')+pathSep+record.get('Name'));
	    			if( record.get('CPath').length>1 && record.get('CPath').substring(0,2) == '/\\')
	    				record.set('CPath', record.get('CPath').substring(2));
	    		});
	    		
	    		return false;
	    	}
    	}
    });
    
    var selModel = Ext.create('Ext.selection.CheckboxModel', {
        listeners: {
            selectionchange: function(sm, selections) {
                if(selections.length > 0){
                }
                else{
                	
                }
            }
        }
   });
   
    var filesStore = Ext.create('Ext.data.Store', {
    	storeId:'filesStore',
    	autoLoad:false,
        model: 'BrowseIdx',
        proxy: {
            type: 'ajax',
            url: '/api/Node/'+nodeId+'/BrowseIndex/'+taskId+'/',
            extraParams: {format:'json', path:fs, filter:'onlyleaves'},
            reader:{
	        	type:'json',
	        	applyDefaults: true,
	        	root:'Children'
	        }
        },
       	listeners:{
       		load:function(){
       			selModel.selectAll();
       		}
       	}
    });
    
    var searchStore = Ext.create('Ext.data.Store', {
    	storeId:'searchStore',
    	autoLoad:false,
        model: 'BrowseIdx',
        proxy: {
            type: 'ajax',
            url: '/api/Node/'+nodeId+'/BrowseIndex/'+taskId+'/',
            extraParams: {format:'json', path:fs, filter:'search:'},
            reader:{
	        	type:'json',
	        	applyDefaults: true,
	        	root:'Children'
	        }
        }
    });
    
	
	var searchBar = new Ext.toolbar.Toolbar({
	  store: searchStore,
	  displayInfo: true,
	  dock: 'top',
	  align: 'right',
	  items   :    [
	    {
    		xtype:'clearablebox',
    		fieldLabel:i18n.getMsg('generic.search'),
    		width:280,
    	}
	  ],
	});
	
	var afTree = new Ext.tree.Panel( {
		id:'afTree',
        store: idxStore,
        rootVisible: true,
        useArrows: true,
		hideHeaders: true,
        singleExpand: false,
        overlapHeader:true,
        scroll: 'vertical',
        layout:'fit',
		frame:true,
		height:'90%',
		width:'40%',
		padding:'0 0 15 0',
		border:0,
        columns: [
	         {
	            xtype: 'treecolumn', //this is so we know which column will show the tree
	           	width:'100%',
	            dataIndex: 'Name',
	        }
        ],
        dockedItems: searchBar,
        listeners:{
	        beforeitemexpand:function(thisItem, eOpts){ //function(view, record, item, index, event) {
	           	if(thisItem.get('Type') == 'fs'){
	           		console.debug('asked to browse FS/basepath, changing index root to '+thisItem.get('Name'));
	           		//idxStore.getProxy().setExtraParam('path', thisItem.get('Name'));
	           		/*idxStore.setProxy( {
			            type: 'ajax',
			            url: '/api/Node/'+nodeId+'/BrowseIndex/'+taskId+'/grut',
			            extraParams: {format:'json', path:thisItem.get('Name'), filter:'onlynodes'},
			            reader: {
			                type: 'json',
			                root: 'Children',
			            }
			        });*/
	           	}
	            if(thisItem.get('checked') == true){
	    			thisItem.cascadeBy(function(n) {
					    n.set('checked', thisItem.get('checked'));
					});
	            }
	            return false;
	         },
	         checkchange: function(node, checked, options){
	         	console.log('fs tree checkchange');
	         	console.log('fs tree checkchange : '+node.get('Name')+' is checked:'+checked);
    			
    			node.cascadeBy(function(n) {
				    n.data.checked = checked;
				    n.checked = checked;
				    n.set('checked', checked);
				});
	         },
	         selectionchange:function(thisObj, selected, eOpts){
	         	//idxStore.getProxy().setExtraParam('path', selected[0].get('Name'));
	         	filesStore.load({
	         		params:{
		         		parentId: selected[0].get('Id'),
		         		format:'json', 
		         		path:selected[0].get('r'), 
		         		filter:'onlyleaves'
		         	}
	         	});
	         }
	         /*itemappend: function(thisNode, insertedNode, index, options ){
	         	if(thisNode.get('checked') == true)
		            insertedNode.set('checked', true);
	         }*/
        }    
    });
    
    var filesGrid = new Ext.grid.Panel( {
	    store: filesStore,
	   	height: '90%',
	   	layout: 'fit',
	   	frame: true,
	    width: '60%',
	    scroll: 'vertical',
	    selModel: selModel,
	    padding:'0 0 15 0',
	    border: false,
	    columns: [
	        { 
	        	width: 150, flex:1,
	        	dataIndex: 'Name',
	        	text: i18n.getMsg('generic.name'),
	        	renderer:function(value, metaData, record, colIndex, store, view){
	        		if(record.get('Type') == 'fs'){ // filesystems/mountpoiunts/drives
	    				value = '<img class="i" src="/images/disk.png"/>&nbsp;'+value;
	    				//record.set('iconCls', 'v');
	    			}
	    			else if(record.get('Type') == '0'){//files
	    				//metadata.tdAttr += ' class="f"';
	    				//record.set('iconCls', 'f');
	    				value = '<img class="i" src="/images/f.png"/>&nbsp;'+value;
	    			}
	    			else if(record.get('Type') == '2'){//links
	    				value = '<img class="i" src="/images/l.png"/>&nbsp;'+value;
	    				//metadata.tdClass = 'l';
	    				//record.set('iconCls', 'l');
	    				//record.set('leaf', true);
	    			}
	    			return value;
	        	}
	        },{ 
	        	width: 60, flex:0,
	        	dataIndex: 'Size',
	        	text: i18n.getMsg('generic.size'),
	        	renderer:function(value){
	        			return FormatSize(value);
	        	}
	        },
	    ]
	});
	
    var fsItemsPanel = new Ext.panel.Panel({
    	title:i18n.getMsg('browser.tabs.FS'),
    	layout: 'hbox',
    	width: '90%',
    	height: 550,
    	scroll: false,
    	items:[
    		afTree,
    		filesGrid
    	]
    });
    
	var winIdxBrowse = new Ext.Window({ 
 		id: 'winIdxBrowse',
        width: 750,
       	height: 570,
        title: i18n.getMsg('browser.title'), //"Browse node",
		scroll: false,
        autoDestroy :true,
        resizable: true,
		items: [
			new Ext.tab.Panel({
			    activeTab: 0,
			    items:[
			    	fsItemsPanel
			    	//volumesTree,
			    	//vmTree,
			    	//spoTree
			    ] , 
			})
		],
        buttons: [{
                text:i18n.getMsg('generic.ok'),
                disabled:false,
                handler:function(){
                	var valz = new Array();
                	Ext.each(afTree.getChecked(), function(checkedNode){
			    		var newPath = Ext.create('BrowseIdx', {
			    			Id: checkedNode.get('Id'),
			    			Name: checkedNode.get('Name'),
			    			CPath: checkedNode.get('CPath')/*.substring(1)*/,
			    			IncludePolicy: '*',
			    			Recursive: true,
			    			Type: 'FS:local',
			    		});
			    		valz.push(newPath);
                	});
                	winIdxBrowse.close();
                	callbackFn(valz);
                }
            },{
                text: i18n.getMsg('generic.cancel'),
                handler: function(){
                    winIdxBrowse.close();
                    return null;
                }
            }
        ] 
     });
	winIdxBrowse.show();
});
}
 