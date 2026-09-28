using UnityEngine;
using System.Collections;

public abstract class AttackBehaviorSO : ScriptableObject
{
    public virtual string PlusDescription => "Enhanced move";
    public abstract IEnumerator Execute(AttackContext context);
}
