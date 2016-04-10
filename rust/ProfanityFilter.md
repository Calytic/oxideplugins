Censor / kick / ban / temp ban / ignore swear words. For toggling the modes just simply edit the script with your Notepad/Notepad++. By default it temp bans players for 30 minutes.

**Configuration**

TempBanOnBadWords  -  This will enable temp bans when set to true

BanOnBadWord  -  This will ban forever when set to true

KickOnBadWord  -  This will kick when set to true

TempBanTime  -  Time to ban 1 = 30 minutes, 2 = 60 minutes  ect...


Also, if you want to just ignore the text set TempBanOnBadWords,  BanOnBadWord, and KickOnBadWord to false and it will just be ignored.


To add words to look for IllegalWords in the script and at the end and add

````
, "new word"
````

Be sure no capitals are used as the player chat is converted to lower case.