using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace ApodCrawler
{
    public class Program
    {
        private const string RootDirectory = "C:\\Users\\Alex Addams\\Pictures\\APOD"; // Where should we save the files
        private static readonly DateTime StartFrom = ApodCrawler.MinimumDate; // Where should we start crawling from

        public static void Main(string[] args)
        {
            var log = new LoggerConfiguration()
                .WriteTo.File("apodcrawler.log")
                .MinimumLevel.Warning()
                .CreateLogger();

            var crawler = new ApodCrawler(log);
            var fileDownloader = new FileDownloader(log, RootDirectory);

            var year = StartFrom.Year;
            DownloadImages(year, crawler, fileDownloader, log).Wait();

            Console.WriteLine("All done, press enter to close");
            Console.ReadLine();
        }

        private static async Task DownloadImages(int year, ApodCrawler crawler, FileDownloader fileDownloader, ILogger log)
        {
            while (year < DateTime.Now.Year)
            {
                var month = 1; // This is part of the loop, start at the beginning
                while (month <= 12)
                {
                    var start = new DateTime(year, month, 01);
                    if (start < StartFrom) // Might not be starting in January or on the 1st
                    {
                        start = StartFrom;
                    }

                    var end = new DateTime(year, month, DateTime.DaysInMonth(year, month));

                    try
                    {
                        var getUrls = await crawler.GatherUrlsFromRange(start, end);
                        var urlsToDownload = getUrls
                            .Where(url => fileDownloader.AlreadyDownloaded(Path.GetFileName(url)) == false)
                            .Select(url => new Uri(url));

                        foreach (var uri in urlsToDownload)
                        {
                            Console.WriteLine($"Downloading {uri}");
                            var result = await fileDownloader.DownloadImage(uri); // Do this one at a time, no need to overload their server

                            var fileName = Path.GetFileName(uri.AbsolutePath);

                            var image = result?.ToImage();
                            if (image == null)
                            {
                                if (result != null)
                                {
                                    SaveOther(fileName, result); // The file wasn't an image but was something
                                }
                            }
                            else
                            {
                                SaveImage(fileName, image);
                                image.Dispose(); // while there is no harm to holding the memory stream open, it doesn't hurt to clean up
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        Console.WriteLine("Intervention needed");
                        Console.ReadLine();
                        continue; // Loop back around with the new changes
                    }

                    var report = $"{new DateTime(year, month, 1):MMMM yyyy} done";
                    Console.WriteLine(report);
                    log.Information(report);
                    month++;
                }

                year++;
            }
        }

        private static void SaveOther(string fileName, byte[] theFile) // Not all files are images
        {
            var thePath = Path.Combine(RootDirectory, "other", fileName);
            Console.WriteLine($"Saving other file {thePath}");
            File.WriteAllBytes(thePath, theFile);
        }

        private static void SaveImage(string fileName, Image image)
        {
            string thePath;
            if (image.Width < 1024)
            {
                thePath = Path.Combine(RootDirectory, "small", fileName);
            }
            else if (image.Width > 1920)
            {
                thePath = Path.Combine(RootDirectory, "large", fileName);
            }
            else
            {
                thePath = Path.Combine(RootDirectory, "medium", fileName);
            }

            Console.WriteLine($"Saving image {thePath}");
            image.Save(thePath);
        }
    }
}
