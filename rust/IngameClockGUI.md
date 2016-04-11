This plugin displays ingame and server time.

**How to use it:
/clock** — Toggle clock (by default the clock is showing)
**/clock server** — Toggle between ingame or server time (you also can use "/clock s")

**Config (**default values in square brackets**):**
**ShowSeconds **[false]** —  **Toggles showing of seconds
**UpdateTimeInSeconds **[2]** — **How fast plugin will be updating a time
**ServerTime **[false] — Display server time instead of ingame time
**PreventChangingTime **[false] — Disable change time function (ingame/server time) for players
**TimeFormat **[24] — 12/24-hour time format
**

Position** — Changes position of clock
**Size** — Changes size of clock
**FontSize **— Changes size of text in clock
**TextColor **= RGBA(float) — Changes text color of clock
**BackgroundColor **= RGBA(float) — Changes background color of clock
**Prefix/Postfix** — text before/after clock

**Notifications (test mode):**

In the config file in TimedInfo add your notification in the form of "[start time - end time] Text".

Prefix "s" is for server time.
Example:

````
[6:00 - 9:00] Raid time!

s[12:58-13:00] Something
````