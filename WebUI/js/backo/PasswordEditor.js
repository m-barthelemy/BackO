Ext.define('Password', {
    extend: 'Ext.data.Model',
    idProperty: 'Id',
    fields: [
    	{name: 'Id',     		type: 'int'},
        {name: 'Value',    		type: 'string'},
 	],
 	validations:[
 		{type: 'length', field: 'Value', min:4, message: 'Password must be at least 4 characters'}
 	],
 	proxy: {
        type: 'rest',
        url : '/api/Passwords/',
        reader:{
        	type:'json',
        	applyDefaults: true
        }
    }
 });

	
Ext.define('backo.PasswordEditor', {
    extend: 'Ext.form.field.Picker',
    alias: 'widget.passwordeditor',
    editable: false,
    hideTrigger: true,
    pickerOffset: [ 0, -20 ],
    passwordId:0,
    width: 220,
    listeners: {
        focus: function( fld, e, opts ) {
            fld.expand();
        }
    },
    cancelEdit: function() {
        var me = this;
        me.fireEvent( 'blur' );
        me.collapse();       
    },
    applyValues: function() {
        var me = this,
            form = me.picker,
            vals = form.getForm().getValues(); 
           var pass1 = form.getForm().findField('password1').getValue();
           var pass2 = form.getForm().findField('password2').getValue();
           if(pass1 != pass2){
           		form.getForm().findField('msg').setValue('Values do not match');
           }
           else{ //password match, save or update
			    var passObj = Ext.create('Password',{
			    	Value: pass1
			    });
			    if(passwordId >0) // update mode
			    	passObj.set('Id',passwordId);
			    passObj.save({
			    	success: function(record, operation){
				        me.setValue(record.get('Id'));
				        console.debug('pass id='+record.get('Id'));
				        form.getForm().reset();
				        me.fireEvent('blur');
		        		me.collapse();  
				    }
    			});
		        
		   }      
    },
    createPicker: function() {
        var me = this,
            format = Ext.String.format;
        passwordId = me.getValue() ;
       
        return Ext.create('Ext.form.Panel', {
            //title: 'Enter Product Details',
            bodypadding:5,
            pickerField: me,
            ownerCt: me.ownerCt,
            renderTo: document.body,
            floating: true,
            bodyPadding:8,
            width:220,
            defaults:{labelWidth:80},
            items: [
                {
                    xtype: 'textfield',
                    inputType:'password',
                    fieldLabel: 'Password',
                    labelAlign: 'left',
                    anchor: '100%',
                    name: 'password1',
                },{
                    xtype: 'textfield',
                    inputType:'password',
                    fieldLabel: 'Re-Type',
                    labelAlign: 'left',
                    anchor: '100%',
                    name: 'password2',
                },{
                	xtype:'displayfield',
                	name:'msg',
                	style:'color:red;'
                }                          
            ],
            dockedItems: [
                {
                    xtype: 'toolbar',
                    dock: 'bottom',
                    padding:1,
                    margin:0,
                    items: [
                        {
                            xtype: 'button',
                            name:'save',
                            text: i18n.getMsg('generic.ok'),
                            iconCls: 'okIcon',
                            handler: function( btn, e, opts ) {
                            	me.applyValues();
                            }                                
                        },'->',
                        {
                            xtype: 'button',
                            name:'cancel',
                            text: i18n.getMsg('generic.cancel'),
                            iconCls: 'cancelIcon',
                            handler: function( btn, e, opts ) {
                            	me.picker.getForm().reset();
                                me.cancelEdit();
                            }                                
                        }
                        
                    ]                    
                }
            ]
            
        })            
    }
});

//});