
Ext.Loader.setConfig({
    enabled: true
});
Ext.Loader.setPath('Ext.ux', '/ext4/ux');

Ext.require([
 	'Ext.data.proxy.Rest',
    'Ext.data.*',
    'Ext.grid.*',
    'Ext.tree.*',
    'Ext.form.*',
	]);
	
Ext.onReady(function () {
	var params = Ext.urlDecode(window.location.search.substring(1));
	
	i18n = new Ext.i18n.Bundle({
        bundle:'wui', 
        path:'/i18n',
        lang:Ext.util.Cookies.get('lang')
    });
	i18nTask = new Ext.i18n.Bundle({
        bundle:'taskmsg', 
        path:'/i18n',
        lang:Ext.util.Cookies.get('lang')
    });

i18n.onReady(function(){
//i18nTask.onReady(function(){
 Ext.get('globalStatsTitle').dom.innerText = i18n.getMsg('globalStats.title');
 
 
 var pluginsStore = Ext.create('Ext.data.Store', {
    	storeId:'pluginsStore',
    	autoLoad:true,
        model: 'Plugin',
        //fields: ['Key','Value'],
        proxy: {
            type: 'ajax',
            url: '/api/Misc/Plugins/IStorageDiscoverer/',
            extraParams: {format: 'json'},
            reader:{
	        	type:'json',
	        	applyDefaults: true
	        }
        }
    });
 var pluginsgrid = new Ext.grid.Panel({
 	renderTo : Ext.get('panel'),
  	store: pluginsStore,
    height: 250,
    width: 350,
    scroll:'vertical',
    viewConfig:{ markDirty:false },
    columns: [
        { 
        	width: 120, flex:0,
        	text:'Name',
        	dataIndex: 'Name',
        	
        },{ 
        	width: 120, flex:0,
        	text:'Category',
        	dataIndex: 'Category',
        	
        },
    ]
});

var pluginslist = new Ext.form.ComboBox({
		align:'left',
        id: 'StorageLayoutProvider',
        name : 'StorageLayoutProvider',
        renderTo: Ext.get('panel'),
        store: pluginsStore,
        fieldLabel: i18n.getMsg('addbs.storageProxying'),
        labelWidth: 200,
        valueField:'Name',
        displayField:'Name',
        value: 'local',
        typeAhead: true,
        allowBlank:true,
        emptyText:'Proxying...',
        queryMode: 'remote',
        triggerAction: 'all',
        forceSelection:true,
        selectOnFocus:true,
        width:490
    })
 
});

});