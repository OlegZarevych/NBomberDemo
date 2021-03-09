using System;
using System.Net.Http;
using System.Text;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Plugins.Http.CSharp;
using NBomber.Plugins.Network.Ping;
using Newtonsoft.Json;
using Serilog;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseUrl = "https://exploresolutionapi.azurewebsites.net";
            string loginUrl = baseUrl + "/api/Authentication/request";
            string getAllToursUrl = baseUrl + "/api/tours/getAll";

            // Step

            var loginStep = HttpStep.Create("sign In", context => 
            
                Http.CreateRequest("POST", loginUrl)
                .WithBody(new StringContent(JsonConvert.SerializeObject(new Login { Username = "user", Password = "pass" }), Encoding.UTF8, "application/json"))
                .WithCheck(async response =>
                {
                    string token = await response.Content.ReadAsStringAsync();
                    context.Data.Add("token", token);
                    return token != string.Empty ? Response.Ok() : Response.Fail();
                }));

            var getToursStep = HttpStep.Create("Get all tours", context =>
            {
                var authToken = context.Data["token"];
                return Http.CreateRequest("GET", getAllToursUrl)
                           .WithHeader("Authorization", $"Bearer {authToken}");
            });

            // Scenario
            var scenario = ScenarioBuilder.CreateScenario("Get All Tours Scenario", new[] { loginStep, getToursStep })
                .WithWarmUpDuration(TimeSpan.FromSeconds(10))
                .WithLoadSimulations(new[]
                {
                    Simulation.RampConstant(copies: 10, during: TimeSpan.FromSeconds(30)),
                    Simulation.KeepConstant(copies: 10, during: TimeSpan.FromSeconds(30))
                }) ;

            var pingPluginConfig = PingPluginConfig.CreateDefault(new[] { baseUrl });
            var pingPlugin = new PingPlugin(pingPluginConfig);

            var result = NBomberRunner.RegisterScenarios(scenario)
                .WithWorkerPlugins(new[] { pingPlugin })
                .WithTestSuite("http")
                .WithTestName("tracing_test")
                .WithLoggerConfig(() => new LoggerConfiguration().MinimumLevel.Verbose()) // set log to verbose
                .WithReportFolder("ReportFolder123")
                .Run();

            // result.ScenarioStats[0].FailCount
        }
    }

    public class Login
    {
        public string Username { get; set; }

        public string Password { get; set; }
    }
}
