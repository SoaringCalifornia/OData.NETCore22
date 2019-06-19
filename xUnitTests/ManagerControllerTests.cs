using SimpleOData;
using System;
using Xunit;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Configuration;
using SimpleOData.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using xUnitTests.Models;
using xUnitTests.Common;
using SimpleOData.Controllers;
using System.Collections.Generic;
using System.Diagnostics;

namespace xUnitTests
{
    /// <summary>
    /// Testing throttling on OData controller 
    /// </summary>
    public class ManagerControllerTests
    {
        private readonly IConfiguration _config;
        private readonly IOptions<Settings> _settings;
        private readonly IServiceProvider _serviceProvider;
        private readonly TestServer _server;
        private readonly HttpClient _client;

        private readonly string uriOdata;
        private readonly string uriApi;

        public ManagerControllerTests()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            _server = new TestServer(new WebHostBuilder()
                .UseEnvironment("Development")
                .UseContentRoot(AppContext.BaseDirectory)
                .UseConfiguration(_config)
                .UseStartup<Startup>());

            _serviceProvider = new ServiceCollection()
                .AddOptions()
                .Configure<Settings>(_config.GetSection(typeof(Settings).Name))
                .BuildServiceProvider();

            _settings = _serviceProvider.GetService<IOptions<Settings>>();

            Assert.NotNull(_settings);

            uriOdata = $"/{_settings.Value.routePrefixOData}/{typeof(Person).Name}/";
            uriApi = $"/api/{typeof(ManagerController).Name.Replace("Controller", "")}/";

            _client = _server.CreateClient();
        }

        [Theory]
        [InlineData(5)]
        public async Task TestThrottleDelaySecs(int throttleDelaySecs)
        {
            // Set throttleDelaySecs
            var request = new HttpRequestMessage(HttpMethod.Post, $"{uriApi}throttle/{throttleDelaySecs}");

            using (var response = await _client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            // Check if value was assigned properly
            request = new HttpRequestMessage(HttpMethod.Get, $"{uriApi}");

            using (var response = await _client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var res = HttpContentHelper.DerializeJsonFromStream<Dictionary<string, int>>(await response.Content.ReadAsStreamAsync());

                // Assert
                Assert.True(res.ContainsKey("throttleDelaySecs"));

                // Assert
                Assert.True(res["throttleDelaySecs"] == throttleDelaySecs);
            }

            // Verify is throttling is indeed working as expected
            request = new HttpRequestMessage(HttpMethod.Get, $"{uriOdata}?$count=true&$top=1");

            Stopwatch stopwatch = Stopwatch.StartNew();

            using (var response = await _client.SendAsync(request))
            {
                stopwatch.Stop();

                response.EnsureSuccessStatusCode();

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var res = HttpContentHelper.DerializeJsonFromStream<ODataResponse<Person>>(await response.Content.ReadAsStreamAsync());

                // Assert - Only one record returned
                Assert.Single(res.value);

                // Assert - Executed at least as long as throttleDelaySecs
                Assert.True(stopwatch.ElapsedMilliseconds > throttleDelaySecs * 1000);
            }

            // Reset throttleDelaySecs
            request = new HttpRequestMessage(HttpMethod.Post, $"{uriApi}throttle/0");
            request.Content = HttpContentHelper.CreateHttpContent(0);

            using (var response = await _client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

    }
}
