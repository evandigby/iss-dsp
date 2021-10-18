using System.Text.Json.Serialization;

namespace Api   
{
    public static partial class ISSLocations
    {
        public class AddLatLonRequest
        {
            [JsonPropertyName("fileName")]
            public string FileName { get; set; }
        }
    }
}
