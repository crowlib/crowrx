using UnityEngine;
using UnityEditor;

// ReSharper disable CheckNamespace
namespace CrowRx.Editor
{
    public class EditorCrowRx : UnityEditor.Editor, IEditorCrowRx
    {
        protected MonoScript _monoScript;

        /// <summary>실시간 표시가 필요한 인스펙터에서만 지속적인 다시 그리기를 요청합니다.</summary>
        protected virtual bool ContinuousRepaint => false;

        public override bool RequiresConstantRepaint() => ContinuousRepaint;

        public override void OnInspectorGUI()
        {
            // 스크립트 링크
            if (_monoScript)
            {
                this.FieldObject(
                    name: "Script",
                    value: _monoScript,
                    allow_scene_objects: false,
                    enabled: false);
            }

            OnDraw();
        }

        private void OnValidate() => OnValidateProcess();

        private void OnEnable()
        {
            if (target)
            {
                if (target is ScriptableObject scriptableObject)
                {
                    _monoScript = MonoScript.FromScriptableObject(scriptableObject);
                }
                else if (target is MonoBehaviour monoBehaviour)
                {
                    _monoScript = MonoScript.FromMonoBehaviour(monoBehaviour);
                }
                else
                {
                    _monoScript = null;
                }
            }

            if (this is IOnDuringSceneGui onDuringSceneGui)
            {
                SceneView.duringSceneGui += onDuringSceneGui.OnDuringSceneGui;
                SceneView.RepaintAll();
            }

            if (this is IOnHierarchyChanged onHierarchyChanged)
            {
                EditorApplication.hierarchyChanged += onHierarchyChanged.OnHierarchyChanged;
            }

            try
            {
                OnEnter();
            }
            catch
            {
                UnsubscribeEvents();
                _monoScript = null;
                throw;
            }
        }

        private void OnDisable()
        {
            try
            {
                OnExit();
            }
            finally
            {
                UnsubscribeEvents();
                _monoScript = null;
            }
        }

        private void UnsubscribeEvents()
        {
            if (this is IOnDuringSceneGui onDuringSceneGui)
            {
                SceneView.duringSceneGui -= onDuringSceneGui.OnDuringSceneGui;
            }

            if (this is IOnHierarchyChanged onHierarchyChanged)
            {
                EditorApplication.hierarchyChanged -= onHierarchyChanged.OnHierarchyChanged;
            }
        }

        protected virtual void OnValidateProcess() { }

        protected virtual void OnEnter() { }

        protected virtual void OnDraw() { }

        protected virtual void OnExit() { }
    }
}
