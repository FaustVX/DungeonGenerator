### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
INITITEM001 | InitItem | Error | Attributed interfaces must declare the constrained Init method
INITITEM002 | InitItem | Error | Non-partial item classes require a static constructor
INITITEM003 | InitItem | Error | Existing static constructors must call all required initializers
INITITEM004 | InitItem | Error | Attributed interfaces must not be generic
INITITEM005 | InitItem | Error | Item declares the generic initializer required by generated constructors
INFOITEM001 | InfoItem | Error | Attributed interfaces must declare a supported static GetInfo method
INFOITEM002 | InfoItem | Error | Non-partial item classes must implement IInfoItem.GetInfo manually
INFOITEM003 | InfoItem | Info | Existing IInfoItem.GetInfo implementations must call all applicable info methods
