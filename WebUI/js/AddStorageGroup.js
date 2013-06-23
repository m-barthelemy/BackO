Ext.require([
    'Ext.data.*',
    'Ext.grid.*',
    'Ext.tree.*',
    'Ext.form.*',
    'Ext.window.*',
]);
Ext.onReady(function(){
	Ext.Loader.setConfig({
        enabled: true,
        disableCaching: false
    });
    var params = Ext.urlDecode(window.location.search.substring(1));
	
	var i18n = Ext.create('Ext.i18n.Bundle',{
		bundle: 'wui',
		lang: Ext.util.Cookies.get('lang'),
		path: '/i18n',
		noCache: true
	});

i18n.onReady(function(){

	var form = Ext.create('Ext.form.Panel',{
		id:'addSg',
		renderTo: Ext.get('panel'),
        model: 'StorageGroup',
        url : '/api/StorageGroups/',
        monitorValid:true,
        border: true,
        bodyPadding: 10,
		height:500,
		
        items: [
        	{
				xtype:'textfield',
                fieldLabel:'Name',
                //id: 'Name',
                name: 'Name',
                width:350,
                allowBlank:false,
                blankText:'A value is required.'
			},
			{
				xtype:'textfield',
                fieldLabel:'Description',
                //id: 'Description',
                name: 'Description',
                width:350,
                allowBlank:false,
                blankText:'A value is required.'
			},
			{
				xtype:'button',
                text: i18n.getMsg('nodeconf.apply'), //'Apply',
                formBind:true,
                handler: function() {
                	var record = Ext.create('StorageGroup',{
                		
                	});
                	form.getForm().updateRecord(record);
                	record.save();
                	form.getForm().reset();
                }
			}
        ]
	
	
	});

});
});
