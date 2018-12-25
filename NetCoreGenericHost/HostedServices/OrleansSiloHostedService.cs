using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Kritner.OrleansGettingStarted.Grains;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCoreGenericHost.Settings;
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
        private IOptions<SiloConfigSettings> _siloOptions;
        private IOptions<OrleansProviderSettings> _providerOptions;
        private IOptions<OrleansDashboardSettings> _dashboardOptions;

        public OrleansSiloHostedService(IApplicationLifetime applicationLifetime,
            IOptions<SiloConfigSettings> siloOptions,
            IOptions<OrleansProviderSettings> providerOptions,
            IOptions<OrleansDashboardSettings> dashboardOptions,
            ILogger<OrleansSiloHostedService> logger)
        {
            _applicationLifetime = applicationLifetime;
            _siloOptions = siloOptions;
            _providerOptions = providerOptions;
            _dashboardOptions = dashboardOptions;

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
            try
            {
                await _siloHost.StartAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Start up Silo Host failed");
                throw;
            }
        }

        private async void OnApplicationStopping()
        {
            _logger.LogInformation("stopping Orleans silo host...");

            try
            {
                await _siloHost.StopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when shutdown Silo Host");
            }
        }

        private ISiloHost CreateSiloHost()
        {
            var builder = new SiloHostBuilder();

            if (_dashboardOptions.Value.Enable)
            {
                builder.UseDashboard(options =>
                {
                    options.Port = _dashboardOptions.Value.Port;
                });
            }

            builder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = _siloOptions.Value.ClusterId;
                options.ServiceId = _siloOptions.Value.ServiceId;
            });

            if (string.IsNullOrEmpty(_siloOptions.Value.AdvertisedIp) || "*".Equals(_siloOptions.Value.AdvertisedIp))
            {
                builder.ConfigureEndpoints(_siloOptions.Value.SiloPort, _siloOptions.Value.GatewayPort,
                    listenOnAnyHostAddress: _siloOptions.Value.ListenOnAnyHostAddress);
            }
            else
            {
                var ip = IPAddress.Parse(_siloOptions.Value.AdvertisedIp);

                builder.ConfigureEndpoints(ip, _siloOptions.Value.SiloPort, _siloOptions.Value.GatewayPort,
                    listenOnAnyHostAddress: _siloOptions.Value.ListenOnAnyHostAddress);
            }

            if (_providerOptions.Value.DefaultProvider == "MongoDB")
            {
                var mongoDbOption = _providerOptions.Value.MongoDB;
                builder.UseMongoDBClustering(options =>
                {
                    var clusterOption = mongoDbOption.Cluster;

                    options.ConnectionString = clusterOption.DbConn;
                    options.DatabaseName = clusterOption.DbName;

                    // see:https://github.com/OrleansContrib/Orleans.Providers.MongoDB/issues/54
                    options.CollectionPrefix = clusterOption.CollectionPrefix;
                })
                .UseMongoDBReminders(options =>
                {
                    var reminderOption = mongoDbOption.Reminder;

                    options.ConnectionString = reminderOption.DbConn;
                    options.DatabaseName = reminderOption.DbName;

                    if (!string.IsNullOrEmpty(reminderOption.CollectionPrefix))
                    {
                        options.CollectionPrefix = reminderOption.CollectionPrefix;
                    }

                })
                .AddMongoDBGrainStorageAsDefault(options =>
                {
                    var storageOption = mongoDbOption.Storage;

                    options.ConnectionString = storageOption.DbConn;
                    options.DatabaseName = storageOption.DbName;

                    if (!string.IsNullOrEmpty(storageOption.CollectionPrefix))
                    {
                        options.CollectionPrefix = storageOption.CollectionPrefix;
                    }
                });
            }

            builder.ConfigureServices(services =>
            {
                services
                    .AddLogging(loggingBuilder => loggingBuilder.AddSerilog())
                    .AddTransient<VisitTracker>();
            })
            .ConfigureApplicationParts(parts =>
            {
                parts.AddFromApplicationBaseDirectory().WithReferences();
            })
            .ConfigureLogging(logging =>
            {
                logging.AddSerilog(dispose: true);
            })
            ;

            var host = builder.Build();
            
            return host;
        }
    }
}