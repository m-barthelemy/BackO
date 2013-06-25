//Represents the client groups and nodes tree used in multiple places.
// It's just a classic treePanel with 1 custom added property : shown
// 'shown' is used to customize the view (ie the displayed columns) depending on the context

Ext.define('backo.NodesTree',{
	extend	: 'Ext.tree.Panel',
	requires: ['Ext.i18n.Bundle', 'Ext.ux.form.SearchField'],
	config	:{
		shown:[],
	},
	shown	:[],
	initComponent: function() {
        this.callParent();
        var me = this;
        var shouldHideField = function(fieldName){
	    	for(var i=0; i < me.shown.length; i++)
	    		if(me.shown[i] == fieldName)
	    			return false;
	    	return true; 
	    };
        for(var i=0; i < this.columns.length; i++){
        	this.columns[i]['hidden'] = shouldHideField(this.columns[i]['dataIndex']);
        }
    },
	model		: 'Node',
	layout		: 'fit',
	anchor		: '100%',
	collapsible	: false,
	useArrows	: true,
	rootVisible	: false,
	multiSelect	: true,
	singleExpand: false,
	draggable	: false,    
	stateful	: false,   
	stripeRows	: true,
	viewConfig 	: {
		enableDD 	: true,
	    plugins		: {
	        ptype: 'treeviewdragdrop',
	        containerScroll: true
	    }
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
	    	return '<span data-qtip="#'+record.get('Id')+'<br/>'+record.get('Description')+'">'+value+'</span>';
	    }
	},{
	    text		: i18n.getMsg('nodestree.currentIP'),
	    flex		: 0,
	    width		: 90,
	    dataIndex	: 'IP',
	},{
	    text		: i18n.getMsg('nodestree.hostName'),
	    flex		: 0,
	    width		: 100,
	    dataIndex	: 'HostName',
	},{
	    text		: i18n.getMsg('generic.kind'),
	    flex		: 0,
	    width		: 70,
	    dataIndex	: 'Kind',
	    hidden		: true,
	    renderer	:function(value, metaData, record, colIndex, store, view){
	    	if(record.get('Group') == -1) return '';
	    	return  i18n.getMsg('generic.kind.'+value);
	   	}
	},{
	    text		: i18n.getMsg('nodestree.hypervisor'),
	    flex		: 0,
	    width		: 90,
	    dataIndex	: 'Hypervisor',
	},{
	    text		: i18n.getMsg('generic.description'),
	    flex		: 0,
	    width		: 200,
	    dataIndex	: 'Description',
	},{
	    text		: i18n.getMsg('nodestree.createDate'),
	    flex		: 0,
	    width		: 110,
	    dataIndex	: 'CreationDate',
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
	    dataIndex:'Certificate',
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
	
	dockedItems: [{
	    dock: 'top',
	    xtype: 'toolbar',
	    items: [
	    Ext.create('Ext.ux.form.SearchField', {
	        width: 200,
	        fieldLabel: 'Search',
	        labelWidth: 65,
	        //xtype: 'searchfield',
	        store: this.store
	    }), '->', {
	        xtype: 'component',
	        itemId: 'status',
	        tpl: 'Matching threads: {count}',
	        style: 'margin-right:5px'
	    }]
	}],
});


