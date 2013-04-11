using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

[assembly: AssemblyVersion("1.0.0.*")]

namespace Gaillard.SharpCover
{
    public static class Program
    {
        public const string RESULTS_FILENAME = "coverageResults.txt", MISS_PREFIX = "MISS ! ";
        private const string KNOWNS_FILENAME = "coverageKnowns.txt", HITS_FILENAME = "coverageHits.txt", MISSES_FILENAME = "coverageMisses.txt";
        private static readonly MethodInfo countMethodInfo = typeof(Counter).GetMethod("Count");

        //immutable
        private sealed class InstrumentConfig
        {
            public readonly IEnumerable<string> AssemblyPaths;
            public readonly string TypeInclude,
                                   TypeExclude,
                                   MethodInclude,
                                   MethodExclude,
                                   HitsPath = Path.Combine(Directory.GetCurrentDirectory(), HITS_FILENAME);
            private readonly IDictionary<string, IEnumerable<int>> methodOffsetExcludes = new Dictionary<string, IEnumerable<int>>();
            private readonly IDictionary<string, IEnumerable<string>> methodLineExcludes = new Dictionary<string, IEnumerable<string>>();

            public InstrumentConfig(string json)
            {
                if (File.Exists(json))
                    json = File.ReadAllText(json);

                var config = JObject.Parse(json);

                AssemblyPaths = config.SelectToken("assemblies", true).Values<string>();
                TypeInclude = ((string)config.SelectToken("typeInclude")) ?? ".*";
                TypeExclude = ((string)config.SelectToken("typeExclude")) ?? ".^";//any char and THEN start of line matches nothing
                MethodInclude = ((string)config.SelectToken("methodInclude")) ?? ".*";
                MethodExclude = ((string)config.SelectToken("methodExclude")) ?? ".^";//any char and THEN start of line matches nothing


                foreach (var methodBodyExclude in config.SelectToken("methodBodyExcludes") ?? new JArray()) {
                    var method = (string)methodBodyExclude.SelectToken("method", true);
                    var offsets = (methodBodyExclude.SelectToken("offsets") ?? new JArray()).Values<int>();
                    var lines = (methodBodyExclude.SelectToken("lines") ?? new JArray()).Values<string>();
                    methodOffsetExcludes.Add(method, offsets);
                    methodLineExcludes.Add(method, lines);
                }
            }

            public bool HasOffset(string method, int offset)
            {
                IEnumerable<int> offsets;
                return methodOffsetExcludes.TryGetValue(method, out offsets) && offsets.Contains(offset);
            }

            public bool HasLine(string method, string line)
            {
                IEnumerable<string> lines;
                return methodLineExcludes.TryGetValue(method, out lines) && lines.Select(l => l.Trim()).Contains(line.Trim());
            }
        }

        private static void Instrument(Instruction instruction,
                                       MethodReference countReference,
                                       MethodDefinition method,
                                       ILProcessor worker,
                                       string lastLine,
                                       InstrumentConfig config,
                                       TextWriter writer,
                                       ref int instrumentIndex)
        {
            //if the previous instruction is a Prefix instruction then this instruction MUST go with it.
            //we cannot put an instruction between the two.
            if (instruction.Previous != null && instruction.Previous.OpCode.OpCodeType == OpCodeType.Prefix)
                return;

            if (config.HasOffset(method.FullName, instruction.Offset))
                return;

            if (lastLine != null && config.HasLine(method.FullName, lastLine)) {
                return;
            }

            var lineNum = -1;
            if (instruction.SequencePoint != null)
                lineNum = instruction.SequencePoint.StartLine;

            var line = string.Join(", ",
                                   "Method: " + method.FullName,
                                   "Line: " + lineNum,
                                   "Offset: " + instruction.Offset,
                                   "Instruction: " + instruction);

            writer.WriteLine(line);

            var pathParamLoadInstruction = worker.Create(OpCodes.Ldstr, config.HitsPath);
            var lineParamLoadInstruction = worker.Create(OpCodes.Ldc_I4, instrumentIndex);
            var registerInstruction = worker.Create(OpCodes.Call, countReference);

            //inserting method before instruction  because after will not happen after a method Ret instruction
            worker.InsertBefore(instruction, pathParamLoadInstruction);
            worker.InsertAfter(pathParamLoadInstruction, lineParamLoadInstruction);
            worker.InsertAfter(lineParamLoadInstruction, registerInstruction);

            ++instrumentIndex;

            //change try/finally etc to point to our first instruction if they referenced the one we inserted before
            foreach (var handler in method.Body.ExceptionHandlers) {
                if (handler.FilterStart == instruction)
                    handler.FilterStart = pathParamLoadInstruction;

                if (handler.TryStart == instruction)
                    handler.TryStart = pathParamLoadInstruction;
                if (handler.TryEnd == instruction)
                    handler.TryEnd = pathParamLoadInstruction;

                if (handler.HandlerStart == instruction)
                    handler.HandlerStart = pathParamLoadInstruction;
                if (handler.HandlerEnd == instruction)
                    handler.HandlerEnd = pathParamLoadInstruction;
            }

            //change instructions with a target instruction if they referenced the one we inserted before to be our first instruction
            foreach (var iteratedInstruction in method.Body.Instructions) {
                var operand = iteratedInstruction.Operand;
                if (operand == instruction) {
                    iteratedInstruction.Operand = pathParamLoadInstruction;
                    continue;
                }

                if (!(operand is Instruction[]))
                    continue;

                var operands = (Instruction[])operand;
                for (var i = 0; i < operands.Length; ++i) {
                    if (operands[i] == instruction)
                        operands[i] = pathParamLoadInstruction;
                }
            }
        }

