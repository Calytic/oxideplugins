Example of how to modify it so first open the plugin in any text editor and find the below line and replace 76561200445525877 with your 64-bit stream id.

````
        ulong[] IDS = {76561200445525877};

        string[] NameColours = { "green" };

        string[] TextColours = { "purple" };
````


How to add multiple users:

````
        ulong[] IDS = {76561200445525877, 76561200445525899};

        string[] NameColours = { "green", "red" };

        string[] TextColours = { "purple", "green" };
````


**Note: **with the colour names like green you can also enter hex codes.