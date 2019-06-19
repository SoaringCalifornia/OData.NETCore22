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

namespace xUnitTests
{
    /// <summary>
    /// Testing odata/person OData controller
    /// Originally was generic-driven test class but was taken apart and simplified for this example
    /// </summary>
    public class PersonControllerTests
    {
        private readonly IConfiguration _config;
        private readonly IOptions<Settings> _settings;
        private readonly IServiceProvider _serviceProvider;
        private readonly TestServer _server;
        private readonly HttpClient _client;

        private readonly string uri;
        private readonly int idCRUD;
        private readonly int odataCount;

        public PersonControllerTests()
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

            uri = $"/{_settings.Value.routePrefixOData}/{typeof(Person).Name}/";
            odataCount = _settings.Value.testSeedCount;
            idCRUD = odataCount + 1;

            _client = _server.CreateClient();
        }


        [Fact]
        public async Task TestCRUD()
        {
            var entity = DbContextHelper.InitObject(new Person(), idCRUD);

            // GET record which is not exists yet 
            var request = new HttpRequestMessage(HttpMethod.Get, $"{uri}{entity.id}");

            using (var response = await _client.SendAsync(request))
            {
                // Assert
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }

            // POST new record
            request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = HttpContentHelper.CreateHttpContent(entity);

            using (var response = await _client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                // Assert
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            }

            // GET record created above and compare with original 
            request = new HttpRequestMessage(HttpMethod.Get, $"{uri}{entity.id}");

            using (var response = await _client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var res = HttpContentHelper.DerializeJsonFromStream<Person>(await response.Content.ReadAsStreamAsync());

                // Assert
                Assert.Equal(HttpContentHelper.ObjectToString(res), HttpContentHelper.ObjectToString(entity));
            }

            // PUT updated record
            request = new HttpRequestMessage(HttpMethod.Put, $"{uri}{entity.id}");

            entity = DbContextHelper.InitObject(new Person(), idCRUD + 1);
            entity.id = idCRUD;

            request.Content = HttpContentHelper.CreateHttpContent(entity);

            using (var response = await _client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                // Assert
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }

            // GET updated record and compare with original 
            request = new HttpRequestMessage(HttpMethod.Get, $"{uri}{entity.id}");

            using (var response = await _client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var res = HttpContentHelper.DerializeJsonFromStream<Person>(await response.Content.ReadAsStreamAsync());

                // Assert
                Assert.Equal(HttpContentHelper.ObjectToString(res), HttpContentHelper.ObjectToString(entity));
            }

            // PATCH record
            request = new HttpRequestMessage(HttpMethod.Patch, $"{uri}{entity.id}");

            entity = DbContextHelper.InitObject(new Person(), idCRUD + 2);
            entity.id = idCRUD;

            request.Content = HttpContentHelper.CreateHttpContent(entity);

            using (var response = await _client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                // Assert
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }

            // GET updated record and compare with original 
            request = new HttpRequestMessage(HttpMethod.Get, $"{uri}{entity.id}");

            using (var response = await _client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var res = HttpContentHelper.DerializeJsonFromStream<Person>(await response.Content.ReadAsStreamAsync());

                // Assert
                Assert.Equal(HttpContentHelper.ObjectToString(res), HttpContentHelper.ObjectToString(entity));
            }

            // Attempt to POST same record again
            request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = HttpContentHelper.CreateHttpContent(entity);

            using (var response = await _client.SendAsync(request))
            {
                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }

            // DELETE record 
            request = new HttpRequestMessage(HttpMethod.Delete, $"{uri}{entity.id}");

            using (var response = await _client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                // Assert
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }

            // Attempt to GET deleted record 
            request = new HttpRequestMessage(HttpMethod.Get, $"{uri}{entity.id}");

            using (var response = await _client.SendAsync(request))
            {
                // Assert
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Theory]
        [InlineData("GET", 1)]
        [InlineData("GET", 10)]
        [InlineData("GET", 100)]
        public async Task TestCountTop(string method, int top)
        {
            // Arrange
            var request = new HttpRequestMessage(new HttpMethod(method), $"{uri}?$count=true&$top={top}");

            using (var response = await _client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var res = HttpContentHelper.DerializeJsonFromStream<ODataResponse<Person>>(await response.Content.ReadAsStreamAsync());

                // Assert - Top
                Assert.Equal(res.value.Count, top);

                // Assert - Count +/- 1
                Assert.True((res.odataCount >= odataCount) && (res.odataCount <= (odataCount + 1)));
            }
        }

        [Theory]
        [InlineData("id", 1, 10)]
        [InlineData("id", 15, 20)]
        [InlineData("id", 50, 50)]
        public async Task TestFilterOrderAsc(string keyField, int rangeStart, int rangeEnd)
        {
            int expectedCount = rangeEnd - rangeStart + 1;

            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, $"{uri}?$count=true&$orderby={keyField}&$filter=({keyField} ge {rangeStart}) and ({keyField} le {rangeEnd})");

            using (var response = await _client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var res = HttpContentHelper.DerializeJsonFromStream<ODataResponse<Person>>(await response.Content.ReadAsStreamAsync());

                // Assert - Values count 
                Assert.Equal(res.value.Count, expectedCount);

                // Assert - Count
                Assert.Equal(res.odataCount, expectedCount);

                // Assert - first id 
                Assert.Equal(res.value[0].id, rangeStart);

                // Assert - last id 
                Assert.Equal(res.value[res.value.Count - 1].id, rangeEnd);
            }
        }

        [Theory]
        [InlineData("id", 1, 10)]
        [InlineData("id", 15, 20)]
        [InlineData("id", 50, 50)]
        public async Task TestFilterOrderDesc(string keyField, int rangeStart, int rangeEnd)
        {
            int expectedCount = rangeEnd - rangeStart + 1;

            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, $"{uri}?$count=true&$orderby={keyField} desc&$filter=({keyField} ge {rangeStart}) and ({keyField} le {rangeEnd})");

            using (var response = await _client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var res = HttpContentHelper.DerializeJsonFromStream<ODataResponse<Person>>(await response.Content.ReadAsStreamAsync());

                // Assert - Values count 
                Assert.Equal(res.value.Count, expectedCount);

                // Assert - Count
                Assert.Equal(res.odataCount, expectedCount);

                // Assert - first id 
                Assert.Equal(res.value[0].id, rangeEnd);

                // Assert - last id 
                Assert.Equal(res.value[res.value.Count - 1].id, rangeStart);
            }
        }

        [Theory]
        [InlineData("first", "first1", 12)]
        [InlineData("first", "first22", 1)]
        [InlineData("first", "ir", 100)]
        public async Task TestFilterContains(string fieldName, string contains, int expectedCount)
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, $"{uri}?$count=true&$filter=contains({fieldName},'{contains}')");

            using (var response = await _client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var res = HttpContentHelper.DerializeJsonFromStream<ODataResponse<Person>>(await response.Content.ReadAsStreamAsync());

                // Assert - Values count 
                Assert.Equal(res.value.Count, expectedCount);

                // Assert - Count
                Assert.Equal(res.odataCount, expectedCount);

                // Assert 
                foreach (var val in res.value)
                {
                    Assert.Contains(contains, val.first);
                }
            }
        }

        [Theory]
        [InlineData("GET")]
        public async Task TestGetAll(string method)
        {
            // Arrange
            var request = new HttpRequestMessage(new HttpMethod(method), uri);

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
