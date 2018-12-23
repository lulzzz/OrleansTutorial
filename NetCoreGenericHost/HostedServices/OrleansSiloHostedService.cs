using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Kritner.OrleansGettingStarted.GrainInterfaces;
using Kritner.OrleansGettingStarted.Grains;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;

namespace NetCoreGenericHost.HostedServices
{
    public class OrleansSiloHostedService : IHostedService
    {
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly ILogger<OrleansSiloHostedService> _logger;
        private ISiloHost _siloHost;

        public OrleansSiloHostedService(IApplicationLifetime applicationLifetime, ILogger<OrleansSiloHostedService> logger)
        {
            _applicationLifetime = applicationLifetime;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("register application life time");
            _applicationLifetime.ApplicationStarted.Register(OnApplicationStartedAsync);
            _applicationLifetime.ApplicationStopping.Register(OnApplicationStopping);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //do nothing
            return Task.CompletedTask;
        }

        private async void OnApplicationStartedAsync()
        {
            _logger.LogInformation("initialize Orleans silo host...");

            _siloHost = CreateSiloHost();
           await _siloHost.StartAsync();
        }

        private async void OnApplicationStopping()
        {
            _logger.LogInformation("stopping Orleans silo host...");
            await _siloHost.StopAsync();
        }

        private static ISiloHost CreateSiloHost()
        {
            // define the cluster configuration
            var builder = new SiloHostBuilder()
                .UseDashboard(options => { options.Port = 8099; })
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "HelloWorldApp";
                })
                .ConfigureServices(services =>
                {
                    services
                        .AddLogging(loggingBuilder => loggingBuilder.AddSerilog())
                        .AddTransient<VisitTracker>();
                })
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(VisitTracker).Assembly).WithReferences())
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                //.ConfigureLogging(logging =>
                //{
                //    logging.AddConsole();
                //    logging.AddDebug();
                //})
                ;

            builder.AddMemoryGrainStorageAsDefault();

            var host = builder.Build();
            return host;
        }
    }
}