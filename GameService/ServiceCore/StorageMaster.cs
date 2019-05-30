using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ServiceProtocol.Common;
using Newtonsoft.Json;

namespace GameService.ServiceCore
{
    public class StorageMaster
    {
        static readonly string c_blobBotBase = "Bots";
        static readonly string c_botInfoFile = "botinfo.json";

        static StorageMaster s_storage = new StorageMaster();
        public static StorageMaster Get()
        {
            return s_storage;
        }

        public async Task<List<KokkaKoroBot>> ListBots()
        {
            // Get all of the bot files
            List<IListBlobItem> items = await GetAllBotFiles();
            return await GetBotInfos(items);
        }

        public async Task<ServiceBot> DownloadBot(string botName)
        {
            // Get the blob storage
            CloudBlobContainer container = GetBlob();

            // Get all items in the folder.
            List<IListBlobItem> items = await GetAllBotFiles(botName);

            // Check for items and the main exe.
            if (items.Count == 0)
            {
                throw new Exception("No files found for the requested bot.");
            }

            // Find the bot info
            List<KokkaKoroBot> infos = await GetBotInfos(items);
            if(infos.Count != 1)
            {
                throw new Exception("No or many bot info files found");
            }
            KokkaKoroBot info = infos[0];

            bool foundExe = false;
            foreach(IListBlobItem item in items)
            {
                string[] parts = item.Uri.ToString().Split("/");
                if(parts.Length > 0 && parts[parts.Length -1] == info.EntryDll)
                {
                    foundExe = true;
                    break;
                }
            }
            if(!foundExe)
            {
                throw new Exception($"Bot exe file ({info.EntryDll}) not found in bot files!");
            }

            // Check if the box exists locally
            string basePath = $"{c_blobBotBase}/{botName}";
            string localBotPath = GetBothPath(botName);

            // Check if we already have the bot, and if it's current.
            // If we fail don't worry about it.
            try
            {
                string localInfo = $"{localBotPath}/{c_botInfoFile}";
                if (File.Exists(localInfo))
                {
                    KokkaKoroBot local = JsonConvert.DeserializeObject<KokkaKoroBot>(await File.ReadAllTextAsync(localInfo));
                    if(local.Equals(info))
                    {
                        // We have the current version locally, so just use it.
                        return new ServiceBot(info, localBotPath);
                    }
                }
            }
            catch { }

            // Make sure the folder is clean.
            // But then make sure the folder exists again.
            Directory.Delete(localBotPath, true);
            GetBothPath(botName);

            // Try to download all of the files locally.
            // If we fail delete them all.
            try
            {
                foreach (IListBlobItem item in items)
                {
                    if (item is CloudBlockBlob blockBlob)
                    {
                        // Remove the base path
                        string relativePath = blockBlob.Name.Substring(basePath.Length + 1);

                        // If we find a sub folder, make sure it exits.
                        if (relativePath.Contains("/"))
                        {
                            string folderPath = relativePath.Substring(0, relativePath.LastIndexOf("/"));
                            Directory.CreateDirectory($"{localBotPath}/{folderPath}");
                        }

                        string path = $"{localBotPath}{relativePath}";
                        await blockBlob.DownloadToFileAsync(path, FileMode.Create);
                    }
                }
            }
            catch(Exception e)
            {
                Directory.Delete(localBotPath, true);
                throw e;
            }

            return new ServiceBot(info, localBotPath);
        }

        private async Task<KokkaKoroBot> GetBotInfoObject(CloudBlockBlob block)
        {
            return KokkaKoroBot.ParseAndValidate(await block.DownloadTextAsync());
        }

        private string GetBothPath(string botId)
        {
            //site / wwwroot / 
            //app_data/jobs/continuous
            string localPath = $"{Directory.GetCurrentDirectory()}/Bots/{botId}/";
            Directory.CreateDirectory(localPath);
            return localPath;
        }

        private async Task<List<KokkaKoroBot>> GetBotInfos(List<IListBlobItem> items)
        {
            List<KokkaKoroBot> bots = new List<KokkaKoroBot>();
            foreach (IListBlobItem item in items)
            {
                if (item is CloudBlockBlob blockBlob)
                {
                    if (blockBlob.Name.ToLower().EndsWith(c_botInfoFile))
                    {
                        KokkaKoroBot info = await GetBotInfoObject(blockBlob);
                        if (info != null)
                        {
                            bots.Add(info);
                        }
                    }
                }
            }
            return bots;
        }

        private async Task<List<IListBlobItem>> GetAllBotFiles(string botName = null)
        {
            // Get the blob storage
            CloudBlobContainer container = GetBlob();

            // Get all items in the folder.
            List<IListBlobItem> items = new List<IListBlobItem>();
            BlobContinuationToken token = new BlobContinuationToken();
            while (token != null)
            {
                BlobResultSegment result = await container.ListBlobsSegmentedAsync($"{c_blobBotBase}/{(botName == null ? String.Empty : botName)}", true, BlobListingDetails.None, 10000, token, null, null);
                items.AddRange(result.Results);
                token = result.ContinuationToken;
            }
            return items;
        }

        private CloudBlobContainer GetBlob()
        {
            string storageConnectionString = Environment.GetEnvironmentVariable("StorageConnectionString");

            // Parse the storage acount
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);          

            // Create the CloudBlobClient that represents the Blob storage endpoint for the storage account.
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

            return cloudBlobClient.GetContainerReference("general");
        }        
    }
}
