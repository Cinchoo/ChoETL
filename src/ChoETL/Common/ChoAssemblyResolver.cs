namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    #endregion NameSpaces

    #region ChoAssemblyManager Class

    public static class ChoAssemblyManager
    {
        #region Shared Data Members (Private)

        /// <summary>
        /// Holds the loaded assemblies.
        /// </summary>
        private static Dictionary<string, Assembly> _assemblyCache = new Dictionary<string, Assembly>();
        private static readonly object _syncRoot = new object();
        /// <summary>
        /// Holds the missing assembly cache.
        /// </summary>
        private static List<string> _missingAssemblyCache = new List<string>();

        #endregion

        #region Constructors

        static ChoAssemblyManager()
        {
            Clear();
        }

        #endregion

        #region Shared Members (Public)

        public static void Clear()
        {
            lock (_syncRoot)
            {
                _assemblyCache.Clear();
                _missingAssemblyCache.Clear();
            }
        }

        public static bool ContainsAssembly(string assemblyName)
        {
            ChoGuard.ArgumentNotNullOrEmpty(assemblyName, "AssemblyName");

            return _assemblyCache.ContainsKey(assemblyName);
        }

        public static bool ContainsAssembly(Assembly assembly)
        {
            ChoGuard.ArgumentNotNull(assembly, "Assembly");

            return _assemblyCache.ContainsKey(assembly.FullName);
        }

        public static void AddAssemblyToCache(Assembly assembly)
        {
            ChoGuard.ArgumentNotNull(assembly, "Assembly");

            lock (_syncRoot)
            {
                if (ContainsAssembly(assembly)) return;

                _assemblyCache.Add(assembly.FullName, assembly);
            }
        }

        public static Assembly GetAssemblyFromCache(string assemblyFileName)
        {
            ChoGuard.ArgumentNotNullOrEmpty(assemblyFileName, "AssemblyFileName");

            return _assemblyCache[assemblyFileName];
        }

        public static void AddMissingAssembly(string assemblyFileName)
        {
            ChoGuard.ArgumentNotNullOrEmpty(assemblyFileName, "Assembly File Name");

            lock (_syncRoot)
            {
                if (_missingAssemblyCache.Contains(assemblyFileName)) return;
                
                _missingAssemblyCache.Add(assemblyFileName);
            }
        }

        #endregion

        internal static bool ContainsAsMissingAssembly(string assemblyFileName)
        {
            lock (_syncRoot)
            {
                return _missingAssemblyCache.Contains(assemblyFileName);
            }
        }
    }

    #endregion ChoAssemblyManager Class

    public static class ChoAssemblyResolver
    {
        #region Shared Data Members (Private)

        private static HashSet<string> _paths = new HashSet<string>();

        #endregion Shared Data Members (Private)

        #region Public Instance Constructors

        static ChoAssemblyResolver()
        {
            //_paths.Add(Directory.GetDirectories(ChoAssembly.GetEntryAssemblyLocation(), "*.*", SearchOption.AllDirectories));
            AppDomain.CurrentDomain.AssemblyResolve +=
                new ResolveEventHandler(AssemblyResolve);

            AppDomain.CurrentDomain.AssemblyLoad +=
                new AssemblyLoadEventHandler(AssemblyLoad);
        }

        #endregion Public Instance Constructors

        #region Public Shared Methods

        /// <summary> 
        /// Installs the assembly resolver by hooking up to the 
        /// <see cref="AppDomain.AssemblyResolve" /> event.
        /// </summary>
        public static void Attach()
        {
        }

        /// <summary> 
        /// Uninstalls the assembly resolver.
        /// </summary>
        public static void Clear()
        {
            ChoAssemblyManager.Clear();

            AppDomain.CurrentDomain.AssemblyResolve -=
                new ResolveEventHandler(AssemblyResolve);

            AppDomain.CurrentDomain.AssemblyLoad -=
                new AssemblyLoadEventHandler(AssemblyLoad);
        }

        #endregion Public Instance Methods

        #region Private Shared Methods

        /// <summary> 
        /// Resolves an assembly not found by the system using the assembly 
        /// cache.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">A <see cref="ResolveEventArgs" /> that contains the event data.</param>
        /// <returns>
        /// The loaded assembly, or <see langword="null" /> if not found.
        /// </returns>
        private static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly assembly = DiscoverAssembly(sender, args);
            if (assembly != null)
                ChoAssembly.AddToLoadedAssembly(assembly);

            return assembly;
        }
         
        private static Assembly DiscoverAssembly(object sender, ResolveEventArgs args)
        {
            bool isFullName = args.Name.IndexOf("Version=") != -1;

            // first try to find an already loaded assembly
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (isFullName)
                {
                    if (assembly.FullName == args.Name)
                    {
                        // return assembly from AppDomain
                        return assembly;
                    }
                }
                else if (assembly.GetName(false).Name == args.Name)
                {
                    // return assembly from AppDomain
                    return assembly;
                }
            }

            if (ChoAssemblyManager.ContainsAsMissingAssembly(args.Name))
                return null;

            // find assembly in cache
            if (ChoAssemblyManager.ContainsAssembly(args.Name))
            {
                // return assembly from cache
                return (Assembly)ChoAssemblyManager.GetAssemblyFromCache(args.Name);
            }
            else
            {
                //String resourceName = "AssemblyLoadingAndReflection." + new AssemblyName(args.Name).Name + ".dll";

                //if (Assembly.GetExecutingAssembly().GetManifestResourceInfo(resourceName) != null)
                //{
                //    using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                //    {
                //        Byte[] assemblyData = new Byte[stream.Length];

                //        stream.Read(assemblyData, 0, assemblyData.Length);

                //        return Assembly.Load(assemblyData);
                //    }
                //}

                string assmeblyFileName = null;
                string[] asms = args.Name.Split(new char[] { ',' });
                int index = args.Name.IndexOf(',');
                var name = index < 0 ? args.Name + ".dll" : args.Name.Substring(0, index) + ".dll";

                Assembly resAssembly = LoadAssemblyFromResource(name);
                if (resAssembly != null)
                    return resAssembly;

                bool fileFound = false;
                foreach (string path in _paths)
                {
                    if (path == null || path.Trim().Length == 0) continue;

                    assmeblyFileName = Path.Combine(path, name);

                    if (File.Exists(assmeblyFileName))
                    {
                        fileFound = true;
                        break;
                    }
                }

                if (fileFound)
                {
                    return Assembly.LoadFile(assmeblyFileName);
                }
                else if (!assmeblyFileName.IsNullOrEmpty())
                    ChoAssemblyManager.AddMissingAssembly(args.Name);
            }

            return null;
        }

        private static Assembly LoadAssemblyFromResource(string name)
        {
            //Assembly thisAssembly = Assembly.GetEntryAssembly();

            foreach (Assembly thisAssembly in ChoAssembly.GetLoadedAssemblies())
            {
                if (thisAssembly.IsDynamic) continue;
                try
                {
                    //Load form Embedded Resources - This Function is not called if the Assembly is in the Application Folder
                    var resources = thisAssembly.GetManifestResourceNames().Where(s => s.EndsWith(name));
                    if (resources.Count() > 0)
                    {
                        var resourceName = resources.First();
                        using (Stream stream = thisAssembly.GetManifestResourceStream(resourceName))
                        {
                            if (stream == null) return null;
                            var block = new byte[stream.Length];
                            stream.Read(block, 0, block.Length);
                            return Assembly.Load(block);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ChoETLLog.Error(ex.ToString());
                }
            }
            return null;
        }

        /// <summary>
        /// Occurs when an assembly is loaded. The loaded assembly is added 
        /// to the assembly cache.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">An <see cref="AssemblyLoadEventArgs" /> that contains the event data.</param>
        private static void AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            ChoAssemblyManager.AddAssemblyToCache(args.LoadedAssembly);
        }

        #endregion Private Instance Methods
    }
}
