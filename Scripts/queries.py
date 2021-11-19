#######################################################################
#
# Queries that will be added to the DatabaseQueries table when
# create-cms-db.py is run - it's called from managedb.py.
#
#######################################################################

predefined_queries = {}

predefined_queries['Inventory'] = '''
select I.InventoryID
     , I.Barcode
     , I.ChemicalName
     , I.CASNumber
     , L.Path as Location
     , O.Name as Owner
     , G.Name as 'Group'
     , DATE_FORMAT(DateIn, '%Y-%m-%d %h:%i:%s') as DateIn
     , DATE_FORMAT(ExpirationDate, '%Y-%m-%d %h:%i:%s') as ExpirationDate
     , I.ContainerSize
     , I.RemainingQuantity
     , I.Units
     , I.State
from InventoryItems I
inner join StorageLocations L on I.LocationID = L.LocationID
left outer join Owners O on I.OwnerID = O.OwnerID
left outer join StorageGroups G on I.GroupID = G.GroupID
@Where
'''

predefined_queries['Audits'] = '''
select A.Barcode, A.AuditTime, A.User, L1.Path as PreviousLoc, L2.Path as Location
from InventoryAudits A 
     left outer join StorageLocations L1 on A.PreviousLocationID = L1.LocationID 
     left outer join StorageLocations L2 on A.LocationID = L2.LocationID
@Where
order by A.Barcode, A.AuditTime
'''

predefined_queries['LogEntries'] = '''
select * from (select * 
               from LogEntries 
               where EntryDateTime between @FromDate and @ToDate
               order by LogEntryID desc) sub 
order by LogEntryID
'''

predefined_queries['TotalQuantities'] = '''
select CASNumber
     , Units
     , SUM(RemainingQuantity) as Total
from InventoryItems 
group by CASNumber, Units 
order by CASNumber
'''


predefined_reports = [
    {
        "ReportName": "Log Entries",
        "Description": "Last 10,000 log entries",
        "Query": "LogEntries",
        "Roles": "admin",
        "Widgets": "fromdate,todate",
        "Where": ""
    },
    {
        "ReportName": "Inventory Audit",
        "Description": "Inventory audit actions",
        "Query": "Audits",
        "Roles": "admin,manage,edit",
        "Widgets": "locationpicker,fromdate,todate",
        "Where": "INSTR(L2.Path, @Path) and A.AuditTime between @FromDate and @ToDate"
    },
    {
        "ReportName": "Inventory",
        "Description": "Inventory",
        "Query": "Inventory",
        "Roles": "admin,manage,report,view,edit",
        "Widgets": "locationpicker",
        "Where": "INSTR(L.Path, @Path)"
    },
    #{
    #    "ReportName": "Total Quantities",
    #    "Description": "List total remaining quantities of chemicals by CAS Number",
    #    "Roles": "admin,manage",
    #    "Widgets": "",
    #    "Where": ""
    #},
]
