using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WalletService.Data;

namespace WalletService.IntegrationTest.Hooks
{
    [Binding]
    public class TestServerHook
    {
        private readonly ScenarioContext _scenarioContext;
        private TestServer _server;
        private HttpClient _client;

        public TestServerHook(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [BeforeScenario]
        public void BeforeScenario()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Test.json")
                .Build();
            var builder = new WebHostBuilder().UseConfiguration(configuration).UseStartup<Startup>();
            _server = new TestServer(builder);

            _client = _server.CreateClient();
            _scenarioContext.Set(_client, "HttpClient");
        }

        [AfterScenario]
        public void AfterScenario()
        {
            using (var serviceScope = _server.Host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;
                var dbContext = services.GetRequiredService<WalletDbContext>();
                dbContext.Dispose();
            }
            _client.Dispose();
            _server.Dispose();
        }
    }
}