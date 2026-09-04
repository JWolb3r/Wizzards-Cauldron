using WizzardsCauldron.Core;
using UnityEngine;

namespace WizzardsCauldron.Interactions
{
    [DisallowMultipleComponent]
    public sealed class WandPotionCollisionExclusion : MonoBehaviour
    {
        private void Awake()
        {
            ApplyCollisionExclusions();
        }

        private void Start()
        {
            // Start covers objects that were enabled after this wand's Awake.
            ApplyCollisionExclusions();
        }

        private int ApplyCollisionExclusions()
        {
            Collider[] wandColliders =
                GetComponentsInChildren<Collider>(true);
            PotionController[] potions =
                FindObjectsByType<PotionController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            int ignoredPairCount = 0;

            foreach (PotionController potion in potions)
            {
                if (potion == null ||
                    potion.transform.IsChildOf(transform))
                {
                    continue;
                }

                Collider[] potionColliders =
                    potion.GetComponentsInChildren<Collider>(true);

                foreach (Collider wandCollider in wandColliders)
                {
                    if (wandCollider == null)
                    {
                        continue;
                    }

                    foreach (Collider potionCollider in potionColliders)
                    {
                        if (potionCollider == null)
                        {
                            continue;
                        }

                        Physics.IgnoreCollision(
                            wandCollider,
                            potionCollider,
                            true);
                        ignoredPairCount++;
                    }
                }
            }

            return ignoredPairCount;
        }
    }
}
