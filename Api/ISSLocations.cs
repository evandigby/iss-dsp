using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SSC;
using System.Xml.Serialization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Net.Http.Formatting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using Azure.Storage;
using Azure.Storage.Files.DataLake;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Net;

namespace Api
{
    public static partial class ISSLocations
    {
        private static string DataLakeAccountName => Environment.GetEnvironmentVariable("DataLakeAccountName");
        private static string DataLakeAccountKey => Environment.GetEnvironmentVariable("DataLakeAccountKey");
        private static string DataLakeFileSystemName => Environment.GetEnvironmentVariable("DataLakeFileSystemName"); 


        [FunctionName("AddLatLon")]
        public static async Task<IActionResult> AddLatLon([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req)
        {
            AddLatLonRequest data;
            try
            {
                data = await JsonSerializer.DeserializeAsync<AddLatLonRequest>(req.Body);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }


            if (Path.GetExtension(data.FileName).ToLowerInvariant() != ".csv")
                return new OkObjectResult(Enumerable.Empty<LogEntry>());

            StorageSharedKeyCredential sharedKeyCredential =
                    new StorageSharedKeyCredential(DataLakeAccountName, DataLakeAccountKey);

            string dfsUri = "https://" + DataLakeAccountName + ".dfs.core.windows.net";

            var dataLakeServiceClient = new DataLakeServiceClient(new Uri(dfsUri), sharedKeyCredential);

            DataLakeFileSystemClient fileSystemClient = dataLakeServiceClient.GetFileSystemClient(DataLakeFileSystemName);
            DataLakeDirectoryClient directoryClient = fileSystemClient.GetDirectoryClient("parquet-output");

            var file = directoryClient.GetFileClient(data.FileName);

            if (!await file.ExistsAsync())
            {
                return new BadRequestObjectResult("No file at specified path");
            }

            var fileStream = await file.OpenReadAsync();

            using var streamReader = new StreamReader(fileStream);

            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };

            using var csvData = new CsvReader(streamReader, csvConfig);


            csvData.Context.RegisterClassMap<LogEntryMap>();

            var records = csvData.GetRecords<LogEntry>().ToList();

            try
            {
                var outputEntries = await PopulateISSLocations(records);

                DataLakeDirectoryClient outputDirectoryClient = fileSystemClient.GetDirectoryClient("latlon-output");

                var outputFile = await outputDirectoryClient.CreateFileAsync(data.FileName);

                var writeStream = await outputFile.Value.OpenWriteAsync(true);

                using var textWriter = new StreamWriter(writeStream);

                var outcsv = new CsvWriter(textWriter, csvConfig);

                await outcsv.WriteRecordsAsync(outputEntries);

                return new OkObjectResult(data);
            }
            catch (Exception ex)
            {
                return new ContentResult
                {
                    StatusCode = (int)HttpStatusCode.BadGateway,
                    Content = ex.Message,
                    ContentType = "text/plain"
                };
            }
        }

        public static async Task<IEnumerable<LogEntry>> PopulateISSLocations(IEnumerable<LogEntry> logEntries)
        {
            var output = logEntries
                .OrderBy(e => e.SignalStart)
                .ThenBy(e => e.SignalEnd)
                .ToList();

            var minDate = output.Min(logEntry => logEntry.SignalPeak);
            var maxDate = output.Max(logEntry => logEntry.SignalPeak);

            if (maxDate - minDate < TimeSpan.FromMinutes(2))
            {
                maxDate = minDate.AddMinutes(2);
            }

            var issData = await ISSClient.GetISSData(DateTime.SpecifyKind(minDate, DateTimeKind.Utc), DateTime.SpecifyKind(maxDate, DateTimeKind.Utc));

            if (!(issData.Result is DataResult dataResult))
            {
                throw new Exception("invalid response from ISS API");
            }


            var issRecords = dataResult.Data.Single();
            var issCoordinates = issRecords.Coordinates.Where(c => c.CoordinateSystem == CoordinateSystem.Gm).Single();

            var issRecordTimes = issRecords.Time.Select(t => DateTime.SpecifyKind(t, DateTimeKind.Utc)).OrderBy(t => t).ToList();

            var i = 0;


            return output.Select(entry =>
            {
                for (; i < issRecordTimes.Count; i++)
                {
                    if (i + 1 == issRecordTimes.Count)
                    {
                        break;
                    }

                    var currDiff = Math.Abs((entry.SignalPeak - issRecordTimes[i]).TotalMilliseconds);
                    var nextDiff = Math.Abs((entry.SignalPeak - issRecordTimes[i + 1]).TotalMilliseconds);
                    if (nextDiff < currDiff)
                        continue;
                    else
                        break;
                }

                return new LogEntry
                {
                    SignalStart = entry.SignalStart,
                    SignalPeak = entry.SignalPeak,
                    SignalEnd = entry.SignalEnd,
                    SignalFrequency = entry.SignalFrequency,
                    SignalPower = entry.SignalPower,
                    Latitude = (decimal)issCoordinates.Latitude[i],
                    Longitude = (decimal)issCoordinates.Longitude[i]
                };
            });
        }

        [FunctionName("ISS")]
        public static async Task<IActionResult> ISS([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            var start= req.Query["start"];
            var end = req.Query["end"];

            if (!DateTime.TryParse(start, out DateTime startDate))
                return new BadRequestObjectResult("invalid start date");

            if (!DateTime.TryParse(end, out DateTime endDate))
                return new BadRequestObjectResult("invalid end date");

            if (startDate > endDate)
                return new BadRequestObjectResult("start date must be before end date");

            if ((endDate - startDate) < TimeSpan.FromMinutes(2))
                return new BadRequestObjectResult("end date must be greater than or requal to 2 minutes later than start date");

            var result = await ISSClient.GetISSData(startDate, endDate);

            return new OkObjectResult(result);
        }
    }
}
