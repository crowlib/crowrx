// ReSharper disable CheckNamespace
namespace CrowRx.Editor
{
    public static class RuntimeTypeCacheEditor
    {
        [UnityEditor.MenuItem("Tools/CrowRx/Runtime Type Cache/Gather Types")]
        private static void GatherTypes()
        {
            RuntimeTypeCache.GatherTypes();
        }
    }
}
