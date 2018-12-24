using System;
using System.Threading.Tasks;
using Kritner.OrleansGettingStarted.GrainInterfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Logging;
using Orleans.Runtime;
using Serilog;
using Serilog.Events;

namespace OrleansClientDemo
{
    class Program
    {
        const int initializeAttemptsBeforeFailing = 5;

        private static int attempt = 0;

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

            return await RunMainAsync();
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

            var client = new ClientBuilder()
                .UseMongoDBClustering(options =>
                {
                    options.ConnectionString = @"mongodb://localhost:27017";
                    options.DatabaseName = @"orleans_conf-Clustering";
                    // see:https://github.com/OrleansContrib/Orleans.Providers.MongoDB/issues/54
                    options.CollectionPrefix = "demo";
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "HelloWorldApp";
                })
                .ConfigureApplicationParts(manager =>
                {
                    manager.AddApplicationPart(typeof(IVisitTracker).Assembly).WithReferences();
                })
                .ConfigureLogging(builder =>
                {
                    builder.AddSerilog(dispose: true);
                })
                .Build();

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
