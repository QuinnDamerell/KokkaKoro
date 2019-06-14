using GameService.ServiceCore;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using Newtonsoft.Json;
using ServiceProtocol;
using ServiceProtocol.Common;
using ServiceProtocol.Requests;
using ServiceProtocol.Responses;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Managers
{
    public class BotManager
    {
        readonly string c_tempFolderPath = "Temp";
        static readonly string c_botInfoFile = "botinfo.json";

        static BotManager s_botMan = new BotManager();
        public static BotManager Get()
        {
            return s_botMan;
        }

        public async Task<ServiceBot> GetBotCopy(string botName)
        {
            // First get the local bot
            ServiceBot bot = await StorageMaster.Get().DownloadBot(botName);

            // Next, get the bot to copy itself to a temp location just for this execution.
            // The bot will automatically clean up it's temp flies when it's done executing or the class
            // is destroyed.
            bot.CopyToTemp(Directory.GetCurrentDirectory() + $"/{c_tempFolderPath}");

            // Return the bot.
            return bot;
        }

        public async Task<KokkaKoroBot> UploadBot(AddOrUpdateBotOptions request, string userName)
        {
            if(!request.Bot.IsValid())
            {
                throw new Exception("The bot details aren't valid.");
            }

            // Try to get the current bot.
            ServiceBot localBot = null;
            try
            {
                localBot = await StorageMaster.Get().DownloadBot(request.Bot.Name);
            }
            catch (BotNotFound)
            {
                // This is ok, it means it's a new bot.
            }
            catch(Exception e)
            {
                throw new Exception($"Failed to download existing bot {e.Message}");
            }

            // Hash the bot password.
            HashPassword(request.Bot);

            // We already have a bot, validate.
            if(localBot != null)
            {
                // We have the local bot, validate this bot. 
                KokkaKoroBot localInfo = localBot.GetBotInfo();
                if (!localInfo.Name.Equals(request.Bot.Name))
                {
                    throw new Exception("The bot names don't match.");
                }
                // Let it pass if the local password is empty.
                if (!String.IsNullOrWhiteSpace(localInfo.Password) && !localInfo.Password.Equals(request.Bot.Password))
                {
                    throw new Exception("Incorrect password for bot.");
                }
                if(request.Bot.Revision <= localInfo.Revision)
                {
                    if(request.Bot.Minor <= localInfo.Minor)
                    {
                        if(request.Bot.Major <= localInfo.Major)
                        {
                            throw new Exception($"The bot version must be greater than the current service version [{localInfo.Major}.{localInfo.Minor}.{localInfo.Revision}]");
                        }
                    }
                }
            }

            // We are good!
            // Try to unzip the file to the bots folder.
            string tempZipPathFile = $"{c_tempFolderPath}/upload-{request.Bot.Name}-{Guid.NewGuid()}.zip";
            string tempZipFolder = $"{c_tempFolderPath}/upload-folder-{request.Bot.Name}-{Guid.NewGuid()}";

            try
            {
                // Idk why we need to do this, but this fixes the string.
                string data = request.Base64EncodedZipedBotFiles.Replace(' ', '+');

                // Write the base encoded string to the zip file.
                Byte[] bytes = Convert.FromBase64String(data);
                File.WriteAllBytes(tempZipPathFile, bytes);

                // Unzip the files.
                ZipFile.ExtractToDirectory(tempZipPathFile, tempZipFolder);

                // Delete any current botinfo files
                DeleteFile($"{tempZipFolder}/{c_botInfoFile}");

                // Write the updated bot info.
                WriteBotInfo(tempZipFolder, request.Bot);

                // Copy the files to the local dir
                StorageMaster.Get().CopyTempBotToLocal(tempZipFolder, request.Bot);

                // Upload the bot to azure.
                await StorageMaster.Get().UploadLocalBotToAzure(request.Bot);
            }
            catch(Exception e)
            {
                throw new Exception($"Failed to unpack bot files. {e.Message}");
            }
            finally
            {
                DeleteFile(tempZipPathFile);
                DeleteFolder(tempZipFolder);
            }

            // Kill the password we return.
            request.Bot.Password = "";
            return request.Bot;
        }

        private void WriteBotInfo(string tempPath, KokkaKoroBot bot)
        {
            string json = JsonConvert.SerializeObject(bot);
            File.WriteAllText($"{tempPath}/{c_botInfoFile}", json);
        }

        private void HashPassword(KokkaKoroBot bot)
        {
            string unique = bot.Name + "." + bot.Password;
            using (var sha = new System.Security.Cryptography.SHA256Managed())
            {
                byte[] textData = System.Text.Encoding.UTF8.GetBytes(unique);
                byte[] hash = sha.ComputeHash(textData);
                bot.Password = BitConverter.ToString(hash).Replace("-", String.Empty);
            }
        }

        private void DeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ) { }
        }

        private void DeleteFolder(string path)
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch (Exception) { }
        }
    }
}
