using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

[assembly: AssemblyVersion("1.0.2.*")]

namespace Gaillard.SharpCover
{
    public static class Counter
    {
        [ThreadStatic]
        private static HashSet<int> indexes;

        [ThreadStatic]
        private static BinaryWriter writer;

        [ThreadStatic]
        private static string path;

        public static void Count(string pathPrefix, int index)
        {
            if (path == null) {
                path = pathPrefix + "|" + Process.GetCurrentProcess().Id + "|" + Thread.CurrentThread.ManagedThreadId;
                indexes = new HashSet<int>();
                writer = new BinaryWriter(File.Open(path, FileMode.CreateNew));
            }

            if (indexes.Add(index))
                writer.Write(index);
        }
    }
}
