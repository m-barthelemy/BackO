Ext.require([
 	'Ext.data.proxy.Rest',
    'Ext.data.*',
    'Ext.grid.*',
    'Ext.tree.*',
    'Ext.form.*',
    'Ext.window.*',
	]);
	
Ext.onReady(function () {
	Ext.Loader.setConfig({
        enabled: true,
        disableCaching: false,
        paths: {
            'Ext.ux':'/js/ext4/ux'
        }
    });
	
	/*var i18n = Ext.create('Ext.i18n.Bundle',{
		bundle: 'wui',
		lang: Ext.util.Cookies.get('lang'),
		path: '/i18n',
		noCache: true
	});*/


i18n.onReady(function(){
 Ext.get('hubTitle').dom.innerText = i18n.getMsg('hublogs.title');
 
  Ext.tip.QuickTipManager.init();
  Ext.apply(Ext.tip.QuickTipManager.getQuickTip(), {
	showDelay: 500,      // Show 500ms after entering target
	hideDelay: 1000,
	dismissDelay: 30000,
	autoHide:true,
	closable:false,
  });
	
  Ext.define('HubLogEntryM', {
    extend: 'Ext.data.Model',
    fields: [
        {name: 'Date',     type: 'date'},
        {name: 'Origin',     type: 'string'},
        {name: 'Subsystem', type: 'string'},
        {name: 'Severity', type: 'string'}, 
        {name: 'Message', type: 'string'}, 
    ]
  });
  
  var logStore = new Ext.data.JsonStore( {
        model: 'HubLogEntryM',
        autoLoad:false,
        //groupField:'bsName',
        proxy: {
            type: 'ajax',
            url:'/api/Hub/Logs',
			extraParams: {
		        format	: 'json'
		    },
			//remoteSort:true,
			baseParams:{start: 0, limit: 20}
        },
        reader:{
        	type:'json',
        	//root:'records',
        	totalProperty:'count',
        },
  });
  
	var combo = new Ext.form.field.ComboBox({
	  name : 'perpage',
	  width: 40,
	  store: new Ext.data.ArrayStore({
	    fields: ['id'],
	    data  : [
	      ['20'],
	      ['30'],
	      ['40'],
	      ['50'],
	    ]
	  }),
	  mode : 'local',
	  value: '20',
	  listWidth     : 40,
	  triggerAction : 'all',
	  displayField  : 'id',
	  valueField    : 'id',
	  editable      : false,
	  forceSelection: true
	});
	

	var bbar = new Ext.PagingToolbar({
	  store:       logStore, //the store you use in your grid
	  displayInfo: true,
	  pageSize: 20,
	  items   :    [
	    '-',
	    i18n.getMsg('generic.perPage'),
	    combo,
	    '-',
        {
      		xtype:'button',
		    icon:'/images/excel.png',
		    handler:function(button){
		        var gridPanel=button.up('gr');
		        //var dataURL='data:application/ms-excel;base64,'+Ext.ux.exporter.Exporter.exportAny(gr, 'csv');
		        //var dataURL='data:text/csv,'+Ext.ux.exporter.Exporter.exportAny(gr, 'csv');
		        var dataURL='data:application/vnd.ms-excel,'+Ext.ux.exporter.Exporter.exportAny(Ext.getCmp('gr'), 'excel');
		        window.location.href=dataURL;
		    }
		}
	  ],
	  paramNames:{start: 'start', limit: 'limit'},
      toggleHandler: function(btn, pressed){
        var view = grid.getView();
        view.showPreview = pressed;
        view.refresh();
     }

	});
	
	combo.on('select', function(combo, record) {
	  bbar.pageSize = parseInt(record.get('id'), 10);
	  bbar.doLoad(bbar.cursor);
	}, this);
	
	var gr = Ext.create('Ext.grid.Panel', {
	    id:'gr', 
	    height:'100%',
	    store: logStore,
	    columns:[{
            text:i18n.getMsg('hublogs.date'), 
            width:115,
            dataIndex:'Date',
        },{
            text:i18n.getMsg('hublogs.origin'), 
            width:120,
            dataIndex:'Origin',
        },
        {
            text:i18n.getMsg('hublogs.subsystem'), 
            width:80,
            dataIndex:'Subsystem',
        },
        {
            text:'<img src="/images/sq_di.png" border="0" align="left"/>',
            width:25,
            dataIndex:'Severity',
            //id:'severity',
            renderer :function(value, metaData, record, rowIndex, colIndex, store) {
				var severity = value; //store.getAt(rowIndex).get('severity');
				if (severity == 'ERROR')
                    return '<div> &nbsp;<img src="/images/sq_re.gif" border="0" valign="middle"/> &nbsp;</div>';
                else if (severity == 'WARNING')
                    return '<div> &nbsp;<img src="/images/sq_ye.gif" border="0" valign="middle"/> &nbsp;</div>';
                else if (severity == 'NOTICE')
                    return '<div> &nbsp;<img src="/images/sq_bl.gif" border="0" valign="middle"/> &nbsp;</div>';
                else if (severity == 'INFO')
                    return '<div> &nbsp;<img src="/images/sq_gr.gif" border="0" valign="middle"/> &nbsp;</div>';
                else 
                    return '<div> &nbsp;<img src="/images/sq_di.png" border="0" valign="middle"/> &nbsp;</div>';
			}
        },
        {
            text:i18n.getMsg('hublogs.message'), 
            width:385,
            dataIndex:'Message',
            flex:1,
            //id:'message',
            renderer:function(value){
            	return '<div data-qtip="' + value + '">'+value+'</div>';
            }
        }
        ],
	    renderTo:Ext.get('HubLogs'),
	    frame: false,
	    bbar: bbar
	    /*tbar:[{
                text:'',
                width:230,
                disabled:true
               
               }],*/
	    //renderTo:Ext.get('bPlanGrid'),
	    /*plugins:[new Ext.ux.grid.Search({
				iconCls:'icon-zoom'
				,readonlyIndexes:['note']
				,disableIndexes:['pctChange']
				,minChars:3
				,autoFocus:true
				,position: 'top'
				,align:'right'
				,width:80
				})
		]*/

	});
	
	logStore.load({params:{start:0, limit:20}}); 
	
});
});