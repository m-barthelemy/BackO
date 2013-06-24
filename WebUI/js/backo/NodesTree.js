//Represents the client groups and nodes tree used in multiple places.
// It's just a classic treePanel with 1 custom added property : displayedColumns
// displayedColumns is used to customize the view (ie the displayed columns) depending on the context

Ext.define('backo.NodesTree',{
	extend: 'Ext.tree.Panel',
	requires: ['Ext.i18n.Bundle'],
	initComponent: function() {
        this.callParent();
        this.displayedColumns.IP=true;
		/*	'IP':true,
			'Name': true,
			'Status':true
		};*/
    },
	bundle:{
		bundle: 'wui',
		lang: Ext.util.Cookies.get('lang'),
		path: '/i18n',
		noCache: false
	},
	
	model: 'Node',
	layout:'fit',
	anchor:'100%',
	collapsible: false,
	useArrows: true,
	rootVisible: false,
	multiSelect: true,
	singleExpand: false,
	draggable:true,    
	stateful:false,   
	stripeRows:true,
	// Grouping in tree is buggy (extjs 4.2) since it prevents collapsing group 
	//after opening it, and displays hidden field mixed with regular ones (!?)
	//plugins:[groupingFeature],
	//features: [groupingFeature],
	//features: [{ ftype: 'grouping' }],
	viewConfig : {
		enableDD : true,
	    plugins: {
	        ptype: 'treeviewdragdrop',
	        containerScroll: true
	    },
	   /* itemmove:function(thisObj, oldParent, newParent, idx, eOpts){
	  		console.debug('changed node group!');
	  		if(newParent.get('Group') == -1){
	  			thisObj.set('Group', newParent.get('Id'));
	  			thisObj.save();	
	  		}
	  		//nStore.sync();
	  	},*/
	},
	columns: [{
	    xtype		: 'treecolumn', //this is so we know which column will show the tree
	    header		: i18n.getMsg('nodestree.node'),
	    width		: 200,
	    groupable	: false,
	    dataIndex	: 'Name',
	    renderer	: function(value, metaData, record, colIndex, store, view){
	    	value = (record.get('Name') == '') ? record.get('HostName') : record.get('Name');
	    	value = (value == '') ? record.get('IP') : value;
	    	if(record.get('Group') == -1)
	    		value = '<b>'+value+'</b>';
	    	//value = '<img src="/images/computer.png" style="vertical-align:middle;">'+value;
	    	return '<span data-qtip="#'+record.get('Id')+'">'+value+'</span>';
	    }
	},{
	    text		: i18n.getMsg('nodestree.currentIP'),
	    flex		: 0,
	    width		: 90,
	    dataIndex	: 'IP',
	    hidden		: this.displayedColumns.IP
	},{
	    text		: i18n.getMsg('nodestree.hostName'),
	    flex		: 0,
	    width		: 100,
	    dataIndex	: 'HostName',
	    hidden		: true,
	},{
	    text		: i18n.getMsg('generic.kind'),
	    flex		: 0,
	    width		: 80,
	    dataIndex	: 'Kind',
	    hidden		: true,
	    renderer	:function(value, metaData, record, colIndex, store, view){
	    	if(record.get('Group') == -1) return '';
	    	return  i18n.getMsg('nodestree.kind.'+value);
	   	}
	},{
	    text		: i18n.getMsg('nodestree.hypervisor'),
	    flex		: 0,
	    width		: 90,
	    dataIndex	: 'Hypervisor',
	    hidden		: true,
	},{
	    text		: i18n.getMsg('generic.description'),
	    flex		: 0,
	    width		: 200,
	    dataIndex	: 'Description',
	    hidden		: true,
	},{
	    text		: i18n.getMsg('nodestree.createDate'),
	    flex		: 0,
	    width		: 110,
	    dataIndex	: 'CreationDate',
	    hidden		: true,
	    renderer	: function(value, metaData, record, colIndex, store, view){
	    	if(record.get('Group') == -1) return '';
	    	return record.get('CreationDate').toLocaleString();
	   	}
	},{
	    text		: i18n.getMsg('nodestree.version'),
	    flex		: 0,
	    width		: 65,
	    dataIndex	: 'Version',
	},{
	    text		: i18n.getMsg('nodestree.os'),
	    flex		: 0,
	    dataIndex	: 'OS',
	    width		: 35,
	    renderer	: function(value, metaData, record, colIndex, store, view){
	    	if(record.get('Group') == -1) return '';
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
	    	else/* if(value.length > 1)*/
	    		return '<img src="/images/Unknown-xs.png" title="Unknown os : '+value+'"/>';
	    }
	},{
	    text:  i18n.getMsg('nodestree.quota'),
	    flex: 0,
	    dataIndex: 'Quota',
	    width:65,
	    renderer: function(value, metaData, record, colIndex, store, view){
	    	if(record.get('Group') == -1) return '';
	    	return FormatSize(value)+' ('+i18n.getMsg('nodestree.usedQuota')+' : '+FormatSize(record.get('UsedQuota')+')');
	    }
	},{
	    text:  i18n.getMsg('nodestree.usedQuota'),
	    flex: 0,
	    width:70,
	    dataIndex: 'UsedQuota',
	    hidden: true,
	    renderer:function(value){ return FormatSize(value);}
	},{
	    text:  i18n.getMsg('nodestree.certificate'),
	    flex: 0,
	    width:30,
	    renderer:function(value, metaData, record, colIndex, store, view){
	    	if(record.get('Group') == -1) return '';
	    	if(record.get('Locked') == true)
	    		return '<img src="/images/locked.png" height="20"/>';
	    	else
	    		return '<img src="/images/security-high.png"/>';
	    	// <TODO> warning/error status when sthg is wrong with cert policy
	    	/*if(value == "sec-OK")
	    		return '<img src="/images/security-high.png"/>';
	    	else if(value == "sec-ERROR")
	    		return '<img src="/images/security-low.png"/>';
	    	else if(value == "sec-WARNING")
	    		return '<img src="/images/security-medium.png"/>';
	    	else
	    		return "";*/
	    }
	},{
		text:  i18n.getMsg('nodestree.status'),
	    flex: 0,
	    width:90,
	    dataIndex: 'Status',
	    hidden:true,
	    renderer:function(value, metaData, record, colIndex, store, view){
	    	if(record.get('Group') == -1) return '';
	    	return i18n.getMsg('nodestree.status.'+value);
	    },
	},{
		text:  i18n.getMsg('nodestree.lastconnection'),
	    flex: 1,
	    width:100,
	    dataIndex: 'LastConnection',
	    renderer:function(value, metaData, record, colIndex, store, view){
	    	if(record.get('Group') == -1) return '';
	    	return record.get('LastConnection').toLocaleString();
	   	}
	},{
	    text:  i18n.getMsg('nodestree.delegations'),
	    flex: 1,
	    //dataIndex: '',
	    
	}
	],
});


