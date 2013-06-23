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
    
	var i18n = Ext.create('Ext.i18n.Bundle',{
		bundle: 'wui',
		lang: Ext.util.Cookies.get('lang'),
		path: '/i18n',
		noCache: true
	});

i18n.onReady(function(){
   
  
	//propsGrid.store = propertyStore;
	
     var propertyStore = new Ext.data.JsonStore({
        autoLoad: true,  //autoload the data
        
        fields: ['Key','Value'],
      	 proxy: {
	        type: 'ajax',
	        url: '/api/Hub/Configuration/',
	    },
    }); 
	
	var fm = Ext.form;
	

    var grid = new Ext.grid.Panel({
        store:propertyStore,
        renderTo:Ext.get('panel'),
        height: 550,
        autoExpandColumn: 'property', // column with this id will be expanded
       /* title: i18n.getMsg('hubconf.title'),*/
        //frame: true,
        clicksToEdit:2,
        columns:[
        	{
		    	id:'property',
		        header:  i18n.getMsg('hubconf.key'),
		        width:210,
		        flex:0,
		        sortable: true,
		        dataIndex: 'Key',
		        renderer:function(value){
		        	return '<span class="greyV"><b>'+value+'</b></span>';
		        }
		    },{
		    	id:'value',
		        header:  i18n.getMsg('hubconf.value'),
		        width: 300,
		        flex:1,
		        sortable: true,
		        dataIndex: 'Value',
				field:{
	                xtype:'textfield',
	                allowBlank:false
	            },
		    },{
		    	id:'description',
		        header:  i18n.getMsg('hubconf.desc'),
		        width: 300,
		        flex:2,
		        sortable: true,
		        dataIndex: 'description',
		        renderer: function(value, metaData, record, colIndex, store, view){
		        	return i18n.getMsg('hubconf.keys.'+record.get('Key'));
		        }
		    }
        ],
        selType: 'cellmodel',
	    plugins: [
	        Ext.create('Ext.grid.plugin.CellEditing', {
	            clicksToEdit: 2,
	            listeners:{
	            	
	            }
	        })
	    ],
        bbar: [
        	{
        		text:'&nbsp;&nbsp;Save',
               // iconCls:'icon-prev',
                icon:'/images/save.png',
                width:65,
                height:24,
                listeners:{
                     scope:this
                    //,click:{fn:this.addRecord,buffer:200}
               	}
        	},
        	{
        		text:'&nbsp;&nbsp;Apply',
                //iconCls:'icon-prev',
                icon:'/images/apply.png',
                width:65,
                height:24,
                listeners:{
                     scope:this
                    //,click:{fn:this.addRecord,buffer:200}
               	}
        	
        	},
        	{
        		text:'&nbsp;&nbsp;Revert',
                //iconCls:'icon-prev',
                icon:'/images/revert.png',
                width:65,
                height:24,
                listeners:{
                     scope:this
                    //,click:{fn:this.addRecord,buffer:200}
               	}
            }
        ]
       /* tbar: [{
            text: 'Add Plant',
            handler : function(){
                // access the Record constructor through the grid's store
                var Plant = grid.getStore().recordType;
                var p = new Plant({
                    common: 'New Plant 1',
                    light: 'Mostly Shade',
                    price: 0,
                    availDate: (new Date()).clearTime(),
                    indoor: false
                });
                grid.stopEditing();
                store.insert(0, p);
                grid.startEditing(0, 0);
            }
        }]*/
    });
    
     propertyStore.load();
     propertyStore.sort('key');
     
    // simulate updating the grid data via a button click
  /*  new Ext.Button({
        renderTo: 'button-container',
        text: 'Update source',
        handler: function(){
            propsGrid.setSource({
                '(name)': 'Property Grid',
                grouping: false,
                autoFitColumns: true,
                productionQuality: true,
                created: new Date(),
                tested: false,
                version: 0.8,
                borderWidth: 2
            });
        }
    });*/
});
});