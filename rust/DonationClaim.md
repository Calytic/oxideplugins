[GitHub - IsaacOwens/DonationClaim: Oxide plugin for Rust](https://github.com/IsaacOwens/DonationClaim)
**

**If you have updated to v0.5 from any previous version, you must delete your original config file.****


Alright folks.  I've created an automatic donation system for Rust.  It is **fully configurable **(now actually true in 0.5).  **Add as many packages and commands as you like!**


Currently, there is no step-by-step guide for troubleshooting your PayPal account, MySQL server, and PHP web server configuration.  It is recommended that you have previous experience setting up and troubleshooting these components before attempting to use this plugin.  That being said, the provided code should save you a lot of hassle while setting up the donation system.


You must have a PayPal business account to set up IPN notifications.  No business account = this plugin will not work.

**Install Guide**

In your MySQL server, create a database called **'rustserver'**.


Create a new table and stored procedure.

````
CREATE TABLE IF NOT EXISTS rustserver.ibn_table (

    `id` INT(11) NOT NULL AUTO_INCREMENT,

    `itransaction_id` VARCHAR(60) NOT NULL,

    `ipayerid` VARCHAR(60) NOT NULL,

    `iname` VARCHAR(60) NOT NULL,

    `iemail` VARCHAR(60) NOT NULL,

    `itransaction_date` DATETIME NOT NULL,

    `ipaymentstatus` VARCHAR(60) NOT NULL,

    `ieverything_else` TEXT NOT NULL,

    `item_name` VARCHAR(255) DEFAULT NULL,

    `claimed` INT(11) NOT NULL DEFAULT '0',

    `claim_date` DATETIME DEFAULT NULL,

    PRIMARY KEY (`id`)

)  ENGINE=MYISAM AUTO_INCREMENT=9 DEFAULT CHARSET=LATIN1;
````


````
CREATE DEFINER=`root`@`localhost` PROCEDURE rustserver.claim_donation(IN email_address VARCHAR(255))

BEGIN


set email_address = REPLACE(email_address,'@@','@');


set @ID = (

select    IBN.id

from    rustserver.ibn_table as IBN

where    IBN.iemail = email_address

        and IBN.claimed = 0

        and IBN.claim_date IS NULL

        and IBN.ipaymentstatus = "Completed"

ORDER BY IBN.itransaction_date DESC

LIMIT 1);


UPDATE rustserver.ibn_table

SET    claimed = 1, claim_date = NOW()

WHERE id = @ID;



select    IBN.item_name

from    rustserver.ibn_table as IBN

where    IBN.id = @ID;


END
````

Next, you must deploy two PHP files on a web server.  The first is the actual PayPal IPN listener, and the other is where your MySQL credentials will be stored.


IPN Listener code:


````
<?php


include 'MySQLCredentials.php';

//read the post from PayPal system and add 'cmd'

$req = 'cmd=_notify-validate';


foreach ($_POST as $key => $value) {

    $value = urlencode(stripslashes($value));

    $req .= "&$key=$value";

}


//post back to PayPal system to validate

$header = "POST /cgi-bin/webscr HTTP/1.1\r\n";

$header .= "Content-Type: application/x-www-form-urlencoded\r\n";

$header .= "Host: www.paypal.com\r\n"; //Change this to www.sandbox.paypal.com\r\n for testing

$header .= "Connection: close\r\n";

$header .= "Content-Length: " . strlen($req) . "\r\n\r\n";

$fp = fsockopen ('ssl://www.paypal.com', 443, $errno, $errstr, 30);  //Change this to ssl://www.sandbox.paypal.com for testing

//


//error connecting to paypal

if (!$fp) {

    //

}


//successful connection

if ($fp) {

    fputs ($fp, $header . $req);


    while (!feof($fp)) {

        $res = fgets ($fp, 1024);

        $res = trim($res); //NEW & IMPORTANT


        if (strcmp($res, "VERIFIED") == 0) {

            $transaction_id = $_POST['txn_id'];

            $payerid = $_POST['payer_id'];

            $firstname = $_POST['first_name'];

            $lastname = $_POST['last_name'];

            $payeremail = $_POST['payer_email'];

            $paymentdate = $_POST['payment_date'];

            $paymentstatus = $_POST['payment_status'];

            $mdate= date('Y-m-d h:i:s',strtotime($paymentdate));

            $otherstuff = json_encode($_POST);

            $item_name = $_POST['item_name'];

            $conn = new mysqli($dbhost,$dbusername,$dbpassword);

            if ($conn->connect_error) {

                trigger_error('Database connection failed: '  . $conn->connect_error, E_USER_ERROR);

            }

            // insert in our IPN record table

            $query = "INSERT INTO rustserver.ibn_table

            (itransaction_id,ipayerid,iname,iemail,itransaction_date, ipaymentstatus,ieverything_else,item_name)

            VALUES

            ('$transaction_id','$payerid','$firstname $lastname','$payeremail','$mdate','$paymentstatus','$otherstuff','$item_name')";

            $result = $conn->query($query);


            $conn->close();

        }


        if (strcmp ($res, "INVALID") == 0) {

            //insert into DB in a table for bad payments for you to process later

        }

    }


    fclose($fp);

}




?>
````

MySQLCredentials.php


````
<?php


    $dbusername     = 'root'; //db username

    $dbpassword     = 'yourpassword'; //db password

    $dbhost     = 'localhost'; //db host

    $dbname     = 'rusterver'; //db name


?>
````

Configure the MySQLCredentials.php file to match your username, password, and host (IP).


You may test your IPN listener at this point on PayPal.com using the IPN simulator.  You will need to change the IPN listener PHP code to accept messages from the PayPal sandbox servers (sandbox.paypal.com).  **Just make sure to remove sandbox. from those URLs before you go live.**


Check your MySQL database to make sure rows are being inserted every time you simulate an IPN.


Donation Claim recognizes donation packages by the "item_name" attribute from PayPal.  You can designate the item name when you set up a PayPal button link on the PayPal website.  Note for later in the installation process: Be sure that the item name in your donation link matches the Donation Claim config item names.


After testing and confirming that your IPN listener is working, you can add the notification URL (your IPN listener PHP page) to your PayPal account ([instructions](https://developer.paypal.com/docs/classic/ipn/integration-guide/IPNSetup/)).


Install the Donation Claim plugin on your Rust server.  Open the DonationClaim config file and add your MySQL credentials:


````

   "MySQLDatabase":"rustserver",

   "MySQLIP":"localhost",

   "MySQLpassword":"",

   "MySQLPort":3306,

   "MySQLusername":"root",

  ...
````

Add a package in the config using the format used in example packages.  **Remember:** this must match the item name you designated in the PayPal button or link.

````
  ...

      "VIP":[

  ...
````

Then, add console commands that correspond to the package.  Use the **{0}** delimiter in place of the player name.


````
  ...

         "grant user {0} airstrike.buystrike",

         "grant user {0} canhorse",

         "grant user {0} canchicken",

         "grant user {0} canboar",

         "grant user {0} canstag",

         "grant user {0} canwolf",

         "grant user {0} canbear",

         "grant user {0} backpack.use",

         "grant user {0} helivote.use",

         "grant user {0} nodurability.allowed"

  ...

 
````

Add as many packages and commands as you like! (updated in 0.5)


If you're having problems with the config, try using a tool like [JSON Formatter & Validator](https://jsonformatter.curiousconcept.com/)


Reload the Donation Claim plugin to update the configs.


After a player donates, they can open chat and type **/claimreward**, followed by their PayPal email address (e.g. **/claimdonation [johnsmith@gmail.com](mailto:johnsmith@gmail.com)**).