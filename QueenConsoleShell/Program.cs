using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Queen.Language;
using Queen.Language.IntermediateTree;
using Queen.Language.CodeDom;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace QueenConsoleShell
{
    class InvalidCommandlineArgumentException : System.Exception
    {
        public InvalidCommandlineArgumentException(string arg)
            : base(string.Format("Invalid argument: {0}", arg))
        {
        }
    }
    class CommandlineArgumentValueMissingException : System.Exception
    {
        public CommandlineArgumentValueMissingException(string arg)
            : base(string.Format("Value missing for: {0}", arg))
        {
        }
    }

    class CompileOptions
    {
        public struct NamespaceImport
        {
            public string namespaceName;
            public string moduleName;
        }
        public List<string> InputFiles = new List<string>();
        public string OutputFile = null;
        public string RootNamespace = null;
        public List<NamespaceImport> ImportedNamespaces = new List<NamespaceImport>();
        public List<string> ReferencedAssemblies = new List<string>();
        public bool Verbose = false;
        public bool Repeat = false;

        private static readonly Regex validNamespaceRegex = new Regex(@"^([a-zA-Z0-9_]+\.)*[a-zA-Z0-9_]+$");
        private static readonly Regex validModuleRegex = new Regex(@"^[a-zA-Z0-9_]+$");

        private static string ExpandPath(string p)
        {
            if (Path.IsPathRooted(p))
            {
                return p;
            }
            else
            {
                return Path.Combine(Directory.GetCurrentDirectory(), p);
            }
        }

        public void Parse(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i].Trim();
                if (arg.StartsWith("-"))
                {
                    if (arg == "-o")
                    {
                        if (i == args.Length - 1)
                        {
                            throw new CommandlineArgumentValueMissingException(arg);
                        }

                        OutputFile = ExpandPath(args[i + 1]);
                        i++;
                    }
                    else if (arg == "-n")
                    {
                        if (i == args.Length - 1)
                        {
                            throw new CommandlineArgumentValueMissingException(arg);
                        }

                        RootNamespace = ExpandPath(args[i + 1]);
                        if ((!validNamespaceRegex.Match(RootNamespace).Success) ||
                            RootNamespace.Contains("__"))
                        {
                            throw new Exception("Invalid root namespace: " + RootNamespace);
                        }
                       
                        i++;
                    }
                    else if (arg == "-l")
                    {
                        if (i == args.Length - 1)
                        {
                            throw new CommandlineArgumentValueMissingException(arg);
                        }

                        var s = ExpandPath(args[i + 1]);
                        ReferencedAssemblies.Add(s);
                        i++;
                    }
                    else if (arg == "-i")
                    {
                        if (i == args.Length - 1)
                        {
                            throw new CommandlineArgumentValueMissingException(arg);
                        }

                        var s = args[i + 1];
                        var idx = s.IndexOf('=');
                        if (idx < 0)
                        {
                            throw new Exception("Invalid value for -i: '" + s + "'");
                        }

                        var imp = new NamespaceImport();

                        imp.moduleName = s.Substring(0, idx);
                        imp.namespaceName = s.Substring(idx + 1);

                        if ((!validModuleRegex.Match(imp.moduleName).Success) ||
                           imp.moduleName.Contains("__"))
                        {
                            throw new Exception("Invalid module name: " + RootNamespace);
                        }
                        if ((!validNamespaceRegex.Match(imp.namespaceName).Success) ||
                           imp.namespaceName.Contains("__"))
                        {
                            throw new Exception("Invalid namespace: " + RootNamespace);
                        }

                        i++;
                    }
                    else if (arg == "-v")
                    {
                        Verbose = true;
                    }
                    else if (arg == "-r")
                    {
                        Repeat = true;
                    }
                    else
                    {
                        throw new InvalidCommandlineArgumentException(arg);
                    }
                }
                else
                {
                    InputFiles.Add(ExpandPath(arg));
                }
            }
        }
    }

    class Program
    {

        private readonly CompileOptions options;

        private static void PrintHelp()
        {
            Console.WriteLine(@"
Q-Language Command-line Compiler
======

Usage
------

Queen.Compiler.Cli.exe [OPTIONS...] SOURCE-FILES...

Options
------

 * -o OUTPUT-EXE-FILENAME
 * -n ROOT-NAMESPACE - specifies the root namespace.
 * -l DLL-NAME - adds an assembly reference.
 * -i MODULE-NAME=NAMESPACe - binds .NET namespace to the specified module name.
 * -v - displays verbose output.
 * -r - repeats the compilation process for accurate benchmarking.

");
        }

        private struct ParserError
        {
            public string FilePath;
            public string Message;
            public int Line, Column;
        }
        private class CompileItem
        {
            public string FilePath;
            public string Name;
            public Queen.Language.CodeDom.CodeSourceFile SourceDom;
            public List<ParserError> Errors = new List<ParserError>();
            public string Code = null;
        }

        private void ReportNotice(string msg)
        {
            if (!options.Verbose) return;
            if (options.Repeat) return;
            Console.WriteLine("[N] " + msg);
        }

        private void ReportError(string msg)
        {
            if (options.Repeat) return;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[E] " + msg);
            Console.ResetColor();
        }

        private void ReportWarning(string msg)
        {
            if (options.Repeat) return;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[W] " + msg);
            Console.ResetColor();
        }

        private string FormatReport(string file, int line, int col, string msg)
        {
            return string.Format("{0} [{1}:{2}] {3}", file, line, col, msg);
        }

        private string FormatTicks(long ticks)
        {
            double secs = ticks / (double)System.Diagnostics.Stopwatch.Frequency;
            long isecs = (long)Math.Floor(secs);
            int m = (int)((isecs / 60) % 60);
            int h = (int)(isecs / 3600);
            secs -= (double)((long)m * 60 + (long)h * 3600);

            string tim = string.Format("{0}:{1:00}:{2:00.000000}", h, m, secs);
            return tim;
        }

        private long lastTicks = 0;

        private void OutputTimeMeasurement(string msg, long ticks)
        {
            //string tim = FormatTicks(ticks);
            //ReportNotice("- " + tim + " : " + msg);
            ReportNotice(string.Format("- {0} (+{1}) : {2}",
                FormatTicks(ticks), FormatTicks(ticks - lastTicks), msg));
            lastTicks = ticks;
        }

        private Program(CompileOptions options)
        {
            this.options = options;
        }

        private void Prepare()
        {
            // load compiler's classes
            foreach (var typ in typeof(Queen.Language.IntermediateCompiler).Assembly.GetTypes())
            {
                foreach (var method in typ.GetMethods(BindingFlags.DeclaredOnly |
                                BindingFlags.NonPublic |
                                BindingFlags.Public | BindingFlags.Instance |
                                BindingFlags.Static))
                {
                    if (method.IsAbstract || method.IsGenericMethod || method.IsGenericMethodDefinition) continue;
                    try
                    {
                        System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(method.MethodHandle);
                    }
                    catch (Exception) { }
                }
            }
        }

        public static int Main(string[] args)
        {
            var opts = new CompileOptions();
            if (args.Length == 0)
            {
                PrintHelp();
                return 1;
            }
            try
            {
                opts.Parse(args);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                PrintHelp();
                return 1;
            }

            var prg = new Program(opts);
            if (opts.Repeat)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Preparing...");
                try
                {
                    new Program(opts).Run();
                }
                catch (Exception) { }
                opts.Repeat = false;
                Console.WriteLine("Preparation done, starting the main compilation.");
                Console.ResetColor();
            }
            else if(opts.Verbose)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[W] reported timing measurement includes JIT compilcation time, therefore " +
                    "this might be inappropriate for accurate benchmarking. Use -r option to ensure " +
                    "all required code is prepared.");
                Console.ResetColor();
            }
            return prg.Run();
        }

        public int Run()
        {
            if (options.InputFiles.Count == 0)
            {
                ReportError("No input files");
                return 1;
            }

            if (options.OutputFile == null)
            {
                if (options.InputFiles.Count == 1)
                {
                    var p = options.InputFiles[0];
                    var dr = Path.GetDirectoryName(p);
                    p = Path.GetFileNameWithoutExtension(p) + ".exe";
                    options.OutputFile = Path.Combine(dr, p);
                    ReportNotice(string.Format("Output file name defaults to {0}", options.OutputFile));
                }
                else
                {
                    ReportError("Cannot decide the default output file name when multiple input files are specified");
                    return 1;
                }
            }
            string outputAssemblyPath = options.OutputFile;
            string outputAssemblyDirectory = Path.GetDirectoryName(outputAssemblyPath);

            if (options.RootNamespace == null)
            {
                var n = Path.GetFileNameWithoutExtension(outputAssemblyPath);
                n = new Regex(@"[^a-zA-Z0-9_.]").Replace(n, "");
                n = new Regex(@"_+").Replace(n, "_");
                options.RootNamespace = n;
                ReportNotice(string.Format("Default namespace defaults to {0}", options.RootNamespace));
            }

            Prepare();

            var items = new List<CompileItem>();
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            foreach (var file in this.options.InputFiles)
            {
                var item = new CompileItem();
                item.FilePath = file;
                item.Name = Path.GetFileNameWithoutExtension(item.FilePath);
                items.Add(item);
            }

            OutputTimeMeasurement("Compilation started", sw.ElapsedTicks);

            ReportNotice(string.Format("Parsing {0} input file(s) using up to {1} thread(s)",
                items.Count, System.Threading.Tasks.TaskScheduler.Current.MaximumConcurrencyLevel));

            var parserExceptions = new List<Exception>();

            System.Threading.Tasks.Parallel.ForEach(items, delegate(CompileItem item)
            {
                var parser = new Queen.Language.Parser();
                parser.ErrorReported += delegate(object sender, Queen.Language.ParserErrorEventArgs args)
                {
                    item.Errors.Add(new ParserError()
                    {
                        FilePath = item.FilePath,
                        Message = args.Message,
                        Line = args.Line,
                        Column = args.Column
                    });
                };
                try
                {
                    if (item.Code != null)
                    {
                        using (var reader = new StringReader(item.Code))
                        {
                            item.SourceDom = parser.Parse(reader);
                            item.SourceDom.Name.Text = item.Name;
                        }
                    }
                    else
                    {
                        using (var reader = new StreamReader(item.FilePath, Encoding.UTF8))
                        {
                            item.SourceDom = parser.Parse(reader);
                            item.SourceDom.Name.Text = item.Name;
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (parserExceptions)
                    {
                        parserExceptions.Add(ex);
                    }
                }
            });

            OutputTimeMeasurement("Parse Done", sw.ElapsedTicks);

            foreach (var ex in parserExceptions)
            {
                if (ex is FileNotFoundException)
                {
                    ReportError(string.Format("Fatal error while parsing input file(s): {0}", ex.Message));
                }
                else
                {
                    // might be fatal and useful for debugging...
                    ReportError(string.Format("Fatal error while parsing input file(s): {0}", ex.ToString()));
                }
                return 3;
            }

            bool hadError = false;
            var sources = new Queen.Language.CodeDom.CodeSourceFile[items.Count];
            var sourceMap = new Dictionary<Queen.Language.CodeDom.CodeSourceFile, string>();
            for (int i = 0; i < sources.Length; i++)
            {
                var item = items[i];
                sources[i] = item.SourceDom;
                sourceMap.Add(item.SourceDom, item.FilePath);

                foreach (var err in item.Errors)
                {
                    ReportError(FormatReport(err.FilePath, err.Line, err.Column, err.Message));
                    hadError = true;
                }
            }


            if (hadError)
            {
                ReportError("Compilation failed while parsing the input file(s).");
                return 2;
            }


            var cliOptions = new Queen.Language.CliCompiler.CliCompilerOptions();
            cliOptions.AssemblyBuilderAccess = System.Reflection.Emit.AssemblyBuilderAccess.Save;
            cliOptions.AssemblyDirectory = outputAssemblyDirectory;
            cliOptions.AssemblyName = Path.GetFileNameWithoutExtension(outputAssemblyPath);
            cliOptions.RootNamespace = options.RootNamespace;
            cliOptions.IsReleaseBuild = false;
            cliOptions.ModuleName = Path.GetFileName(outputAssemblyPath);

            var compiler = new Queen.Language.CliCompiler.CliIntermediateCompiler();
            // reference to QueenLib is already added
            foreach (var s in options.ReferencedAssemblies)
            {
                try
                {
                    var asm = Assembly.ReflectionOnlyLoadFrom(s);
                    compiler.AddAssemblyReference(asm);
                }
                catch (Exception ex)
                {
                    ReportError("Failed to load a referenced assembly.: " + ex.Message);
                    return 3;
                }
            }
            foreach (var imp in options.ImportedNamespaces)
            {
                compiler.ImportNamespace(imp.namespaceName, imp.moduleName);
            }
            //compiler.AddAssemblyReference(typeof(Queen.Kuin.KuinGameGlobal).Assembly);
            //compiler.AddAssemblyReference(typeof(VorbisDecoder.VorbisDecoder).Assembly);
            compiler.ImportNamespace("Queen.ConsoleDebug", "Dbg");
            compiler.ErrorReported += delegate(object sender, Queen.Language.IntermediateCompileErrorEventArgs args)
            {
                string path = null;

                if (args.SourceFile != null && sourceMap.TryGetValue(args.SourceFile, out path))
                {
                    ReportError(FormatReport(path, args.Line, args.Column, args.Message));
                }
                else
                {
                    ReportError(args.Message);
                }
                hadError = true;
            };

            var root = compiler.IntermediateCompile(sources);

            OutputTimeMeasurement("Semantic Analysis Done", sw.ElapsedTicks);

            if (hadError)
            {
                ReportError("Compilation failed during the semantic analysis.");
                return 2;
            }

            var assembler = new Queen.Language.CliCompiler.CliCompiler();
            assembler.Options = cliOptions;

            ModuleBuilder module;
            var outputAsm = assembler.Build(root, out module);

            OutputTimeMeasurement(".NET Class Generation Done", sw.ElapsedTicks);

            // find entry point
            System.Reflection.MethodInfo defaultEntryPoint = null;
            System.Reflection.MethodInfo entryPoint = null;
            foreach (var type in outputAsm.GetTypes())
            {
                if (!type.IsPublic)
                {
                    continue;
                }
                {
                    var meth = type.GetMethod("AppMain");
                    if (!(meth == null || !meth.IsStatic))
                    {
                        if (meth.GetParameters().Length > 0 || meth.ReturnType.Name != "Void")
                        {
                            ReportError("The found entry point has an invalid definition.");
                            ReportError("Failed to find the proper entry point.");
                            return 2;
                        }
                        if (entryPoint != null)
                        {
                            ReportError("Multiple entry points were found.");
                            ReportError("Failed to find the proper entry point.");
                            return 2;
                        }
                        entryPoint = meth;
                    }
                }
            }

            if (entryPoint == null)
                entryPoint = defaultEntryPoint;

            if (entryPoint == null)
            {
                ReportError("Failed to find the proper entry point.");
                return 2;
            }

            outputAsm.SetEntryPoint(entryPoint, PEFileKinds.ConsoleApplication);

            outputAsm.Save(Path.GetFileName(outputAssemblyPath));

            OutputTimeMeasurement(".NET Assembly Written", sw.ElapsedTicks);
            ReportNotice("Compilation done.");

            return 0;
        }
        /*
        static void Main(string[] cmdArgs)
        {
            

            string prog = Console.In.ReadToEnd();
            string txt = prog;
            int numErrors = 0;
            var parser = new Queen.Language.Parser();
            Queen.Language.CodeDom.CodeSourceFile sf;
            parser.ErrorReported += delegate(object sender2, Queen.Language.ParserErrorEventArgs args)
            {
                Console.WriteLine(string.Format("({0}, {1}): {2}", args.Line, args.Column, args.Message));
                numErrors += 1;
            };
            GC.Collect();
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            sf = parser.Parse(new System.IO.StringReader(txt));
            Console.WriteLine("");
            Console.WriteLine("Parse time: {0}s\n", (double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency);

            sw.Reset();
            sw.Start();

            var icomp = new Queen.Language.CliCompiler.CliIntermediateCompiler();
            icomp.ErrorReported += delegate(object sender2, Queen.Language.IntermediateCompileErrorEventArgs args)
            {
                Console.WriteLine(string.Format("({0}, {1}): {2}", args.Line, args.Column, args.Message));
                numErrors += 1;
            };
            icomp.AddAssemblyReference(typeof(QueenConsoleShell.Dbg.Dbg_Global).Assembly);
            icomp.ImportNamespace(typeof(QueenConsoleShell.Dbg.Dbg_Global).Namespace, "Dbg");


            var it = icomp.IntermediateCompile(new CodeSourceFile[] { sf });

            Console.WriteLine("");
            Console.WriteLine("ICompile time: {0}s\n", (double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency);

            if (numErrors == 0)
            {
                var cmp = new Queen.Language.CliCompiler.CliCompiler();
                var opt = new Queen.Language.CliCompiler.CliCompilerOptions();
                opt.AssemblyName = "QueenTest";
                opt.RootNamespace = "QueenTest";
                cmp.Options = opt;

                var asm = cmp.Build(it);

                Console.WriteLine("");
                Console.WriteLine("Compile time: {0}s", (double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency);

                Console.WriteLine("");
                Console.WriteLine("Types: ");

                foreach (Type t in asm.GetTypes())
                {
                    Console.WriteLine(t.FullName);
                }

                var cls = asm.GetType("QueenTest.Unnamed.Global_Unnamed");
                var meth = cls.GetMethod("AppMain");

                if (cmdArgs.Length == 1)
                {
                    asm.SetEntryPoint(meth);
                    asm.Save(cmdArgs[0] + ".exe");
                    Console.WriteLine("Written to " + cmdArgs[0] + ".exe");
                    return;
                }

                string obj = meth.Invoke(null, new object[] { }).ToString();
                Console.WriteLine("");
                Console.WriteLine("Execute time: {0}s\n", (double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency);
                Console.WriteLine("");
                Console.WriteLine("Output:");
                Console.WriteLine(obj);

            }
        }

        */
    }
}
