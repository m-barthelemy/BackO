Ext.define('backo.ClearableBox', {
    extend: 'Ext.form.field.Trigger',
    alias: 'widget.clearablebox',
    initComponent: function () {
        var me = this;

        me.triggerCls = 'x-form-clear-trigger';

        me.callParent(arguments);
    },
    // override onTriggerClick
    onTriggerClick: function() {
        this.setRawValue('');
    }
});