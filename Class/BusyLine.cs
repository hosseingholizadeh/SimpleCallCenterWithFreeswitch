using System.Collections.Generic;
using System.Linq;

namespace FreeswitchListenerServer.Class
{
    internal class BusyLine
    {
        private static List<string> List = new List<string>();
        private static object locker = new object();

        internal static void Add(string number)
        {
            lock (locker)
            {
                if (!string.IsNullOrWhiteSpace(number) && !List.Contains(number))
                    List.Add(number);
            }
        }

        internal static void Remove(string number)
        {
            var item = List.FirstOrDefault(p => p == number);
            if (item != null)
                List.Remove(item);
        }

        internal static bool IsExists(string number)
            => List.Any(p => p == number);
    }
}
