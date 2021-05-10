# CustomsQueueBot
Discord.Net bot for managing a user pool for size limited games. (Paladins, Overwatch,  CS:GO, CoD, etc...)


# Installation

** Under Construction **



# Release Notes:

  0.8.1
  
  1. Renamed "Newest" to "New" which combined the functionality of "Next" and "Newest" commands.

  2. Added a 'maximum number of players to be pulled' setting in the bot config. This is mostly to allow for numeric passwords. Default is 10.
  
  3. Began implementing logging services with Serilog. Why not, right?

  0.7.1
  1. Added a minimum number of games to play setting in the bot config. Defaults to 2.
  2. Remodeled the "Random" command. It now just mixes up the player list to be use with "Next".
  3. Fixed "Newest" pulling inactive players.
  4. Added notice embed to "Newest" if pulled list is empty.
  5. Renamed command "qstat" to "stats"
  6. Began implementing SQLite database. Why? Why not? But seriously... Why again?



  0.6f
  
  -Added "Quit" command, which will remove users from the queue list. Used when users have finished playing for the night.
  
  -Added Role checks to reaction handler.
  
  -Added "Config" command, which allows for changing the config file values from the bot. 
  
  -Added "Recall" command, which pings users from the most recently pulled list again as a last call.
  
  -Added "Least" command, which shows players who have played less than 3 games in the queue.
  
  -Fixed bug that randomly added people to the list and would pull them for lobbies.
  
  -Fixed "Next" not working properly. It works properly now. Proper.
  
  -More Improved logic and embed stability!
  
  -Fixed "list" command. It lists properly now.
  
  -"LastCall" command is being completely reworked.

0.6d

-Improved logic and embed stability.

-Last Call disabled for time being.

-Ban and Unban work as intended now.

-Still having issues with list command stopping.



0.6c

...

0.6b

-Made certain mod commands useable with either discord user id or by using an @mention.

-Added Last Call, Ban, Unban, Active, Status commands.

-Last Call, Ban, Unban don't 100% work.

0.5b

-Bot "released".
