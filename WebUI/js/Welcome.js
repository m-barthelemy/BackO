Ext.Loader.setConfig({enabled:true});

Ext.require([
    'Ext.data.*',
    'Ext.grid.*',
    'Ext.tree.*',
    'Ext.form.*',
    'Ext.chart.*',
]);



Ext.onReady(function () {
	
	
	
	/*var i18n = new Ext.i18n.Bundle({
        bundle:'wui', 
        path:'i18n',
        lang:Ext.util.Cookies.get('lang')
    });*/

i18n.onReady(function(){
	var colorsList = null;
	Ext.get('weTitle').dom.innerText = i18n.getMsg('welcome.title');
	
   /* Ext.define('sgM', {
        extend: 'Ext.data.Model',
        fields: [
           // {name: 'id',     type: 'string'},
            {name: 'userName',     type: 'string'},
            {name: 'share', type: 'number'},
            {name: 'available', type: 'string'},
            {name: 'percent', type: 'string'},
            //{name: 'iconCls', type: 'string'},
        ]
    });*/
	
	
    var sgStore = new Ext.data.Store( {
    	autoLoad:true,
        model: 'StorageGroup',
        proxy: {
            type: 'ajax',
            url: '/api/StorageGroups/'
        },
        reader:{
        	type:'json',
        },
        listeners:{
        	load:function(){
        		//alert("store nb="+sgStore.getTotalCount());
				//colorsList = new Array(sgStore.getTotalCount());
				var i =0;
				sgStore.each(function(rec) {
				    var business = rec.get('Online')+rec.get('Offline')/rec.get('Storage')*100
				    //alert(business);
				    if(business < 80)
				    	//colors.push('#67E02F');
				    	colorsList.push(["#67E02"+ Math.floor(((Math.random() - 0.5) * 100))]);
				    else if(business > 90)
				    	colorsList.push(["#CC0025"]);
				    else
				    	colorsList.push(["#F7A41E"]);
				    
				});
        	}
        }
    });

    var panel1 = Ext.create('widget.panel', {
        width: 240,
       	height: 240,
        title: i18n.getMsg('welcome.sgChart.title'),
        renderTo: Ext.get('sgChart'), //Ext.getBody(),
        layout: 'fit',
       	padding:0,
       	margins:0,
       	bodyStyle:'background:transparent;',
       	iconCls:'task-sg',
       	align:'top',
        items: {
            xtype: 'chart',
            id: 'chartCmp',
            animate: true,
            store: sgStore,
            shadow: true,
            /*legend: {
                position: 'bottom',
                itemSpacing:2,
                padding:2,
                labelFont:'9px Arial',
                zIndex:0,
            },*/
            insetPadding: 5,
            theme: 'Base:gradients',
            series: [{
            	//title:'Storage groups space',
                type: 'pie',
                field: 'Storage',
                showInLegend: true,
                //legendItem:'percent',
                donut: 25,
                style: {
                	opacity: 0.8,
            	},
                //
                
                //colorSet:colorsList, //['#67E02F', '#CC0025', '#F7A41E', '#67E02F', '#67E02F' ], //colorsList, 
                tips: {
                  trackMouse: true,
                  width: 150,
                  height: 28,
                  anchor:'left',
                  renderer: function(storeItem, item) {
                    //calculate percentage.
                    var total = 0;
                    sgStore.each(function(rec) {
                        total += rec.get('Storage');
                    });
                    var iconStyle = "";
                    var percentFree =  (storeItem.get('Online')+storeItem.get('Offline')/storeItem.get('Storage')*100);
                    if(percentFree < 10)
                    	iconStyle="sq_re.gif";
                    else if(percentFree > 800)
                    	iconStyle="sq_gr.gif";
                    else
                    	iconStyle="sq_ye.gif";
                    this.setTitle('<img style="vertical-align:middle;border:0;" src="/images/'+iconStyle+'">'+storeItem.get('userName') + ': ' +storeItem.get('percent')+ '%');
                  }
                },
                highlight: {
                  segment: {
                    margin: 5
                  }
                },
                label: {
                    field: 'Name',
                    display: 'rotate',
                    //display: 'insideEnd',
                    contrast: true,
                    font: '12px Arial',
                    minMargin:0,
                    renderer:function(value){
                    
                    	return value.split(' ').join('\n');
                    }
                }
            }]
        }
    });
  
    /*
    Ext.create('Ext.chart.Chart', {
	   	renderTo: Ext.get('sgChart'),
	   	width: 300,
	   	height: 300,
	   	align:'right',
   		id: 'chartCmp',
   		title:'Storage groups space',
        animate: true,
        store: store1,
        shadow: true,
        
        insetPadding: 60,
        theme: 'Base:gradients',
        series: [{
            type: 'pie',
            field: 'data1',
            showInLegend: true,
            donut: 30,
            tips: {
              trackMouse: true,
              width: 140,
              height: 28,
              renderer: function(storeItem, item) {
                //calculate percentage.
                var total = 0;
                store1.each(function(rec) {
                    total += rec.get('data1');
                });
                this.setTitle(storeItem.get('name') + ': ' + Math.round(storeItem.get('data1') / total * 100) + '%');
              }
            },
            highlight: {
              segment: {
                margin: 20
              }
            },
            label: {
                field: 'name',
                display: 'rotate',
                contrast: true,
                font: '14px Arial'
            }
        }]
    });*/
   
});

});