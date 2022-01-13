with 
newestConnectionEntries as -- get only newest entires
	(select distinct on("GtfsStopId", "OsmStopId") *
	from public."Connections"
	order by "GtfsStopId", "OsmStopId", "CreatedAt" desc)
,activeConnections as -- get only newest added entries
	(select *
	from newestConnectionEntries
	where "OperationType" = 0) -- only added entries
,tilesUserCount as -- get tiles where more than one user have an active/added connection 
	(select stop."TileId", count(distinct conn."UserId")
	from activeConnections as conn
	join public."Stops" as stop on stop."Id" = conn."GtfsStopId"
	group by stop."TileId" 
	having count(distinct conn."UserId") > 1)
,tileAssignments as -- create tile-user assignments
	(select distinct on (stop."TileId") stop."TileId", conn."UserId" 
	from activeConnections as conn
	join public."Stops" as stop on stop."Id" = conn."GtfsStopId"
	join tilesUserCount as tc on tc."TileId" = stop."TileId" 
	order by stop."TileId", conn."UserId")
,connectionAssignments as -- create connection-user pairs (from tile) to update
	(select conn."Id", ta."UserId"
	from activeConnections as conn
	join public."Stops" as stop on stop."Id" = conn."GtfsStopId"	
	join tileAssignments as ta on ta."TileId" = stop."TileId" 
	where conn."UserId" != ta."UserId") -- don't update "UserId" if it's already correct
-- update connection with assigned users
update public."Connections" as conn
set "UserId" = ca."UserId"
from connectionAssignments as ca
where conn."Id" = ca."Id";