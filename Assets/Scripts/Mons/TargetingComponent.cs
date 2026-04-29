using UnityEngine;
using System.Collections;

public class TargetingComponent : MonoBehaviour
{
    [SerializeField] private float targetingDelaySeconds = 0.25f;

    public IEnumerator AcquireTargetRoutine(System.Action<Transform> onTargetAcquired)
    {
        yield return new WaitForSeconds(targetingDelaySeconds);

        // Placeholder: later replace with proper nearest/aggro target selection.
        onTargetAcquired?.Invoke(null);
    }
}
