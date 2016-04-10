**Email API** is an API for sending email messages via supported transactional email services. Keep in mind that alone this does nothing, as it's an API meant for plugins to utilize. To use this with a supported plugin, simply install like you would any other plugin!
**Amazon SES**


* **$0.10 per thousand emails**
* Signup at http://aws.amazon.com/ses/



* **Get your API key here!**
* **Configuration:**


````

    "PrivateKey": "yoursecretkey",

    "PublicKey": "youraccesskey",

    "Service": "amazon",

 
````


**Elastic Email Service**


* **First 1,000 emails free, then $$$**
* Signup at https://elasticemail.com/



* **Get your API key here!**
* **Configuration:**


````

    "PrivateKey": "yourapikey",

    "Service": "elastic",

    "Username": "youremail"

 
````


**Mad Mimi Service**


* **Unlimited emails to 100 contacts**
* Signup at https://madmimi.com/



* **Activate Mailer API here!**
* **Get your API key here!**
* **Configuration:**


````

    "PrivateKey": "yourapikey",

    "Service": "madmimi",

    "Username": "changeme"

 
````


**Mailjet Service**


* **First 6,000 emails per month free, then $$$**
* Signup at https://www.mailjet.com/



* **Get your API key here!**
* **Configuration:**


````

    "PrivateKey": "yourpassword",

    "PublicKey": "yourusername",

    "Service": "mailjet",

 
````


**Mailgun Service**


* **First 10,000 emails per month free, then $$$**
* Signup at https://mailgun.com/



* **Get your API key here!**
* **Configuration:**


````

    "Domain": "sandbox12345.mailgun.org",

    "PrivateKey": "yourapikey",

    "Service": "mailgun",

 
````


**Mandrill Service**


* **First 12,000 emails per month free, then $$$**
* Signup at https://mandrill.com/



* **Get your API key here!**
* **Configuration:**


````

    "PrivateKey": "yourapikey",

    "Service": "mandrill",

 
````


**PostageApp Service**


* **First 100 emails per day free (1,000 per month), then $$$**
* Signup at http://postageapp.com/



* **Configuration:**


````

    "PrivateKey": "yourapikey",

    "Service": "postageapp",

 
````


**Postmark Service**


* **First 25,000 initial emails free, then $$$**
* Signup at https://postmarkapp.com/



* **Get your API key here!**
* **Configuration:**


````

    "PrivateKey": "yourservertoken",

    "Service": "postmark",

 
````


**SendGrid Service**


* **First 25,000 emails per month free:** https://sendgrid.com/partner/google
* **First 15,000 emails per month free:** https://sendgrid.com/partner/github-education



* **Configuration:**


````

    "PrivateKey": "yourpassword",

    "Service": "sendgrid",

    "Username": "yourusername"

 
````


**SendinBlue Service**


* **First 300 emails per day free, then $$$**
* Signup at https://sendinblue.com/



* **Get your API key here!**
* **Configuration:**


````

    "PrivateKey": "yourapikey",

    "Service": "sendinblue",

 
````


**Configuration**

You need to set your email provider API keys and other required information for this to work. You can configure all of the settings and messages in the EmailAPI.json file under the oxide/config directory.
**Default Configuration**

````
{

  "Api": {

    "Domain": "",

    "PrivateKey": "",

    "PublicKey: "",

    "Service": "",

    "Username": ""

  },

  "Messages": {

    "InvalidService": "Configured email service is not valid!",

    "MessageRequired": "Message not given! Please enter one and try again",

    "SendFailed": "Email failed to send!",

    "SendSuccess": "Email successfully sent!",

    "SetDomain": "Domain not set! Please set it and try again.",

    "SetPrivateKey": "Private key not set! Please set it and try again.",

    "SetPublicKey": "Public key not set! Please set it and try again.",

    "SetUsername": "Username not set! Please set it and try again.",

    "SubjectRequired": "Subject not given! Please enter one and try again"

  },

  "Settings": {

    "FromEmail": "change@me.tld",

    "FromName": "Change Me",

    "ToEmail": "change@me.tld",

    "ToName": "Change Me"

  }

}
````

The configuration file will update automatically if new options are added or removed. I'll do my best to preserve any existing settings and messages with each new version.
**Plugin Developers**

To call the functions from this API your plugin needs to get the plugin instance.
Code (C):
````
[PluginReference]

Plugin EmailAPI;
````

Code (Lua):
````
local emailApi = plugins.Find("EmailAPI")
````

You can then use this to send notifications using the EmailMessage function.
Code (C):
````
EmailAPI?.Call("EmailMessage", "This is a test email", "This is a test of the Email API!");
````

Code (Lua):
````
emailApi:CallHook("EmailMessage", "This is a test email", "This is a test of the Email API!")
````

The first argument is the email subject, the second argument is the email body.
**Example Plugins**
Code (C):
````
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{

    [Info("Email Test", "Wulf / Luke Spragg", 0.1)]

    [Description("Email API test plugin.")]

    class EmailTest : TheForestPlugin

    {

        [PluginReference]

        Plugin EmailAPI;

        void Loaded()

        {

            if (!EmailAPI)

            {

                Puts("Email API is not loaded! http://oxidemod.org/plugins/1174/");

                return;

            }

            EmailAPI.Call("EmailMessage", "This is a test email", "This is a test of the Email API!");

        }

    }
}
````

Code (Lua):
````
PLUGIN.Title = "Email Test"

PLUGIN.Version = V(0, 0, 1)

PLUGIN.Description = "Email API test plugin."

PLUGIN.Author = "Wulf / Luke Spragg"
function PLUGIN:Init()

    local emailApi = plugins.Find("EmailAPI")

    if not emailApi then print("Email API is not loaded! http://oxidemod.org/plugins/1174/"); return end

    emailApi:CallHook("EmailMessage", "This is a test email", "This is a test of the Email API!")
end
````