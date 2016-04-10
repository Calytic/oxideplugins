Inventory Guardian can save and restore player inventory's **even after forced server wipes!**

**Chat commands:**
/ig save - Save your inventory for later restoration!
/ig restore - Restore your saved inventory.
/ig delsaved - Delete your saved inventory!
/ig save <name> - Save player's inventory for later restoration!
/ig restore <name> - Restore player's saved inventory
/ig delsaved <name> - Delete player's saved inventory!
/ig restoreupondeath - Toggle (Enable/Disable) Inventory restoration upon death for all players on the server!
/ig toggle - Toggle (Enable/Disable) Inventory Guardian!
/ig autorestore - Toggle (Enable/Disable) Automatic Restoration.
/ig authlevel <1/2> - Change I.G required Auth Level.
/ig strip - Clear your current inventory.
/ig strip <name> - Clear player current inventory.
/ig keepcondition - Toggle (Enable/Disable) Items Condition restoration.

**Console commands:**
ig authlevel <1/2> - Change I.G required Auth Level.
ig toggle - Toggle (Enable/Disable) I.G!
ig restoreupondeath - Toggles the Inventory restoration upon death for all players on the server!
ig autorestore - Toggle (Enable/Disable) Automatic Restoration.
ig restoreall - Restore all players inventories (Sleeping and Online).
ig saveall - Save all players inventories (Sleeping and Online).
ig deleteall - Delete all players saved inventories. (Sleeping and Online)
ig stripall - Strip all players inventories. (Sleeping and Online)
ig keepcondition - Toggle (Enable/Disable) Items Condition restoration.
ig strip <name> - Clear player current inventory.
ig delsaved <name> - Delete player's saved inventory!
ig save <name> - Save player's inventory for later restoration!
ig restore <name> - Restore player's saved inventory

**Permissions**
inventoryguardian.admin - Give access to use the admin commands
inventoryguardian.use - Give access to save/restore/delsaved and strip

**I.G** restores and saves the player inventory when:


* On Player death: **If Restore Upon Death is enabled **Save the Inventory, **If Restore Upon Death is disabled **the saved inventory is deleted!
* On Player Respawn: Restore the Inventory. **(If Restore Upon Death is enabled)**
* On Player Connect: Restore the Inventory. **(If detected wipe).**
* On Player Disconnect: Save the Inventory.


**Important Information**


* 
**Note: **Batch commands are not affected if Inventory Guardian is disabled.
* All player commands (With **<name>**): works with Sleeping or Online players.
* **By default:** The required auth level is 2** (owner/admin)**,
**Restore upon death **is disabled!,  So if your players are killing themselves to duplicate items **MAKE SURE TO DISABLE IT!**
* **I.G** does all the work by itself, You don't need do anything, Make sure that the options are default.