using System;
using System.Threading.Tasks;
using Kritner.OrleansGettingStarted.GrainInterfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;

namespace Kritner.OrleansGettingStarted.Grains
{
    public class VisitTracker : Grain<VisitTrackerState>, IVisitTracker
    {
        private readonly ILogger _logger;

        public VisitTracker(ILogger<VisitTracker> logger)
        {
            _logger = logger;
        }

        public Task<int> GetNumberOfVisits()
        {
            return Task.FromResult(State.NumberOfVisits);
        }

        public async Task VisitAsync()
        {
            var now = DateTime.Now;
            if (!State.FirstVisit.HasValue)
            {
                State.FirstVisit = now;
            }

            State.NumberOfVisits++;
            State.LastVisit = now;

            await WriteStateAsync();

            var helloWorld = GrainFactory.GetGrain<IHelloWorld>(Guid.NewGuid());
            var text = await helloWorld.SayHello($"{this.GetPrimaryKeyString()}");
            _logger.LogInformation($"in visiting, has this greeting text={text}");
        }
    }

    public class VisitTrackerState
    {
        public DateTime? FirstVisit { get; set; }
        public DateTime? LastVisit { get; set; }
        public int NumberOfVisits { get; set; }
    }
}