using System;
using System.Threading.Tasks;
using Kritner.OrleansGettingStarted.GrainInterfaces;
using Orleans;

namespace Kritner.OrleansGettingStarted.Client
{
    public class StatefulWorkDemo
    {
        public static async Task DoStatefulWork(IClusterClient client)
        {
            var kritnerGrain = client.GetGrain<IVisitTracker>("kritner@gmail.com");
            var notKritnerGrain = client.GetGrain<IVisitTracker>("notKritner@gmail.com");
            await PrettyPrintGrainVisits(kritnerGrain);
            await PrettyPrintGrainVisits(notKritnerGrain);
            PrintSeparatorThing();

            Console.WriteLine("Ayyy some people are visiting!");
            await kritnerGrain.VisitAsync();
            await kritnerGrain.VisitAsync();
            await notKritnerGrain.VisitAsync();
            PrintSeparatorThing();

            await PrettyPrintGrainVisits(kritnerGrain);
            await PrettyPrintGrainVisits(notKritnerGrain);
            PrintSeparatorThing();

            Console.Write("ayyy kritner's visiting even more!");
            for (int i = 0; i < 5; i++)
            {
                await kritnerGrain.VisitAsync();
            }
            PrintSeparatorThing();
            await PrettyPrintGrainVisits(kritnerGrain);
            await PrettyPrintGrainVisits(notKritnerGrain);
        }
        private static async Task PrettyPrintGrainVisits(IVisitTracker grain)
        {
            Console.WriteLine($"{grain.GetPrimaryKeyString()} has visited {await grain.GetNumberOfVisits()} times");
        }
        private static void PrintSeparatorThing()
        {
            Console.WriteLine($"{Environment.NewLine}-----{Environment.NewLine}");
        }
    }
}