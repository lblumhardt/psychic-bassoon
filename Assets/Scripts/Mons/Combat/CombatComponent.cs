using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class CombatComponent : MonoBehaviour
{
    public List<AttackDataSO> attacks;
    [SerializeField, Min(0.05f)] private float baseAttackInterval = 1f;

    private readonly Dictionary<AttackDataSO, float> _moveReadyTimes = new();
    private float _nextGlobalAttackTime;
    private readonly HashSet<AttackDataSO> _plusMoves = new();
    public bool IsPlusMove(AttackDataSO move) => move != null && _plusMoves.Contains(move);

    public void ConfigureMoves(IReadOnlyList<AttackDataSO> moves, int level = 1)
    {
        attacks = new List<AttackDataSO>(2);
        _moveReadyTimes.Clear();
        _plusMoves.Clear();
        _nextGlobalAttackTime = 0f;
        if (moves == null) return;
        for (int i = 0; i < moves.Count && i < 2; i++)
        {
            if (moves[i] != null && moves[i].behavior != null &&
                (!moves[i].DebugOnly || SandboxSession.Active))
            {
                attacks.Add(moves[i]);
                if (i < Mathf.Clamp(level, 1, 3) - 1) _plusMoves.Add(moves[i]);
            }
        }
    }

    public IEnumerator AttackRoutine(Transform target)
    {
        AttackDataSO selectedAttack = SelectReadyAttack();
        while (selectedAttack == null && attacks != null && attacks.Count > 0)
        {
            float nextReadyTime = GetNextAttackTime();
            float waitSeconds = Mathf.Max(0.01f, nextReadyTime - Time.time);
            yield return new WaitForSeconds(waitSeconds);
            selectedAttack = SelectReadyAttack();
        }

        if (selectedAttack == null || selectedAttack.behavior == null)
        {
            yield break;
        }

        AttackContext context = new AttackContext
        {
            caster = transform,
            target = target,
            combatComponent = this,
            isPlus = _plusMoves.Contains(selectedAttack),
            attackData = selectedAttack
        };

        // Both clocks begin when the move starts. The global clock controls how
        // often this creature acts; the move clock prevents strong moves from spam.
        float now = Time.time;
        CreatureController creature = GetComponent<CreatureController>();
        if (creature != null && creature.IsDead) yield break;
        context.bonusDamage = creature != null ? creature.ConsumeAttackBonus() : 0f;
        float cooldownMultiplier = creature != null ? creature.MoveCooldownMultiplier : 1f;
        _moveReadyTimes[selectedAttack] = now + Mathf.Max(0f, selectedAttack.cooldown) * cooldownMultiplier;
        _nextGlobalAttackTime = now + Mathf.Max(0.05f, baseAttackInterval) /
            context.AttackSpeedMultiplier;

        GetComponent<CreatureVfx>()?.PlayAttack(target);
        BattleMoveTags.Show(this, selectedAttack);
        creature?.AbilityRuntime?.OnMoveUsed();

        Coroutine secondCast = null;
        if (creature != null && creature.HasFlowerScent)
        {
            AttackContext second = context;
            second.randomCast = true;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            second.randomDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            secondCast = StartCoroutine(selectedAttack.behavior.Execute(second));
        }
        yield return selectedAttack.behavior.Execute(context);
        if (secondCast != null) yield return secondCast;

        if (selectedAttack.duration > 0f)
        {
            yield return new WaitForSeconds(selectedAttack.duration);
        }
    }

    private AttackDataSO SelectReadyAttack()
    {
        if (attacks == null || attacks.Count == 0 || Time.time < _nextGlobalAttackTime)
        {
            return null;
        }

        float totalWeight = 0f;
        foreach (AttackDataSO attack in attacks)
        {
            if (!IsReady(attack)) continue;
            totalWeight += Mathf.Max(0f, attack.selectionWeight);
        }

        if (totalWeight <= 0f)
        {
            foreach (AttackDataSO attack in attacks)
            {
                if (IsReady(attack)) return attack;
            }
            return null;
        }

        float roll = Random.value * totalWeight;
        foreach (AttackDataSO attack in attacks)
        {
            if (!IsReady(attack)) continue;
            float weight = Mathf.Max(0f, attack.selectionWeight);
            if (weight <= 0f) continue;
            roll -= weight;
            if (roll <= 0f) return attack;
        }

        return null;
    }

    private bool IsReady(AttackDataSO attack)
    {
        if (attack == null || attack.behavior == null) return false;
        return !_moveReadyTimes.TryGetValue(attack, out float readyTime) || Time.time >= readyTime;
    }

    private float GetNextAttackTime()
    {
        float earliestMoveTime = float.PositiveInfinity;
        foreach (AttackDataSO attack in attacks)
        {
            if (attack == null || attack.behavior == null) continue;
            float readyTime = _moveReadyTimes.TryGetValue(attack, out float storedTime)
                ? storedTime : Time.time;
            earliestMoveTime = Mathf.Min(earliestMoveTime, readyTime);
        }

        if (float.IsPositiveInfinity(earliestMoveTime)) return Time.time;
        return Mathf.Max(_nextGlobalAttackTime, earliestMoveTime);
    }
}
