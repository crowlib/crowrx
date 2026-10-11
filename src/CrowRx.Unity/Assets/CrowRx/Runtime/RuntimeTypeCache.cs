using System;
using System.Collections.Generic;
using System.Reflection;

namespace CrowRx
{
    public static class RuntimeTypeCache
    {
        private sealed class TypeCache
        {
            public readonly List<Type> CachedTypes;
            public readonly Dictionary<string, Type> CachedTypesByName;

            public TypeCache(List<Type> cachedTypes, Dictionary<string, Type> cachedTypesByName)
            {
                CachedTypes = cachedTypes;
                CachedTypesByName = cachedTypesByName;
            }
        }

        private static TypeCache _cache;

        private static readonly string[] s_exceptStartWithAssemblyNames =
        {
            "System.",
            "UnityEngine.",
            "UnityEditor.",
            "Unity.",
            "Mono.",
            "Bee.",
        };

        private static readonly string[] s_exceptContainsAssemblyNames =
        {
            "mscorlib,",
            ".Unity,",

            "EnhancedScroller_asmdef,",
            "CrowRx.Sample,",
            "CrowRx.Network.Sample,",
        };

        private static readonly string[] s_exceptStartWithTypeNames =
        {
            "System.",
            "Microsoft.",
            "Unity.VisualScripting.",
        };

        private static readonly string[] s_exceptContainsTypeNames =
        {
            "<>",
            ".CompilerServices.",
            "PrivateImplementationDetails",
            "UnitySourceGeneratedAssemblyMonoScriptTypes",

            ".Examples."
        };

        public static IReadOnlyList<Type> Types => GetCache().CachedTypes;
        public static IReadOnlyDictionary<string, Type> TypesByName => GetCache().CachedTypesByName;

        private static TypeCache GetCache()
        {
            if (_cache is null)
            {
                GatherTypes();
            }

            return _cache;
        }

        /// <summary>
        /// 메인 스레드에서 타입 목록을 갱신합니다. 게임 시작 전에 호출하면 최초 접근의 탐색을 미리 수행할 수 있습니다.
        /// 완성된 목록과 이름 사전을 함께 공개하며, 실패하면 이전 캐시를 유지합니다.
        /// </summary>
        public static void GatherTypes()
        {
#if UNITY_6000_6_OR_NEWER
            GatherTypes(UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies());
#else
            GatherTypes(AppDomain.CurrentDomain.GetAssemblies());
#endif
        }

        private static void GatherTypes(IReadOnlyList<Assembly> assemblies)
        {
            TypeCache cache = BuildCache(assemblies);
            _cache = cache;
            UnityLog.Info($"Type Count : {cache.CachedTypesByName.Count}");
        }

        private static TypeCache BuildCache(IReadOnlyList<Assembly> assemblies)
        {
            List<Type> types = new();
            for (int i = 0, count = assemblies.Count; i < count; i++)
            {
                Assembly assembly = assemblies[i];
                if (assembly.IsDynamic)
                {
                    continue;
                }

                string assemblyName = assembly.FullName;
                if (IsExcluded(assemblyName, s_exceptStartWithAssemblyNames, s_exceptContainsAssemblyNames))
                {
                    continue;
                }

                Type[] assemblyTypes;
                try
                {
                    assemblyTypes = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    // 로드된 타입만 사용하고 누락된 null 항목은 제외합니다.
                    assemblyTypes = e.Types;
                    UnityLog.Warning($"Some types could not be loaded from {assemblyName}.");
                }

                foreach (Type type in assemblyTypes)
                {
                    if (type is null || type.IsGenericTypeDefinition || !type.IsVisible || type.IsDefined(typeof(ObsoleteAttribute), true))
                    {
                        continue;
                    }

                    if (IsExcluded(type.FullName, s_exceptStartWithTypeNames, s_exceptContainsTypeNames))
                    {
                        continue;
                    }

                    types.Add(type);
                }
            }

            Dictionary<string, Type> typesByName = new(types.Count);
            foreach (Type type in types)
            {
                string typeName = type.GetRealTypeFullName();

                if (!typesByName.TryAdd(typeName, type))
                {
                    UnityLog.Warning($"Same Type Name : {typeName}\nold:{typesByName[typeName]}\nnew:{type}");
                }
            }

            return new TypeCache(types, typesByName);
        }

        private static bool IsExcluded(string name, string[] prefixes, string[] fragments)
        {
            if (name is null)
            {
                return true;
            }

            foreach (string prefix in prefixes)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            foreach (string fragment in fragments)
            {
                if (name.Contains(fragment))
                {
                    return true;
                }
            }

            return false;
        }
    }
}