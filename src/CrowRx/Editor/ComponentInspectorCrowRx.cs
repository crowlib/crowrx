using UnityEngine;

// ReSharper disable CheckNamespace
namespace CrowRx.Editor
{
    public class ComponentInspectorCrowRx<TTargetComponent> : EditorCrowRx
        where TTargetComponent : Component
    {
        protected TTargetComponent TargetComponent => target && target is TTargetComponent targetComponent ? targetComponent : null;


        protected TValueType GetValue<TValueType>(string fieldName, VariableType variableType) => TargetComponent.GetValue<TTargetComponent, TValueType>(fieldName, variableType);
    }
}