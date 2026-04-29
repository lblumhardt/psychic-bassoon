using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class CombatComponent : MonoBehaviour
{
    public List<AttackDataSO> attacks;

    public IEnumerator AttackRoutine(Transform target)
    {
        AttackDataSO selectedAttack = SelectAttack();
        if (selectedAttack == null || selectedAttack.behavior == null)
        {
            yield break;
        }

        AttackContext context = new AttackContext
        {
            caster = transform,
            target = target,
            combatComponent = this,
            attackData = selectedAttack
        };

        yield return selectedAttack.behavior.Execute(context);

        if (selectedAttack.duration > 0f)
        {
            yield return new WaitForSeconds(selectedAttack.duration);
        }

        if (selectedAttack.cooldown > 0f)
        {
            yield return new WaitForSeconds(selectedAttack.cooldown);
        }
    }

    private AttackDataSO SelectAttack()
    {
        if (attacks == null || attacks.Count == 0)
        {
            return null;
        }

        // Placeholder selection strategy: always first configured attack.
        return attacks[0];
    }
}
