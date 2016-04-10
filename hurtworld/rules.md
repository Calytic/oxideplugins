**Server Rules** displays customized rules for your server, with multiple languages supported.

**Commands:**


* /rules - Displays list of rules (default language)
* /rules <list> - Displays all the available callable language rules
* /rules <lang> - Call rules in a specific language, if supported

Click to expand...
**Configuration:**
Code (Java):
````
{

  "MESSAGES": {

    "AVAILABLE LANGS": "Available languages",

    "LANG NOT EXIST": "Could not find <red>{lang}<end> language, type <lime>/rules list<end> for the full list of languages.",

    "TITLE PREFIX": "<orange>•<end> <silver>SERVER RULES<end>:"

  },

  "RULES": {

    "DE": [

      "Cheaten ist verboten!",

      "Respektiere alle Spieler",

      "Spam im Chat zu vermeiden.",

      "Spiel fair und missbrauche keine Bugs oder Exploits."

    ],

    "DK": [

      "Snyd er strengt forbudt.",

      "Respekter alle spillere.",

      "Undgå spam i chatten.",

      "Spil fair og misbrug ikke bugs / exploits."

    ],

    "EN": [

      "Cheating is strictly prohibited.",

      "Respect all players",

      "Avoid spam in chat.",

      "Play fair and don't abuse of bugs/exploits."

    ],

    "ES": [

      "Los trucos están terminantemente prohibidos.",

      "Respeta a todos los jugadores.",

      "Evita el Spam en el chat.",

      "Juega limpio y no abuses de bugs/exploits."

    ],

    "FR": [

      "Tricher est strictement interdit.",

      "Respectez tous les joueurs.",

      "Évitez le spam dans le chat.",

      "Jouer juste et ne pas abuser des bugs / exploits."

    ],

    "HU": [

      "Csalás szigorúan tilos.",

      "Tiszteld minden játékostársad.",

      "Kerüld a spammolást a chaten.",

      "Játssz tisztességesen és nem élj vissza a hibákkal."

    ],

    "IT": [

      "Cheating è severamente proibito.",

      "Rispettare tutti i giocatori.",

      "Evitare lo spam in chat.",

      "Fair Play e non abusare di bug / exploit."

    ],

    "NL": [

      "Vals spelen is ten strengste verboden.",

      "Respecteer alle spelers",

      "Vermijd spam in de chat.",

      "Speel eerlijk en maak geen misbruik van bugs / exploits."

    ],

    "PT": [

      "Usar cheats e totalmente proibido.",

      "Respeita todos os jogadores.",

      "Evita spam no chat.",

      "Nao abuses de bugs ou exploits."

    ],

    "RO": [

      "Cheaturile sunt strict interzise!",

      "Respectați toți jucătorii!",

      "Evitați spamul în chat!",

      "Jucați corect și nu abuzați de bug-uri/exploituri!"

    ],

    "RU": [

      "Запрещено использовать читы.",

      "Запрещено спамить и материться.",

      "Уважайте других игроков.",

      "Играйте честно и не используйте баги и лазейки."

    ],

    "TR": [

      "Hile kesinlikle yasaktır.",

      "Tüm oyuncular Saygı.",

      "Sohbet Spam kaçının.",

      "Adil oynayın ve böcek / açıkları kötüye yok."

    ],

    "UA": [

      "Обман суворо заборонено.",

      "Поважайте всіх гравців",

      "Щоб уникнути спаму в чаті.",

      "Грати чесно і не зловживати помилки / подвиги."

    ]

  },

  "SETTINGS": {

    "COMMAND TRIGGER": "rules",

    "DEFAULT LANGUAGE": "en",

    "ENUMERATE RULES": true,

    "PREFIX": "<white>[<end> <cyan>SERVER RULES<end> <white>]<end>",

    "REMOVE CHAT COLORS": false

  }
}
````