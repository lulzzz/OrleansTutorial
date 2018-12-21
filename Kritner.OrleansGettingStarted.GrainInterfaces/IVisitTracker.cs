using System.Threading.Tasks;
using Orleans;

namespace Kritner.OrleansGettingStarted.GrainInterfaces
{
    public interface IVisitTracker : IGrainWithStringKey
    {
        Task<int> GetNumberOfVisits();
        Task VisitAsync();
    }
}