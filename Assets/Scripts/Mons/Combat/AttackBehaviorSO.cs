using UnityEngine;
using System.Collections;

public abstract class AttackBehaviorSO : ScriptableObject
{
    public abstract IEnumerator Execute(AttackContext context);
}
