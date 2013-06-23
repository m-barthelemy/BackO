Ext.onReady(function(){

    Ext.QuickTips.init();

    // turn on validation errors beside the field globally
    Ext.form.Field.prototype.msgTarget = 'side';
    var nodesChecked = [];
    var pathsChecked = [];
    
    nodesTreeLoader  = Ext.extend(Ext.ux.XmlTreeLoader, {
		                 		params: "path=/",
						        processAttributes: function(attr){
						            if (attr.d) {
						            	attr.id = '-root-';
						                attr.text = attr.d;
						                attr.loaded = true;
						                attr.leaf = false;
						            }
						           else 
						                if (attr.n) {  
						                	attr.id = attr.n;                 
						                    attr.text = attr.n;
						                    attr.leaf = false;
						                    attr.checked = false;
						                }
						                else if (attr.f) {                   
						                    attr.text = attr.f;
						                    attr.leaf = true;
						                }
						                else if (attr.v) {   
						                	attr.id = attr.v;     
						                	attr.icon = '/images/kdf.png';            
						                    attr.text = attr.v;
						                    attr.leaf = false;
						                    attr.checked = false;
						                }
						                else if (attr.x) {   
						                	attr.id = attr.x;  
						                	attr.icon = '/images/cl-i.png';              
						                    attr.text = attr.x;
						                    attr.leaf = false;
						                    attr.checked = false;
						                }
						                else if (attr.u) {   
						                	attr.id = attr.u;  
						                	attr.icon = '/images/f-g.png';              
						                    attr.text = attr.u;
						                    attr.leaf = false;
						                    attr.checked = false;
						                }
						        }
	});
								
					 
	
	var nodesTree = new Ext.ux.tree.ColumnTree({
        rootVisible:false,
        id:'nodesTree',
        
        autoScroll:true,
        title: '<img src="/images/1.png" class="gIcon"/> Select clients to add this backupset to',
        checkModel: 'multiple',
		onlyLeafCheckable: true, 
        columns:[{
            width:200,
            dataIndex:'userName',
            checked:'false'
        },{
            width:100,
            dataIndex:'ip'
        } 
        ],
        loader: new Ext.tree.TreeLoader({
            dataUrl: '/Get.aspx?w=Clients',
            uiProviders:{
                 'col': Ext.ux.ColumnTreeCheckNodeUI
            }
        }),
       
		
  		listeners: {
            'checkchange': function(node){
            	/*if(node.attributes.checked == true){
		        	node.attributes.checked = false;
		        }
		         else{
		         	node.attributes.checked = true;
		         }*/
		        // alert(node.id);
		       //alert( document.getElementById(node.id).checked);
		       
		       if(nodesChecked[node.id] == null){
		       		var newNode = [node.id, document.getElementById(node.id).checked];
		       		nodesChecked.push(newNode);
		       	}
		       	else{
		       		nodesChecked[node.id] = document.getElementById(node.id).checked;
		       	}
		        // alert(node.attributes.checked);
              }
		 },
  
        root: new Ext.tree.AsyncTreeNode({
            text:'Client'
        })
    });
		       	    	    	    	    
    	    	    	    	    	    	    	    
    var fs = new Ext.FormPanel({
    	id:'fs',
    	//renderTo: Ext.getBody(),
        frame: false,
        title:'<img src="/images/2.png" class="gIcon"/> Configure what to backup and how',
        labelAlign: 'left',
        labelWidth: 95,
        autoWidth:true,
        autoHeight:true,
        bodyStyle: 'padding:10px;',
        waitMsgTarget: true,

        items: [
        
        	   new Ext.form.FieldSet({
                title: 'Directories and files to backup',
                autoHeight: true,
                layout: "column", 
                defaultType: 'textfield',
                items: [
                	{
                            xtype: 'label',
                            text: 'root path',
                            columnWidth: .06
                    },
                	{
                		xtype:'textfield',
                        fieldLabel: 'root path',
                        label: 'root path',
                        textLabel: 'root path',
                        emptyText: 'enter a base path',
                        name: 'basePath',
                        columnWidth: .15,
                        align:'left'
                    }, 
                    {
                            xtype: 'label',
                            text: ' ...or browse',
                            columnWidth: .08
                    },
                    new Ext.Button( {
		                fieldLabel:'', 
		                 text:'', 
		                 width:25,
		                 icon:'/images/browse.png', 
		                 cls:'x-btn-text-icon', 
		                 handler:function(){
		                 
		                 	fTreeLoader  = Ext.extend(Ext.ux.XmlTreeLoader, {
		                 		params: "path=/",
						        processAttributes: function(attr){
						            if (attr.d) {
						            	attr.id = '-root-';
						                attr.text = attr.d;
						                attr.loaded = true;
						                attr.leaf = false;
						            }
						           else 
						                if (attr.n) {  
						                	attr.id = attr.n;                 
						                    attr.text = attr.n;
						                    attr.leaf = false;
						                    attr.checked = false;
						                }
						                else if (attr.f) {                   
						                    attr.text = attr.f;
						                    attr.leaf = true;
						                }
						                else if (attr.v) {   
						                	attr.id = attr.v;     
						                	attr.icon = '/images/kdf.png';            
						                    attr.text = attr.v+' '+Math.round(attr.size/1024/1024/1024)+'GB, '+Math.round(attr.avail/1024/1024/1024)+'GB free';
						                    attr.leaf = false;
						                    attr.checked = false;
						                }
						                else if (attr.x) {   
						                	attr.id = attr.x;  
						                	attr.icon = '/images/cl-i.png';              
						                    attr.text = attr.x;
						                    attr.leaf = false;
						                    attr.checked = false;
						                }
						                else if (attr.u) {   
						                	attr.id = attr.u;  
						                	attr.icon = '/images/f-g.png';              
						                    attr.text = attr.u;
						                    attr.leaf = false;
						                    attr.checked = false;
						                }
						        }
						    });
								
					 
							var fTree = new Ext.tree.TreePanel({
								title: '',
								renderTo: Ext.get('winBrowse'),
								collapsible: false,
								checkModel: 'multiple',
								border: false,
								id:'fTree',
								autoScroll:true,
								animate: true,
								enableDD: false,
								containerScroll: false,
								BodyStyle:'overflow-y:auto !important',
								height: 450,
								layout : 'vbox',
								loader: new fTreeLoader({
									dataUrl: '/Get.aspx?w=Browse&node=matt&path='
								}),
								rootVisible: false, 
								listeners: {
								        beforeexpandnode: {
								            fn:function (node){
							                	node.loader = new fTreeLoader({
							                    	dataUrl: fTree.getLoader().dataUrl+'/'+node.getPath().substr(2)
												});
												node.reload();
											}
								        }
								    }
							});
 
							// set the root node
							var fTreeRoot = new Ext.tree.AsyncTreeNode({
								text: '',
								draggable: false,
								id: '/', // id of the root node
								expanded:true
							});
							// render the tree
							fTree.setRootNode(fTreeRoot);
							
		                 	var winBrowse = new Ext.Window({
		                 		id:'winBrowse',
		                 		fieldLabel:'winBrowse',
		                        width:600,
		                       	height:500,
		                        plain: true,
		                        title:"Browse node",
								autoScroll:true,
		                        //shim :false,
		                        modal:true,
		                        rederTo: Ext.getBody(),
		                        autoDestroy :true,
		                        monitorValid:true,
		                        resizable:true,
		                      	//failure: function () {},
								items: fTree,
				                buttons: [{
					                    text:'Ok',
					                    disabled:false,
					                    handler:function(){
					                    	var msg = [];
					                    	var selectedPaths = fTree.getChecked();
					                    	Ext.each(selectedPaths, function(node){
							                    var pArray =['path',node.getPath()];
							                    msg.push(pArray);
							                });
							                pathsChecked = msg;
							                //winBrowse.close();
							                winBrowse.hide();
					                    }
					                },{
					                    text: 'Close',
					                    handler: function(){
					                        winBrowse.hide();
					                        win.close();
					                    	}
					                }
				                	] 
		                       
		                     });
		              		winBrowse.show();
		                 	
		                 }, 
		                 descriptionText:'' 
                 	})
                 	
                   ]}),
                    new Ext.form.FieldSet({
                title: 'Files and subdirectories selection rules',
                autoHeight: true,
                defaultType: 'textfield',
                items: [
                    {
                        fieldLabel: 'include rule',
                        emptyText: 'enter a rule',
                        name: 'includePolicy',
                        width:190
                    }, {
                        fieldLabel: 'exclude rule',
                        
                        emptyText: 'enter a rule',
                        name: 'excludePolicy',
                        width:190
                    }
                ]
            }),
            
           	new Ext.form.FieldSet({
                title: 'Storage options',
                autoHeight: true,
                layout: "column", 
                defaultType: 'textfield',
                items: [
                 		{
	                 		xtype:'checkbox',
	                        fieldLabel: 'Encrypt',
	                        boxLabel:'Encrypt',
	                        columnWidth: .1,
	                        name: 'encrypt',
	                         id:'encrypt'
                    	},
                    	{
	                 		xtype:'checkbox',
	                        columnWidth: .1,
	                        name: 'compress',
	                        id:'compress',
	                       	fieldLabel: 'Compress',
							boxLabel:'Compress',
	                        checked:true
                    	}
                ]
            }) 
            
          ]});   
            
        // preops and postops
        var opsPanel = new Ext.FormPanel({
    	id:'opsPanel',
        frame: false,
        title:'<img src="/images/3.png" class="gIcon"/> Configure pre and post-backup custom operations',
        labelAlign: 'right',
        labelWidth: 85,
        autoWidth:true,
        autoHeight:true,
        bodyStyle: 'padding:10px;',
        waitMsgTarget: true,
        items: [
        	   new Ext.form.FieldSet({
                title: 'Commands to execute on client node',
                autoHeight: true,
                layout: "column", 
                height: 300,
                defaultType: 'textfield',
                items: [
                	{
                            xtype: 'label',
                            text: 'Pre-backup operations',
                            columnWidth: .10
                    },
                	{
                		xtype:'textarea',
                        fieldLabel: '',
                        label: '',
                        textLabel: '',
                        emptyText: 'put script or commands to execute',
                        name: 'preops',
                        columnWidth: .40,
                        align:'left',
                        autoCreate: {
							tag: "textarea",
							rows:15,
							height: 80,
							columns:10,
							autocomplete: "off",
							wrap: "off"
						},
                    },
                    {
                            xtype: 'label',
                            text: 'Post-backup operations',
                            columnWidth: .10
                    },
                	{
                		xtype:'textarea',
                        fieldLabel: '',
                        label: '',
                        textLabel: '',
                        emptyText: 'put script or commands to execute',
                        name: 'postops',
                        columnWidth: .40,
                        align:'left',
                        autoCreate: {
							tag: "textarea",
							rows:15,
							height: 80,
							columns:10,
							autocomplete: "off",
							wrap: "off"
						},
                    },
                    ]
                    })
                   ]
        });
                   
       var schedPanel = new Ext.FormPanel({
    	id:'schedPanel',
        frame: false,
        title:'<img src="/images/4.png" class="gIcon"/> Choose when and how to backup',
        labelAlign: 'right',
        labelWidth: 85,
        autoWidth:true,
        autoHeight:true,
        bodyStyle: 'padding:10px;',
        waitMsgTarget: true,
     	
    	});
    	
    	
    	
    	
    var stoPanel = new Ext.FormPanel({
    	id:'stoPanel',
    	//renderTo: Ext.getBody(),
        frame: false,
        title:'<img src="/images/5.png" class="gIcon"/> Configure storage and archiving policy',
        labelAlign: 'left',
        labelWidth: 95,
        autoWidth:true,
        autoHeight:true,
        bodyStyle: 'padding:10px;',
        waitMsgTarget: true,

        items: [
        		new Ext.form.FieldSet({
                title: 'Redundancy',
                autoHeight: true,
                layout: "column", 
                defaultType: 'textfield',
                items: [
                	{
                            xtype: 'label',
                            text: 'Store ',
                            columnWidth: .05
                    },
                    new Ext.form.ComboBox({
		    			columnWidth: .04,
		                height:24,
		                align:'left',
		                id:'redundancy',
		                store: new Ext.data.ArrayStore({
		                    fields: ['bType'],
		                    data : [['1'],['2'],['3']]
		                }),
		                valueField:'bType',
		                displayField:'bType',
		                typeAhead: true,
		                mode: 'local',
		                triggerAction: 'all',
		                emptyText:'1',
		                selectOnFocus:true,
		                width:50
		        	}),
                    {
                            xtype: 'label',
                            text: 'copies. ',
                            columnWidth: .05
                    }
                ]
                }),
                
        	   new Ext.form.FieldSet({
                title: 'Retention',
                autoHeight: true,
                layout: "column", 
                defaultType: 'textfield',
                items: [
                	{
                            xtype: 'label',
                            text: 'Keep full sets during ',
                            columnWidth: .2
                    },
                    new Ext.form.ComboBox({
		    			columnWidth: .09,
		                height:24,
		                align:'left',
		                id:'retention',
		                store: new Ext.data.ArrayStore({
		                    fields: ['bType', 'daysV'],
		                    data : [['1 week (7 days)','7'],['2 weeks (14 days)','14'],['3 weeks (21 days)','21'],['1 month (31 days)','31'],['2 months (62 days)','61'], ['6 months (183 days)','183'], ['1 year (365 days)','365'], ['2 years (731 days)','731']]
		                }),
		                valueField:'daysV',
		                displayField:'bType',
		                typeAhead: true,
		                mode: 'local',
		                triggerAction: 'all',
		                emptyText:'1 month',
		                selectOnFocus:true,
		                width:50
		        	})
                   
                ]
                }),
                
                new Ext.form.FieldSet({
                title: 'Archiving',
                autoHeight: true,
                layout: "column", 
                defaultType: 'textfield',
                items: [
                	{
                            xtype: 'label',
                            text: 'After normal retention period: ',
                            columnWidth: .1
                    },
                 
                ]
                })
          ],
          buttons:[
     			{
     			id:'create',
     			text:'Create!',
     			handler:function(){
					var nodesTreeV = Ext.getCmp('nodesTree').getChecked();
					var fsV = fs.getForm().getValues(); 
					var opsPanelV = opsPanel.getForm().getValues(); 
					var schedPanelV = schedPanel.getForm().getValues(); 
					var stoPanelV = stoPanel.getForm().getValues(); 
					var pampelle = [];
					pampelle.push(Ext.encode(nodesChecked));
					pampelle.push(Ext.encode(pathsChecked));
					pampelle.push(Ext.encode(fsV));
					pampelle.push(Ext.encode(opsPanelV));
					pampelle.push(Ext.encode(schedPanelV));
					pampelle.push(Ext.encode(stoPanelV));
					//alert('pampelle='+pampelle);
					
					var postdata = "";
					
					var conn = new Ext.data.Connection(); 
				    conn.request({ 
				            url: 'Set.aspx?w=AddBackupSet', 
				            method: 'POST', 
				            scope: this, 
				            params: pampelle, 
				            success: function(responseObject) { 
				             var okMsg = Ext.Msg.alert('Status', responseObject.responseText); 
				             //Ext.getCmp('create').disable();
				            }, 
				             failure: function(responseObject) { 
				                 Ext.Msg.alert('Status', 'Unable to save changes. Error:'+responseObject.responseText); 
				            } 
				    }); 
					
				}	
     			
     			},
     			{text:'Cancel'}
     		]
    });
                                                
                                                
    function addField(){
    	var dow = ["Sunday","Monday","Tuesday","Wednesday","Thursday","Friday","Saturday"];
    	var schedFs = new Ext.form.FieldSet({
    		title: 'Scheduling',
                autoHeight: true,
                defaultType: 'textfield'
    	});
    	var fieldz = [];
        for(i=0;i<24;i++){
			fieldz.push(["hh",i+":00"]);
			fieldz.push(["hh",i+":30"]);
		}
    	for(var i=0; i<dow.length;i++){
    		var daysFs = new Ext.form.FieldSet({
	    		title: '',
	            border: false,
	            id:dow[i]+'Fields',
	           	bodyStyle:'height:25px;margin:0px;padding:0px;',
	           	layout: "column", 
	           	align:'left',
	            defaultType: 'textfield'
    		});
        	var a = new Ext.form.Checkbox({
            	boxLabel: dow[i],
            	checked:true,
            	id:dow[i]+'Do',
            	height:24,
            	bodyStyle:'height:25px;margin:0px;padding:0px;',
            	columnWidth:.11
            });
            var b = new Ext.form.Label({
                xtype: 'label',
                text: ' Hour',
                align:'right',
                bodyStyle:'height:25px;margin:0px;padding:0px;',
                columnWidth: .05
            });
            var c = new Ext.form.ComboBox({
    			columnWidth: .08,
                fieldLabel: 'Hour:',
                height:24,
                bodyStyle:'height:25px;margin:0px;padding:0px;',
                id:dow[i]+'Hour',
                store: new Ext.data.ArrayStore({
                    fields: ['hh','nt'],
                    data : fieldz
                }),
                valueField:'nt',
                displayField:'nt',
                typeAhead: true,
                mode: 'local',
                triggerAction: 'all',
                emptyText:'...',
                selectOnFocus:true,
                width:50
        	});
        	var d = new Ext.form.Label({
                xtype: 'label',
                text: '        Type',
                 align:'right',
                 bodyStyle:'height:25px;margin:0px;padding-left:10px !important;',
                columnWidth: .05
            });
        	var e = new Ext.form.ComboBox({
    			columnWidth: .08,
                label: 'Type:',
                boxLabel:'Type:',
                height:24,
                align:'left',
                id:dow[i]+'Type',
                store: new Ext.data.ArrayStore({
                    fields: ['bType'],
                    data : [['Full'],['Incremental'],['Differential'],['BSDiff']]
                }),
                valueField:'bType',
                displayField:'bType',
                typeAhead: true,
                mode: 'local',
                triggerAction: 'all',
                emptyText:'...',
                selectOnFocus:true,
                width:50
        	});
            daysFs.add(a);
            daysFs.add(b);
            daysFs.add(c);
            daysFs.add(d);
            daysFs.add(e);
            schedFs.add(daysFs);
        }
        schedPanel.add(schedFs);
        
    };
    addField();
    var viewport = new Ext.Panel({
    	renderTo: Ext.get("panel"),
        layout: 'accordion',
        layoutConfig:{animate:true},
        autoHeight:true,
        autoWidth:true,
        items: [
        		//nodesPanel,
        		nodesTree,
        		fs, 
        		opsPanel,
        		schedPanel,
        		stoPanel
               ]
	});

	var sendForms = function(){
		var nodesTreeV = nodesTree.getChecked();
		var fTree = fTree.getChecked();
		var fsV = fs.getForm().getValues(); 
		var opsPanelV = opsPanel.getForm().getValues(); 
		var schedPanelV = schedPanel.getForm().getValues(); 
		var stoPanelV = stoPanel.getForm().getValues(); 
		alert('got it');
	}

});

// A reusable error reader class for XML forms
Ext.form.XmlErrorReader = function(){
    Ext.form.XmlErrorReader.superclass.constructor.call(this, {
            record : 'field',
            success: '@success'
        }, [
            'id', 'msg'
        ]
    );
};
Ext.extend(Ext.form.XmlErrorReader, Ext.data.XmlReader);