Ext.require([
    'Ext.data.*',
    'Ext.grid.*',
    'Ext.tree.*',
    'Ext.form.*',
    'Ext.ux.*',
    'backo.PasswordEditor'
]);


Ext.onReady(function () {
Ext.Loader.setConfig({enabled:true, disableCaching: false});
	
	var i18n = Ext.create('Ext.i18n.Bundle',{
		bundle: 'wui',
		lang: Ext.util.Cookies.get('lang'),
		path: '/i18n',
		noCache: true
	});
	
i18n.onReady(function(){

	var colorsList = null;
	Ext.get('usersTitle').dom.innerText = i18n.getMsg('users.title');
	
    var usersStore = new Ext.data.JsonStore( {
    	autoLoad:true,
        model: 'User',
        proxy: {
            type: 'rest',
            url: '/api/Users',
            extraParams: {
		        format	: 'json'
		    }
        }
    });

	Ext.define('Cultures', {
        extend: 'Ext.data.Model',
        fields: ['Key', 'Value']
    });

    var cultureStore = Ext.create('Ext.data.Store', {
        model: 'Cultures',
        proxy: {
            type: 'ajax',
            url: '/api/Misc/Cultures',
            extraParams: {
		        format	: 'json'
		    }
        },
    });
    
    cultureStore.load();
    cultureStore.sort([
    	{property: 'Key',  direction: 'ASC'},
	]);

	
    var usersGrid = Ext.create('Ext.grid.Panel', { //new Ext.grid.Panel( {
    id:'usersGrid',
    store: usersStore,
  
    columns: [
    	{
        	header: i18n.getMsg('generic.login'),
        	dataIndex: 'Login', 
            editor:{
                xtype:'textfield',
                allowBlank:false
            },
            width:100,
            
        },{
        	header: i18n.getMsg('generic.name'), //'Name',  
        	dataIndex: 'Name', 
            editor:{
                xtype:'textfield',
                allowBlank:false
            },
            width:160,
            renderer:function(value){
            	return '<span class="u-admin">'+value+'</span>';
            }
        },{
			header: i18n.getMsg('generic.password'),
			dataIndex: 'PasswordId',
			width: 160,
			flex:1,
			editor: {
				xtype: 'passwordeditor',
			},
			renderer: function() {
				return '******'
			}
		},{
        	header: i18n.getMsg('users.grid.email'), //'Email', 
        	dataIndex: 'Email', 
        	flex:0, 
            editor:{
                xtype:'textfield',
                allowBlank:false
            },
            width:210,
        }, {
        	header: i18n.getMsg('users.grid.language'), //'Language', 
        	dataIndex: 'Culture', 
        	flex:0, 
            editor:{
                xtype:'combo',
                allowBlank:false,
                store: cultureStore,
                valueField:'Key',
                displayField:'Value',
                queryMode:'remote',
                typeAhead: true,
                //typeAheadDelay: 150,
                lazyRender: false,
            },
            width:145,
            renderer: function(value, metaData, record, colIndex, store, view){
            	return "<img src='/i18n/flags/"+value.substring(3).toLowerCase()+".png'/>&nbsp;"+value;
            	//return "<img src='/i18n/flags/"+value.substring(3).toLowerCase()+".png'/>&nbsp;"+cultureStore.findRecord('Key',value).get('Value');
            },
            
        },{
        	header: i18n.getMsg('users.grid.enabled'), //'Active', 
        	dataIndex: 'IsEnabled', 
        	xtype:'checkcolumn',
        	flex:0, 
            /*editor:{
                xtype:'checkbox',
            },*/
            width:55
        },{
        	//xtype:'datecolumn',
        	header: i18n.getMsg('users.grid.lastLogin'), //'Name',  
        	dataIndex: 'LastLoginDate',
        	width:210,
        	renderer:function(value){
        		if(value == null)
        			return "<i>"+i18n.getMsg('generic.never')+"</i>";
        		else
        			return value.toLocaleString();
        	}
        }/*,
        {
        	header: i18n.getMsg('users.grid.roles'),
        	dataIndex: '', 
        	flex:1, 
       	}*/
    ],
    listeners:{
		selectionchange: function(thisObj, selected, eOpts ){
			if(selected.length >0){
				Ext.getCmp('delete').enable();
			}
			else{
				Ext.getCmp('delete').disable();
			}
		}
	},
    //selType: 'cellmodel',
    plugins: [
        Ext.create('Ext.grid.plugin.CellEditing', {
            clicksToEdit:2,
            listeners:{
            	afteredit:function(editor, e){
            		usersStore.sync();
            		usersGrid.getView().refresh();
            		return false;
            	}
            },
        })
    ],
    bbar: [
    	{
            text: i18n.getMsg('generic.add'),
            icon:'/images/adduser.png',
            handler : function(){
                var r = Ext.ModelManager.create({
                    name: 'New user',
                    culture: 'en-US',
                    
                }, 'User');
                usersStore.insert(0, r);
                cellEditing.startEditByPosition({row: 0, column: 0});
            }
        },{
        	id:'delete',
            text: i18n.getMsg('generic.delete'),
            disabled:true,
            icon:'/images/delete.png',
            handler : function(){
                usersStore.remove(usersGrid.getView().getSelectionModel().getSelection());
				usersStore.sync();
            }
        }
    ],
    height: '100%',
    //layout:'fit',
    renderTo: Ext.get('gPanel')
});

function afterEdit(e){
            		alert(value);
            		e.record.set('language', value);
            		e.record.commit();
            		e.grid.update();
            	}

    
});

});