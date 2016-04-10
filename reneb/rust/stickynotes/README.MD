Add sticky notes to locations for other players to read

**Features:**

- For Admins only or also players

- Choose notes to be anonymous

- Choose in how much time should a sticky note auto delete

- Limit notes per players

- easily implement this plugin inside an outer plugin (shop?)

**Player Commands:**

- /note Message => Add a sticky note on your location

- /note => remove the sticky note where you are

**Admins Commands:**

- /note_reset => reset all sticky notes

- /note_count => count the number of sticky notes deployed on your server

- /note_read RADIUS => read all sticky notes around you (default is 30)


````
{

  "Settings": {

    "authLevel": 1,

    "messageName": "Sticky-Note"

  },

  "Messages": {

    "NoteCMD1": "/note - to remove sticky notes where you are standing at",

    "NoteCMDAdmin1": "/note_count - to see how many notes are around the map",

    "NoPermissions": "You do not have the permission to use this command",

    "YouMustSpecifyASteamID": "You must specify a steamID",

    "NewMessage": "You've got a new message from: ",

    "NoteCMDAdmin2": "/note_read RADIUS - Read all notes around you in the radius (default is 30m)",

    "WrongNoteArgument1": "You must add a message after /note",

    "NoMoreStickyNotesLeft": "You are not allowed to add any other sticky notes at the moment",

    "NoteCMDAdmin0": "/note_reset - to reset all notes",

    "CountNotes": " total notes deployed",

    "NoteCMD0": "/note \"Message\" - to add a sticky note where you are for players to see when they come here",

    "SuccessfullyAddedTheStickyNote": "You successfully added the sticky note",

    "NoNotesAroundYou": "No notes were found around your position",

    "NotesDeletedAroundPos": " notes were deleted around your position",

    "SuccessfullyResetNotes": "Successfully resetted all notes",

    "MessageAutoDestroyed": "This sticky note was auto destroyed",

    "NoSourcePlayer": "No players set as adding the note",

    "NoMessageSet": "You didnt put any message"

  },

  "StickyNotes": {

    "maxStickyNotesPerPlayer": 5,

    "zoneRadius": 2,

    "timeBeforeDestroy": 86400,

    "levelForCommandUsage": 0,

    "anonymous": false,

    "overRideLimitAuthLevel": 1

  }

}
````


**WARNING:**

Do note put zoneRadius under 2 meters (1 or 0) as notes will NOT work