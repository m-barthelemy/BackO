/*!
 * Extensible 1.6.0-b1
 * Copyright(c) 2010-2012 Extensible, LLC
 * licensing@ext.ensible.com
 * http://ext.ensible.com
 */
Ext.define('Extensible.lang.Number', {
    statics: {
        getOrdinalSuffix: function(num) {
            if (!Ext.isNumber(num)) {
                return '';
            }
            switch (num) {
                case 1:
                case 21:
                case 31:
                    return "st";
                case 2:
                case 22:
                    return "nd";
                case 3:
                case 23:
                    return "rd";
                default:
                    return "th";
            }
        }
    }
}, function() {
    Extensible.Number = Extensible.lang.Number;
})
