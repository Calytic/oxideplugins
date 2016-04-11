/**
 * Vision Locks
 * 
 * OxideMod Plugin for Rust
 * 
 * @author  VisionMise
 * @version  0.1.0
 * @beta
 */


/**
 * VisionLocks
 *
 * Oxide Plugin Object
 * @type {Object}
 */
var VisionLocks = {

    Title:          "VisionLocks",
    Author:         "VisionMise",
    Version:        V(0, 1, 0),
    Description:    "Indestructible Locks for Rust",

    OnEntityTakeDamage: function(entity, info) {

        //Return null if no entity is found;
        if (!entity) return;

        //Get the lock slot, if any
        var lock        = entity.GetSlot(0);

        //If no lock present, return null
        if (!lock) return;

        //If lock present, check its locked status
        var locked      = lock.IsLocked();

        //If locked == true, override default, else return null
        return (locked);
    }

}