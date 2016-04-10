**Command**:

- /remote "NAME" - spawn a remote on the wall that you are looking at.

You will need at least 1 tool cupboard, and access to ALL surrounding tool cupboards.
**WARNING: the remote will be placed on the INSIDE PART of the wall!**
**Oxide Permissions:**

canremoteactivate (default, you may change it in the configs)

To let players create remotes you will need to do:

````
oxide.grant group player canremoteactivate
````


**Features:**

- Anti Trap: This will block players from using Open / Close when players that DONT have access to the remote are nearby. Setting Anti Trap Distance to 0 or 1 should deactivate this function.

- Cost: Make it that players have to pay for this remote switch.
**Default Configs:**

````
{

  "Permission - Oxide Permissions": "canremoteactivate",

  "Remote Activator - Anti Trap Distance": 80,

  "Remote Activator - Cost": {

    "Battery - Small": "1",

    "High Quality Metal": "200"

  },

  "Remote Activator - Max Door Distance": 60,

  "Remote Activator - Max Doors": 20

}
````