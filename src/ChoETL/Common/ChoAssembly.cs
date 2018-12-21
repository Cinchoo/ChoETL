namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Collections;
    using System.Web;
    using System.Collections.Generic;
    using System.Diagnostics;

    #endregion NameSpaces

    public static class ChoAssembly
    {
        #region Constants

        private const string AspNetNamespace = "ASP";

        #endregion Constants

        #region Shared Members (Public)

        internal static void Initialize()
        {
            ChoAssemblyResolver.Attach();
        }

        private static readonly object _entryAssemblyLock = new object();
        private static Assembly _entryAssembly;
        public static Assembly EntryAssembly
        {
            get { return _entryAssembly; }
            set
            {
                if (value != null)
                    _entryAssembly = value;
            }
        }
        public static Assembly GetEntryAssembly()
        {
            if (_entryAssembly != null)
                return _entryAssembly;

            lock (_entryAssemblyLock)
            {
                if (_entryAssembly != null)
                    return _entryAssembly;

                // Try the EntryAssembly, this doesn't work for ASP.NET classic pipeline (untested on integrated)
                Assembly assembly = Assembly.GetEntryAssembly();

#if !NETSTANDARD2_0
                // Look for web application assembly
                HttpContext ctx = HttpContext.Current;
                if (ctx != null)
                    assembly = GetWebApplicationAssembly(ctx);
#endif
                // Fallback to executing assembly
                _entryAssembly = assembly ?? (Assembly.GetExecutingAssembly());
                return _entryAssembly;
            }
        }

        #region GetAssemblies Overloads

        private readonly static object _loadedAssemblyLock = new object();
        private static List<Assembly> _loadedAssemblies = null;

        internal static void AddToLoadedAssembly(Assembly assembly)
        {
            if (assembly == null) return;

            lock (_loadedAssemblyLock)
            {
                _loadedAssemblies.Add(assembly);
            }
        }

        public static Assembly[] GetLoadedAssemblies()
        {
            if (_loadedAssemblies != null)
                return _loadedAssemblies.ToArray();

            lock (_loadedAssemblyLock)
            {
                if (_loadedAssemblies != null)
                    return _loadedAssemblies.ToArray();

                _loadedAssemblies = new List<Assembly>();

                Assembly[] loadedAssemblies = null;
                try
                {
                    LoadReferencedAssemblies();
                    loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                }
                catch (System.Security.SecurityException)
                {
                    // Insufficient permissions to get the list of loaded assemblies
                }

                if (loadedAssemblies != null)
                {
                    // Search the loaded assemblies for the type
                    foreach (Assembly assembly in loadedAssemblies)
                    {
                        DiscoverNLoadAssemblies(assembly, _loadedAssemblies);
                    }
                }

                return _loadedAssemblies.ToArray();
            }
        }

        private static void LoadReferencedAssemblies()
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            var loadedPaths = loadedAssemblies.Select((a) =>
                {
                    if (!a.IsDynamic)
                    {
                        try
                        {
                            return a.Location;
                        }
                        catch (Exception ex)
                        {
                            ChoETLLog.Error(ex.ToString());
                        }
                    }
                    return String.Empty;
                }).ToArray();

            if (ChoETLFrxBootstrap.EnableLoadingReferencedAssemblies)
            {
                var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
                var toLoad = referencedPaths.Where(r => !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase)).ToList();
                toLoad.ForEach(path =>
                {
                    try
                    {
                        if (ChoETLFrxBootstrap.IgnoreLoadingAssemblies.Contains(path)
                            || ChoETLFrxBootstrap.IgnoreLoadingAssemblies.Contains(Path.GetFileName(path)))
                        {

                        }
                        else
                            loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path)));
                    }
                    catch
                    {

                    }
                }
                );
            }
        }

        private static void DiscoverNLoadAssemblies(Assembly assembly, List<Assembly> assemblies)
        {
            assemblies.Add(assembly);
        }

        public static Assembly[] LoadAssemblies(string directory)
        {
            return LoadAssemblies(new string[] { directory });
        }

        public static Assembly[] LoadAssemblies(string[] directories)
        {
            List<Assembly> assemblies = new List<Assembly>();

            foreach (string directory in directories)
            {
                if (directory == null) continue;
                foreach (string file in Directory.GetFiles(directory, "*.dll;*.exe", SearchOption.AllDirectories)) //TODO: Filter needs to be configurable
                {
                    if (file == null) continue;

                    try
                    {
                        Assembly assembly = Assembly.LoadFile(file);
                        if (assembly != null)
                        {
                            DiscoverNLoadAssemblies(assembly, assemblies);
                            //fileProfile.Info(file);
                        }
                    }
                    catch (ChoFatalApplicationException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        ChoETLLog.Error(ex.ToString());
                    }
                }
            }
            return assemblies.ToArray();
        }

        #endregion GetAssemblies Overloads

        /// <summary>
        /// Gets the assembly location path for the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to get the location for.</param>
        /// <returns>The location of the assembly.</returns>
        /// <remarks>
        /// <para>
        /// This method does not guarantee to return the correct path
        /// to the assembly. If only tries to give an indication as to
        /// where the assembly was loaded from.
        /// </para>
        /// </remarks>
        public static string GetAssemblyLocationInfo(Assembly assembly)
        {
            if (assembly.GlobalAssemblyCache)
            {
                return "Global Assembly Cache";
            }
            else
            {
                try
                {
                    // This call requires FileIOPermission for access to the path
                    // if we don't have permission then we just ignore it and
                    // carry on.
                    return assembly.Location;
                }
                catch (System.Security.SecurityException)
                {
                    return "Location Permission Denied";
                }
            }
        }

        /// <summary>
        /// Gets the fully qualified name of the <see cref="Type" />, including 
        /// the name of the assembly from which the <see cref="Type" /> was 
        /// loaded.
        /// </summary>
        /// <param name="type">The <see cref="Type" /> to get the fully qualified name for.</param>
        /// <returns>The fully qualified name for the <see cref="Type" />.</returns>
        /// <remarks>
        /// <para>
        /// This is equivalent to the <c>Type.AssemblyQualifiedName</c> property,
        /// but this method works on the .NET Compact Framework 1.0 as well as
        /// the full .NET runtime.
        /// </para>
        /// </remarks>
        public static string AssemblyQualifiedName(Type type)
        {
            return type.FullName + ", " + type.Assembly.FullName;
        }

        /// <summary>
        /// Gets the short name of the <see cref="Assembly" />.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly" /> to get the name for.</param>
        /// <returns>The short name of the <see cref="Assembly" />.</returns>
        /// <remarks>
        /// <para>
        /// The short name of the assembly is the <see cref="Assembly.FullName" /> 
        /// without the version, culture, or public key. i.e. it is just the 
        /// assembly's file name without the extension.
        /// </para>
        /// <para>
        /// Use this rather than <c>Assembly.GetName().Name</c> because that
        /// is not available on the Compact Framework.
        /// </para>
        /// <para>
        /// Because of a FileIOPermission security demand we cannot do
        /// the obvious Assembly.GetName().Name. We are allowed to get
        /// the <see cref="Assembly.FullName" /> of the assembly so we 
        /// start from there and strip out just the assembly name.
        /// </para>
        /// </remarks>
        public static string AssemblyShortName(Assembly assembly)
        {
            string name = assembly.FullName;
            int offset = name.IndexOf(',');
            if (offset > 0)
            {
                name = name.Substring(0, offset);
            }
            return name.Trim();
        }

        /// <summary>
        /// Gets the file name portion of the <see cref="Assembly" />, including the extension.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly" /> to get the file name for.</param>
        /// <returns>The file name of the assembly.</returns>
        /// <remarks>
        /// <para>
        /// Gets the file name portion of the <see cref="Assembly" />, including the extension.
        /// </para>
        /// </remarks>
        public static string AssemblyFileName(Assembly assembly)
        {
            return System.IO.Path.GetFileName(assembly.Location);
        }

        public static string GetAssemblyName()
        {
            return GetAssemblyName(Assembly.GetCallingAssembly().FullName);
        }

        public static string GetAssemblyName(object assemblyObject)
        {
            if (assemblyObject == null) return null;
            return GetAssemblyName(assemblyObject.GetType().Assembly.FullName);
        }

        public static string GetAssemblyName(string assemblyFullName)
        {
            if (assemblyFullName == null) return null;
            if (assemblyFullName.IndexOf(",") < 0) return assemblyFullName;
            return assemblyFullName.Substring(0, assemblyFullName.IndexOf(","));
        }

        #endregion

        #region Shared Members (Private)

#if !NETSTANDARD2_0

        private static Assembly GetWebApplicationAssembly(HttpContext context)
        {
            ChoGuard.ArgumentNotNull(context, "context");

            IHttpHandler handler = context.CurrentHandler;
            if (handler == null)
                return null;

            Type type = handler.GetType();
            while (type != null && type != typeof(object) && type.Namespace == AspNetNamespace)
                type = type.BaseType;

            return type.Assembly;
        }

#endif

        #endregion Shared Members (Private)
    }
}
