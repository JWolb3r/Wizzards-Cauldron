using UnityEngine;
using WizzardsCauldron.Core;

namespace WizzardsCauldron.Interactions
{
    /// <summary>
    /// Adds a project-owned intake volume at the visible cauldron while the
    /// protected original trigger and its references remain untouched.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CauldronIntakeProxy : MonoBehaviour
    {
        [SerializeField]
        private CauldronIntake _intake;

        public CauldronIntake Intake => _intake;

        public void Configure(CauldronIntake intake)
        {
            _intake = intake;
            EnsureTrigger();
        }

        private void Reset()
        {
            EnsureTrigger();
        }

        private void OnValidate()
        {
            EnsureTrigger();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_intake == null)
            {
                return;
            }

            _intake.TryProcessPotion(
                other.GetComponentInParent<PotionController>());
        }

        private void OnTriggerExit(Collider other)
        {
            if (_intake == null)
            {
                return;
            }

            _intake.StopTrackingPotion(
                other.GetComponentInParent<PotionController>());
        }

        private void EnsureTrigger()
        {
            BoxCollider trigger = GetComponent<BoxCollider>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }
        }
    }
}
