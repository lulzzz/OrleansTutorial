using System;
using System.Net;
using System.Threading.Tasks;
using Kritner.OrleansGettingStarted.Grains;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;


namespace Kritner.OrleansGettingStarted.SiloHost
{
    class Program
    {
        public static async Task<int> Main(string[] arg)
        {
            try
            {
                var host = await StartSilo();
                Console.WriteLine("Press Enter to terminate...");
                Console.ReadLine();

                await host.StopAsync();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static async Task<ISiloHost> StartSilo()
        {
            // define the cluster configuration
            var builder = new SiloHostBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "HelloWorldApp";
                })
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(VisitTracker).Assembly).WithReferences())
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.AddDebug();
                });

            

            //builder.AddMemoryGrainStorage(MyDefineConstants.MemoryProviderName);
            builder.AddCosmosDBGrainStorageAsDefault(options =>
            {
                options.AccountEndpoint = @"https://localhost:8081";
                options.AccountKey =
                    @"C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
                options.DB = "orleans-persist";
                options.Collection = @"orleans_data";

                //reset Cosmos DB on silo start
                options.DropDatabaseOnInit = false;

                //be sure to add this because Grains' CRUD is base on store procedure.
                options.AutoUpdateStoredProcedures = true;

                options.CanCreateResources = true;
                options.DeleteStateOnClear = true;
            });

            var host = builder.Build();
            await host.StartAsync();
            return host;
        }
    }
}
