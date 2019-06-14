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
            Console.Clear();
            log.Info("");
            log.Info("Welcome to the bot uploader!");
            log.Info("****************************");
            log.Info("");
            log.Info("This utility allows you to upload bots for the Kokka Koro game service.");
            log.Info("The bots must be platform independent dotnet 2.2 compiled binaries.");
            log.Info("Thus, the entry binary should be a (.dll).");
            log.Info();
            log.Info("To generate these files, run...");
            log.Info("   `dotnet publish -c Release <your bot>.csproj`");
            log.Info("The files will then be located...");
            log.Info("   `<project root>/bin/Release/netcoreapp2.2/publish/`");
            log.Info();
            if (log.GetDecission("Got it"))
            {
                log.Info("Great! Let's go!");
            }
            else
            {
                log.Info("Too bad! Let's go!");
            }
            log.Info();

            string path = optPath;
            do
            {
                if (String.IsNullOrWhiteSpace(path))
                {
                    path = GetLastPath();
                    if (path != null)
                    {
                        log.Info($"Previous path found: [{path}]");
                        if (log.GetDecission("Would you like to use the previous bot path"))
                        {
                            break;
                        }
                    }                
                    path = log.GetString("Enter a the file path to the publish directory");
                }
            } while (false);                

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
                    log.Info();
                    log.Info("This will create a file called botinfo.json in the directory. That file is what defines your bot name and details.");
                    log.Info("   Name     - The name of your bot. [a-z,0-9]");
                    log.Info("   Password - Protects your bot from unloaders that aren't yourself. ");
                    log.Info("              This is stored in the bot info file and needs to be remembered to upload update.");
                    log.Info("   EntryDll - The name of the dotnet dll with the main() function. (usually the project name)");
                    log.Info("   Version  - [Major].[Minor].[Revision] Must increase for updates.");
                    log.Info();
                    var dlls = ListDlls(path);
                    if (dlls.Count == 0)
                    {
                        log.Info("No dlls were found in the directory, we failed to upload the new bot.");
                        return;
                    }
                    string botName = log.GetString("Please enter a bot name", true);
                    string password = log.GetString("Please enter a password");
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

            if(bot == null || !bot.IsValid())
            {
                log.Info("Failed to get a valid bot info. Upload failed.");
                return;
            }
                        
            log.Info($"We have a valid bot info file. {bot.Name} - {bot.Major}.{bot.Minor}.{bot.Revision}");
            SetLastPath(path);

            // Make sure the dll file still exists
            if (!File.Exists($"{path}/{bot.EntryDll}"))
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
                log.Info("!!!");
                log.Info($"!!! Bot Upload Failed: [{e.Message}] !!!");
                log.Info("!!!");
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

        private string GetLastPath()
        {
            try
            {
                return File.ReadAllText("LastBotFilePath.txt");
            }
            catch (Exception) { }
            return null;
        }

        private void SetLastPath(string path)
        {
            try
            {
                File.WriteAllText("LastBotFilePath.txt", path);
            }
            catch (Exception) { }
        }
    }
}
