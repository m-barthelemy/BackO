Ext.Loader.setConfig({
    enabled: true,
    paths: {'Ext': '/js/ext4'}
    //paths: {'Ext.ux': '/js/ext4/ux'},
});
 
 
 Ext.require([
    'Ext.data.*',
    'Ext.grid.*',
    'Ext.tree.*',
    'Ext.form.*',
    'Ext.window.*',
    'Ext.fx.target.Sprite',
	]);
	
  
 Ext.onReady(function () {
 	var params = Ext.urlDecode(window.location.search.substring(1));
 	if(params.logout){ // WS api call to logout
 		Ext.Ajax.request({
	       	url: '/api/auth/logout',
	       	method:'GET'
	    });
 	
 	}
 	//TODO! move User model to a clean, common model file, avoiding duplicated definition here and inside menu.js
 	Ext.define('UserLogin', {
	 	extend: 'Ext.data.Model',
	    fields: [
	       
	        {name: 'UserName', 		type: 'string'}, // corresponds to 'Login' field in server User object
	        {name: 'Password', 		type: 'string'},
	        {name: 'Culture', 		type: 'string'},
	        {name: 'Roles'							}
	    ],
	    proxy: {
            type: 'rest',
            url: '/api/auth/credentials',
	        extraParams: {format: 'json'}
        }
    });
    
 	var panel = Ext.create('Ext.form.Panel', {
        width: '100%',
        bodyPadding: 10,
        layout: 'vbox',
        height: 200,
        border:0,
        url: '/api/auth/credentials',
		renderTo: Ext.get("loginPanel"),
        items: [
          {
            xtype: 'textfield',
            id:'login',
            height:30,
            width:'200',
            fieldLabel: 'login',
            allowBlank: false,
            hideTrigger:true,
            anchor: '100%',
            fieldCls:'input',
            listeners:{
            	render:function(thisObj, opts){
            		thisObj.focus();
            	}
            }
          },{
            xtype: 'textfield',
            id:'password',
            inputType: 'password',
            height:30,
            width:'200',
            fieldLabel: 'password',
            allowBlank: false,
            hideTrigger:true,
            anchor: '100%',
            fieldCls:'input',
            listeners: {
            	specialkey: function(field, e){
                    if (e.getKey() == e.ENTER) {
                        submitLogin();
                    }
                }
            }
          },{
            xtype: 'button',
            id:'send',
            height:30,
            width:'80',
            align:'right',
            fieldLabel: 'Login',
            text:'Login',
            formBind: true,
            type:'submit',
            anchor: '0%',
            fieldCls:'input',
            handler: function(){submitLogin();}
          },{
            	xtype: 'label',
            	id:'errorMsg',
            	fieldCls:'error',
            	cls:'error'
            }
       ]
     });
     
     var submitLogin = function() {
 		Ext.getCmp('send').disable();
 		Ext.getCmp('send').setIcon("/images/loading.gif");
        var login = Ext.getCmp('login').getValue();
        var password = Ext.getCmp('password').getValue();
        var user = Ext.create('UserLogin', {UserName: login, Password: password});
    	user.save({
			success: function(record, operation) {
				
				if(params.redir)
					window.location = params.redir;
				else{
					Ext.util.Cookies.set('lang', record.get('Culture'));
					window.location = '/html/Welcome.html';
					
				}
				/*Ext.Ajax.request({
                   	url: '/api/Users/Current',
                   	method:'GET',
                	params:{format:'json'},
				    success: function(response, opts) {
				    	console.debug('currentuser responsetext='+response.responseText);
			    		var params = Ext.urlDecode(window.location.search.substring(1));
						console.debug('url params='+JSON.stringify(params));
						if(params.redir)
							window.location = params.redir;
						else
							window.location = '/html/Welcome.html?lang='+lang;
			    	}
                });*/
			},
			failure: function(response) {
				Ext.getCmp('errorMsg').setText('Login failed. ('+response.statusText+')');
				console.warn('Login failed : ');
			}
		});
	       
	   };
 
 
 });