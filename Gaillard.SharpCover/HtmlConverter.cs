using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gaillard.SharpCover
{
    class HtmlConverter
    {  
        private static int _YellowPercent = 70;
        private static int _RedPercent = 50;

        private static int _MaxDepth = 0;

        public static int ViewHtml(string inFile, string[] args)
        {
            if (args.Length > 2 && args[2] == "--help")
            {
                Console.WriteLine("Arguments required. Call: view html [Options]");
                Console.WriteLine("   Options are:");
                Console.WriteLine("     -red=<int>       Lines below this percentage are colored red, default=70");
                Console.WriteLine("     -yellow=<int>    Lines below this percentage are colored yellow, default=50");
                return 1;
            }

            if (!File.Exists(inFile))
            {
                Console.WriteLine(String.Format("The File \"{0}\" does not exist!", inFile));
                return 1;
            }

            if (!ParseArguments(args))
            {
                return 1;
            }

            Dictionary<string, HitCounter> dict = FillDict(File.ReadAllLines(inFile));

            BuildHtml(dict, inFile.Replace(".txt", ".html"));

            Console.WriteLine("Html successfully generated.");
            return 0;
        }

        private static bool ParseArguments(string[] args)
        {
            for (int i=2; i<args.Length; i++)
            {
                string[] split = args[i].Split('=');
                if (split.Length != 2)
                {
                    Console.WriteLine(string.Format("Invalid Arguments: {0}", args[i]));
                    return false;
                }

                int val;
                if (!int.TryParse(split[1], out val))
                {
                    Console.WriteLine(string.Format("Cannot parse number: {0}", args[i]));
                    return false;
                }

                if (val < 0 || val > 100)
                {
                    Console.WriteLine(string.Format("Invalid Value: \"{0}\". The percentage must be between 0 and 100", args[i]));
                    return false;
                }

                switch (split[0])
                {
                    case "-yellow":
                        _YellowPercent = val;
                        break;
                    case "-red":
                        _RedPercent = val;
                        break;
                    default:
                        Console.WriteLine(string.Format("Unknown Argument: {0}", args[i]));
                        return false;
                }
            }

            if (_YellowPercent < _RedPercent)
            {
                Console.WriteLine(string.Format("Warning: The yellow percentage ({0}) should always be higher than the red percentage ({1}), " +
                    "otherwise the yellow color will not be used.", _YellowPercent, _RedPercent));
            }
            return true;
        }

        private static Dictionary<string, HitCounter> FillDict(string[] lines)
        {
            Dictionary<string, HitCounter> dict = new Dictionary<string, HitCounter>();
            foreach (string line in lines)
            {
                string functionName = line.Substring(1).Split('|')[0];
                if (functionName.StartsWith("-", StringComparison.Ordinal) || 
                    functionName.StartsWith("Content-Type:", StringComparison.Ordinal) || 
                    functionName.StartsWith("Content-Length:", StringComparison.Ordinal) ||
                    !functionName.Contains(' ') ||
                    !functionName.Contains("::") ||
                    string.IsNullOrWhiteSpace(functionName))
                {
                    continue;
                }

                HitCounter c;

                if (!dict.TryGetValue(functionName, out c))
                {
                    c = new HitCounter();
                    dict.Add(functionName, c);

                    int depth = SplitCall(functionName).Length;
                    if (_MaxDepth < depth)
                        _MaxDepth = depth;
                }

                if (line[0] == '+')
                {
                    c.Hit++;
                }
                else if (line[0] == '-')
                {
                    c.Miss++;
                }
            }

            return dict;
        }

        private static string SplitCallTrace(string call)
        {
            string[] splitSpace = call.Split(' ');

            if (splitSpace.Length == 1)
                return "";

            string[] splitColon = splitSpace[1].Split(new char[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);

            return splitColon[0];
        }

        private static string[] SplitCall(string call)
        {
            string[] splitSpace = call.Split(' ');
            string[] splitColon = splitSpace[1].Split(new char[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
            string[] splitDot = splitColon[0].Split('.');

            string[] o = new string[splitDot.Length + 1];
            Array.Copy(splitDot, o, splitDot.Length);
            o[splitDot.Length] = splitColon[1];

            return o;
        }


        private static Dictionary<string, HitCounter> GetEntriesOnLevel(Dictionary<string, HitCounter> dict, string prefix)
        {
            Dictionary<string, HitCounter> dictOut = new Dictionary<string, HitCounter>();
            foreach (var x in dict)
            {
                if ( SplitCallTrace(x.Key).StartsWith(prefix, StringComparison.Ordinal))
                {
                    dictOut.Add(x.Key, x.Value);
                }
            }

            return dictOut;
        }

        private static string PrintRecursive(Dictionary<string, HitCounter> dict, string prefix, int level)
        {
            if (prefix.Contains(':'))
            {
                foreach (var entry in dict)
                {
                    if (entry.Key.Contains(prefix))
                    {
                        return AddEntry(prefix, level, entry.Value);
                    }
                }
            }

            Dictionary<string, HitCounter> d = GetEntriesOnLevel(dict, prefix);

            HitCounter c = GetSumm(d);

            StringBuilder s = new StringBuilder();
            s.Append(AddEntry(prefix, level, c));

            List<string> prefixes = new List<string>();

            level++;
            foreach (var entry in d)
            {
                string[] traceSplit = SplitCall(entry.Key);

                string nextPrefix = "";
                for (int i=0; i<level; i++)
                {
                    if (i > 0)
                    {
                        if (traceSplit[i].Contains('('))
                        {
                            nextPrefix += "::";
                        }
                        else
                        {
                            nextPrefix += ".";
                        }
                    }
                    nextPrefix += traceSplit[i];
                }
                if (!prefixes.Contains(nextPrefix))
                {
                    prefixes.Add(nextPrefix);
                    s.Append(PrintRecursive(d, nextPrefix, level));
                }
            }

            return s.ToString();
        }

        private static HitCounter GetSumm(Dictionary<string, HitCounter> dict)
        {
            HitCounter c = new HitCounter();
            foreach (var x in dict)
            {
                c += x.Value;
            }
            return c;
        }

        private static string AddEntry(string name, int level, HitCounter c)
        {
            StringBuilder s = new StringBuilder();
            double percent = (c.Hit / (double)(c.Hit + c.Miss)) * 100;
            s.Append("<tr ");
            if (percent < _RedPercent)
            {
                s.Append("bgcolor=\"#FF5050\" ");
            }
            else if (percent < _YellowPercent)
            {
                s.Append("bgcolor=\"FFFF00\" ");
            }
            else
            {
                s.Append("bgcolor=\"00FF00\" ");
            }
            s.Append(string.Format("data-depth=\"{0}\" class=\"collapse level{0}\">", level));
            s.Append(string.Format("<td>{0}{1}%</td>", name.Contains("::")?"":"<span class=\"toggle collapse\"></span>", Math.Round(percent, 2)));
            s.Append(string.Format("<td>{0}</td>", c.Hit));
            s.Append(string.Format("<td>{0}</td>", c.Miss));
            s.Append(string.Format("<td>{0}</td>", string.IsNullOrWhiteSpace(name) ? "/" : name));
            s.Append("</tr>\n");

            return s.ToString();
        }

        private static string GetResource(string resource)
        {
            Stream s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
            using (StreamReader reader = new StreamReader(s, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private static void BuildHtml(Dictionary<string, HitCounter> dict, string outPath)
        {
            StringBuilder s = new StringBuilder();

            s.Append("<!DOCTYPE html>\n<html>\n");
            s.Append("<head>\n");

            s.Append(string.Format("<script type=\"text/javascript\">\n{0}\n</script>\n\n", GetResource("SharpCoverHtmlConverter.Resources.jquery.js")));
            s.Append(string.Format("<style>\n{0}</style>\n\n", GetResource("SharpCoverHtmlConverter.Resources.normalize.css")));

            s.Append("<style type=\"text/css\">\ntable td {\nborder: 1px solid #eee;\n}\n");
            for (int i=1; i<=_MaxDepth; i++)
            {
                s.Append(string.Format(".level{0} td:first-child {{\npadding-left: {1}px;\n}}\n", i, i * 15));
            }
            s.Append(".collapse .toggle {\n    background: url(\"http://mleibman.github.com/SlickGrid/images/collapse.gif\");\n}\n.expand .toggle {\n    background: url(\"http://mleibman.github.com/SlickGrid/images/expand.gif\");\n}\n.toggle {\n    height: 9px;\n    width: 9px;\n    display: inline-block;   \n}\n</style>\n\n");

            s.Append(string.Format("<script type=\"text/javascript\">\n{0}\n</script>\n", GetResource("SharpCoverHtmlConverter.Resources.toggleScript.js")));
            s.Append("</head>\n\n");

            s.Append("<body>\n");
            s.Append("<table id=\"mytable\">\n");
            s.Append("<tbody>\n");
            s.Append("<tr> <th>%</th> <th>Hit</th> <th>Miss</th> <th>Name</th> </tr>\n");
            s.Append(PrintRecursive(dict, "", 0));

            s.Append("</tbody>");
            s.Append("</table>\n");
            s.Append("</body>\n");
            s.Append("</html>");

            File.WriteAllText(outPath, s.ToString());
        }
    }
}
