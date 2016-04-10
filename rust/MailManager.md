**Introduction**

This is my first plugin and im happy to share it with you.

I would like to thank, unbeknownst to them, LaserHydra for his Admin Tickets, which was a great blueprint for this plugin and Domestos for his FriendsAPI and Private Messaging, which helped me learn what I needed to accomplish this plugin.
**

Description**

Allows players to send, receive and manage in-game mail.

**Notes**


* Use console commands to bypass new chat limit for large messages.
* Use "++" for new lines in messages.
* UsePermissions applies to mail.read and mail.send only.



**Permissions**

This plugin uses oxide permissions.


mailmanager.read - Allows players to read messages

mailmanager.send - Allows players to send messages

mailmanager.nocd - Allows players to bypass send and reply cooldown

mailmanager.admin - Allow players to send messages to all players and bypass limits

````
grant <group|user> <name|id> mailmanager.read

revoke <group|user> <name|id> mailmanager.read
````


**Usage for players
**

Chat

/mail - View help

/mail reminder - Enable or disable new mail reminder

/mail limits - View mail limits

/mail inbox - View all messages in inbox

/mail read <id> - Read message with ID

/mail preview <subject> <message> - Preview message before sending

/mail send <player> <subject> <message> - Send new message to player

/mail reply <message> - Reply to last message read

/mail forward <player> <id> - Forward message id to player

/mail delete <id> - Delete message with ID

/mail deleteall - Delete all messages in inbox (cannot be undone)

/mail console - View available console commands


Console

mail.preview <subject> <message> - Preview message before sending

mail.send <player> <subject> <message> - Send new message to player

**Usage for administrators**

/mail globalsend <subject> <message> - Send new message to all players

/mail globaldelete - Delete all mail for all players (cannot be undone)

/mail <group | clan> <group_name | clan_name> <subject> <message> - Send new message to group or clan

**Configuration file**

````
{

  "Messages": {

  "ChangedStatus": "New mail reminder <color=#cd422b>{status}</color>.",

  "CheckConsole": "Your mail information has been sent to the console.  <color=#cd422b>Press F1</color> to view it.",

  "CoolDown": "You must wait <color=#cd422b>{cooldown} seconds</color> before sending another message.",

  "DeleteAll": "All messages successfully deleted.",

  "Deleted": "Message with ID <color=#cd422b>{id}</color> successfully deleted.",

  "GlobalDelete": "Mail for all players has been deleted.",

  "GroupNotFound": "The <color=#cd422b>{group}</color> was not found.",

  "MultiPlayer": "Multiple players found.  Provide a more specific username.",

  "NewMail": "You received new mail from <color=#cd422b>{player}</color> with subject <color=#f9169f>{subject}</color>.  Use <color=#cd422b>/mail read {id}</color> to read it.",

  "NoMail": "You do not have any mail.",

  "NoPlayer": "Player not found.  Please try again.",

  "NoReply": "You must read a message before you can reply.",

  "NotDeleted": "No message with ID <color=#cd422b>{id}</color> found.",

  "NotNumber": "The ID must be a number.",

  "NotRead": "No mail with ID <color=#cd422b>{id}</color> found.",

  "ReadOnlyNewMail": "You received new mail from <color=#cd422b>{player}</color> with subject <color=#f9169f>{subject}</color>.  Use <color=#cd422b>/mail read {id}</color> to read it.  You have read only permission and will not be able to respond.",

  "ReadOnlyReminder": "You have new mail.  Use <color=#cd422b>/mail inbox</color> to view them.  You have read only permission and will not be able to respond.",

  "ReadOnlySent": "Your message was successfully sent to <color=#cd422b>{player}</color> with subject <color=#f9169f>{subject}</color>.  <color=#cd422b>{player}</color> has read only permission and will not be able to respond.",

  "Reminder": "You have new mail.  Use <color=#cd422b>/mail inbox</color> to view them.",

  "Self": "You cannot send mail to yourself.",

  "SelfInboxFull": "<color=#cd422b>Your inbox is currently full.  You will not be able to receive new mail until you delete old messages.</color>",

  "SelfNoPermission": "You do not have permission to use this command.",

  "SendAll": "Your message was successfully sent to <color=#cd422b>{total} players</color> with subject <color=#f9169f>{subject}</color>.",

  "SendAllFail": "Your message was not sent to any players.  No players found or incorrect player permissions.",

  "SendAllGroup": "Your message was successfully sent to <color=#cd422b>{total} players</color> in <color=#cd422b>{group}</color> with subject <color=#f9169f>{subject}</color>.",

  "SenderInboxFull": "Your message could not be sent to <color=#cd422b>{player}</color>.  Their inbox is currently full.",

  "SenderNoPermission": "Your message could not be sent to <color=#cd422b>{player}</color>.  They do not have the required permissions.",

  "Sent": "Your message was successfully sent to <color=#cd422b>{player}</color> with subject <color=#f9169f>{subject}</color>.",

  "WrongArgs": "Syntax error.  Use <color=#cd422b>/mail</color> for help.",

  "WrongArgsConsole": "Syntax error.  Use <color=#cd422b>/mail console</color> for help."

  },

  "Settings": {

  "AllowConsoleSend": "true",

  "CoolDown": "30",

  "EnablePopup": "true",

  "InboxLimit": "15",

  "MessageSize": "13",

  "Prefix": "[<color=#cd422b>Mail Manager</color>]",

  "Reminder": "true",

  "ReminderInterval": "300",

  "SendToChat": "true",

  "SendToConsole": "true",

  "Timestamp": "MM/dd/yyyy @ h:mm tt",

  "UsePermissions": "true"

  }

}
````

Configuration file will be created and updated automatically.

**More to come**


* Your suggestions


**Known Issues**


* None