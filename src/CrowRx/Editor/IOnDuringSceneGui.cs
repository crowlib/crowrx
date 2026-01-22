using UnityEditor;

// ReSharper disable CheckNamespace
namespace CrowRx.Editor
{
	public interface IOnDuringSceneGui
	{
		void OnDuringSceneGui(SceneView scene_view);
	}
}