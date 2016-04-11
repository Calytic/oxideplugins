This plugin is using for AutoReply players with some information when  they messages have an keyword.

By befault this plugin have keywords set up for replying on "wipe" key.

How does it work:

If player have word what is in this plugin dictionary in his message, this message will be blocked and player will receive preset info right instead of his message.

By default it will looks like this:

1. Player says: "When will be next wipe?"

2. Plugin will answer him: "Last wipe: ??? Next wipe ???" you can replace those "???" by anything you want.

3. Chat is cleared from those "wipe requestors... etc"



You can set your own words by yourself, it's kinda messy right now but order is like that:
1. Adding new wordgroup:
/ar_new groupname baseword reply - let's figure out what does those parameters mean.
groupname - you will use this in future edition of this new word rule (this is not the word itself)
baseword - first word for check for this group, (yes you can have more then one word for same reply).
reply - This is message what player will get. Synt is: "TEST {lastwipe}" where {lastwipe} is optional param what you can set up later (list of pre-set params will be at the bottom).
Example: You want to add auto server-time reply:
/ar_new time whattime "TIME: {gametime}"
Result of adding this rulle willbe:

if player write "whattime" in chat - plugin will auto reply him with current in-gmae time (because "gametime" param is pre-set);

2. Adding new word for check in existing wordgroup:
/ar_word add groupname word

where:
add - mode name (it can also be "del" - this will remove word from wordgroup)
groupname - the name of youp you added before
word - word itself
Example: 
/ar_word add groupname  time timenow
Result:

When player will type "timenow" - plugin will reply with: "TIME: 22:00:00" or whatever time is now.

3. Changing reply message and adding new attribute to it:

/ar_reply set groupname ReplyNum newreply

I hope it's starting to be clear now, but still:
groupname - the name of youp you added before
ReplyNum - number of reply for changing
newreply - reply in format: "TEXT {online} {sleepers}" where "{online}"  is optional params(or attribute, whatever you call it) (C#6 syntax)

To add new attribute:
/ar a add attribname textvalue

where
a - worting with attributes
add - adding new one (you also can set or del them)
attribname - desired attribute name
textvalue - that's what players will see

Example:

Adding 2 new attributes:
/ar a add site "[www.mysite.org](http://www.mysite.org)"

/ar a add lastnews "New item will be added"


adding new rule using just added attributes
/ar_new news sitenews "Server site: {site}, latest news: {lastnews}"


And now edit reply message:
/ar_reply news 0 "Please visit server site: {site}, read fresh news: {lastnews}"

Where 0 - it's the number of reply (check /ar_list)

Result:

If player write sitenews, plugin will reply:

"Please visit server site: [www.mysite.org](http://www.mysite.org), read fresh news: New item will be added"

5. Adding new character to auto-replace:

Some of tricky player trying to avoid this plugin by using characters from another alphabet:

to add new replacement:
/ar c add k o

Now all characters k will be replaced with o before check (they will not be replaced in players messages if check fails), mostly this filter is setted up.

6. Printing existed rules:

/ar_list  - prints all wordgroups
/ar_list attr - prints attribs mapping.
/ar_info groupname - info about selected word group: words, replies, attribs

7. Pre-set attributes:

player - prints player name
time - prints server time
lastwipe, nextwipe - by default prints ??? but can be eddited
gametime - in-game time
online - current online
sleepers - current sleepers


Config params:
"replyInterval": 5, - interval how often player can get reply message for every group.
 "minPriveledge": 0, - max auth level by being tracked with this plugin (0 for players, 1 - players + moders, 2 - all include admin)
"ReplyName": "AutoReply" - default name for all replies.

8. Matching logic:

/ar_match groupname true/false

this change logic of players message matching:
false - as it was before: if any part of message match any word in group = block;
true - players message should fully match any word in group

If you found any bug or error, please reply with log and situation where you got it.



All donations are apreciated! Thanks for supporting my work!

You can [donate here](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=843Z3T75ZZWVG)