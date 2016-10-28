using System;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("ExternalWallStack", "Dyceman - Deadlaugh (Dan)", "1.0.0", ResourceId = 0)]
    [Description("Allows the players to stack high external walls on top of each other.")]

    class ExternalWallStack : RustPlugin
    {

        #region Custom Functions
        /*
         * Name: HasRunPermission
         * Parameters: BasePlayer player, string cmdPermission
         * Return: Boolean
         * Description: Checks if the user has the permission of the value of the cmdPermission variable or is an owner of the server.
         */
        private bool HasRunPermission(BasePlayer player, string cmdPermission)
        {
            // If the player is equal to null then return false.
            if (player == null)
                return false;

            // return true or false (return true if the user has the permission or is an owner of the server) (return false if the user doesn't have the permission or isn't an owner of the server).
            return permission.UserHasPermission(player.userID.ToString(), cmdPermission) || player.net?.connection?.authLevel == 2;
        }


        /*
         * Name: CreateStackWall
         * Parameters: int amount, BaseEntity entity, BasePlayer player
         * Return: HashSet<ExternalWallLink>
         * Description: Creates the high external walls that stack on top of the first high external wall.
         */
        private HashSet<ExternalWallLink> CreateStackWall(int amount, BaseEntity entity, BasePlayer player)
        {
            // If the value of amount is less than 1 then set the value of amount to 1 else set the value of amount to the value of amount.
            amount = (amount < 1) ? 1 : amount;

            // Create an emply "list" that will contain ExternalWallLink(s).
            HashSet<ExternalWallLink> links = new HashSet<ExternalWallLink>();

            // If the configuration key "RequireMaterials" is set to true.
            if (this.configRequireMaterials == true)
            {
                // Find the item's definition for the type of high external wall that is being placed.
                ItemDefinition itemDefinition = ItemManager.FindItemDefinition(entity.ShortPrefabName);

                // Count how much high external walls the user has in their inventory.
                int canPlaceAmount = player.inventory.GetAmount(itemDefinition.itemid);

                // Subtract how much high external walls the user has in their inventory by one.
                canPlaceAmount = canPlaceAmount - 1;

                // If the amount of high external walls the user has in their inventory is less than one then return an empty list of ExternalWallLink(s).
                if (canPlaceAmount < 1)
                    return links;

                // If the amount of high external walls the users has in their inventory is greater than the amount allowed to be placed then set the value of amount to the value of amount...
                // ...else set the value of amount to the value of how much high external walls the user has in their inventory.
                amount = (canPlaceAmount > amount) ? amount : canPlaceAmount;

                // Take # (based now the value of amount) of high external walls from the player.
                player.inventory.Take(new List<Item>(), itemDefinition.itemid, amount);
                // Notify the player of how much high external walls are being taken out of their inventory.
                player.Command("note.inv", itemDefinition.itemid, -amount);
            }

            // Create an emply ExternalWallLink.
            ExternalWallLink entityLink;

            // Loop until the value of index is greater than the value of amount plus one.
            for (int index = 1; index < amount + 1; index++)
            {

                // Create an high external wall.
                BaseEntity wall = GameManager.server.CreateEntity(entity.PrefabName, entity.transform.position + new Vector3(0f, 5.5f * (float)index, 0f), entity.transform.rotation, true);
                // Activate the high external wall game object.
                wall.gameObject.SetActive(true);
                // Spawn the high external wall game object.
                wall.Spawn();
                // Notify the server of the placement and rotation changes of the high external wall.
                wall.TransformChanged();

                // Get the BaseCombatEntity component of the high external wall.
                BaseCombatEntity combatEntity = wall.GetComponentInParent<BaseCombatEntity>();

                // If the component can be found.
                if (combatEntity != null)
                {
                    // Change the health of the high external wall to max health.
                    combatEntity.ChangeHealth(combatEntity.MaxHealth());
                }

                // Set the owner of the high external wall to the player.
                wall.OwnerID = player.userID;

                // Tell the server to to send a update to the players for the high external wall.
                wall.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

                // Set the ExternalWallLink's game object to the high external wall's game object.
                entityLink = new ExternalWallLink(wall.gameObject);

                // Add the ExternalWallLink to the list of ExternalWallLink(s).
                links.Add(entityLink);
            }

            // Return the list of ExternalWallLink(s).
            return links;
        }


        /*
         * Name: GetConfig
         * Parameters: string name, T value
         * Return: if it can't find the configuration then return the value of value else return the value of the configuration.
         * Description: Obtains a configuration by the name.
         */
        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));
        #endregion

        #region MonoBehavior
        class ExternalWallLink
        {
            // Create an empty GameObject.
            GameObject gameObject;

            // Create boolean variable and set it to false.
            public bool isRemoving = false;

            /*
             * Name: ExternalWallLink
             * Parameters: GameObject go
             * Return: Nothing because it is an constructor
             * Description: Constructor for the ExternalWallLink class.
             */
            public ExternalWallLink(GameObject go)
            {
                // Set the class variable gameObject to the value of the argrument.
                this.gameObject = go;
            }

            /*
             * Name: entity
             * Parameters: None
             * Return: NULL if it can't get the BaseEntity component else return the BaseEntity component of the game object
             * Description: Constructor for the ExternalWallLink class.
             */
            public BaseEntity entity()
            {
                return (this.gameObject.GetComponent<BaseEntity>() == null) ? null : this.gameObject.GetComponent<BaseEntity>();
            }
        }

        class ExternalWallController : MonoBehaviour
        {
            // Declare an empty list of ExternalWallLink(s).
            HashSet<ExternalWallLink> links;

            /*
             * Name: Awake
             * Parameters: None
             * Return: None
             * Description: It is called when this component is added to a game object.
             */
            void Awake()
            {
                // Create an empty list of ExternalWallLink(s).
                links = new HashSet<ExternalWallLink>();
            }

            /*
             * Name: entityLinks
             * Parameters: None
             * Return: HashSet<ExternalWallLink>
             * Description: Return the list of ExternalWallLink(s).
             */
            public HashSet<ExternalWallLink> entityLinks()
            {
                // Return the value of the variable links.
                return this.links;
            }

            /*
             * Name: addLink
             * Parameters: ExternalWallLink linkEntity
             * Return: None
             * Description: Add a *new* entry to the list of ExternalWallLink(s).
             */
            public void addLink(ExternalWallLink linkEntity)
            {
                // If the variable links doesn't contain argument.
                if (this.links.Contains(linkEntity) == false)
                    // Add the ExternalWallLink to the list of ExternalWallLink(s).
                    this.links.Add(linkEntity);
            }
        }
        #endregion

        #region Initialization

        // Obtain the plugin RemoverTool.
        [PluginReference]
        Plugin RemoverTool;

        // Declare the variables that will contain the values of their respective configuration values.
        int configStackHeight;
        bool configUsePermission, configRequireMaterials;
        string pluginPermission, pluginName, pluginColor = "#FF6600";

        // Create empty list of BaseEntity(s).
        List<BaseEntity> removingEntity = new List<BaseEntity>();
        // Create empty list of player user id(s).
        HashSet<ulong> playerToggleCommand = new HashSet<ulong>();


        /*
         * Name: LoadDefaultConfig
         * Parameters: None
         * Return: None
         * Description: Called when the config for a plugin should be initialized.
         */
        protected override void LoadDefaultConfig()
        {
            Config["StackHeight"] = configStackHeight = GetConfig("StackHeight", 2);
            Config["UsePermission"] = configUsePermission = GetConfig("UsePermission", true);
            Config["RequireMaterials"] = configRequireMaterials = GetConfig("RequireMaterials", true);
            SaveConfig();
        }

        /*
         * Name: Init
         * Parameters: None
         * Return: None
         * Description: Called when a plugin is being initialized.
         */
        void Init()
        {
            // Call the function LoadDefaultConfig.
            LoadDefaultConfig();

            // Set the variable pluginPermission value to externalwallstack.wstack.
            this.pluginPermission = new StringBuilder(this.GetType().Name.ToLower()).Append(".wstack").ToString();

            // Set the leading plugin name in chat to "<color=#FF6600>[ExternalWallStack]</color> :".
            this.pluginName = new StringBuilder("<color=").Append(this.pluginColor).Append(">[").Append(this.GetType().Name).Append("]</color> : ").ToString();

            // Register messages to the plugin lang file.
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "NotAuthorized", "You're not the permissions to use this chat command!" },
                { "CommandToggle", "High External Wall stacking is <color={0}>{1}</color>"}

            }, this);

            // If the permission externalwallstack.wstack doesn't exist.
            if (!permission.PermissionExists(pluginPermission))
                // Register the permission externalwallstack.wstack.
                permission.RegisterPermission(pluginPermission, this);
        }

        /*
         * Name: OnServerInitialized
         * Parameters: None
         * Return: None
         * Description: Called after the server startup has been completed and is awaiting connections.
         */
        void OnServerInitialized()
        {
            // If the plugin RemoverToll can't be found.
            if (RemoverTool == null)
                // Print a warning to the server console.
                PrintWarning("RemoverTool by Reneb was not found!");
        }

        /*
         * Name: Unload
         * Parameters: None
         * Return: None
         * Description: Called when a plugin is being unloaded.
         */
        void Unload()
        {
            // Clear the list of player user id(s).
            this.playerToggleCommand.Clear();
            // Clear the list of BaseEntity(s).
            this.removingEntity.Clear();
        }

        /*
         * Name: OnPlayerDisconnected
         * Parameters: BasePlayer player, string reason
         * Return: None
         * Description: Called after the player has disconnected from the server.
         */
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            // If the list of player user id(s) contains the player's user id.
            if (this.playerToggleCommand.Contains(player.userID) == true)
                // Remove the player's user id from the list of player user id(s).
                this.playerToggleCommand.Remove(player.userID);
        }
        #endregion

        #region Hooks
        /*
         * Name: OnEntityBuilt
         * Parameters: Planner planner, GameObject gameObject
         * Return: None
         * Description: Called when any structure is built (walls, ceilings, stairs, etc.).
         */
        void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            // If the argument for planner is null or the argument for gameObject is null then don't proceed any further.
            if (planner == null || gameObject == null)
                return;

            // Obtain the BaseEntity component from the game object a store it into the variable baseEntity.
            BaseEntity baseEntity = gameObject.GetComponent<BaseEntity>();

            // If the BaseEntity component was found or the planner doesn't have an owner then don't proceed any further.
            if (baseEntity == null || planner.GetOwnerPlayer() == null)
                return;

            // Obtain the owner of the planner.
            BasePlayer player = planner.GetOwnerPlayer();

            // If the entity is a High External Wall and the player has wall stacking enabled.
            if (baseEntity.ShortPrefabName.Contains("wall.external.high") == true && this.playerToggleCommand.Contains(player.userID))
            {
                // Declare empty variales.
                BaseEntity linkingEntity;
                ExternalWallController linkingExternalController;
                HashSet<ExternalWallLink> externalLinks;

                // Set the value of the variable externalLinks to the return value of the function CreateStackWall.
                externalLinks = this.CreateStackWall(this.configStackHeight, baseEntity, player);

                // If the list of ExternalWallLink(s) is not empty.
                if(externalLinks.Count > 0)
                {
                    // Create a new ExternalWallLink for the game object and store it in the variable initialExternalLink.
                    ExternalWallLink initialExternalLink = new ExternalWallLink(gameObject);
                    // Add the value of the variable initialExternalLink to the list of ExternalWallLink(s).
                    externalLinks.Add(initialExternalLink);

                    // Go through the list of ExternalWallLink(s).
                    foreach (ExternalWallLink externalLink in externalLinks)
                    {
                        // Set the value of the variable linkingEntity to the current ExternalWallLink('s) BaseEntity.
                        linkingEntity = (BaseEntity)externalLink.entity();

                        // If the BaseEntity component was found.
                        if (linkingEntity != null)
                        {
                            // Add the ExternalWallController to the current game object.
                            linkingExternalController = linkingEntity.gameObject.AddComponent<ExternalWallController>();

                            // Go through the list of ExternalWallLink(s).
                            foreach (ExternalWallLink externalAddLinkage in externalLinks)
                            {
                                // If the current link is the parent current link then continue (skip it).
                                if (externalAddLinkage == externalLink) continue;

                                // Link the other ExternalWallLink(s) to the ExternalWallController.
                                linkingExternalController.addLink(externalAddLinkage);
                            }
                        }
                    }
                }
                
            }

            // Set the value of the variable baseEntity to null.
            baseEntity = null;
        }

        /*
         * Name: OnRemovedEntity
         * Parameters: BaseEntity entity
         * Return: None
         * Description: Called when any structure is removed by the removal tool.
         */
        void OnRemovedEntity(BaseEntity entity)
        {
            // If the BaseEntity is null then don't proceed any further.
            if (entity == null) return;

            // If the BaseEntity is an High External Wall.
            if (entity.ShortPrefabName.Contains("wall.external.high") == true)
            {
                // If the entity is being removed.
                if (this.removingEntity.Contains(entity) == true)
                {
                    // Remove the entity from the list of BaseEntity(s).
                    this.removingEntity.Remove(entity);
                    // Don't proceed any further.
                    return;
                }
                else
                    // If the entity isn't being removed then add it to the list of BaseEntity(s).
                    this.removingEntity.Add(entity);

                // Obtain the ExternalWallController component from the BaseEntity('s) game object.
                ExternalWallController controller = entity.gameObject.GetComponent<ExternalWallController>();

                // If the ExternalWallController component wasn't found then don't proceed any further.
                if (controller == null) return;

                // Declare empty variable.
                BaseEntity linkEntity;

                // Get the owner of the BaseEntity.
                BasePlayer player = BasePlayer.Find(entity.OwnerID.ToString());

                // Go through the list of ExternalWallLink(s) in the ExternalWallController component.
                foreach (ExternalWallLink externalLink in controller.entityLinks())
                {
                    // If the ExternalWallLink isn't being removed.
                    if (externalLink.isRemoving == false)
                    {
                        // Remove the ExternalWallLink.
                        externalLink.isRemoving = true;

                        // Get the BaseEntity that owns the ExternalWallLink component.
                        linkEntity = externalLink.entity();

                        // If the BaseEntity is null then move to the next ExternalWallLink.
                        if (linkEntity == null) continue;

                        // If the player is required to use materials.
                        if (this.configRequireMaterials == true)
                        {
                            // Create the High External Wall and give it to the player.
                            Item item = ItemManager.CreateByName(entity.ShortPrefabName, 1);
                            player.inventory.GiveItem(item, null);
                            player.Command("note.inv", item.info.itemid, item.amount);
                        }

                        // Kill the BaseEntity.
                        linkEntity.Kill(BaseNetworkable.DestroyMode.Gib);
                    }
                    
                }
            }
        }

        /*
         * Name: OnEntityDeath
         * Parameters: BaseCombatEntity entity, HitInfo info
         * Return: None
         * Description: Called when any thing is killed.
         */
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            // If the BaseCombatEntity is null then don't proceed any further.
            if (entity == null) return;

            // If BaseCombatEntity is an High External Wall.
            if (entity.ShortPrefabName.Contains("wall.external.high") == true)
            {
                // Obtain the ExternalWallController component from the BaseCombatEntity.
                ExternalWallController controller = entity.gameObject.GetComponent<ExternalWallController>();

                // Declare an empty variable.
                ExternalWallController controllerLink;

                // If the ExternalWallController component can't be found in the BaseCombatEntity then don't proceed any further.
                if (controller == null) return;

                // Declare an empty variable.
                BaseEntity linkEntity;

                // Go through the list of ExternalWallLink(s) in the ExternalWallController component.
                foreach (ExternalWallLink externalLink in controller.entityLinks())
                {
                    // Get the BaseEntity that owns the ExternalWallLink component.
                    linkEntity = externalLink.entity();

                    // If the BaseEntity is null then move to the next ExternalWallLink.
                    if (linkEntity == null) continue;

                    // Obtain the ExternalWallController component from the current BaseEntity
                    controllerLink = linkEntity.gameObject.GetComponent<ExternalWallController>();

                    // If the ExternalWallController component couldn't be found then move to the next ExternalWallLink.
                    if (controllerLink == null) continue;

                    // Sever link between the BaseCombatEntity and the current BaseEntity.
                    controllerLink.entityLinks().RemoveWhere(link => link.entity() == null || link.entity().gameObject == entity.gameObject);
                }
            }
                
        }

        #endregion

        #region Chat Commands
        /*
         * Name: cmdWStack
         * Parameters: BasePlayer player, string command, string[] args
         * Return: None
         * Description: Called when a player type /wstack in chat.
         */
        [ChatCommand("wstack"), Permission("externalwallstack.wstack")]
        private void cmdWStack(BasePlayer player, string command, string[] args)
        {
            // If the player can't be found then don't proceed any further.
            if (player == null)
                return;

            // If the user doesn't have the permission externalwallstack.wstack and the configuration requires the user to have the permission externalwallstack.wstack.
            if (this.HasRunPermission(player, this.pluginPermission) == false && this.configUsePermission == true)
            {
                // Notify the player that they don't have the permission to run this chat command.
                PrintToChat(player, lang.GetMessage("NotAuthorized", this, null));

                // Don't proceed any further.
                return;
            }

            // If the list of player user id(s) contains the player's user id that is running this command.
            if (this.playerToggleCommand.Contains(player.userID) == true)
            {
                //  Notify the player that they have High External Wall stacking OFF.
                PrintToChat(player, new StringBuilder(this.pluginName).AppendFormat(lang.GetMessage("CommandToggle", this, null), "#CC0000","OFF").ToString());

                // Remove the player's user id from the list of player user id(s).
                this.playerToggleCommand.Remove(player.userID);

                // Don't proceed any further.
                return;
            }

            // Add the player's user id to the list of player user id(s).
            this.playerToggleCommand.Add(player.userID);

            //  Notify the player that they have High External Wall stacking ON.
            PrintToChat(player, new StringBuilder(this.pluginName).AppendFormat(lang.GetMessage("CommandToggle", this, null), "#2C6700","ON").ToString());
        }
        #endregion

    }
}
