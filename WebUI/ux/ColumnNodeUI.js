/*!
 * Ext JS Library 3.3.0
 * Copyright(c) 2006-2010 Ext JS, Inc.
 * licensing@extjs.com
 * http://www.extjs.com/license
 */
Ext.ns('Ext.ux.tree');

/**
 * @class Ext.ux.tree.ColumnTree
 * @extends Ext.tree.TreePanel
 * 
 * @xtype columntree
 */
Ext.ux.tree.ColumnTree = Ext.extend(Ext.tree.TreePanel, {
    lines : false,
    borderWidth : Ext.isBorderBox ? 0 : 2, // the combined left/right border for each cell
    cls : 'x-column-tree',
    onRender : function(){
        Ext.tree.ColumnTree.superclass.onRender.apply(this, arguments);
        this.headers = this.header.createChild({cls:'x-tree-headers'});

        var cols = this.columns, c;
        var totalWidth = 0;
        var scrollOffset = 19; // similar to Ext.grid.GridView default

        for(var i = 0, len = cols.length; i < len; i++){
             c = cols[i];
             totalWidth += c.width;
             this.headers.createChild({
                 cls:'x-tree-hd ' + (c.cls?c.cls+'-hd':''),
                 cn: {
                     cls:'x-tree-hd-text',
                     html: c.header
                 },
                 style:'width:'+(c.width-this.borderWidth)+'px;'
             });
        }
        this.headers.createChild({cls:'x-clear'});
        // prevent floats from wrapping when clipped
        this.headers.setWidth(totalWidth+scrollOffset);
        this.innerCt.setWidth(totalWidth);
    }
});

Ext.reg('columntree', Ext.ux.tree.ColumnTree);

//backwards compat
Ext.tree.ColumnTree = Ext.ux.tree.ColumnTree;


/**
 * @class Ext.ux.tree.ColumnNodeUI
 * @extends Ext.tree.TreeNodeUI
 */
Ext.ux.tree.ColumnNodeUI = Ext.extend(Ext.tree.TreeNodeUI, {
    focus: Ext.emptyFn, // prevent odd scrolling behavior

    renderElements : function(n, a, targetNode, bulkRender){
        this.indentMarkup = n.parentNode ? n.parentNode.ui.getChildIndent() : '';

        var t = n.getOwnerTree();
        var cols = t.columns;
        var bw = t.borderWidth;
        var c = cols[0];

        var buf = [
             '<li class="x-tree-node"><div ext:tree-node-id="',n.id,'" class="x-tree-node-el x-tree-node-leaf ', a.cls,'">',
                '<div class="x-tree-col" style="width:',c.width-bw,'px;">',
                    '<span class="x-tree-node-indent">',this.indentMarkup,"</span>",
                    '<img src="', this.emptyIcon, '" class="x-tree-ec-icon x-tree-elbow">',
                    '<img src="', a.icon || this.emptyIcon, '" class="x-tree-node-icon',(a.icon ? " x-tree-node-inline-icon" : ""),(a.iconCls ? " "+a.iconCls : ""),'" unselectable="on">',
                    '<a hidefocus="on" class="x-tree-node-anchor" href="',a.href ? a.href : "#",'" tabIndex="1" ',
                    a.hrefTarget ? ' target="'+a.hrefTarget+'"' : "", '>',
                    '<span unselectable="on">', n.text || (c.renderer ? c.renderer(a[c.dataIndex], n, a) : a[c.dataIndex]),"</span></a>",
                "</div>"];
         for(var i = 1, len = cols.length; i < len; i++){
             c = cols[i];

             buf.push('<div class="x-tree-col ',(c.cls?c.cls:''),'" style="width:',c.width-bw,'px;">',
                        '<div class="x-tree-col-text">',(c.renderer ? c.renderer(a[c.dataIndex], n, a) : a[c.dataIndex]),"</div>",
                      "</div>");
         }
         buf.push(
            '<div class="x-clear"></div></div>',
            '<ul class="x-tree-node-ct" style="display:none;"></ul>',
            "</li>");

        if(bulkRender !== true && n.nextSibling && n.nextSibling.ui.getEl()){
            this.wrap = Ext.DomHelper.insertHtml("beforeBegin",
                                n.nextSibling.ui.getEl(), buf.join(""));
        }else{
            this.wrap = Ext.DomHelper.insertHtml("beforeEnd", targetNode, buf.join(""));
        }

        this.elNode = this.wrap.childNodes[0];
        this.ctNode = this.wrap.childNodes[1];
        var cs = this.elNode.firstChild.childNodes;
        this.indentNode = cs[0];
        this.ecNode = cs[1];
        this.iconNode = cs[2];
        this.anchor = cs[3];
        this.textNode = cs[3].firstChild;
	 this.columnNodes = new Array();
        for (var i = 1, len = cols.length; i < len; i++) {
            this.columnNodes.push(this.elNode.childNodes[i].firstChild);
        }
    }
});

