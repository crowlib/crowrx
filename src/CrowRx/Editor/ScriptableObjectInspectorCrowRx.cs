using UnityEngine;

// ReSharper disable CheckNamespace
namespace CrowRx.Editor
{
	public class ScriptableObjectInspectorCrowRx<TTargetComponent> : EditorCrowRx
		where TTargetComponent : ScriptableObject
	{
		private TTargetComponent _target;
		protected new TTargetComponent target
		{
			get
			{
				if (_target == null)
				{
					_target = base.target as TTargetComponent;
				}
				return _target;
			}
		}
	}
}