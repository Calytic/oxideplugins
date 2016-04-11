**BuildingWrapper** is meant to simplify setting up ZoneManager zones around buildings.  This plugin will allow a player to look at a building, execute a command, and have a zone automatically created, moved, scaled, and rotated to fit that building.

**Please Note**: This plugin **requires modification **of the ZoneManager code, otherwise zones may not be created properly.  See "ZoneManager Modification" below.

**Usage (chat):**
/bw wrap [zone_id] <shape> <buffer> - Wrap the building which is being looked at in a zone.  If the [zone_id] is the ID of an existing zone, the zone will be moved, scaled, and rotated if necessary to fit the building being looked at.


The [zone_id] parameter is required, but entering "auto" will enable an ID to be auto-generated.

Optional parameters include shape (either "box" or "sphere" - "sphere" is default if not entered), and buffer size (defaults to 1), which is the distance the zone should extend beyond the building.

**Shapes:**
box - Wraps the building in a box zone - the zone size and rotation is the smallest surrounding rectangle that can fit around the building perimiter.
sphere - Wraps the building in the smallest possible spherical zone which encompasses the entire building.

**Permissions:**

Requires the ZoneManager permission (zonemanager.zone) or auth level > 0 to execute.

**Planned Improvements:**

- Extend functionality to extend an existing zone to enclose the target building (resize, rotate, and/or move original zone)

**ZoneManager Modification:**

The procedure UpdateZoneDefinition in ZoneManager must be modified to accept a rotation value (in degrees), otherwise the box zone orientation will match the player's direction:

Code (C#):
````
case "rotation":

    float rotation = Convert.ToSingle(args[i + 1]);

    if(rotation != null)

        zone.Rotation = Quaternion.AngleAxis(rotation, Vector3.up).eulerAngles;

    else

    {

        zone.Rotation = player?.GetNetworkRotation() ?? Vector3.zero;/* + Quaternion.AngleAxis(90, Vector3.up).eulerAngles*/

        zone.Rotation.x = 0;

    }

    editvalue = zone.Rotation;

    break;
````