using System;
using System.Reflection;
using TradingPlatform.BusinessLayer;

namespace CheckDOM
{
    class Program
    {
        static void Main()
        {
            try
            {
                var type = typeof(IChartWindow);
                Console.WriteLine("EVENTS:");
                foreach(var e in type.GetEvents(BindingFlags.Public | BindingFlags.Instance))
                {
                    Console.WriteLine($"{e.EventHandlerType.Name} {e.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
