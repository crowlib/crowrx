using UnityEngine;
using UnityEngine.Events;


namespace CrowRx.Helper
{
    public class TriggerEventListener : MonoBehaviourCrowRx
    {
        [SerializeField] private UnityEvent<Collider> onTriggerEnter;
        [SerializeField] private UnityEvent<Collider> onTriggerExit;


        private void OnTriggerEnter(Collider other) => onTriggerEnter?.Invoke(other);
        private void OnTriggerExit(Collider other) => onTriggerExit?.Invoke(other);
    }
}