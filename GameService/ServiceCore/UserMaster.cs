using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public class UserMaster
    {
        static UserMaster s_instance = new UserMaster();
        public static UserMaster Get()
        {
            return s_instance;
        }

        Dictionary<string, KokkaKoroUser> m_users = null;

        // Throws if fails!
        public async Task<bool> ValidateUserPasscode(KokkaKoroUser user)
        {
            if(String.IsNullOrEmpty(user.Passcode) || String.IsNullOrWhiteSpace(user.UserName))
            {
                return false;
            }

            // This function will return a copy of the user we will use for auth.
            // It will also hash the password on the original user.
            user = HashAndPrepareUser(user);

            // Make sure we are ready.
            await EnsureUserDict();

            bool addedUser = false;
            lock(m_users)
            {
                if(m_users.ContainsKey(user.UserName))
                {
                    // This user already exists, check if the passcodes match.
                    KokkaKoroUser localUser = m_users[user.UserName];
                    return user.Passcode.Equals(localUser.Passcode);                    
                }
                else
                {
                    // The user doesn't exist, add them.
                    m_users.Add(user.UserName, user);
                    addedUser = true;
                }
            }

            if(addedUser)
            {
                await UploadUserList();
            }

            return true;
        }

        private KokkaKoroUser HashAndPrepareUser(KokkaKoroUser user)
        {
            // Create a copy that we will use for auth. We don't want to change the current user object except the password.
            KokkaKoroUser newUser = new KokkaKoroUser() { Passcode = user.Passcode, UserName = user.UserName };

            // Make user name not case sensitive.
            newUser.UserName = newUser.UserName.ToLower().Trim();
            string unique = newUser.UserName + "." + newUser.Passcode;

            // Hash the passcode.
            using (var sha = new System.Security.Cryptography.SHA256Managed())
            {
                byte[] textData = System.Text.Encoding.UTF8.GetBytes(unique);
                byte[] hash = sha.ComputeHash(textData);
                newUser.Passcode = BitConverter.ToString(hash).Replace("-", String.Empty);
            }

            // Replace the og user pass with the hash as well. Just so it doesn't leak.
            user.Passcode = newUser.Passcode;

            return newUser;
        }

        // Throws if fails!
        private async Task EnsureUserDict()
        {
            if(m_users == null)
            {
                List<KokkaKoroUser> users = await StorageMaster.Get().GetUserList();
                m_users = new Dictionary<string, KokkaKoroUser>();
                lock (m_users)
                {
                    foreach (KokkaKoroUser user in users)
                    {
                        m_users.Add(user.UserName, user);
                    }
                }
            }
        }

        private async Task UploadUserList()
        {
            List<KokkaKoroUser> users = new List<KokkaKoroUser>();
            lock(m_users)
            {
                foreach(KeyValuePair<string, KokkaKoroUser> p in m_users)
                {
                    users.Add(p.Value);
                }
            }

            // Since we updated our user table, write it back out.
            await StorageMaster.Get().SetUserList(users);
        }
    }
}