        private static void Instrument(MethodDefinition method, MethodReference countReference, InstrumentConfig config, TextWriter writer, ref int instrumentIndex)
        {
            if (!Regex.IsMatch(method.FullName, config.MethodInclude) || Regex.IsMatch(method.FullName, config.MethodExclude))
                return;

            var worker = method.Body.GetILProcessor();

            method.Body.SimplifyMacros();

            string lastLine = null;//the sequence point for instructions that dont have one is the last set (if one exists)
            //need to copy instruction list since we modify using worker inserts
            foreach (var instruction in new List<Instruction>(method.Body.Instructions).OrderBy(i => i.Offset)) {
                var sequencePoint = instruction.SequencePoint;
                if (sequencePoint != null) {
                    var line = File.ReadLines(sequencePoint.Document.Url).ElementAtOrDefault(sequencePoint.StartLine - 1);
                    if (line != null)
                        lastLine = line;
                }

                Instrument(instruction, countReference, method, worker, lastLine, config, writer, ref instrumentIndex);
            }

            method.Body.OptimizeMacros();
        }

        private static void Instrument(TypeDefinition type, MethodReference countReference, InstrumentConfig config, TextWriter writer, ref int instrumentIndex)
        {
            if (type.FullName == "<Module>")
                return;

            if (!Regex.IsMatch(type.FullName, config.TypeInclude) || Regex.IsMatch(type.FullName, config.TypeExclude))
                return;

            foreach (var method in type.Methods)
                Instrument(method, countReference, config, writer, ref instrumentIndex);
        }

        private static void Instrument(string assemblyPath, InstrumentConfig config, TextWriter writer, ref int instrumentIndex)
        {
            //Mono.Cecil.[Mdb|Pdb].dll must be alongsize this exe to include sequence points from ReadSymbols
            var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { ReadSymbols = true });
            var countReference = assembly.MainModule.Import(countMethodInfo);

            foreach (var type in assembly.MainModule.GetTypes())//.Types doesnt include nested types
                Instrument(type, countReference, config, writer, ref instrumentIndex);

            assembly.Write(assemblyPath, new WriterParameters { WriteSymbols = true });

            var counterPath = typeof(Counter).Assembly.Location;

            File.Copy(counterPath, Path.Combine(Path.GetDirectoryName(assemblyPath), Path.GetFileName(counterPath)), true);
        }

        private static int Check()
        {
            var hits = new HashSet<int>();
            using (var hitsStream = File.OpenRead(HITS_FILENAME))
            using (var hitsReader = new BinaryReader(hitsStream)) {
                while(hitsStream.Position < hitsStream.Length)
                    hits.Add(hitsReader.ReadInt32());
            }

            var missCount = 0;
            var knownIndex = 0;

            using (var resultsWriter = new StreamWriter(RESULTS_FILENAME)) {//overwrites
                foreach (var knownLine in File.ReadLines(KNOWNS_FILENAME)) {
                    if (hits.Contains(knownIndex))
                        resultsWriter.WriteLine(knownLine);
                    else {
                        resultsWriter.WriteLine(MISS_PREFIX + knownLine);
                        ++missCount;
                    }

                    ++knownIndex;
                }
            }

            File.Delete(HITS_FILENAME);
            File.Delete(KNOWNS_FILENAME);
            File.Delete(MISSES_FILENAME);

            var missRatio = (double)missCount / (double)knownIndex;
            var coverage = Math.Round((1.0 - missRatio) * 100.0, 2);

            Console.WriteLine(string.Format("Overall coverage was {0}%.", coverage));

            return missCount == 0 ? 0 : 1;
        }

        public static int Main(string[] args)
        {
            try {
                if (args[0] == "instrument") {
                    var config = new InstrumentConfig(args[1]);

                    //so it exists regardless and is deleted
                    File.WriteAllText(KNOWNS_FILENAME, string.Empty);
                    File.WriteAllText(config.HitsPath, string.Empty);

                    //used to track the line index of the instrumented instruction in the knowns file
                    var instrumentIndex = 0;

                    using (var writer = new StreamWriter(KNOWNS_FILENAME, true)) {
                        foreach (var assemblyPath in config.AssemblyPaths)
                            Instrument(assemblyPath, config, writer, ref instrumentIndex);
                    }

                    return 0;
                } else if (args[0] == "check")
                    return Check();

                Console.Error.WriteLine("need 'instrument' or 'check' command");
            } catch (Exception e) {
                Console.Error.WriteLine(e);
            }

            return 2;
        }
    }
}
