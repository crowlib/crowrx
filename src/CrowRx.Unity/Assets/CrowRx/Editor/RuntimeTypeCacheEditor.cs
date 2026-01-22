// ReSharper disable CheckNamespace
namespace CrowRx.Editor
{
    public static class RuntimeTypeCacheEditor
    {
        [UnityEditor.Callbacks.DidReloadScripts(0)]
		private static void OnScriptReload()
        {
            RuntimeTypeCache.GatherTypes();
        }
    }
}