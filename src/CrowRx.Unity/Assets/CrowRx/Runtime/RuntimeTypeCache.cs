// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;


namespace CrowRx
{
    using Utility;


    public static class RuntimeTypeCache
    {
        private static readonly List<Type> _cachedTypes = new();
        private static Dictionary<string, Type> _cachedTypesByName;
        

        private static readonly string[] _exceptStartWithAssemblyNames =
        {
            "System.",
            "UnityEngine.",
            "UnityEditor.",
            "Unity.",
            "Mono.",
            "Bee.",
        };

        private static readonly string[] _exceptContainsAssemblyNames =
        {
            "mscorlib,",
            ".Unity,",

            "EnhancedScroller_asmdef,",
            "CrowRx.Sample,",
            "CrowRx.Network.Sample,",
        };

        private static readonly string[] _exceptStartWithTypeNames =
        {
            "System.",
            "Microsoft.",
            "Unity.VisualScripting.",
        };

        private static readonly string[] _exceptContainsTypeNames =
        {
            "<>",
            ".CompilerServices.",
            "PrivateImplementationDetails",
            "UnitySourceGeneratedAssemblyMonoScriptTypes",

            ".Examples."
        };


        public static IReadOnlyList<Type> Types
        {
            get
            {
                if (_cachedTypes.Count == 0)
                {
                    GatherTypes();
                }

                return _cachedTypes;
            }
        }

        public static IReadOnlyDictionary<string, Type> TypesByName
        {
            get
            {
                if (_cachedTypes.Count == 0)
                {
                    GatherTypes();
                }

                return _cachedTypesByName;
            }
        }


        public static void GatherTypes()
        {
            _cachedTypes.Clear();

            _cachedTypes.AddRange(
                AppDomain.CurrentDomain.GetAssemblies()
                    .Distinct()
                    .Where(assembly =>
                    {
                        if (assembly.IsDynamic)
                        {
                            return false;
                        }

                        string assemblyName = assembly.GetName().FullName;

                        return
                            !_exceptStartWithAssemblyNames.Any(exceptAssemblyName => assemblyName.StartsWith(exceptAssemblyName)) &&
                            !_exceptContainsAssemblyNames.Any(exceptAssemblyName => assemblyName.Contains(exceptAssemblyName));
                    })
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(type =>
                    {
                        if (type.IsGenericTypeDefinition ||
                            !type.IsVisible ||
                            Attribute.GetCustomAttribute(type, typeof(ObsoleteAttribute)) is not null)
                        {
                            return false;
                        }

                        string typeName = type.FullName;

                        return typeName is not null &&
                               !_exceptStartWithTypeNames.Any(exceptAssemblyName => typeName.StartsWith(exceptAssemblyName)) &&
                               !_exceptContainsTypeNames.Any(exceptAssemblyName => typeName.Contains(exceptAssemblyName));
                    }));

            _cachedTypesByName = new Dictionary<string, Type>();

            foreach (Type type in _cachedTypes)
            {
                string typeName = type.GetRealTypeFullName();

                if (!_cachedTypesByName.TryAdd(typeName, type))
                {
                    UnityLog.Warning($"Same Type Name : {typeName}\nold:{_cachedTypesByName[typeName]}\nnew:{type}");
                }
            }

            UnityLog.Info($"Type Count : {_cachedTypesByName.Count}");
        }
    }
}