//backwards compat
Ext.tree.ColumnNodeUI = Ext.ux.tree.ColumnNodeUI;

Ext.tree.ColumnTreeNodeUI = Ext.extend(Ext.tree.TreeNodeUI, {  
    focus: Ext.emptyFn, // prevent odd scrolling behavior  
    renderElements : function(n, a, targetNode, bulkRender){  
        this.indentMarkup = n.parentNode ? n.parentNode.ui.getChildIndent() : '';  
        var t = n.getOwnerTree();  
        var cols = t.columns;  
        var bw = t.borderWidth;  
        var c = cols[0];  
        var cb = typeof a.checked == 'boolean';  
        if(typeof this.checkModel != 'undefined'){  
            cb = (!this.onlyLeafCheckable || n.isLeaf());  
        }  
        var href = a.href ? a.href : Ext.isGecko ? "" : "#";  
        var buf = ['<li class="x-tree-node"><div ext:tree-node-id="',n.id,'" class="x-tree-node-el x-tree-node-leaf x-unselectable ', a.cls,'" unselectable="on">',  
                '<div class="x-tree-col" style="width:',c.width-bw,'px;">',  
                    '<span class="x-tree-node-indent">',this.indentMarkup,"</span>",  
                    '<img src="', this.emptyIcon, '" class="x-tree-ec-icon x-tree-elbow">', 
		     cb ? ('<input class="x-tree-node-cb" type="checkbox" id="'+ n.id+ '" ' + (a.checked ? 'checked="checked" />' : '/>')) : '', 
                    '<img src="', a.icon || this.emptyIcon, '" class="x-tree-node-icon',(a.icon ? " x-tree-node-inline-icon" : ""),(a.iconCls ? " "+a.iconCls : ""),'" unselectable="on">',  
                     
                    '<a hidefocus="on" class="x-tree-node-anchor" href="',href,'" tabIndex="1" ',  
                    a.hrefTarget ? ' target="'+a.hrefTarget+'"' : "", '>',  
                    '<span unselectable="on">', n.text || (a[c.dataIndex]?(c.renderer ? c.renderer(a[c.dataIndex], n, a) : a[c.dataIndex]):''),"&nbsp;</span></a>",  
                "</div>"];  
         for(var i = 1, len = cols.length; i < len; i++){  
             c = cols[i];  
             buf.push('<div class="x-tree-col ',(c.cls?c.cls:''),'" style="width:',c.width-bw,'px;">',  
                        '<div class="x-tree-col-text">',
			
			(a[c.dataIndex]?(c.renderer ? c.renderer(a[c.dataIndex], n, a) : a[c.dataIndex]):''),"&nbsp;</div>",  
                      "</div>");  
         }  
         buf.push('<div class="x-clear"></div>',  
            '</div>',  
            '<ul class="x-tree-node-ct" style="display:none;"></ul>',  
            "</li>");  
              
        if(bulkRender !== true && n.nextSibling && n.nextSibling.ui.getEl()){  
            this.wrap = Ext.DomHelper.insertHtml("beforeBegin",n.nextSibling.ui.getEl(), buf.join(""));  
        }else{  
            this.wrap = Ext.DomHelper.insertHtml("beforeEnd", targetNode, buf.join(""));  
        }  
        this.elNode = this.wrap.childNodes[0];  
        this.ctNode = this.wrap.childNodes[1];  
        var cs = this.elNode.firstChild.childNodes;  
        this.indentNode = cs[0];  
        this.ecNode = cs[1];  
        this.iconNode = cs[2];  
          
        var index = 3;  
        if(cb){  
            this.checkbox = cs[3];  
            index++;  
        }  
        this.anchor = cs[index];  
        this.textNode = cs[index].firstChild;  
    }  
});  


