using System;

namespace SimpleOData.Models
{
    /// <summary>
    /// Class generated using http://json2csharp.com from https://my.api.mockaroo.com/people.json?key=6c5c8160
    /// </summary>
    public class Person
    {
        public int id { get; set; }
        public string first { get; set; }
        public string last { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public DateTimeOffset? dob { get; set; }
        public string picture { get; set; }
        public string hobby { get; set; }
    }
}
