using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleOData.Models;

namespace SimpleOData.Controllers
{
    /// <summary>
    /// Simple manager controller
    /// Used to specify throttling in seconds
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ManagerController : ControllerBase
    {
        public static int version = 1;
        public static int throttleDelaySecs;

        protected readonly IServiceProvider _serviceProvider;
        protected readonly IOptions<Settings> _settings;

        public ManagerController(IServiceProvider serviceProvider, IOptions<Settings> settings)
        {
            _serviceProvider = serviceProvider;
            _settings = settings;
        }

        [HttpGet]
        public ActionResult<IDictionary<string, int>> Get()
        {
            return new Dictionary<string, int>
            {
                { "version", version },
                { "throttleDelaySecs", throttleDelaySecs }
            };
        }

        [HttpPost]
        [Route("[action]/{throttleDelaySecs}")]
        public void Throttle(int throttleDelaySecs)
        {
            Interlocked.Exchange(ref ManagerController.throttleDelaySecs, throttleDelaySecs);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task Seed()
        {
            var ctx = _serviceProvider.GetService(typeof(PeopleContext));
            await DbContextHelper.PopulateDbAsync<Person>(ctx as PeopleContext, _settings.Value.urlMockPeople);
        }
    }
}
