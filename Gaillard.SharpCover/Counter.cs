using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

[assembly: AssemblyVersion("1.0.1.*")]

namespace Gaillard.SharpCover
{
    public static class Counter
    {
        private static object mutex = new object();
        private static BinaryWriter writer;
        private static readonly IDictionary<string,ISet<int>> indexes = new Dictionary<string,ISet<int>>();
        private static bool registered = false;
        private static ISet<int> set = null;

        public static void Count(string path, int index)
        {
            lock (mutex) {
                if (!registered) {
                    AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
                    registered = true;
                }

                if (!indexes.TryGetValue(path, out set)) {
                    indexes.Add(path, new HashSet<int>{index});
                } else {
                    set.Add(index);
                }
            }
        }

        private static void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            lock (mutex) {
                foreach (var kvp in indexes) {
                    using (var writer = new BinaryWriter(File.Open(kvp.Key, FileMode.Append))) {
                        foreach(var i in kvp.Value)
                            writer.Write(i);
                    }
                }
            }
        }
    }
}
