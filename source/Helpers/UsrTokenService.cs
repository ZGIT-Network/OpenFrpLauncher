using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using static QRCoder.PayloadGenerator;

namespace OpenFrp.Launcher.Helpers
{
    internal static class UsrTokenService
    {
        private static string Token
        {
            get => App.Settings.Token;
            set => App.Settings.Token = value;
        }
        internal static ObservableCollection<Model.PlatformUser> PlatformUserCache = new ObservableCollection<Model.PlatformUser> { };
        
        public static void SaveUser()
        {
            WriteConfig();
            App.Settings.Save();
        }

        public static string? GetCurrentUserAutoLogonId(string name)
        {
            for (int i = 0; i < PlatformUserCache.Count; i++)
            {
                if (PlatformUserCache[i].Username == name)
                {
                    return PlatformUserCache[i].AutoLoginId;
                }
            }
            return null;
        }

        public static Model.PlatformUser? GetUserFromAutoLoginId(string autoLoginId)
        {
            for (int i = 0; i < PlatformUserCache.Count; i++)
            {
                if (PlatformUserCache[i].AutoLoginId == autoLoginId)
                {
                    return PlatformUserCache[i];
                }
            }
            return null;
        }

        internal static void WriteConfig()
        {
            if (PlatformUserCache.Any())
            {
                App.Settings.Token = System.Text.Json.JsonSerializer.Serialize(PlatformUserCache.Select(x => new OpenFrp.Launcher.Properties.Settings.UserProperty(x)));
            }
            else
            {
                App.Settings.Token = "[]";
            }
        }

        public static void AddUser(string name,string email,string authorization, bool saveNow = false)
        {
            if (!PlatformUserCache.Any(x => x.Username == name))
            {
                PlatformUserCache.Add(new Model.PlatformUser
                {
                    EmailAddress = email,
                    UserAuthorzation = authorization,
                    Username = name
                });
            }
            else
            {
                for (int i = 0; i < PlatformUserCache.Count; i++)
                {
                    if (PlatformUserCache[i].Username == name)
                    {
                        PlatformUserCache[i].EmailAddress = email;
                        PlatformUserCache[i].UserAuthorzation = authorization;
                        break;
                    }
                }
            }
            if (saveNow)
            {
                SaveUser();
            }
        }

        public static void AddUser(Model.PlatformUser user,bool saveNow = false)
        {
            if (!PlatformUserCache.Any(x => x.Username == user.Username))
            {
                PlatformUserCache.Add(user);
            }
            else if (!string.IsNullOrEmpty(user.Username) && !string.IsNullOrEmpty(user.EmailAddress) && !string.IsNullOrEmpty(user.UserAuthorzation))
            {
                AddUser(user.Username!,user.EmailAddress!,user.UserAuthorzation!,saveNow);
            }
            if (saveNow)
            {
                SaveUser();
            }
        }

        public static void RemoveUser(Model.PlatformUser user, bool saveNow = false)
        {
            RemoveUser(user.Username, saveNow);
        }

        public static void RemoveUser(string username, bool saveNow = false)
        {
            lock (PlatformUserCache)
            {
                foreach (var pf in PlatformUserCache)
                {
                    if (pf.Username == username)
                    {
                        PlatformUserCache.Remove(pf);
                        break;
                    }
                }
            }
            if (saveNow)
            {
                SaveUser();
            }
        }

        public static void RefreshPlatformUsers()
        {
            try
            {
                var jsp = System.Text.Json.JsonSerializer.Deserialize<OpenFrp.Launcher.Properties.Settings.UserProperty[]>(Token);

                if (jsp is { })
                {
                    if (jsp.Length > 0)
                    {
                        PlatformUserCache = new ObservableCollection<Model.PlatformUser>(jsp.Select(x => new Model.PlatformUser(x)));
                    }

                    return;
                }
            }
            catch
            {

            }
            if (PlatformUserCache.Count > 0)
            {
                PlatformUserCache.Clear();
            }
        }
    }
}
