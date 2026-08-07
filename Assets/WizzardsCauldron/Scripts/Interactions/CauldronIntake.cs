using System;
using System.Collections.Generic;
using UnityEngine;
using WizzardsCauldron.Core;

namespace WizzardsCauldron.Interactions
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CauldronIntake : MonoBehaviour
    {
        [SerializeField] private CauldronController _cauldron;

        private readonly HashSet<PotionController> _potionsInside =
            new HashSet<PotionController>();

        public event Action<PotionController, PotionAcceptanceResult>
            PotionProcessed;

        private void Reset()
        {
            _cauldron = GetComponentInParent<CauldronController>();
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnValidate()
        {
            BoxCollider intakeCollider = GetComponent<BoxCollider>();

            if (intakeCollider != null)
            {
                intakeCollider.isTrigger = true;
            }

            if (_cauldron == null)
            {
                _cauldron =
                    GetComponentInParent<CauldronController>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            PotionController potion =
                other.GetComponentInParent<PotionController>();

            if (potion == null)
            {
                return;
            }

            if (!_potionsInside.Add(potion))
            {
                return;
            }

            if (_cauldron == null)
            {
                Debug.LogError(
                    "CauldronIntake has no CauldronController.",
                    this);
                return;
            }

            PotionAcceptanceResult result =
                _cauldron.TryAccept(potion);

            PotionProcessed?.Invoke(potion, result);
            LogResult(potion, result);
        }

        private void OnTriggerExit(Collider other)
        {
            PotionController potion =
                other.GetComponentInParent<PotionController>();

            if (potion != null)
            {
                _potionsInside.Remove(potion);
            }
        }

        private void OnDisable()
        {
            _potionsInside.Clear();
        }

        private void LogResult(
            PotionController potion,
            PotionAcceptanceResult result)
        {
            string potionName =
                potion.Definition != null
                    ? potion.Definition.DisplayName
                    : potion.name;

            switch (result)
            {
                case PotionAcceptanceResult.Accepted:
                    Debug.Log(
                        $"Accepted {potionName}. " +
                        $"Health: {_cauldron.TotalHealth}, " +
                        $"capacity: {_cauldron.UsedCapacity}/" +
                        $"{_cauldron.MaximumCapacity}.",
                        this);
                    break;

                case PotionAcceptanceResult.TooFull:
                    Debug.Log(
                        $"Rejected {potionName}: not enough capacity. " +
                        $"Remaining: {_cauldron.RemainingCapacity}.",
                        this);
                    break;

                case PotionAcceptanceResult.AlreadyUsed:
                    Debug.Log(
                        $"Rejected {potionName}: already used.",
                        this);
                    break;

                case PotionAcceptanceResult.NotInPuzzle:
                    Debug.Log(
                        $"Rejected {potionName}: not part of this puzzle.",
                        this);
                    break;

                case PotionAcceptanceResult.GameFinished:
                    Debug.Log(
                        $"Rejected {potionName}: the attempt is finished.",
                        this);
                    break;

                case PotionAcceptanceResult.InvalidPotion:
                    Debug.Log(
                        $"Rejected {potionName}: invalid potion data.",
                        this);
                    break;
            }
        }
    }
}