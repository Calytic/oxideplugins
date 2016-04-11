This plugin logs the crafter's name, the amount and the item name whenever someone crafts something. When a crafting operation is cancelled, this plugin logs when it was cancelled.

Each crafting operation has an ID that counts up for each player. With this ID, you are able to uniquely identify a crafting operation and which exact crafting operation was cancelled until the user disconnects, which resets the ID.

**Example**

I start crafting 800x Gun Powder. When this is done, I start crafting 200x Gun Powder and cancel after 50x. The log contains:

````
[2/9/2016 9:22:00 PM] [12] sqroot (76561198055024904) started: 800x Gun Powder

[2/9/2016 9:22:35 PM] [13] sqroot (76561198055024904) started: 200x Gun Powder

[2/9/2016 9:22:48 PM] [13] sqroot (76561198055024904) cancelled at: 150x Gun Powder
````

The 13 is a crafting task ID and unique for all crafting operations of each player. Because of this, I can safely say that I indeed cancelled the crafting operation with 200x Gun Powder, and not the crafting operation with 800x Gun Powder.

The 76561198055024904 is my steamID64.

**Configuration**

CraftLog.json contains the configuration for this plugin.
whitelist limits the items to log to the specified items. If the whitelist contains no elements, the whitelist is ignored and all items are logged.

Example: "whitelist": ["Timed Explosive Charge", "Bandage"] only logs crafting operations involving C4 and Bandages.

**Log Conversion**

To convert the log file to JSON, XML, CSV or an SQLite3 database, use the following tool (compiled for Windows, x86-64):
[clconv.exe](http://www.mediafire.com/download/7adgopgahgia155/clconv.exe)


To use the tool, open your command line and enter the following command:
clconv <format> <input> <output>

format is one of json/xml/csv/sqlite3 and the format to parse to.
input specifies the input log.
output specifies the output location.


The source code for this tool can be found here:
[[Go] package main    import (      "bufio"      "database/sql"      "encoding/csv"      "encoding - Pastebin.com](http://pastebin.com/65FmPB7E)


To compile the tool you first have to setup a Go workspace.

* Install Go from [Downloads - The Go Programming Language](https://golang.org/dl/)
* Set the installation directory as an environment variable called GOROOT
* Add GOROOT/bin to your PATH environment variable

* Create a directory where you want your workspace to be
* Inside of that directory, create the subdirectories "src", "pkg" and "bin"
* Set the workspace directory as an environment variable called GOPATHNext you have to install gcc to be able to compile the sqlite3 C library.

* When using Linux, install gcc using a package manager of your choice
* When using Windows, install [TDM-GCC : News](http://tdm-gcc.tdragon.net/) for either 32bit or 64bitNow you have to download the sqlite3 library by opening a command line window and using go get github.com/mattn/go-sqlite3. 

Now we get to the actual building.

* Create a directory "clconv" in GOPATH/src
* Create a file "clconv.go" in that directory
* Paste the source code linked earlier to that file
* Open a command line window in that directory
* Run go buildYou should now have an executable binary for the tool in the clconv directory.