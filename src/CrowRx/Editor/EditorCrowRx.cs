using UnityEngine;

using UnityEditor;

// ReSharper disable CheckNamespace
namespace CrowRx.Editor
{
    public class EditorCrowRx : UnityEditor.Editor, IEditorCrowRx
    {
        public static EditorCrowRx Instance { get; private set; }


        protected MonoScript _monoScript;

        private bool _isEnter = false;


        public override void OnInspectorGUI()
        {
            if (_isEnter)
            {
                // 스크립트 링크
                if (_monoScript)
                    this.FieldObject(
                        name: "Script",
                        value: _monoScript,
                        allow_scene_objects: false,
                        enabled: false);

                OnDraw();
            }

            Repaint();
        }

        private void OnValidate()
        {
            Instance = this;

            if (target)
            {
                if (target is ScriptableObject scriptableObject)
                    _monoScript = MonoScript.FromScriptableObject(scriptableObject);
                else if (target is MonoBehaviour monoBehaviour)
                    _monoScript = MonoScript.FromMonoBehaviour(monoBehaviour);
                else
                    _monoScript = null;
            }

            OnValidateProcess();
        }

        private void OnEnable()
        {
            OnValidate();

            if (_isEnter == false)
            {
                _isEnter = true;

                if (Instance is IOnDuringSceneGui onDuringSceneGui)
                {
                    SceneView.duringSceneGui += onDuringSceneGui.OnDuringSceneGui;
                    SceneView.RepaintAll();
                }

                if (Instance is IOnHierarchyChanged onHierarchyChanged)
                    EditorApplication.hierarchyChanged += onHierarchyChanged.OnHierarchyChanged;

                OnEnter();
            }
        }

        private void OnDisable()
        {
            if (_isEnter)
            {
                OnExit();

                if (Instance is IOnDuringSceneGui onDuringSceneGui)
                    SceneView.duringSceneGui -= onDuringSceneGui.OnDuringSceneGui;

                if (Instance is IOnHierarchyChanged onHierarchyChanged)
                    EditorApplication.hierarchyChanged -= onHierarchyChanged.OnHierarchyChanged;

                _isEnter = false;
            }

            _monoScript = null;

            Instance = null;
        }

        protected virtual void OnValidateProcess() { }

        protected virtual void OnEnter() { }

        protected virtual void OnDraw() { }

        protected virtual void OnExit() { }
    }
}