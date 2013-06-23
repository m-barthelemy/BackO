
Extensible.calendar.data.EventMappings = {
    EventId: {
        name:    'EventId',
        mapping: 'Id',
        type:    'int'
    },
    CalendarId: {
        name:    'CalendarId',
        mapping: 'Level',
        type:    'string'
    },
    Title: {
        name:    'Title',
        mapping: 'Level',
        type:    'string'
    },
    StartDate: {
        name:       'StartDate',
        mapping:    'StartDate',
        type:       'date',
        dateFormat: 'c'
    },
    EndDate: {
        name:       'EndDate',
        mapping:    'StartDate',
        type:       'date',
        dateFormat: 'c'
    },
    RRule: { // not currently used
        name:    'RecurRule', 
        mapping: 'rrule', 
        type:    'string' 
    },
    Location: {
        name:    'Location',
        mapping: 'loc',
        type:    'string'
    },
    Notes: {
        name:    'Notes',
        mapping: 'notes',
        type:    'string'
    },
    Url: {
        name:    'Url',
        mapping: 'url',
        type:    'string'
    },
    IsAllDay: {
        name:    'IsAllDay',
        mapping: 'ad',
        type:    'boolean',
        defaultValue: false,
    },
    Reminder: {
        name:    'Reminder',
        mapping: 'rem',
        type:    'string'
    }
};
