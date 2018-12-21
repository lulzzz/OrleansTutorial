using System;
using System.Threading.Tasks;
using Kritner.OrleansGettingStarted.GrainInterfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;

namespace Kritner.OrleansGettingStarted.Client
{
    class Program
    {
        const int initializeAttemptsBeforeFailing = 3;

        private static int attempt = 0;

        static async Task<int> Main(string[] args)
        {
            return await RunMainAsync();
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                using (var client = await StartClientWithRetries())
                {
                    ///await DoClientWork(client);
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
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "HelloWorldApp";
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.AddDebug();
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

        /*
        private static async Task DoClientWork(IClusterClient client)
        {
            Console.WriteLine("Hello, what should I call you?");
            var name = Console.ReadLine();

            if (string.IsNullOrEmpty(name))
            {
                name = "anon";
            }

            // example of calling grains from the initialized client
            var grain = client.GetGrain<IHelloWorld>(Guid.NewGuid());
            Console.WriteLine("Grain instance should be re-use");
            Console.ReadKey();
            Console.WriteLine($"{await grain.SayHello("1")}");
            Console.WriteLine($"{await grain.SayHello("2")}");
            Console.WriteLine($"{await grain.SayHello("3")}");

            
            Console.WriteLine("Can create multiple grain instance");
            var grain1 = client.GetGrain<IHelloWorld>(Guid.NewGuid());
            var grain2 = client.GetGrain<IHelloWorld>(Guid.NewGuid());
            var grain3 = client.GetGrain<IHelloWorld>(Guid.NewGuid());
            Console.WriteLine($"{await grain1.SayHello("instance_1")}");
            Console.WriteLine($"{await grain2.SayHello("instance_2")}");
            Console.WriteLine($"{await grain3.SayHello("instance_3")}");
        }
        */
    }
}
