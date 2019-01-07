using System;
using System.IO;
using System.Threading.Tasks;
using Kritner.OrleansGettingStarted.GrainInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Logging;
using Orleans.Runtime;
using OrleansClientDemo.Settings;
using Serilog;
using Serilog.Events;

namespace OrleansClientDemo
{
    class Program
    {
        const int initializeAttemptsBeforeFailing = 5;

        private static int attempt = 0;

        private static ClusterInfoSettings _clusterInfo;
        private static OrleansProviderSettings _providerInfo;

        static async Task<int> Main(string[] args)
        {
            var logConfig = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Trace()
                .WriteTo.Debug();

            Log.Logger = logConfig.CreateLogger();

            ReadJsonFileSettings(args);

            return await RunMainAsync();
        }

        private static void ReadJsonFileSettings(string[] args)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables(prefix: "ORLEANS_CLIENT_APP_")
                .AddCommandLine(args);

            var config = builder.Build().GetSection("Orleans");

            _clusterInfo = new ClusterInfoSettings();
            config.GetSection("Cluster").Bind(_clusterInfo);


            _providerInfo = new OrleansProviderSettings();
            config.GetSection("Provider").Bind(_providerInfo);
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                Console.WriteLine("Press enter to begin connect");
                Console.ReadLine();
                using (var client = await StartClientWithRetries())
                {
                    await StatefulWorkDemo.DoStatefulWork(client);

                    Console.WriteLine("Press any key to stop Client.");
                    Console.ReadKey();
                    await client.Close();
                }

                return 0;
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
                return 1;
            }
        }

        private static async Task<IClusterClient> StartClientWithRetries()
        {
            attempt = 0;

            var clientBuilder = new ClientBuilder();

            if (_providerInfo.DefaultProvider == "MongoDB")
            {
                clientBuilder.UseMongoDBClustering(options =>
                {
                    var mongoSetting = _providerInfo.MongoDB.Cluster;
                    options.ConnectionString = mongoSetting.DbConn;
                    options.DatabaseName = mongoSetting.DbName;

                    // see:https://github.com/OrleansContrib/Orleans.Providers.MongoDB/issues/54
                    options.CollectionPrefix = mongoSetting.CollectionPrefix;
                })
                .Configure<ClientMessagingOptions>(options =>{
                    options.ResponseTimeout = TimeSpan.FromSeconds(20);
                    options.ResponseTimeoutWithDebugger = TimeSpan.FromMinutes(60);
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "HelloWorldApp";
                });
            }


            clientBuilder.ConfigureApplicationParts(manager =>
            {
                manager.AddApplicationPart(typeof(IVisitTracker).Assembly).WithReferences();
            })
            .ConfigureLogging(builder =>
            {
                builder.AddSerilog(dispose: true);
            });
            var client = clientBuilder.Build();

            await client.Connect(RetryFilter);
            Console.WriteLine("Client successfully connect to silo host");

            return client;
        }

        private static async Task<bool> RetryFilter(Exception exception)
        {

            if (exception.GetType() != typeof(SiloUnavailableException))
            {
                Console.WriteLine($"Cluster client failed to connect to cluster with unexpected error.  Exception: {exception}");
                return false;
            }

            attempt++;

            Console.WriteLine($"Cluster client attempt {attempt} of {initializeAttemptsBeforeFailing} failed to connect to cluster.  Exception: {exception}");

            if (attempt > initializeAttemptsBeforeFailing)
            {
                return false;
            }

            await Task.Delay(TimeSpan.FromSeconds(4));
            return true;
        }
    }
}
