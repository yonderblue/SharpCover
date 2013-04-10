using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

[assembly: AssemblyVersion("1.0.0.*")]

namespace Gaillard.SharpCover
{
    public static class Counter
    {
        private static BinaryWriter writer;
        private static readonly ISet<int> indexes = new HashSet<int>();

        public static void Count(string path, int index)
        {
            if (writer == null) {
                writer = new BinaryWriter(File.Open(path, FileMode.Append));
            }

            if (indexes.Add(index))
                writer.Write(index);
        }
    }
}