Ext.ux.ColumnTreeCheckNodeUI = function() {  
    //多选: 'multiple'(默认)  
    //单选: 'single'  
    //级联多选: 'cascade'(同时选父和子);'parentCascade'(选父);'childCascade'(选子)  
    this.checkModel = 'multiple';  
      
    //only leaf can checked  
    this.onlyLeafCheckable = false;  
      
    Ext.ux.ColumnTreeCheckNodeUI.superclass.constructor.apply(this, arguments);  
};  
Ext.extend(Ext.ux.ColumnTreeCheckNodeUI, Ext.tree.ColumnTreeNodeUI, {  
      
    renderElements : function(n, a, targetNode, bulkRender){  
        var t = n.getOwnerTree();  
        this.checkModel = t.checkModel || this.checkModel;  
        this.onlyLeafCheckable = t.onlyLeafCheckable || false;  
          
        Ext.ux.ColumnTreeCheckNodeUI.superclass.renderElements.apply(this, arguments);  
          
        var cb = (!this.onlyLeafCheckable || n.isLeaf());  
        if(cb){  
            Ext.fly(this.checkbox).on('click', this.check.createDelegate(this,[null]));  
	   // this.on('mouseover', this.mouseover.createDelegate(this,[null]));  
        }  
        
        //else{
	    // Ext.fly(this).on('mouseover', this.mouseover.createDelegate(this,[null]));  
	//    this.on('mouseover', alert('flette'),this);
	//}
    },  
    mouseover:function(lute){
     // tree.fireEvent('mouseover', this.node, false); 
     alert('fÄÄrskha');
    },
    // private  
    check : function(checked){  
        var n = this.node;  
        var tree = n.getOwnerTree();  
        this.checkModel = tree.checkModel || this.checkModel;  
          
        if( checked == null ) {  
            checked = this.checkbox.checked;  
        } else {  
            this.checkbox.checked = checked;  
        }  
          
        n.attributes.checked = checked;  
        tree.fireEvent('check', n, checked);  
          
        if(!this.onlyLeafCheckable){  
            if(this.checkModel == 'cascade' || this.checkModel == 'parentCascade'){  
                var parentNode = n.parentNode;  
                if(parentNode !== null) {  
                    this.parentCheck(parentNode,checked);  
                }  
            }  
            if(this.checkModel == 'cascade' || this.checkModel == 'childCascade'){  
                if( !n.expanded && !n.childrenRendered ) {  
                    n.expand(false,false,this.childCheck);  
                }else {  
                    this.childCheck(n);    
                }  
            }  
        } else if(this.checkModel == 'single'){  
            var checkedNodes = tree.getChecked();  
            for(var i=0;i<checkedNodes.length;i++){  
                var node = checkedNodes[i];  
                if(node.id != n.id){  
                    node.getUI().checkbox.checked = false;  
                    node.attributes.checked = false;  
                    tree.fireEvent('check', node, false);  
                }  
            }  
        }  
    },  
      
    // private  
    childCheck : function(node){  
        var a = node.attributes;  
        if(!a.leaf) {  
            var cs = node.childNodes;  
            var csui;  
            for(var i = 0; i < cs.length; i++) {  
                csui = cs[i].getUI();  
                if(csui.checkbox.checked ^ a.checked)  
                    csui.check(a.checked);  
            }  
        }  
    },  
      
    // private  
    parentCheck : function(node ,checked){  
        var checkbox = node.getUI().checkbox;  
        if(typeof checkbox == 'undefined')return ;  
        if(!(checked ^ checkbox.checked))return;  
        if(!checked && this.childHasChecked(node))return;  
        checkbox.checked = checked;  
        node.attributes.checked = checked;  
        node.getOwnerTree().fireEvent('check', node, checked);  
          
        var parentNode = node.parentNode;  
        if( parentNode !== null){  
            this.parentCheck(parentNode,checked);  
        }  
    },  
      
    // private  
    childHasChecked : function(node){  
        var childNodes = node.childNodes;  
        if(childNodes || childNodes.length>0){  
            for(var i=0;i<childNodes.length;i++){  
                if(childNodes[i].getUI().checkbox.checked)  
                    return true;  
            }  
        }  
        return false;  
    },  
      
    toggleCheck : function(value){  
        var cb = this.checkbox;  
        if(cb){  
            var checked = (value === undefined ? !cb.checked : value);  
            this.check(checked);  
        }  
    }  
});  
