using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json.Linq;

namespace YahooScrape
{
    class Program
    {
        static void Main(string[] args)
        {
            // http://www.codeproject.com/Articles/552378/Yahoo-e-splusYQLplusAPIplusandplusC-plusTu
            // http://stackoverflow.com/questions/3840762/how-do-you-urlencode-without-using-system-web
            // http://www.codeproject.com/Articles/42575/Yahoo-Managed

            // http://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20yahoo.finance.quotes%20where%20symbol%20in%20(%22ACC%22)&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys
            StringBuilder theWebAddress = new StringBuilder();
            theWebAddress.Append("http://query.yahooapis.com/v1/public/yql?");
            theWebAddress.Append("q=" + Uri.EscapeUriString("select * from yahoo.finance.quotes where symbol in (\"WBC.AX\",\"FET.AX\") and startDate = \"2016-06-30\" and endDate = \"2016-06-30\""));
            theWebAddress.Append("&format=json");
            theWebAddress.Append("&diagnostics=false");

            string results = "";

            using (WebClient wc = new WebClient())
            {
                results = wc.DownloadString(theWebAddress.ToString());
            }

            JObject dataObject = JObject.Parse(results);
            JArray jsonArray = (JArray)dataObject["query"]["results"]["Result"];

            foreach (var locationResult in jsonArray)
            {
                Console.WriteLine("Name:{0}", locationResult["Title"]);
                Console.WriteLine("Address:{0}", locationResult["Address"]);
                Console.WriteLine("Longitude:{0}", locationResult["Longitude"]);
                Console.WriteLine("Latitude:{0}", locationResult["Latitude"]);
                Console.WriteLine(Environment.NewLine);
            }

            Console.ReadLine();



            var baseLocation = ConfigurationManager.AppSettings["BaseLocation"];

            /// 
            /// Step 1: Load the stocks names. Every S&P500 stock will be download by default if nothing entered
            /// 

            // We ask the user which stocks to download, plus the dates
            Console.WriteLine("Please write the name of the stocks you want, separated by comma (ej: APPL, GM, T, etc). Leaving this blank and pressing enter, will download ALL S&P500 Stocks.");
            var userStocks = Console.ReadLine().ToUpper();

            Console.WriteLine("Please enter date information. If you leave the fields in blank, the default range is 01/01/2000 - 01/01/2014");
            Console.WriteLine("What's the Date?");
            var finishDateStr = Console.ReadLine();

            var finishDate = Convert.ToDateTime(finishDateStr);
            var finishDay = finishDate.Day;
            var finishMonth = finishDate.Month;
            var finishYear = finishDate.Year;

            var startDate = finishDate.AddMonths(-1);
            var startDay = startDate.Day;
            var startMonth = startDate.Month;
            var startYear = startDate.Year;

            // The List, where we will save the stock's names that could actually be downloaded.
            // We use a list, because we don't know the size it will have; it changes on each loop.
            var tickers = new List<string>();

            // We can have 2 different inputs: a list of stocks, or the default ALL stocks
            // First, the default choice: ALL stocks
            if (string.IsNullOrWhiteSpace(userStocks))
            {
                var stockListLocation = baseLocation + ConfigurationManager.AppSettings["Stocklist"];
                if (!File.Exists(stockListLocation))
                {
                    Console.WriteLine("Could not load file: " + stockListLocation);
                    throw new Exception();
                }

                // The following code will read the  names from de csv file with all S&P500 stocks
                // we use a "using" block so that C# will automatically clean up all unused resources
                using (var reader = new StreamReader(stockListLocation))
                {
                    while (!reader.EndOfStream)
                    {
                        var stock = reader.ReadLine();
                        
                        // In case there are symbols in the stock names that would not be recognized by Yahoo API
                        if (stock != null && (!tickers.Contains(stock) && !stock.Contains('/')))
                        {
                            tickers.Add(stock);
                        }
                    }
                }
            }
            // Given the user wrote what stocks to search for, we build the list with them
            else
            {
                // remove whitespaces from the string
                userStocks = userStocks.Replace("  ", string.Empty);
                var companyLists = userStocks.Split(',');
                tickers.AddRange(companyLists.Where(stock => !string.IsNullOrWhiteSpace(stock)));
            }


            /// 
            /// Step 2: Download the listed stocks from Yahoo Finance, daily & weekly data
            ///

            const string weekDirectory = "";
            var dayOrWeek = new[] { "d", "w" };
            var webClient = new WebClient();

            var dataDirectory = baseLocation + @"\Data\";

            // Create the directories in case they don't exist
            if (!Directory.Exists(dataDirectory))

            {
                Directory.CreateDirectory(dataDirectory);
                Console.WriteLine("Data directory created.");
            }

            var urlPrototype = @"http://ichart.yahoo.com/table.csv?s={0}&a={1}&b={2}&c={3}&d={4}&e={5}&f={6}&g=d&f=sd1ol2l3pv&ignore=.csv";
            var dayFilePrototype = "{0}_{1}.{2}.{3}-{4}.{5}.{6}.csv";


            // Parameters for download
            foreach (var stock in tickers)
            {
                // The Yahoo Finance URL for each parameter
                var url = string.Format(urlPrototype, stock, startMonth, startDay, startYear, finishMonth, finishDay, finishYear);

                // Files Downloader for each scenario
                var dayFileName = string.Format(dayFilePrototype, stock.ToUpper(), startMonth, startDay, startYear, finishMonth, finishDay, finishYear);
                var dayFile = Path.Combine(dataDirectory, dayFileName);
                if (!File.Exists(dayFile))
                {
                    webClient.DownloadFile(url, dayFile);
                    Console.WriteLine(stock + " Daily data downloaded successfully!");
                }
                else
                {
                    Console.WriteLine("A file with Daily data for " + stock + " and the specified date already exists");
                }
            }
            Console.WriteLine("Downloads completed. Press enter to close the program.");
            Console.ReadLine();
        }
    }
}