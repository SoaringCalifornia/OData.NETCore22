using System.IO;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http;

namespace xUnitTests.Common
{
    /// <summary>
    /// HttpContent management handy functions
    /// </summary>
    public static class HttpContentHelper
    {
        /// <summary>
        /// Create HttpContent from provided content object
        /// </summary>
        /// <param name="content">Object to be passed as HttpContent</param>
        /// <returns>Create HttpContent object</returns>
        public static HttpContent CreateHttpContent(object content)
        {
            HttpContent httpContent = null;

            if (content != null)
            {
                var ms = new MemoryStream();
                SerializeJsonIntoStream(content, ms);
                ms.Seek(0, SeekOrigin.Begin);
                httpContent = new StreamContent(ms);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }

            return httpContent;
        }

        public static void SerializeJsonIntoStream(object value, Stream stream)
        {
            using (var sw = new StreamWriter(stream, new System.Text.UTF8Encoding(false), 1024, true))
            using (var jtw = new JsonTextWriter(sw) { Formatting = Formatting.None })
            {
                var js = new JsonSerializer();
                js.Serialize(jtw, value);
                jtw.Flush();
            }
        }

        public static T DerializeJsonFromStream<T>(Stream stream)
        {
            using (StreamReader sr = new StreamReader(stream))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                JsonSerializer serializer = new JsonSerializer();
                return serializer.Deserialize<T>(reader);
            }
        }

        public static string ObjectToString(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}
