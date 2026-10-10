using UnityEngine;

using UnityEditor;
using UnityEditor.SceneManagement;


namespace CrowRx.Editor
{
	public class PrefabInformation
	{
		readonly GameObject originalGameObject;

		public PrefabInformation(GameObject g)
		{
			originalGameObject = g;
			Refresh();
		}

		/// <summary>대상의 현재 프리팹 상태를 다시 조회합니다. 계층 변경 후 호출해 스냅샷을 갱신합니다.</summary>
		public void Refresh()
		{
			isPartOfPrefabStage = isPrefabStageRoot = false;
			isPartOfPrefabInstance = isPrefabInstanceRoot = false;
			isPrefabAssetRoot = isPartOfPrefabAsset = false;
			nearestInstanceRoot = prefabAssetRoot = null;
			prefabAssetPath = null;
			prefabAssetType = PrefabAssetType.NotAPrefab;
			if (!originalGameObject) return;

			PrefabStage stage = PrefabStageUtility.GetPrefabStage(originalGameObject);
			isPartOfPrefabStage = stage != null;
			isPrefabStageRoot = stage != null && stage.prefabContentsRoot == originalGameObject;
			isPartOfPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(originalGameObject);
			isPrefabInstanceRoot = PrefabUtility.IsAnyPrefabInstanceRoot(originalGameObject);
			nearestInstanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(originalGameObject);
			isPartOfPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(originalGameObject);
			if (isPartOfPrefabAsset)
			{
				prefabAssetRoot = originalGameObject.transform.root.gameObject;
				isPrefabAssetRoot = prefabAssetRoot == originalGameObject;
			}
			prefabAssetType = PrefabUtility.GetPrefabAssetType(originalGameObject);
			string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(originalGameObject);
			if (string.IsNullOrEmpty(path) && stage != null) path = stage.assetPath;
			if (string.IsNullOrEmpty(path) && isPartOfPrefabAsset) path = AssetDatabase.GetAssetPath(originalGameObject);
			prefabAssetPath = string.IsNullOrEmpty(path) ? null : path;
		}

		/* Prefab stage ///////////////////////*/

		/// <summary>
		/// Is root or child node under prefab stage.
		/// </summary>
		public bool isPartOfPrefabStage;

		/// <summary>
		/// This  node is root of editing prefab stage.
		/// </summary>
		public bool isPrefabStageRoot;

		/* Prefab instance /////////////////////*/

		/// <summary>
		/// Is root or child node under prefab instance.
		/// </summary>
		public bool isPartOfPrefabInstance;

		/// <summary>
		/// This node is prefab instance root.
		/// But it could be positioned in scene, under prefab stage or under prefab asset.
		/// </summary>
		public bool isPrefabInstanceRoot;

		/// <summary>
		/// Get node that is nearest instance root above this node.
		/// Or itself if isPrefabInstanceRoot=true.
		/// </summary>
		public GameObject nearestInstanceRoot;

		/* Prefab asset ////////////////////////*/

		/// <summary>
		/// Is root of prefab asset.
		/// </summary>
		public bool isPrefabAssetRoot;

		/// <summary>
		/// Is root or part of prefab asset.
		/// </summary>
		public bool isPartOfPrefabAsset;

		/// <summary>
		/// Type of prefab asset, only if isPartOfPrefabAsset=true.
		/// </summary>
		public PrefabAssetType prefabAssetType;

		/// <summary>
		/// This is variant prefab of other prefab asset.
		/// </summary>
		public bool isPrefabAssetVariant => prefabAssetType == PrefabAssetType.Variant;

		/// <summary>
		/// Nearest Asset path of prefab around selected gameObject.
		/// null if selected object is not part of any prefab.
		/// </summary>
		public string prefabAssetPath;

		/// <summary>
		/// Valid only when isPrefabAssetRoot = true.
		/// Top most prefab asset root.
		/// Is this object itself when isPrefabAssetRoot = true, could be otherwise when false.
		/// </summary>
		public GameObject prefabAssetRoot;

		/* Misc /////////////////////////////////*/

		/// <summary>
		/// This is object placed into scene.
		/// </summary>
		public bool isSceneObject => originalGameObject && !isPartOfPrefabAsset && !isPartOfPrefabStage;

		/// <summary>
		/// This game object is root or part of any prefab instance or asset prefab or prefab in prefab stage.
		/// </summary>
		public bool isPartOfAnyPrefab => prefabAssetPath != null;

		/// <summary>
		/// This game object is indeed root of some prefab.
		/// </summary>
		public bool isRootOfAnyPrefab => isPrefabAssetRoot || isPrefabInstanceRoot || isPrefabStageRoot;

		/* Extra queries ////////////////////////*/

		/// <summary>
		/// Walk one level up of prefab inheritance step.
		/// If this object is variant, this returns object it created from. (Could be another variant or prefab asset)
		/// If this object is prefab instance, this returns prefab it instanced from.
		/// </summary>
		public GameObject GetSourcePrefab()
		{
			return PrefabUtility.GetCorrespondingObjectFromSource(originalGameObject);
		}

		/// <summary>
		/// It is like calling GetSourcePrefab (GetCorrespondingObjectFromSource) in chain all the way to the base.
		/// </summary>
		public GameObject GetFirstSourcePrefab()
		{
			return PrefabUtility.GetCorrespondingObjectFromOriginalSource(originalGameObject);
		}
	}
}
