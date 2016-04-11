BlueprintSync allows multiple servers to share blueprint data via a MySQL database.


To use this plugin you will have to host a MySQL database somewhere.

Set up a MySQL installation ([MySQL :: Download MySQL Community Server](http://dev.mysql.com/downloads/mysql/)), forward the port you are hosting at and create a bpsync database by entering CREATE DATABASE IF NOT EXISTS bpsync; into the MySQL command prompt.


Next configure your servers to use the same bpsync database as explained below.

**Configuration**

The configuration allows you to configure the location of your database and the access to your database.


* host: Sets the IP where the database is hosted.
* port: Sets the port to use to connect to the database.
* user: Sets the name of the database user to connect with.
* password: Sets the password of the database user to connect with.