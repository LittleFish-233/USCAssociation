using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USCAssociation.RobotClient.Tools.Extensions
{
    public static class ListExtensions
    {
        public static List<T> Random<T>(this List<T> sources)
        {
            var rd = new Random();
            var index = 0;
            T temp;
            for (var i = 0; i < sources.Count; i++)
            {
                index = rd.Next(0, sources.Count - 1);
                if (index != i)
                {
                    temp = sources[i];
                    sources[i] = sources[index];
                    sources[index] = temp;
                }
            }
            return sources;
        }
    }
}
