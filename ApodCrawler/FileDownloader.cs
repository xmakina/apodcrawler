using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ILogger = Serilog.ILogger;

namespace ApodCrawler
{
    public class FileDownloader
    {
        private readonly ILogger logger;
        private readonly string rootDirectory;

        public FileDownloader(ILogger logger, string rootDirectory)
        {
            this.logger = logger;
            this.rootDirectory = rootDirectory;
        }

        public bool AlreadyDownloaded(string fileName)
        {
            var files = Directory.GetFiles(rootDirectory, fileName, SearchOption.AllDirectories);
            return files.Any();
        }

        public async Task<byte[]> DownloadImage(Uri uri)
        {
            using (var client = new WebClient())
            {
                try
                {
                    return await client.DownloadDataTaskAsync(uri);
                }
                catch (WebException ex)
                {
                    if (ex.Message.Contains("404"))
                    {
                        logger.Warning($"Image not found {uri.AbsoluteUri}"); // Write this to the log to investigate further
                    }

                    return null;
                }
            }
        }
    }
}