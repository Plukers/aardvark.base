﻿
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Aardvark.Base
{
    public static class IntrospectionProperties
    {
        /// <summary>
        /// Introspection is based on Assembly.GetEntryAssembly which represents the managed 
        /// entry point (i.e. the first assembly that was executed by AppDomain.ExecuteAssembly).
        /// However, startet from an unmanged entry point (like VisualStudio tests) Assembly.GetEntryAssembly
        /// is null. To allow us to start from unmanaged hosting processes this alternative
        /// has been implented.
        /// A second use case are managed interactive shells like fsi.exi. Here we want to
        /// start our dependency walk at a custom assembly instead of the running assembly 
        /// which is the interactive shell host.
        /// </summary>
        public static Assembly CustomEntryAssembly
        {
            get;
            set;
        }
    }

    public static class Introspection
    {
        private static CultureInfo s_cultureInfoEnUs = new CultureInfo("en-us");
        private static readonly string s_cacheDirName;
        private static Dictionary<string, Assembly> s_assemblies;
        private static HashSet<string> s_assembliesThatFailedToLoad = new HashSet<string>();
        private static HashSet<Assembly> s_allAssemblies = new HashSet<Assembly>();


        /// <summary>
        /// Registers an additional assembly at runtime to be
        /// used for subsequent introspection queries. This
        /// may be used for e.g. plugin scenarios.
        /// All assemblies that are reachable from the entry
        /// assembly are registered automatically at startup.
        /// </summary>
        public static void RegisterAssembly(Assembly a)
        {
            if (a == null) return;
            if (s_allAssemblies.Contains(a)) return;

            s_allAssemblies.Add(a);
        }
        
        /// <summary>
        /// Enumerates all known assemblies.
        /// </summary>
        public static IEnumerable<Assembly> AllAssemblies
        {
            get { return s_allAssemblies; }
        }

        /// <summary>
        /// Enumerates all classes implementing the specified interface.
        /// </summary>
        public static IEnumerable<Type> GetAllClassesImplementingInterface(Type interfaceType)
        {
            return AllAssemblies
                .SelectMany(a => GetAllClassesImplementingInterface(a, interfaceType));
        }
        
        /// <summary>
        /// Enumerates all classes inheriting from the specified base class.
        /// </summary>
        public static IEnumerable<Type> GetAllClassesInheritingFrom(Type baseType)
        {
            return AllAssemblies
                .SelectMany(a => GetAllClassesInheritingFrom(a, baseType));
        }
        
        /// <summary>
        /// Enumerates all types decorated with attribute T as tuples of type
        /// and its one or more T-attributes.
        /// </summary>
        public static IEnumerable<Tup<Type, T[]>> GetAllTypesWithAttribute<T>()
        {
            return AllAssemblies
                .SelectMany(a => GetAllTypesWithAttribute<T>(a));
        }

        /// <summary>
        /// Enumerates all methods decorated with attribute T as tuples of MethodInfo
        /// and its one or more T-attributes.
        /// </summary>
        public static IEnumerable<Tup<MethodInfo, T[]>> GetAllMethodsWithAttribute<T>()
        {
            return AllAssemblies
                .SelectMany(a => GetAllMethodsWithAttribute<T>(a));
        }

        /// <summary>
        /// Enumerates all classes from the specified assembly
        /// implementing the specified interface.
        /// </summary>
        public static Type[] GetAllClassesImplementingInterface(Assembly a, Type interfaceType)
        {
            return GetAll___<Type>(a, interfaceType.FullName,
                lines => lines.Select(s => Type.GetType(s)),
                types => types.Where(t => (t.IsClass || t.IsValueType) && t.GetInterfaces().Contains(interfaceType)),
                result => result.Select(t => t.AssemblyQualifiedName)
                );
        }

        /// <summary>
        /// Enumerates all classes from the specified assembly
        /// inheriting from the specified base class.
        /// </summary>
        public static Type[] GetAllClassesInheritingFrom(Assembly a, Type baseType)
        {
            return GetAll___<Type>(a, baseType.FullName,
                lines => lines.Select(s => Type.GetType(s)),
                types => types.Where(t => t.IsSubclassOf(baseType)),
                result => result.Select(t => t.AssemblyQualifiedName)
                );
        }

        /// <summary>
        /// Enumerates all types from the specified assembly
        /// decorated with attribute T as tuples of type
        /// and its one or more T-attributes.
        /// </summary>
        public static Tup<Type, T[]>[] GetAllTypesWithAttribute<T>(Assembly a)
        {
            return GetAll___<Tup<Type, T[]>>(a, typeof(T).FullName,
               lines => lines.Select(s => Type.GetType(s))
                        .Select(t => Tup.Create(
                            t,
                            t.GetCustomAttributes(typeof(T), false).Select(x => (T)x).ToArray())),
               types => from t in types
                        let attribs = t.GetCustomAttributes(typeof(T), false)
                        where attribs.Length > 0
                        select Tup.Create(t, attribs.Select(x => (T)x).ToArray()),
               result => result.Select(t => t.E0.AssemblyQualifiedName)
               );
        }

        /// <summary>
        /// Enumerates all methods from the specified assembly
        /// decorated with attribute T as tuples of MethodInfo
        /// and its one or more T-attributes.
        /// </summary>
        public static Tup<MethodInfo, T[]>[] GetAllMethodsWithAttribute<T>(Assembly a)
        {
            return GetAll___<Tup<MethodInfo, T[]>>(a, typeof(T).FullName,
                  lines => from line in lines
                           let t = Type.GetType(line)
                           from m in t.GetMethods()
                           let attribs = m.GetCustomAttributes(typeof(T), false)
                           where attribs.Length > 0
                           select Tup.Create(m, attribs.Select(x => (T)x).ToArray()),
                  types => from t in types
                           from m in t.GetMethods()
                           let attribs = m.GetCustomAttributes(typeof(T), false)
                           where attribs.Length > 0
                           select Tup.Create(m, attribs.Select(x => (T)x).ToArray()),
                  result => result.Select(m => m.E0.DeclaringType.AssemblyQualifiedName)
                  );
        }


        static Introspection()
        {
            // (1) initializing s_assembliesThatFailedToLoad
            new []
            {
                "Aardvark.Unmanaged",
                "Aardvark.Unmanaged.Diagnostics",
                "ASift.CLI",
                "CUDA.NET",
                "Emgu.CV",
                "FreeImageNET",
                "levmarCLI",
                "LSDCli",
                "OpenNI.Net",
                "OpenSURFcs",
                "PdfSharp",
                "PresentationFramework",
                "PresentationCore",
                "PrimitiveShapesCLI",
                "SiftGpu.CLI",
                "SlamToroCli",
                "System",
                "WindowsBase",
            }
            .ForEach(x => s_assembliesThatFailedToLoad.Add(x));

            // (2) setting up cache directory (s_cacheDirName)
            s_cacheDirName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(s_cacheDirName))
            {
                Report.Warn("SpecialFolder.CommonApplicationData does not exist!");
                s_cacheDirName = "cache"; // using ./cache
            }
            else
            {
                s_cacheDirName = Path.Combine(s_cacheDirName, @"VRVis\Aardvark\cache");
            }
            Report.Line(4, "using cache dir: {0}", s_cacheDirName);

            if (!Directory.Exists(s_cacheDirName))
            {
                Directory.CreateDirectory(s_cacheDirName);
            }

            // (3) enumerating all assemblies reachable from entry assembly
            s_assemblies = new Dictionary<string, Assembly>();
            var entryAssembly = Assembly.GetEntryAssembly();

            // hs: Assembly.GetEntryAssembly() returns null if started from unmanged hosting process (like visualstudio)
            // use CustomEntryAssembly assembly in this cases.
            // prior to rev. 17705 CustomEntryAssembly was only used if entryAssembly is null. However this precludes
            // hosting processes from using custom root assemblies (see commend in IntrospectionProperties.CustomEntryAssembly)
            // if this breaks your code just do not set IntrospectionProperties.CustomEntryAssembly or contact me (hs)
            if (IntrospectionProperties.CustomEntryAssembly != null)
            {
                Report.Warn("[Introspection] Assembly.GetEntryAssembly() was 0 but a CustomEntryAssembly was supplied.");
                EnumerateAssemblies(IntrospectionProperties.CustomEntryAssembly.GetName().Name, IntrospectionProperties.CustomEntryAssembly);
            } else if (entryAssembly != null)
            {
                var entryAssemblyName = entryAssembly.GetName();
                EnumerateAssemblies(entryAssemblyName.Name);
            }
            else
            {
                Report.Warn("[Introspection] Assembly.GetEntryAssembly() == null");

                // hs: why is this necessary here? bootstrapper should be initialized before anything is loaded?
                // sm: so let's see who complains if we comment it out ;-)
                //Bootstrapper.Init();

                Report.Begin("trying all dlls and exes in current directory: {0}", Environment.CurrentDirectory);
                foreach (var s in Directory.GetFiles(Environment.CurrentDirectory, "*.dll").Concat(
                                  Directory.GetFiles(Environment.CurrentDirectory, "*.exe")))
                {
                    try
                    {
                        EnumerateAssemblies(AssemblyName.GetAssemblyName(s).Name);
                        Report.Line("{0}", s);
                    }
                    catch
                    {
                    }
                }
                Report.End();
            }
            
            //Report.Line("s_telemetryEnumerateAssembliesCheckTime1: {0}", s_telemetryEnumerateAssembliesCheckTime1.Value);
            //Report.Line("s_telemetryEnumerateAssembliesCheckTime2: {0}", s_telemetryEnumerateAssembliesCheckTime2.Value);
        }

        private static Telemetry.CpuTime s_telemetryEnumerateAssembliesCheckTime1 = new Telemetry.CpuTime();
        private static Telemetry.CpuTime s_telemetryEnumerateAssembliesCheckTime2 = new Telemetry.CpuTime();

        /// <summary>
        /// Note by hs: Since this function throws and catches exceptions in non exceptional cases we
        /// use [DebuggerNonUserCode] to deactive first chance exceptions here 
        /// at least if non user code is deactivated in Options/Debugging.
        /// </summary>
        /// <param name="name">the name of the entry assembly</param>
        /// <param name="customAssembly">If there the root assembly is not the assembly which has been startet
        /// by the AppDomain a customAssembly is used alternatively.</param>
        [DebuggerNonUserCode]
        private static void EnumerateAssemblies(string name, Assembly customAssembly = null)
        {
            using (s_telemetryEnumerateAssembliesCheckTime1.Timer)
            {
                if (string.IsNullOrEmpty(name)) return;
                if (s_assembliesThatFailedToLoad.Contains(name)) return;
                if (s_assemblies.ContainsKey(name)) return;
            }

            using (s_telemetryEnumerateAssembliesCheckTime2.Timer)
            {
                if (name.StartsWith("System.") ||
                    name.StartsWith("VRLib.CLI") ||
                    name.StartsWith("Microsoft") ||
                    name.StartsWith("LidorSystems") ||
                    name.StartsWith("WeifenLuo.") ||
                    name.StartsWith("OpenCV") ||
                    name.StartsWith("nunit.") ||
                    name.StartsWith("Extreme.Numerics") ||
                    name.StartsWith("fftwlib") ||
                    name.StartsWith("GraphCutsCLI") ||
                    name.StartsWith("Interop.MLApp") ||
                    name.StartsWith("IPP") ||
                    name.StartsWith("IronRuby") ||
                    name.StartsWith("MapTools") ||
                    name.StartsWith("MetaDataExtractor") ||
                    name.StartsWith("mscorlib") ||
                    name.StartsWith("SlimDX") ||
                    name.StartsWith("TDx.TDxInput") ||
                    name.StartsWith("WiimoteLib") ||
                    name.StartsWith("OpenTK") ||
                    name.StartsWith("Kitware") ||
                    name.StartsWith("ICSharpCode") ||
                    name.StartsWith("WindowsFormsIntegration") ||
                    name.StartsWith("Roslyn") ||
                    name.StartsWith("SharpDX") ||
                    name.StartsWith("Aardvark.Jynx.Native") ||
                    name.StartsWith("SurfaceQueueInteropHelper") ||
                    name.StartsWith("ScintillaNET") ||
                    name.StartsWith("IKVM") ||
                    name.StartsWith("Super") ||
                    name.StartsWith("Java")
                    )
                {
                    s_assembliesThatFailedToLoad.Add(name);
                    return;
                }
            }

            try
            {
                var assembly = customAssembly != null ? customAssembly : Assembly.Load(name);
                s_assemblies[name] = assembly;
                RegisterAssembly(assembly);
                foreach (var a in assembly.GetReferencedAssemblies())
                {
                    string s = a.Name;
                    if (!s_assemblies.ContainsKey(s)) EnumerateAssemblies(s);
                }
            }
            catch //(Exception e)
            {
                s_assembliesThatFailedToLoad.Add(name);
                //Report.Warn(e.ToString());
                //Report.Warn("{0}", name);
            }
        }
        private static string CreateCacheFileName(string fileName, Guid guid)
        {
            return Path.Combine(s_cacheDirName,
                string.Format(@"{0}.{1}.txt", Path.GetFileName(fileName), guid)
                );
        }
        private class CacheFileHeader
        {
            public int Version;
            public DateTime TimeStampOfCachedFile;

            public override string ToString()
            {
                Requires.That(Version > 0);
                return string.Format(s_cultureInfoEnUs,
                    "version {0} timestamp {1}", Version, TimeStampOfCachedFile.ToBinary()
                    );
            }

            public static CacheFileHeader Parse(string s)
            {
                Requires.NotEmpty(s);
                if (!s.StartsWith("version")) return null; // old file without header
                var tokens = s.Split(' ');
                if (tokens.Length != 4) throw new FormatException();
                return new CacheFileHeader
                {
                    Version = int.Parse(tokens[1]),
                    TimeStampOfCachedFile = DateTime.FromBinary(long.Parse(tokens[3]))
                };
            }
        }
        private static T[] GetAll___<T>(
            Assembly a, string discriminator,
            Func<IEnumerable<string>, IEnumerable<T>> decode,
            Func<IEnumerable<Type>, IEnumerable<T>> createResult,
            Func<T[], IEnumerable<string>> encode
            )
        {
            var assemblyFileName = a.Location;
            var cacheFileName = CreateCacheFileName(assemblyFileName, discriminator.ToGuid());
            var assemblyTimeStamp = File.GetLastWriteTimeUtc(assemblyFileName);
            if (File.Exists(cacheFileName))
            {
                /* var cacheFileTimeStamp = */ File.GetLastWriteTimeUtc(cacheFileName);
                var lines = File.ReadAllLines(cacheFileName);
                var header = lines.Length > 0 ? CacheFileHeader.Parse(lines[0]) : null;
                if (header != null && header.TimeStampOfCachedFile == assemblyTimeStamp)
                {
                    // return cached types
                    Report.Line(4, "[cache hit ] {0}", a);
                    return decode(lines.Skip(1)).ToArray();
                }
            }

            Report.Line(4, "[cache miss] {0}", a);

            // Notes by hs:
            // previously (rev 19495) typeloadexception resulted in empty result set.
            // even in case of typeloadexception there may be some successfully loaded
            // types in result set. Just continue processing with these types
            // effect: dlls with external unused dependencies don't have to be shipped.
            Type[] ts = new Type[0];
            try
            {
                ts = a.GetTypes().ToArray();
            }
            catch (ReflectionTypeLoadException e)
            {
                Report.Begin("ReflectionTypeLoadException error in assembly {0}", a.GetName().Name);
                Report.Line("You probably forgot to buildpatch your newly added library.");
                Report.Line("Full assembly name is {0}.", a.FullName);
                Report.Line("Exception is {0}", e);
                Report.Begin("Loader exceptions are");
                foreach (var x in e.LoaderExceptions)
                {
                    Report.Line("{0}", x);
                }
                Report.End();
                Report.End();
                ts = e.Types.Where(t => t != null).ToArray();
            }
            
            var result = createResult(ts).ToArray();

            // write cache file
            var headerLine =
                new CacheFileHeader { Version = 1, TimeStampOfCachedFile = assemblyTimeStamp }
                .ToString()
                .IntoIEnumerable()
                ;
            File.WriteAllLines(cacheFileName, headerLine.Concat(encode(result)).ToArray());

            return result;
        }
    }

    public class OnAardvarkInitAttribute : Attribute
    {
        public OnAardvarkInitAttribute() { }
    }

    public class Aardvark
    {
        public static void Init()
        {
            Report.BeginTimed("initializing aardvark");
            var pluginsFile = "plugins.txt";
            Report.BeginTimed("Loading plugins");
            var pluginsList = new List<Assembly>();

            if(File.Exists(pluginsFile))
            {
                var plugins = File.ReadLines(pluginsFile);
                try
                {
                    foreach(var plugin in plugins)
                    {
                        var dll = plugin + ".dll";
                        var exe = plugin + ".exe";
                        if (File.Exists(dll))
                        {
                            Report.Line("loading {0}", dll);
                            pluginsList.Add(Assembly.LoadFile(Path.GetFullPath(dll)));
                        }
                        else if (File.Exists(exe))
                        {
                            Report.Line("loading {0}", exe);
                            pluginsList.Add(Assembly.LoadFile(Path.GetFullPath(exe)));
                        }

                        
                    }
                } 
                catch(Exception e)
                {
                    Report.Warn("Could not load {0} ({1}", pluginsFile, e.Message);
                }
            }


            Report.End();
            LoadAll(pluginsList);
            Report.End();
        }

        private static void LoadAll(IEnumerable<Assembly> xs)
        {
            foreach (var ass in Enumerable.Concat(Introspection.AllAssemblies,xs))
            {
                foreach (var inits in Introspection.GetAllMethodsWithAttribute<OnAardvarkInitAttribute>(ass))
                {
                    var mi = inits.E0;
                    var parameters = mi.GetParameters();
                    if (parameters.Length == 0) mi.Invoke(null, null);
                    else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(IEnumerable<Assembly>)) mi.Invoke(null, new object[] { Introspection.AllAssemblies });
                    else Report.Warn("Strange aardvark init method found: {0}, should be Init : IEnumberable<Assembly> -> unit or unit -> unit", mi);
                }
            }
        }
    }
}
