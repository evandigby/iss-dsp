using ISSWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SSC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ISSWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> GetData()
        {
            var start = HttpContext.Request.Query["start"];
            var end = HttpContext.Request.Query["end"];

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
                    CoordinateOptions = new[]
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
