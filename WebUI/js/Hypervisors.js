Ext.require([
    'Ext.data.*',
    'Ext.grid.*',
    'Ext.tree.*',
    'Ext.form.*',
    'Ext.window.*',
    'backo.PasswordEditor'
	]);
	
Ext.onReady(function(){
	Ext.Loader.setConfig({
        enabled: true,
        disableCaching: false
    });
	
    
	var i18n = Ext.create('Ext.i18n.Bundle',{
		bundle: 'wui',
		lang: Ext.util.Cookies.get('lang'),
		path: '/i18n',
		noCache: true
	});

i18n.onReady(function(){

	var hvStore = new Ext.data.JsonStore( {
    	autoLoad:true,
        model: 'Hypervisor',
        proxy: {
            type: 'rest',
            url: '/api/Hypervisors/',
            extraParams: {format: 'json'},
            noCache:false
        },
        reader:{
        	type:'json',
        	applyDefaults: true
        }
    });
    
	var cellEditing =  Ext.create('Ext.grid.plugin.CellEditing', { clicksToEdit: 1});
	var hvGrid = Ext.create('Ext.grid.Panel', { //new Ext.grid.Panel( {
	    id:'hvGrid',
	    renderTo:Ext.get('panel'),
	    store: hvStore,
	    height: 600,
	    plugins: [cellEditing],
	    columns: [
	    	{
	        	header: i18n.getMsg('generic.id'),   
	        	dataIndex: 'Id', 
	        	width:50
	        },{
	        	header: i18n.getMsg('generic.name'),   
	        	dataIndex: 'Name', 
	            editor:{
	                xtype:'textfield',
	                allowBlank:false
	            },
	            width:150,
	            
	        },{
	        	header: i18n.getMsg('hv.kind'), 
	        	dataIndex: 'Kind', 
	            editor:{
	                xtype:'textfield',
	                allowBlank:false
	            },
	            width:70,
	            
	        },{
				header: i18n.getMsg('hv.url'),
				dataIndex: 'Url',
				width: 250,
				flex:1,
				editor:{
	                xtype:'textfield',
	                allowBlank:false
	            },
			},{
				header: i18n.getMsg('generic.login'),
				dataIndex: 'UserName',
				width: 100,
				editor:{
	                xtype:'textfield',
	                allowBlank:false
	            },
			},{
				header: i18n.getMsg('generic.password'),
				dataIndex: 'PasswordId',
				width: 170,
				editor: {
					xtype: 'passwordeditor',
				},
				renderer: function() {
					return '******'
				}
			},{
				header: i18n.getMsg('hv.lastDiscover'),
				dataIndex: 'LastDiscover',
				width: 180,
				renderer: function(value){
					return value.toLocaleString();
				}
			}
		],
		//selType: 'cellmodel',
		listeners:{
			selectionchange: function(thisObj, selected, eOpts ){
				if(selected.length >0){
					Ext.getCmp('startDiscover').enable();
					Ext.getCmp('delete').enable();
				}
				else{
					Ext.getCmp('startDiscover').disable();
					Ext.getCmp('delete').disable();
				}
			}
		},
	    plugins: [
	        Ext.create('Ext.grid.plugin.CellEditing', {
	            clicksToEdit: 2,
	            listeners:{
	            	afteredit:function(editor, e){
	            		hvStore.sync();
	            		hvGrid.getView().refresh();
	            		return false;
	            	}
	            },
	        })
	    ],
	    bbar: [
	    	{
	            text: i18n.getMsg('generic.add'), //'Add',
	            icon:'/images/adduser.png',
	            handler : function(){
	                var r = Ext.ModelManager.create({
	                    Name: 'New hypervisor',
	                    Url: '',
	                    Kind: '',
	                    
	                }, 'Hypervisor');
	                hvStore.insert(0, r);
	                cellEditing.startEditByPosition({row: 0, column: 1});
	            }
	    	},{
	    		id:'delete',
	            text: i18n.getMsg('generic.delete'), //'Start VM discovery',
	            icon:'/images/delete.png',
	            disabled: true,
	            handler : function(){
					hvGrid.getStore().remove(hvGrid.getView().getSelectionModel().getSelection()[0]);
					hvGrid.getStore().sync();
	            }
	    	},{
	    		id:'startDiscover',
	            text: i18n.getMsg('hv.startDiscovery'), //'Start VM discovery',
	            icon:'/images/start.png',
	            disabled: true,
	            handler : function(){
					 Ext.Ajax.request({
					    url: '/api/Hypervisors/StartDiscovery/'+hvGrid.getView().getSelectionModel().getSelection()[0].get('Id'),
					    method: 'GET',
					    success: function() {
					        console.log('success');
					    },
					    failure: function(response, opts) {
					    	var  robj = Ext.decode(response.responseText);
					        alert('Unable to start discovery for hypervisor "'
					        	+hvGrid.getView().getSelectionModel().getSelection()[0].get('Name')
					        	+'" : '+robj.ResponseStatus.Message);
					    }
					});
	            }
	    	},
	    	
	    	
	    ]
	
	});

});

});
