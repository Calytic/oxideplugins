This plugin will help developers to use crontab expressions for their scheduled works with world clock. For example you want to call a function @ 12 AM Everyday you just need to use following code.(Test3() Function)


Function


Register & Run the Function @ Next Occurrence time.
bool RegisterCron(Plugin Owner,bool UseUTC, string CronSyntax, Action Function)


Get Next Occurrence time in string/DateTime
object GetNextOccurrence(bool UseUTC, string CronSyntax, bool IsString = true)


Example Code

````

//Microsoft

using System;

using System.Collections.Generic;

using System.ComponentModel;

using System.Text;


//Oxide

using Oxide.Core;

using Oxide.Core.Plugins;


namespace Oxide.Plugins

{

    [Info("Cron Test Plugin", "Feramor", "1.0.0")]

    [Description("Testing for unloaded plugins...")]

    public class CronTest : RustPlugin

    {

        private static Core.Logging.Logger RootLogger = Interface.GetMod().RootLogger;


        [PluginReference]

        private Plugin CronLibrary;

        void Test()

        {

            RootLogger.Write(Core.Logging.LogType.Warning, "Run Every 1 Minute : {0}", DateTime.UtcNow.ToString());

        }

        void Test2()

        {

            RootLogger.Write(Core.Logging.LogType.Warning, "Run Every 2 Minute : {0}", DateTime.UtcNow.ToString());

        }

        void Test3()

        {

            RootLogger.Write(Core.Logging.LogType.Warning, "Run Every Night @ 00:00 : {0}", DateTime.UtcNow.ToString());

        }

        void OnServerInitialized()

        {

            if (CronLibrary != null && CronLibrary.IsLoaded)

            {

                CronLibrary?.CallHook("RegisterCron", this, true, "* * * * *", new Action(() => Test()));

                CronLibrary?.CallHook("RegisterCron", this, true, "*/2 * * * *", new Action(() => Test2()));

                CronLibrary?.CallHook("RegisterCron", this, true, "0 0 * * *", new Action(() => Test3()));

            }

            else

                RootLogger.Write(Core.Logging.LogType.Error, "CronLibrary Plugin is not installed...");

        }

    }

}

 
````

Cron Syntax

````
* * * * *

- - - - -

| | | | |

| | | | +----- day of week (0 - 6) (Sunday=0)

| | | +------- month (1 - 12)

| | +--------- day of month (1 - 31)

| +----------- hour (0 - 23)

+------------- min (0 - 59)
````

For more information about Cron Syntax you can check several websites. [Cron - Wikipedia, the free encyclopedia](https://en.wikipedia.org/wiki/Cron)


You can create Cron Syntax easily with [Cron generator crontab | Online network tools](http://cron.nmonitoring.com/cron-generator.html)