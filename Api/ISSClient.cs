using SSC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Api
{
    public static class ISSClient
    {
        public static async Task<Response> GetISSData(DateTime startDate, DateTime endDate)
        {
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
                            Component = CoordinateComponent.Lat,
                            CoordinateSystem = CoordinateSystem.Gm
                        },
                        new FilteredCoordinateOptions
                        {
                            Component = CoordinateComponent.Lon,
                            CoordinateSystem = CoordinateSystem.Gm
                        },
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

            response.EnsureSuccessStatusCode();

            var resultSerializer = new XmlSerializer(typeof(Response));

            var responseString = await response.Content.ReadAsStringAsync();

            return resultSerializer.Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(responseString))) as Response;
        }
    }
}
