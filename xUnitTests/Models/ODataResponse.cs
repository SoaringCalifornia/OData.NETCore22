using Newtonsoft.Json;
using System.Collections.Generic;

namespace xUnitTests.Models
{
    /// <summary>
    /// Standard OData response 
    /// </summary>
    /// <typeparam name="T">Type for expected response object</typeparam>
    public class ODataResponse<T>
    {
        [JsonProperty(PropertyName = "@odata.context")]
        public string odataContext { get; set; }

        [JsonProperty(PropertyName = "@odata.count")]
        public int odataCount { get; set; }

        public List<T> value { get; set; }
    }
}
