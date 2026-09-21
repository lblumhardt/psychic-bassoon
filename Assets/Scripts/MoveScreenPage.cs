using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MoveScreenPage : MonoBehaviour
{
    [SerializeField] private string battleSceneName = "testScene";

    private void Start()
    {
        VisualElement root = ToolkitUi.Attach(this, new Color(0.06f, 0.09f, 0.14f));
        root.style.flexDirection = FlexDirection.Column;
        root.style.paddingTop = 24;
        root.style.paddingBottom = 24;
        root.style.paddingLeft = 24;
        root.style.paddingRight = 24;

        VisualElement header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.alignItems = Align.Center;
        header.style.justifyContent = Justify.SpaceBetween;
        root.Add(header);
        header.Add(ToolkitUi.Label("Configure Moves", 30, Color.white, true));
        header.Add(ToolkitUi.Button("Next Round", () => SceneManager.LoadScene(battleSceneName)));

        Label subtitle = ToolkitUi.Label(
            "Move editing is coming later. These are the moves equipped for the next round.",
            17, new Color(0.72f, 0.79f, 0.87f));
        subtitle.style.marginTop = 12;
        subtitle.style.marginBottom = 20;
        root.Add(subtitle);

        ScrollView roster = new ScrollView();
        roster.style.flexGrow = 1;
        root.Add(roster);
        for (int i = 0; i < RunRoster.Members.Count; i++)
        {
            CreatureInstance member = RunRoster.Members[i];
            CreatureDataSO creature = member.Species;
            CreatureAbilitySO ability = member.Ability;
            var moves = member.EquippedMoves;

            VisualElement row = ToolkitUi.Panel(new Color(0.12f, 0.17f, 0.24f));
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.minHeight = 105;
            row.style.marginBottom = 10;
            roster.Add(row);

            VisualElement identity = new VisualElement();
            identity.style.width = Length.Percent(35);
            identity.Add(ToolkitUi.Label($"{i + 1}. {creature.creatureName}", 22, Color.white, true));
            identity.Add(ToolkitUi.Label(
                $"Ability: {(ability != null ? ability.DisplayName : "None")}",
                16, new Color(0.72f, 0.79f, 0.87f)));
            identity.Add(ToolkitUi.Label(
                $"Held: {(member.HeldItem != null ? member.HeldItem.displayName : "None")}  " +
                $"Spray: {(member.Spray != null ? member.Spray.displayName : "None")}",
                14, new Color(0.72f, 0.79f, 0.87f)));
            CreatureStats stats = member.Stats;
            Label statLabel = ToolkitUi.Label(
                $"HP {stats.hp}  PWR {stats.power}  DEF {stats.defense}\nMOVE {stats.moveSpeed}  ATK SPD {stats.attackSpeed}",
                14, new Color(0.72f, 0.79f, 0.87f));
            statLabel.style.marginTop = 6;
            identity.Add(statLabel);
            row.Add(identity);

            VisualElement moveList = new VisualElement();
            moveList.style.flexGrow = 1;
            row.Add(moveList);
            for (int slot = 0; slot < RunRoster.MoveSlots; slot++)
            {
                AttackDataSO move = moves[slot];
                Label moveLabel = ToolkitUi.Label(
                    $"Move {slot + 1}: {(move != null ? move.attackName : "Empty")}",
                    18, Color.white);
                moveLabel.style.marginBottom = 8;
                moveList.Add(moveLabel);
            }
        }
    }
}
