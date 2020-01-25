using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;

namespace ApodCrawler
{
    public class ApodCrawler
    {
        private readonly ILogger logger;

        public ApodCrawler(ILogger logger)
        {
            this.logger = logger;
        }

        public static DateTime MinimumDate = new DateTime(1995, 06, 16); // The first date of APOD

        public async Task<List<string>> GatherUrlsFromRange(DateTime startDate, DateTime endDate)
        {
            while (true)
            {
                if (startDate.Month != endDate.Month || startDate.Year != endDate.Year)
                {
                    throw new ArgumentException("Dates must be in the same month and year");
                }

                var start = startDate.ToString("yyyy-MM-dd");
                var end = endDate.ToString("yyyy-MM-dd");
                var urlString = $"https://api.nasa.gov/planetary/apod?api_key=Tpf5IVAxWe8WSaJ3AFTD4gOoEKxbd2jc4IvAHs8Y&start_date={start}&end_date={end}";
                var uri = new Uri(urlString);

                try
                {
                    return await FetchUrlsFromDateRange(start, end, uri);
                }
                catch (SlowDownException)
                {
                    Console.WriteLine("Too many requests");
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                }
                catch (BadDayException)
                {
                    Console.WriteLine("Bad day found");
                    return await GatherUrlsFromSplitDateRange(startDate, endDate);
                }
            }
        }

        private static async Task<List<string>> FetchUrlsFromDateRange(string start, string end, Uri uri)
        {
            using (var client = new WebClient())
            {
                try
                {
                    Console.WriteLine($"Getting URLs from {start} to {end}");
                    Console.WriteLine(uri.AbsolutePath);
                    var apodJson = await client.DownloadStringTaskAsync(uri);
                    var apodResponses = JArray.Parse(apodJson);
                    return apodResponses.Select(j => (string)j["hdurl"])
                        .Where(j => j != null) // in one circumstance, hdurl was null
                        .Select(h => h.Trim()) // sometimes there's an excess space
                        .ToList();
                }
                catch (WebException ex)
                {
                    if (ex.Message.Contains("429"))
                    {
                        throw new SlowDownException();
                    }

                    if (ex.Message.Contains("500"))
                    {
                        throw new BadDayException();
                    }

                    throw;
                }
            }
        }

        private async Task<List<string>> GatherUrlsFromSplitDateRange(DateTime startDate, DateTime endDate)
        {
            var dayRange = endDate - startDate;
            if (dayRange.Days == 0)
            {
                logger.Warning($"Found bad day {endDate}");
                return new List<string>();
            }

            var diff = Math.Ceiling(dayRange.Days / 2.0); //Split the current range in two
            var newEndDate = endDate.AddDays(diff * -1);
            var newStartDate = newEndDate.AddDays(1);

            Console.WriteLine($"New pair from {startDate:yyyy-MM-dd} to {newEndDate:yyyy-MM-dd} and {newStartDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
            var earlier = await GatherUrlsFromRange(startDate, newEndDate);
            var later = await GatherUrlsFromRange(newStartDate, endDate);

            return earlier.Concat(later).ToList();
        }
    }

    internal class BadDayException : Exception
    {
    }

    internal class SlowDownException : Exception
    {
    }
}
