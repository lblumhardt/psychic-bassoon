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

    private CreatureInstance[] _creatureOffers => RunShop.Creatures;
    private CreatureItemSO[] _itemOffers => RunShop.Items;
    private CreatureItemSO[] _itemPool;
    private VisualElement _root;
    private int _money { get => RunShop.Money; set => RunShop.Money = value; }
    private int _pendingItemIndex = -1;

    private void Start()
    {
        bool newRound = RunShop.RefreshBudget(RunProgress.Wins + RunProgress.Losses, RoundBudget);
        RunRoster.InitializeIfEmpty(startingCreature);
        _itemPool = Resources.LoadAll<CreatureItemSO>("Items");
        if (newRound || !RunShop.OffersPrepared) RollOffers();
        RunPerks.PrepareDraft();
        _root = ToolkitUi.Attach(this, new Color(0.06f, 0.09f, 0.14f));
        _root.style.flexDirection = FlexDirection.Column;
        Render();
    }

    private void RollOffers()
    {
        RunShop.OffersPrepared = true;
        List<CreatureDataSO> creatures = new();
        if (monsterPool != null)
            foreach (CreatureDataSO creature in monsterPool)
                if (creature != null) creatures.Add(creature);

        for (int i = 0; i < _creatureOffers.Length; i++)
        {
            if (RunShop.CreatureLocked[i] && _creatureOffers[i] != null) continue;
            _creatureOffers[i] = creatures.Count > 0
                ? RunShop.GenerateCreature(creatures[Random.Range(0, creatures.Count)]) : null;
        }

        List<CreatureItemSO> items = new();
        if (_itemPool != null)
            foreach (CreatureItemSO item in _itemPool)
                if (item != null) items.Add(item);

        for (int i = 0; i < _itemOffers.Length; i++)
            if (RunShop.ItemLocked[i] && _itemOffers[i] != null) items.Remove(_itemOffers[i]);

        for (int i = 0; i < _itemOffers.Length; i++)
        {
            if (RunShop.ItemLocked[i] && _itemOffers[i] != null) continue;
            if (items.Count == 0)
            {
                _itemOffers[i] = null;
                continue;
            }
            int choice = RunShop.ChooseItem(items);
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

        ScrollView page = new();
        page.style.flexGrow = 1;
        page.style.minHeight = 0;
        _root.Add(page);

        VisualElement top = new();
        top.style.paddingTop = 16;
        top.style.paddingBottom = 12;
        top.style.paddingLeft = 24;
        top.style.paddingRight = 24;
        page.Add(top);

        VisualElement header = new();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.flexWrap = Wrap.Wrap;
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
        if (RunShop.CreatureStatBonus > 0)
            top.Add(ToolkitUi.Label($"Shop creatures: +{RunShop.CreatureStatBonus} Attack / +{RunShop.CreatureStatBonus} Defense", 14, muted));
        if (RunRoster.Members.Count == 0)
            top.Add(ToolkitUi.Label("Your roster is empty. Buy a creature before the next round or take a loss.", 15, new Color(1f, 0.85f, 0.35f)));

        if (_pendingItemIndex >= 0 && _itemOffers[_pendingItemIndex] != null)
        {
            CreatureItemSO selected = _itemOffers[_pendingItemIndex];
            Label prompt = ToolkitUi.Label(
                selected.slot == CreatureItemSlot.Consumable
                    ? $"Choose a creature below to use {selected.displayName} on. {selected.description}"
                    : $"Choose a creature below for {selected.displayName}. Its current {SlotName(selected)} will be replaced.",
                15, new Color(1f, 0.85f, 0.35f), true);
            prompt.style.marginBottom = 8;
            top.Add(prompt);
        }

        VisualElement offers = new();
        offers.style.flexDirection = FlexDirection.Row;
        offers.style.flexGrow = 1;
        offers.style.flexWrap = Wrap.Wrap;
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
        page.Add(bottom);

        VisualElement rosterHeader = new();
        rosterHeader.style.flexDirection = FlexDirection.Row;
        rosterHeader.style.justifyContent = Justify.SpaceBetween;
        rosterHeader.style.alignItems = Align.Center;
        rosterHeader.style.marginBottom = 10;
        rosterHeader.Add(ToolkitUi.Label(
            $"Your Roster ({RunRoster.Members.Count}/{RunRoster.MaxMembers})", 26, Color.white, true));
        rosterHeader.Add(ToolkitUi.Button("Inspect Loadouts", () => SceneManager.LoadScene(moveSceneName)));
        Button test = ToolkitUi.Button("Test Loadouts", () =>
        {
            SandboxSession.LoadRunTeams();
            SandboxSession.ReturnToShop = true;
            SandboxSession.OpenEditor = true;
            SceneManager.LoadScene("MainMenu");
        });
        test.SetEnabled(RunRoster.Members.Count > 0);
        rosterHeader.Add(test);
        bottom.Add(rosterHeader);
        Label perks = ToolkitUi.Label(RunPerks.Summary, 13, muted);
        perks.style.whiteSpace = WhiteSpace.Normal;
        bottom.Add(perks);

        VisualElement roster = new();
        roster.style.flexGrow = 1;
        bottom.Add(roster);
        for (int i = 0; i < RunRoster.Members.Count; i++)
            AddRosterRow(roster, i, cardColor, muted);
        PerkDraftUi.Show(_root, Render);
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
        VisualElement portrait = ToolkitUi.CreaturePortrait(offer.Species, 96);
        portrait.style.alignSelf = Align.Center;
        portrait.style.marginBottom = 10;
        card.Add(portrait);
        Label name = ToolkitUi.Label(offer.Species.creatureName, 20, Color.white, true);
        name.style.whiteSpace = WhiteSpace.Normal;
        card.Add(name);
        card.Add(ToolkitUi.Label(offer.LevelSummary, 12, new Color(0.45f, 0.8f, 1f)));
        card.Add(ToolkitUi.Label(
            $"HP {stats.hp}  PWR {stats.power}\nDEF {stats.defense}  MOVE {stats.moveSpeed}\nATK SPD {stats.attackSpeed}",
            13, muted));
        Label loadout = ToolkitUi.Label(
            $"{MoveName(offer, 0)}\n{MoveName(offer, 1)}\n{(offer.Ability != null ? offer.Ability.DisplayName : "No ability")}",
            12, Color.white);
        loadout.style.marginTop = 6;
        loadout.style.marginBottom = 10;
        loadout.style.whiteSpace = WhiteSpace.Normal;
        loadout.style.flexGrow = 1;
        card.Add(loadout);
        ToolkitUi.AbilityTooltip(loadout, () => offer.Ability);
        Button buy = ToolkitUi.Button($"Buy · {CreaturePrice}", () => BuyCreature(index));
        buy.SetEnabled(_money >= CreaturePrice && RunRoster.Members.Count < RunRoster.MaxMembers);
        card.Add(buy);
        Button upgrade = ToolkitUi.Button($"Upgrade · {CreaturePrice}", () => ShowMergeChoices(offer, index));
        upgrade.style.marginTop = 4;
        upgrade.SetEnabled(_money >= CreaturePrice && HasMergeTarget(offer));
        card.Add(upgrade);
        AddLockButton(card, index, false);
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
        buy.SetEnabled(_pendingItemIndex == index || (_money >= item.price && CanPurchaseItem(item)));
        card.Add(buy);
        AddLockButton(card, index, true);
    }

    private void AddLockButton(VisualElement card, int index, bool item)
    {
        bool[] locks = item ? RunShop.ItemLocked : RunShop.CreatureLocked;
        Button freeze = ToolkitUi.Button(locks[index] ? "Frozen · Unfreeze" : "Freeze", () =>
        {
            locks[index] = !locks[index];
            Render();
        });
        freeze.tooltip = "Keep this offer through rerolls and future rounds until purchased or unfrozen.";
        freeze.style.height = 30;
        freeze.style.marginTop = 4;
        card.Add(freeze);
    }

    private static VisualElement CreateOfferCard(VisualElement parent, int index, Color color)
    {
        VisualElement card = ToolkitUi.Panel(color);
        card.style.flexGrow = 1;
        card.style.flexBasis = 200;
        card.style.minWidth = 200;
        card.style.marginBottom = 10;
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
        row.style.flexWrap = Wrap.Wrap;
        roster.Add(row);
        VisualElement portrait = ToolkitUi.CreaturePortrait(member.Species, 72);
        portrait.style.marginRight = 14;
        row.Add(portrait);

        VisualElement identity = new();
        identity.style.flexGrow = 1;
        identity.style.minWidth = 230;
        identity.style.marginBottom = 6;
        identity.Add(ToolkitUi.Label($"{index + 1}. {member.Species.creatureName}", 19, Color.white, true));
        identity.Add(ToolkitUi.Label(member.LevelSummary, 13, new Color(0.45f, 0.8f, 1f)));
        if (!string.IsNullOrEmpty(member.NextRoundBonusSummary))
            identity.Add(ToolkitUi.Label(member.NextRoundBonusSummary, 13, new Color(1f, 0.85f, 0.4f)));
        identity.Add(ToolkitUi.Label(
            $"Held: {ItemName(member.HeldItem)}   Spray: {ItemName(member.Spray)}", 13, muted));
        identity.Add(ToolkitUi.Label($"{MoveName(member, 0)} / {MoveName(member, 1)}", 13, Color.white));
        row.Add(identity);

        CreatureStats stats = member.Stats;
        Label statLabel = ToolkitUi.Label(
            $"HP {stats.hp}   PWR {stats.power}   DEF {stats.defense}\nMOVE {stats.moveSpeed}   ATK SPD {stats.attackSpeed}",
            14, muted);
        statLabel.style.marginRight = 16;
        statLabel.style.marginBottom = 6;
        row.Add(statLabel);

        Button merge = ToolkitUi.Button("Merge into…", () => ShowMergeChoices(member, -1));
        merge.SetEnabled(HasMergeTarget(member));
        merge.style.marginRight = 8;
        row.Add(merge);

        if (_pendingItemIndex >= 0 && _itemOffers[_pendingItemIndex] != null)
        {
            CreatureItemSO pending = _itemOffers[_pendingItemIndex];
            Button equip = ToolkitUi.Button(pending.slot == CreatureItemSlot.Consumable ? "Use " + pending.displayName : "Give Item", () => EquipItem(index));
            equip.SetEnabled(pending.consumableEffect != ConsumableEffect.TwinBrother || member.Level < CreatureInstance.MaxLevel);
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
        RunShop.CreatureLocked[index] = false;
        _pendingItemIndex = -1;
        Render();
    }

    private static bool HasMergeTarget(CreatureInstance donor)
    {
        foreach (CreatureInstance member in RunRoster.Members)
            if (member.CanMerge(donor)) return true;
        return false;
    }

    private void ShowMergeChoices(CreatureInstance donor, int shopIndex, CreatureInstance twinReceiver = null, int twinItemIndex = -1)
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
        VisualElement donorPortrait = ToolkitUi.CreaturePortrait(donor.Species, 64);
        donorPortrait.style.marginBottom = 8;
        panel.Add(donorPortrait);
        bool isTwin = twinReceiver != null;
        panel.Add(ToolkitUi.Label(isTwin ? $"Twin Brother · {twinReceiver.Species.creatureName}" : $"Merge {donor.Species.creatureName} into…", 25, Color.white, true));
        Label rules = ToolkitUi.Label(
            isTwin ? "Adds one copy toward leveling up. Keeps this creature's ability, equipment, and stat bonuses.\n" +
            "No new moves. Level 2: choose one plus move. Level 3: both moves are plus." :
            $"Consumes the selected {(shopIndex >= 0 ? "shop" : "roster")} creature" +
            (shopIndex >= 0 ? $" for {CreaturePrice} money. " : ". ") +
            $"Contributes {donor.CopyCount} {(donor.CopyCount == 1 ? "copy" : "copies")}.\n" +
            "Choose moves from both creatures. The receiver keeps its ability, item, and spray.\n" +
            "Level 2: choose one plus move. Level 3: both moves are plus.\n" +
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
            if (isTwin && !ReferenceEquals(receiver, twinReceiver)) continue;
            if (!receiver.CanMerge(donor)) continue;
            VisualElement choice = ToolkitUi.Panel(new Color(0.12f, 0.17f, 0.24f));
            choice.style.marginBottom = 8;
            choices.Add(choice);
            VisualElement receiverPortrait = ToolkitUi.CreaturePortrait(receiver.Species, 56);
            receiverPortrait.style.marginBottom = 8;
            choice.Add(receiverPortrait);
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
            ToolkitUi.AbilityTooltip(preview, () => receiver.Ability);
            int excess = receiver.CopyCount + donor.CopyCount - CreatureInstance.LevelThreeCopies;
            if (receiver.Level == CreatureInstance.MaxLevel)
                choice.Add(ToolkitUi.Label("Max level: move changes only; no stat gain.", 14, new Color(1f, 0.8f, 0.35f)));
            else if (excess > 0)
                choice.Add(ToolkitUi.Label($"{excess} excess copies will be lost at max level.",
                    14, new Color(1f, 0.8f, 0.35f)));
            List<AttackDataSO> available = receiver.MergeMoveChoices(donor);
            int required = Mathf.Min(2, available.Count);
            List<AttackDataSO> selected = available.GetRange(0, required);
            int resultCopies = receiver.CopyCount + donor.CopyCount;
            int plusSlots = resultCopies >= CreatureInstance.LevelThreeCopies ? 2 : resultCopies >= CreatureInstance.LevelTwoCopies ? 1 : 0;
            Label plusPreview = ToolkitUi.Label("", 14, new Color(1f, 0.85f, 0.4f));
            plusPreview.style.whiteSpace = WhiteSpace.Normal;
            choice.Add(plusPreview);
            Label selectionLabel = ToolkitUi.Label("", 15, Color.white, true);
            choice.Add(selectionLabel);
            Button confirm = ToolkitUi.Button(isTwin ? $"Use Twin Brother · {_itemOffers[twinItemIndex].price}" :
                shopIndex >= 0 ? $"Confirm merge · {CreaturePrice}" : "Confirm merge",
                () => { if (isTwin) CompleteTwin(donor, receiver, twinItemIndex, selected);
                    else CompleteMerge(donor, receiver, shopIndex, selected); });
            void UpdateSelection()
            {
                selectionLabel.text = $"Keep {required} distinct moves ({selected.Count}/{required} selected). Unselected moves are lost.";
                confirm.SetEnabled(selected.Count == required);
                List<string> previews = new();
                for (int slot = 0; slot < selected.Count; slot++)
                {
                    bool plus = slot < plusSlots;
                    previews.Add(selected[slot].attackName + (plus ? "+: " + selected[slot].behavior.PlusDescription : ""));
                }
                plusPreview.text = "After merge: " + string.Join(" / ", previews);
            }
            foreach (AttackDataSO move in available)
            {
                bool fromReceiver = false;
                bool fromDonor = false;
                foreach (AttackDataSO equipped in receiver.EquippedMoves) if (equipped == move) fromReceiver = true;
                foreach (AttackDataSO equipped in donor.EquippedMoves) if (equipped == move) fromDonor = true;
                string origin = fromReceiver && fromDonor ? "Both creatures" : fromReceiver ? "Receiver" : "Donor";
                Toggle toggle = new Toggle($"{move.attackName} · {origin} · Base power {move.damage:0.#} · Cooldown {move.cooldown:0.#}s · Range {move.range:0.#}");
                toggle.value = selected.Contains(move);
                toggle.style.color = Color.white;
                toggle.style.marginTop = 6;
                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue) { if (!selected.Contains(move)) selected.Add(move); }
                    else selected.Remove(move);
                    UpdateSelection();
                });
                choice.Add(toggle);
                if (plusSlots == 1)
                {
                    Button makePlus = ToolkitUi.Button("Make " + move.attackName + " the plus move", () =>
                    {
                        if (!selected.Contains(move)) return;
                        selected.Remove(move);
                        selected.Insert(0, move);
                        UpdateSelection();
                    });
                    makePlus.tooltip = "Select this move above, then make it the level-2 plus move.";
                    makePlus.style.height = 28;
                    choice.Add(makePlus);
                }
            }
            UpdateSelection();
            confirm.style.marginTop = 10;
            choice.Add(confirm);
        }
        Button cancel = ToolkitUi.Button("Cancel", () => overlay.RemoveFromHierarchy());
        cancel.style.marginTop = 10;
        cancel.style.flexShrink = 0;
        panel.Add(cancel);
    }

    private void CompleteMerge(CreatureInstance donor, CreatureInstance receiver, int shopIndex, IReadOnlyList<AttackDataSO> selectedMoves)
    {
        bool receiverInRoster = false;
        foreach (CreatureInstance member in RunRoster.Members)
            if (ReferenceEquals(member, receiver)) receiverInRoster = true;
        if (!receiverInRoster) return;
        if (shopIndex >= 0)
        {
            if (shopIndex >= _creatureOffers.Length || !ReferenceEquals(_creatureOffers[shopIndex], donor) ||
                _money < CreaturePrice || !receiver.TryMerge(donor, selectedMoves)) return;
            _money -= CreaturePrice;
            _creatureOffers[shopIndex] = null;
            RunShop.CreatureLocked[shopIndex] = false;
        }
        else if (!RunRoster.TryMerge(donor, receiver, selectedMoves)) return;
        _pendingItemIndex = -1;
        Render();
    }

    private void SelectItem(int index)
    {
        if (index < 0 || index >= _itemOffers.Length || _itemOffers[index] == null) return;
        CreatureItemSO item = _itemOffers[index];
        if (_pendingItemIndex == index) { _pendingItemIndex = -1; Render(); return; }
        if (_money < item.price || !CanPurchaseItem(item)) return;
        if (!item.NeedsCreatureTarget)
        {
            if (item.consumableEffect == ConsumableEffect.PartyGoop)
            {
                if (!RunRoster.ApplyPartyGoop()) return;
            }
            else if (item.consumableEffect == ConsumableEffect.HelpWantedSign) RunShop.ApplyHelpWantedSign();
            else return;
            CompleteItemPurchase(index);
            return;
        }
        _pendingItemIndex = _pendingItemIndex == index ? -1 : index;
        Render();
    }

    private void EquipItem(int creatureIndex)
    {
        if (_pendingItemIndex < 0 || _pendingItemIndex >= _itemOffers.Length ||
            creatureIndex < 0 || creatureIndex >= RunRoster.Members.Count) return;
        CreatureItemSO item = _itemOffers[_pendingItemIndex];
        if (item == null || _money < item.price) return;

        CreatureInstance creature = RunRoster.Members[creatureIndex];
        if (item.slot == CreatureItemSlot.Consumable)
        {
            switch (item.consumableEffect)
            {
                case ConsumableEffect.Goop: creature.AddStatBonus(1, 1); break;
                case ConsumableEffect.DoubleGoop: creature.AddStatBonus(2, 2); break;
                case ConsumableEffect.HyperGoop: creature.AddStatBonus(4, 4, true); break;
                case ConsumableEffect.Botulinum:
                    if (!RunRoster.Remove(creature)) return;
                    break;
                case ConsumableEffect.TwinBrother:
                    if (creature.Level >= CreatureInstance.MaxLevel) return;
                    ShowMergeChoices(creature.CreateTwinDonor(), -1, creature, _pendingItemIndex);
                    return;
                default: return;
            }
        }
        else creature.Equip(item);
        CompleteItemPurchase(_pendingItemIndex);
    }

    private void CompleteItemPurchase(int index)
    {
        _money -= _itemOffers[index].price;
        _itemOffers[index] = null;
        RunShop.ItemLocked[index] = false;
        _pendingItemIndex = -1;
        Render();
    }

    private void CompleteTwin(CreatureInstance donor, CreatureInstance receiver, int index,
        IReadOnlyList<AttackDataSO> moves)
    {
        if (index < 0 || index >= _itemOffers.Length) return;
        CreatureItemSO item = _itemOffers[index];
        if (item == null || item.consumableEffect != ConsumableEffect.TwinBrother || _money < item.price ||
            receiver.Level >= CreatureInstance.MaxLevel) return;
        bool inRoster = false;
        foreach (CreatureInstance member in RunRoster.Members) if (ReferenceEquals(member, receiver)) inRoster = true;
        if (!inRoster || !receiver.TryMerge(donor, moves)) return;
        CompleteItemPurchase(index);
    }

    private static bool CanPurchaseItem(CreatureItemSO item)
    {
        if (item.consumableEffect == ConsumableEffect.HelpWantedSign && item.slot == CreatureItemSlot.Consumable) return true;
        if (item.consumableEffect == ConsumableEffect.TwinBrother && item.slot == CreatureItemSlot.Consumable)
        {
            foreach (CreatureInstance member in RunRoster.Members)
                if (member.Level < CreatureInstance.MaxLevel) return true;
            return false;
        }
        return RunRoster.Members.Count > 0;
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
        return creature.MoveDisplayName(slot);
    }

    private static string ItemName(CreatureItemSO item) => item != null ? item.displayName : "None";
    private static string SlotName(CreatureItemSO item) =>
        item.slot == CreatureItemSlot.HeldItem ? "Held Item" : item.slot == CreatureItemSlot.Spray ? "Spray" : "Consumable";

    private void Sell(int index)
    {
        if (RunRoster.SellAt(index) == null) return;
        _money += SellRefund;
        Render();
    }
}
