using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("CodeLockNerf", "Kyrah Abattoir", "0.1", ResourceId = 1873)]
    [Description("If you die, your character can forget your door codes, better keep that notepad ready.")]
    class CodelockNerf : RustPlugin
    {
        #region CONFIGURATION

        int _cfgMaxUniqueCodes;//How many unique CODES can the player always remember (-1) to disable.
        int _cfgCodeForgetRate;//Percentage of chance to forget a CODE from 0% to 100%

        int _cfgMaxUniqueLocks;//How many unique DOORS can the player always remember (-1) to disable.
        int _cfgLockForgetRate;//Percentage of chance to forget a LOCK from 0% to 100%

        //If true, outputs the door/codes forgotten stats in the server console.
        //Useful for admins.
        bool _cfgOutputstats;

        protected override void LoadDefaultConfig()
        {
            Config["MaxUniqueCodes"] = _cfgMaxUniqueCodes = GetConfig("MaxUniqueCodes", -1);
            Config["CodeForgetChance"] = _cfgCodeForgetRate = GetConfig("CodeForgetChance", 100);

            Config["MaxUniqueLocks"] = _cfgMaxUniqueLocks = GetConfig("MaxUniqueLocks", 0);
            Config["LockForgetChance"] = _cfgLockForgetRate = GetConfig("LockForgetChance", 100);

            Config["OutputStats"] = _cfgOutputstats = GetConfig("OutputStats", true);
        }

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"MsgDied", "{0} died:"},
                {"MsgForgetCode", "forgot {0}/{1} code(s)"},
                {"MsgForgetLock", "forgot {0}/{1} lock(s)"},
            }, this);
        }
        #endregion

        bool _isLoaded = false;




        List<CodeLock> _codelocks;
        readonly FieldInfo _whitelistField = typeof(CodeLock).GetField("whitelistPlayers", (BindingFlags.Instance | BindingFlags.NonPublic));
        readonly FieldInfo _codeField = typeof(CodeLock).GetField("code", (BindingFlags.Instance | BindingFlags.NonPublic));

        //Get all the codelocks on server start/plugin reload.
        void OnServerInitialized()
        {
            _cfgCodeForgetRate = Mathx.Clamp(_cfgCodeForgetRate, 0, 100);
            _cfgLockForgetRate = Mathx.Clamp(_cfgLockForgetRate, 0, 100);
            _codelocks = UnityEngine.Object.FindObjectsOfType<CodeLock>().ToList();
            _isLoaded = true;
        }

        void Loaded()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
        }

        //Adds newly spawned codelocks.
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!_isLoaded)
                return;
            if (!(entity is CodeLock))
                return;
            CodeLock lck = entity as CodeLock;
            if (!_codelocks.Contains(lck))
                _codelocks.Add(lck);
        }

        void OnEntityDeath(BaseEntity entity, HitInfo hitinfo)
        {
            if (!(entity is BasePlayer))
                return;
            BasePlayer player = (BasePlayer)entity;

            //clear codelocks that are gone
            _codelocks.RemoveAll(o => o == null);

            List<CodeLock> known_locks = new List<CodeLock>();
            List<string> known_codes = new List<string>();

            //populate our accounting lists.
            foreach (var codelock in _codelocks)
            {
                if (codelock == null)
                    continue;

                var whitelist = (List<ulong>)_whitelistField.GetValue(codelock);
                var code = (string)_codeField.GetValue(codelock);

                if (whitelist.Contains(player.userID))
                {
                    known_locks.Add(codelock);
                    if (code != null && !known_codes.Contains(code))
                        known_codes.Add(code);
                }
            }

            //shuffle the lists
            var r = new System.Random();
            known_codes.Shuffle((uint)r.Next());
            known_locks.Shuffle((uint)r.Next());

            var locks = known_locks.Count;
            var forgotten_locks = 0;
            var codes = known_codes.Count;
            var forgotten_codes = 0;

            //we know too many codes, time to forget some.
            if ((_cfgMaxUniqueCodes >= 0 && known_codes.Count > _cfgMaxUniqueCodes))
            {
                //1. remove codes randomly.
                var i = known_codes.Count;
                while (i-- > 0)
                {
                    if (known_codes.Count <= _cfgMaxUniqueCodes)
                        break;

                    if (r.Next(1, 100) <= _cfgCodeForgetRate)
                    {
                        known_codes.RemoveAt(i);
                        forgotten_codes++;
                    }
                }

                //2. we remove all doors that match the forgotten codes.
                i = known_locks.Count;
                while (i-- > 0)
                {
                    CodeLock codelock = known_locks[i];
                    if (!known_codes.Contains((string)_codeField.GetValue(codelock)))
                    {
                        var whitelist = (List<ulong>)_whitelistField.GetValue(codelock);
                        whitelist.Remove(player.userID);
                        known_locks.RemoveAt(i);
                        forgotten_locks++;
                    }
                }
            }

            //we know too many doors, time to forget some.
            if ((_cfgMaxUniqueLocks >= 0 && known_locks.Count > _cfgMaxUniqueLocks))
            {
                var i = known_locks.Count;
                while (i-- > 0)
                {
                    if (known_locks.Count <= _cfgMaxUniqueLocks)
                        break;
                    CodeLock codelock = known_locks[i];
                    if (r.Next(1, 100) <= _cfgLockForgetRate)
                    {
                        var whitelist = (List<ulong>)_whitelistField.GetValue(codelock);
                        whitelist.Remove(player.userID);
                        known_locks.RemoveAt(i);
                        forgotten_locks++;
                    }
                }
            }

            if (!_cfgOutputstats)
                return;

            //Display stats.

            if (forgotten_locks == 0 && forgotten_codes == 0)
                return;

            string message = string.Format(GetMessage("MsgDied"),player.displayName);
            if (forgotten_codes > 0)
                message += " "+string.Format(GetMessage("MsgForgetCode"),forgotten_codes,codes);
            if (forgotten_locks > 0)
                message += " "+string.Format(GetMessage("MsgForgetLock"),forgotten_locks,locks);
            Puts(message+".");
        }

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);
    }
}