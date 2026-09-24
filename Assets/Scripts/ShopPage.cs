using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class ShopPage : MonoBehaviour
{
    private const int RoundBudget = 10;
    private const int CreaturePrice = 3;
    private const int SellRefund = CreaturePrice / 2;
    private const int RerollPrice = 1;

    [SerializeField] private string moveSceneName = "MoveScreen";
    [SerializeField] private CreatureDataSO startingCreature;
    [SerializeField] private CreatureDataSO[] monsterPool;

    private readonly CreatureInstance[] _creatureOffers = new CreatureInstance[3];
    private readonly CreatureItemSO[] _itemOffers = new CreatureItemSO[2];
    private CreatureItemSO[] _itemPool;
    private VisualElement _root;
    private int _money;
    private int _pendingItemIndex = -1;

    private void Start()
    {
        _money = RoundBudget;
        RunRoster.InitializeIfEmpty(startingCreature);
        _itemPool = Resources.LoadAll<CreatureItemSO>("Items");
        RollOffers();
        _root = ToolkitUi.Attach(this, new Color(0.06f, 0.09f, 0.14f));
        _root.style.flexDirection = FlexDirection.Column;
        Render();
    }

    private void RollOffers()
    {
        List<CreatureDataSO> creatures = new();
        if (monsterPool != null)
            foreach (CreatureDataSO creature in monsterPool)
                if (creature != null) creatures.Add(creature);

        for (int i = 0; i < _creatureOffers.Length; i++)
            _creatureOffers[i] = creatures.Count > 0
                ? CreatureInstance.Generate(creatures[Random.Range(0, creatures.Count)]) : null;

        List<CreatureItemSO> items = new();
        if (_itemPool != null)
            foreach (CreatureItemSO item in _itemPool)
                if (item != null) items.Add(item);

        for (int i = 0; i < _itemOffers.Length; i++)
        {
            if (items.Count == 0)
            {
                _itemOffers[i] = null;
                continue;
            }
            int choice = Random.Range(0, items.Count);
            _itemOffers[i] = items[choice];
            items.RemoveAt(choice);
        }
        _pendingItemIndex = -1;
    }

    private void Render()
    {
        _root.Clear();
        Color muted = new Color(0.72f, 0.79f, 0.87f);
        Color cardColor = new Color(0.12f, 0.17f, 0.24f);

        VisualElement top = new();
        top.style.height = Length.Percent(52);
        top.style.paddingTop = 16;
        top.style.paddingBottom = 12;
        top.style.paddingLeft = 24;
        top.style.paddingRight = 24;
        _root.Add(top);

        VisualElement header = new();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.marginBottom = 12;
        header.Add(ToolkitUi.Label("Monster Shop", 30, Color.white, true));
        string result = BattleManager.LastResult switch
        {
            RoundResult.Win => "Last round: Victory",
            RoundResult.Loss => "Last round: Defeat",
            _ => "First round"
        };
        header.Add(ToolkitUi.Label($"{result}\n{RunProgress.Summary}", 16, muted));

        VisualElement bank = new();
        bank.style.flexDirection = FlexDirection.Row;
        bank.style.alignItems = Align.Center;
        bank.Add(ToolkitUi.Label($"Money: {_money}", 23, Color.white, true));
        Button reroll = ToolkitUi.Button("Reroll · 1", Reroll);
        reroll.style.marginLeft = 12;
        reroll.SetEnabled(_money >= RerollPrice);
        bank.Add(reroll);
        header.Add(bank);
        top.Add(header);

        if (_pendingItemIndex >= 0 && _itemOffers[_pendingItemIndex] != null)
        {
            CreatureItemSO selected = _itemOffers[_pendingItemIndex];
            Label prompt = ToolkitUi.Label(
                $"Choose a creature below for {selected.displayName}. Its current {SlotName(selected)} will be replaced.",
                15, new Color(1f, 0.85f, 0.35f), true);
            prompt.style.marginBottom = 8;
            top.Add(prompt);
        }

        VisualElement offers = new();
        offers.style.flexDirection = FlexDirection.Row;
        offers.style.flexGrow = 1;
        top.Add(offers);
        for (int i = 0; i < _creatureOffers.Length; i++) AddCreatureCard(offers, i, cardColor, muted);
        for (int i = 0; i < _itemOffers.Length; i++) AddItemCard(offers, i, cardColor, muted);

        VisualElement bottom = new();
        bottom.style.flexGrow = 1;
        bottom.style.paddingTop = 14;
        bottom.style.paddingLeft = 24;
        bottom.style.paddingRight = 24;
        bottom.style.paddingBottom = 16;
        bottom.style.backgroundColor = new Color(0.08f, 0.12f, 0.18f);
        _root.Add(bottom);

        VisualElement rosterHeader = new();
        rosterHeader.style.flexDirection = FlexDirection.Row;
        rosterHeader.style.justifyContent = Justify.SpaceBetween;
        rosterHeader.style.alignItems = Align.Center;
        rosterHeader.style.marginBottom = 10;
        rosterHeader.Add(ToolkitUi.Label(
            $"Your Roster ({RunRoster.Members.Count}/{RunRoster.MaxMembers})", 26, Color.white, true));
        rosterHeader.Add(ToolkitUi.Button("Configure Moves", () => SceneManager.LoadScene(moveSceneName)));
        bottom.Add(rosterHeader);

        ScrollView roster = new();
        roster.style.flexGrow = 1;
        bottom.Add(roster);
        for (int i = 0; i < RunRoster.Members.Count; i++)
            AddRosterRow(roster, i, cardColor, muted);
    }

    private void AddCreatureCard(VisualElement offers, int index, Color cardColor, Color muted)
    {
        CreatureInstance offer = _creatureOffers[index];
        VisualElement card = CreateOfferCard(offers, index, cardColor);
        if (offer == null)
        {
            card.Add(ToolkitUi.Label("Sold", 20, muted, true));
            return;
        }

        CreatureStats stats = offer.Stats;
        card.Add(ToolkitUi.Label(offer.Species.creatureName, 20, Color.white, true));
        card.Add(ToolkitUi.Label(
            $"HP {stats.hp}  PWR {stats.power}\nDEF {stats.defense}  MOVE {stats.moveSpeed}\nATK SPD {stats.attackSpeed}",
            13, muted));
        Label loadout = ToolkitUi.Label(
            $"{MoveName(offer, 0)}\n{MoveName(offer, 1)}\n{(offer.Ability != null ? offer.Ability.DisplayName : "No ability")}",
            12, Color.white);
        loadout.style.marginTop = 6;
        loadout.style.flexGrow = 1;
        card.Add(loadout);
        Button buy = ToolkitUi.Button($"Buy · {CreaturePrice}", () => BuyCreature(index));
        buy.SetEnabled(_money >= CreaturePrice && RunRoster.Members.Count < RunRoster.MaxMembers);
        card.Add(buy);
        Button upgrade = ToolkitUi.Button($"Upgrade · {CreaturePrice}", () => ShowMergeChoices(offer, index));
        upgrade.style.marginTop = 4;
        upgrade.SetEnabled(_money >= CreaturePrice && HasMergeTarget(offer));
        card.Add(upgrade);
    }

    private void AddItemCard(VisualElement offers, int index, Color cardColor, Color muted)
    {
        CreatureItemSO item = _itemOffers[index];
        VisualElement card = CreateOfferCard(offers, index + _creatureOffers.Length, cardColor);
        if (item == null)
        {
            card.Add(ToolkitUi.Label("Sold", 20, muted, true));
            return;
        }

        card.Add(ToolkitUi.Label(item.displayName, 20, Color.white, true));
        Label typeLabel = ToolkitUi.Label(SlotName(item), 13, new Color(0.45f, 0.8f, 1f), true);
        typeLabel.style.marginTop = 3;
        card.Add(typeLabel);
        Label description = ToolkitUi.Label(item.description, 13, muted);
        description.style.whiteSpace = WhiteSpace.Normal;
        description.style.marginTop = 8;
        description.style.flexGrow = 1;
        card.Add(description);
        Button buy = ToolkitUi.Button(
            _pendingItemIndex == index ? "Cancel" : $"Buy · {item.price}",
            () => SelectItem(index));
        buy.SetEnabled(_pendingItemIndex == index || _money >= item.price);
        card.Add(buy);
    }

    private static VisualElement CreateOfferCard(VisualElement parent, int index, Color color)
    {
        VisualElement card = ToolkitUi.Panel(color);
        card.style.flexGrow = 1;
        card.style.flexBasis = 0;
        card.style.marginRight = index == 4 ? 0 : 10;
        parent.Add(card);
        return card;
    }

    private void AddRosterRow(VisualElement roster, int index, Color cardColor, Color muted)
    {
        CreatureInstance member = RunRoster.Members[index];
        VisualElement row = ToolkitUi.Panel(cardColor);
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 8;
        roster.Add(row);

        VisualElement identity = new();
        identity.style.flexGrow = 1;
        identity.Add(ToolkitUi.Label($"{index + 1}. {member.Species.creatureName}", 19, Color.white, true));
        identity.Add(ToolkitUi.Label(member.LevelSummary, 13, new Color(0.45f, 0.8f, 1f)));
        identity.Add(ToolkitUi.Label(
            $"Held: {ItemName(member.HeldItem)}   Spray: {ItemName(member.Spray)}", 13, muted));
        row.Add(identity);

        CreatureStats stats = member.Stats;
        Label statLabel = ToolkitUi.Label(
            $"HP {stats.hp}   PWR {stats.power}   DEF {stats.defense}   MOVE {stats.moveSpeed}   ATK SPD {stats.attackSpeed}",
            14, muted);
        statLabel.style.marginRight = 16;
        row.Add(statLabel);

        Button merge = ToolkitUi.Button("Merge into…", () => ShowMergeChoices(member, -1));
        merge.SetEnabled(HasMergeTarget(member));
        merge.style.marginRight = 8;
        row.Add(merge);

        if (_pendingItemIndex >= 0 && _itemOffers[_pendingItemIndex] != null)
        {
            Button equip = ToolkitUi.Button("Give Item", () => EquipItem(index));
            equip.style.marginRight = 8;
            row.Add(equip);
        }

        Button sell = ToolkitUi.Button($"Sell +{SellRefund}", () => Sell(index));
        sell.SetEnabled(RunRoster.Members.Count > 1);
        row.Add(sell);
    }

    private void BuyCreature(int index)
    {
        if (index < 0 || index >= _creatureOffers.Length || _creatureOffers[index] == null ||
            _money < CreaturePrice || !RunRoster.TryAdd(_creatureOffers[index])) return;
        _money -= CreaturePrice;
        _creatureOffers[index] = null;
        _pendingItemIndex = -1;
        Render();
    }

    private static bool HasMergeTarget(CreatureInstance donor)
    {
        foreach (CreatureInstance member in RunRoster.Members)
            if (member.CanMerge(donor)) return true;
        return false;
    }

    private void ShowMergeChoices(CreatureInstance donor, int shopIndex)
    {
        if (donor == null || !HasMergeTarget(donor)) return;
        VisualElement overlay = new();
        overlay.style.position = Position.Absolute;
        overlay.style.left = overlay.style.right = overlay.style.top = overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.8f);
        overlay.style.justifyContent = Justify.Center;
        overlay.style.alignItems = Align.Center;
        _root.Add(overlay);

        VisualElement panel = ToolkitUi.Panel(new Color(0.08f, 0.12f, 0.18f));
        panel.style.width = Length.Percent(85);
        panel.style.maxWidth = 950;
        panel.style.maxHeight = Length.Percent(90);
        overlay.Add(panel);
        panel.Add(ToolkitUi.Label($"Merge {donor.Species.creatureName} into…", 25, Color.white, true));
        Label rules = ToolkitUi.Label(
            $"Consumes the selected {(shopIndex >= 0 ? "shop" : "roster")} creature" +
            (shopIndex >= 0 ? $" for {CreaturePrice} money. " : ". ") +
            $"Contributes {donor.CopyCount} {(donor.CopyCount == 1 ? "copy" : "copies")}.\n" +
            "The receiving creature keeps its moves, ability, item, and spray.\n" +
            $"Discarded: held item {ItemName(donor.HeldItem)}; spray {ItemName(donor.Spray)}.\n" +
            "Level 2 needs 2 extra copies; level 3 needs 3 more. Each level adds 25% of original stats, rounded up.",
            15, new Color(0.8f, 0.86f, 0.94f));
        rules.style.whiteSpace = WhiteSpace.Normal;
        rules.style.marginTop = rules.style.marginBottom = 12;
        panel.Add(rules);

        ScrollView choices = new();
        choices.style.flexShrink = 1;
        choices.style.minHeight = 0;
        panel.Add(choices);
        for (int i = 0; i < RunRoster.Members.Count; i++)
        {
            CreatureInstance receiver = RunRoster.Members[i];
            if (!receiver.CanMerge(donor)) continue;
            VisualElement choice = ToolkitUi.Panel(new Color(0.12f, 0.17f, 0.24f));
            choice.style.marginBottom = 8;
            choices.Add(choice);
            choice.Add(ToolkitUi.Label($"{i + 1}. {receiver.Species.creatureName} · {receiver.LevelSummary}",
                18, Color.white, true));
            choice.Add(ToolkitUi.Label($"After merge: {receiver.PreviewMergeProgress(donor)}", 15,
                new Color(0.45f, 0.8f, 1f)));
            CreatureStats before = receiver.Stats;
            CreatureStats after = receiver.PreviewMergeStats(donor);
            Label preview = ToolkitUi.Label(
                $"HP {before.hp} → {after.hp}   PWR {before.power} → {after.power}   DEF {before.defense} → {after.defense}\n" +
                $"MOVE {before.moveSpeed} → {after.moveSpeed}   ATK SPD {before.attackSpeed} → {after.attackSpeed}\n" +
                $"Keeps: {ItemName(receiver.HeldItem)} / {ItemName(receiver.Spray)} · " +
                $"{MoveName(receiver, 0)} / {MoveName(receiver, 1)} · " +
                (receiver.Ability != null ? receiver.Ability.DisplayName : "No ability"), 14, Color.white);
            preview.style.whiteSpace = WhiteSpace.Normal;
            choice.Add(preview);
            int excess = receiver.CopyCount + donor.CopyCount - CreatureInstance.LevelThreeCopies;
            if (excess > 0)
                choice.Add(ToolkitUi.Label($"{excess} excess copies will be lost at max level.",
                    14, new Color(1f, 0.8f, 0.35f)));
            Button confirm = ToolkitUi.Button(shopIndex >= 0 ? $"Upgrade this creature · {CreaturePrice}" : "Merge into this creature",
                () => CompleteMerge(donor, receiver, shopIndex));
            confirm.style.marginTop = 6;
            choice.Add(confirm);
        }
        Button cancel = ToolkitUi.Button("Cancel", () => overlay.RemoveFromHierarchy());
        cancel.style.marginTop = 10;
        cancel.style.flexShrink = 0;
        panel.Add(cancel);
    }

    private void CompleteMerge(CreatureInstance donor, CreatureInstance receiver, int shopIndex)
    {
        bool receiverInRoster = false;
        foreach (CreatureInstance member in RunRoster.Members)
            if (ReferenceEquals(member, receiver)) receiverInRoster = true;
        if (!receiverInRoster) return;
        if (shopIndex >= 0)
        {
            if (shopIndex >= _creatureOffers.Length || !ReferenceEquals(_creatureOffers[shopIndex], donor) ||
                _money < CreaturePrice || !receiver.TryMerge(donor)) return;
            _money -= CreaturePrice;
            _creatureOffers[shopIndex] = null;
        }
        else if (!RunRoster.TryMerge(donor, receiver)) return;
        _pendingItemIndex = -1;
        Render();
    }

    private void SelectItem(int index)
    {
        if (index < 0 || index >= _itemOffers.Length || _itemOffers[index] == null) return;
        _pendingItemIndex = _pendingItemIndex == index ? -1 : index;
        Render();
    }

    private void EquipItem(int creatureIndex)
    {
        if (_pendingItemIndex < 0 || _pendingItemIndex >= _itemOffers.Length ||
            creatureIndex < 0 || creatureIndex >= RunRoster.Members.Count) return;
        CreatureItemSO item = _itemOffers[_pendingItemIndex];
        if (item == null || _money < item.price) return;

        RunRoster.Members[creatureIndex].Equip(item);
        _money -= item.price;
        _itemOffers[_pendingItemIndex] = null;
        _pendingItemIndex = -1;
        Render();
    }

    private void Reroll()
    {
        if (_money < RerollPrice) return;
        _money -= RerollPrice;
        RollOffers();
        Render();
    }

    private static string MoveName(CreatureInstance creature, int slot)
    {
        if (creature == null || slot < 0 || slot >= creature.EquippedMoves.Count) return "Empty";
        AttackDataSO move = creature.EquippedMoves[slot];
        return move != null ? move.attackName : "Empty";
    }

    private static string ItemName(CreatureItemSO item) => item != null ? item.displayName : "None";
    private static string SlotName(CreatureItemSO item) =>
        item.slot == CreatureItemSlot.HeldItem ? "Held Item" : "Spray";

    private void Sell(int index)
    {
        if (RunRoster.SellAt(index) == null) return;
        _money += SellRefund;
        Render();
    }
}
