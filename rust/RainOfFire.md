[**Donate here**](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=NQYRAPDV676MY)


This plugin adds random meteor showers to the game. Meteors drop resources and have a chance to start a fire.


**Admin commands**
/rof random --- Start random event right now. (does not reset the timed event)
/rof intervals [time] --- Sets the time(seconds) between the events. Set to zero to disable timed events.
/rof droprate [multiplier] --- Sets the multiplier on dropped items. Set to zero to disable item drops.
/rof onplayer "player name" --- Start event on a player's position. If player name is not specified, it will be called on your position.
/rof onplayer_extreme "player name" --- Start fast paced, small area event on a player's position. If player name is not specified, it will be called on your position.
/rof onplayer_mild "player name" --- Start slow paced, large area event on a player's position. If player name is not specified, it will be called on your position.
/rof barrage --- Launches a barrage of rockets in the direction you are looking. (aim this carefully)
/rof togglemsg --- Toggles event notifications.
/rof damagescale [scale] --- Sets the damage done by meteors. 1.0 is the default rocket damage.

**Console commands**
rof.random --- Start random event right now. (does not reset the timed event)
rof.onposition x y z --- Start event at specified coordinates.


Events trigger every 30 minutes by default.

Meteors drop 25-50 Metal Fragments and 80-120 Stones by default.

Event notifications are disabled by default.

Damage scale is 0.2 by default.