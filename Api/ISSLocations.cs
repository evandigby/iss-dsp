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

namespace DSPSupport
{
    public static class ISSLocations
    {
        [FunctionName("ISS")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
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

            var request = new DataRequest
            {
                Satellites = new SatelliteSpecification[]
                {
                    new SatelliteSpecification
                    {
                        Id = "iss",
                        ResolutionFactor = 1
                    }
                },
                TimeInterval = new TimeInterval
                {
                    Start = startDate,
                    End = endDate
                },
                OutputOptions = new OutputOptions
                {
                    CoordinateOptions = new []
                    {
                        new FilteredCoordinateOptions
                        {
                            Component = CoordinateComponent.X,
                            CoordinateSystem = CoordinateSystem.Geo,
                        },
                        new FilteredCoordinateOptions
                        {
                            Component = CoordinateComponent.Y,
                            CoordinateSystem = CoordinateSystem.Geo,
                        },
                        new FilteredCoordinateOptions
                        {
                            Component = CoordinateComponent.Z,
                            CoordinateSystem = CoordinateSystem.Geo,
                        }
                    }
                }
            };

            XmlSerializer serializer = new XmlSerializer(typeof(DataRequest));

            using var ms = new MemoryStream();

            serializer.Serialize(ms, request);

            ms.Position = 0;
            using var client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));


            var stringData = Encoding.UTF8.GetString(ms.ToArray());
            ms.Position = 0;

            var uri = new Uri("https://sscweb.gsfc.nasa.gov/WS/sscr/2/locations");

            var response = await client.PostAsync(uri, new StringContent(stringData, Encoding.UTF8, "application/xml"));

            if (!response.IsSuccessStatusCode)
            {
                return new ContentResult
                {
                    Content = await response.Content.ReadAsStringAsync(),
                    ContentType = response.Headers.TryGetValues("Content-Type", out IEnumerable<string> output) ? output.First() : "text/html",
                    StatusCode = (int)response.StatusCode
                };
            }

            var resultSerializer = new XmlSerializer(typeof(Response));

            var result = resultSerializer.Deserialize(await response.Content.ReadAsStreamAsync());

            return new OkObjectResult(result);
        }
    }
}
