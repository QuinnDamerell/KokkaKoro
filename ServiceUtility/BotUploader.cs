using KokkaKoro;
using Newtonsoft.Json;
using ServiceProtocol.Common;
using ServiceProtocol.Requests;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace ServiceUtility
{
    class BotUploader
    {
        readonly string c_botInfoFileName = "botinfo.json";

        public async Task DoUpload(Logger log, Service service, string optPath = null)
        {
            string path = optPath;
            if (String.IsNullOrWhiteSpace(path))
            {
                if(log.GetDecission("No path passed, assume the current directory is the bot directory"))
                {
                    // For now, assume the current folder.
                    path = Directory.GetCurrentDirectory();
                }
                else
                {
                    path = log.GetString("Enter a bot file path");
                }               
            }

            log.Info($"Bot path set to [{path}]");

            KokkaKoroBot bot = null;
            while (bot == null)
            {
                if (DoesLocalBotInfoExist(path))
                {
                    log.Info("Found local bot info file...");
                    bot = GetLocalBotFile(path);
                    if (bot == null)
                    {
                        if(log.GetDecission("The bot file can't be read, do you want to delete it and make a new one"))
                        {
                            DeleteLocalBotInfo(path);
                        }
                        else
                        {
                            break;
                        }
                    }
                    if(!bot.IsValid())
                    {
                        if (log.GetDecission("The bot file isn't valid, do you want to delete it and make a new one"))
                        {
                            DeleteLocalBotInfo(path);
                        }
                        else
                        {
                            break;
                        }
                    }
                    if(log.GetDecission("We found a valid bot info file, would you like to bump the revision number"))
                    {
                        bot.Revision++;
                    }
                    // We got it.
                    continue;
                }
                else
                {
                    log.Info("No bot info file was found, making a new one...");
                    var dlls = ListDlls(path);
                    if (dlls.Count == 0)
                    {
                        log.Info("No dlls were found in the directory, we failed to upload the new bot.");
                        return;
                    }
                    string botName = log.GetString("Please enter a bot name");
                    string password = log.GetString("Please enter a password to protect the bot upload");
                    log.Info();
                    int count = 1;
                    foreach(string dll in dlls)
                    {
                        log.Info($" {count}) {dll}");
                        count++;
                    }
                    int dllIndex = log.GetInt("Select which dll is the main entry dll for the bot", 1, dlls.Count -1) - 1;
                    bot = new KokkaKoroBot() { Name = botName, EntryDll = dlls[dllIndex], Password = password, Major = 1, Minor = 0, Revision = 0 };
                }

            }

            if(bot == null)
            {
                log.Info("Failed to get a valid bot info. Upload failed.");
                return;
            }
                        
            log.Info($"We have a valid bot info file. {bot.Name} - {bot.Major}.{bot.Minor}.{bot.Revision}");

            // Make sure the dll file still exists
            if(!File.Exists($"{path}/{bot.EntryDll}"))
            {
                log.Info($"The bot entry dll [{bot.EntryDll}] can't be found in the current bot path.");
                return;
            }

            // Make sure to write the current bot file to disk.
            WriteBotFile(path, bot);

            // Create a zip of the folder.
            log.Info("Zipping bot...");
            string zipPath = ZipDirectory(path);

            // Create the service arguments.
            log.Info("Encoding bot...");
            AddOrUpdateBotOptions options = CreateUploadBotOptions(zipPath, bot);

            // Upload it 
            try
            {
                log.Info("Uploading...");
                await service.AddOrUploadBot(options);
                log.Info("Bot Upload Success!");
                log.Info();
                return;
            }
            catch(Exception e)
            {
                log.Info();
                log.Info();
                log.Info($"!!! Bot Upload Failed: [{e.Message}]");
                log.Info();
                log.Info();
                return;
            }
            finally
            {
                DeleteFile(zipPath);
            }
        }

        private KokkaKoroBot GetLocalBotFile(string path)
        {
            try
            {
                string fileContents = File.ReadAllText($"{path}/{c_botInfoFileName}");
                return JsonConvert.DeserializeObject<KokkaKoroBot>(fileContents);
            }
            catch(Exception)
            {
                return null;
            }
        }

        private bool WriteBotFile(string path, KokkaKoroBot bot)
        {
            try
            {
                string json = JsonConvert.SerializeObject(bot, Formatting.Indented);
                File.WriteAllText($"{path}/{c_botInfoFileName}", json);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    
        private bool DoesLocalBotInfoExist(string path)
        {
            return File.Exists($"{path}/{c_botInfoFileName}");
        }        

        private void DeleteLocalBotInfo(string path)
        {
            DeleteFile($"{path}/{c_botInfoFileName}");
        }

        private void DeleteFile(string fullPath)
        {
            try
            {
                File.Delete(fullPath);
            }
            catch (Exception) { }
        }

        private List<string> ListDlls(string path)
        {
            List<string> dlls = new List<string>();
            foreach(string filePath in Directory.EnumerateFiles(path, "", SearchOption.TopDirectoryOnly))
            {
                if (filePath.ToLower().EndsWith(".dll"))
                {
                    dlls.Add(filePath.Substring(path.Length + 1));
                }
            }
            return dlls;
        }

        private string ZipDirectory(string path)
        {
            string tempPath = $"{Path.GetTempPath()}/botUplaod-{Guid.NewGuid()}.zip";
            ZipFile.CreateFromDirectory(path, tempPath);
            return tempPath;
        }

        private AddOrUpdateBotOptions CreateUploadBotOptions(string zipPath, KokkaKoroBot bot)
        {
            Byte[] bytes = File.ReadAllBytes(zipPath);
            String zipFile = Convert.ToBase64String(bytes);
            bytes = Encoding.Default.GetBytes(zipFile);
            zipFile = Encoding.UTF8.GetString(bytes);
            return new AddOrUpdateBotOptions()
            {
                Bot = bot,
                Base64EncodedZipedBotFiles = zipFile
            };
        }
    }
}
