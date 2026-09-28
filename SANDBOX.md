# Sandbox debugging

Open **Sandbox / Debug** from the main menu. Configure one to five creatures on each team, then choose **Start Test Battle**.

- Creature selection restores that species' exact default stats, first two moves, and first ability. It does not roll stat variation.
- Expand **Moves** to see every authored move. Green rows marked **IN POOL** belong to the selected creature; both slot buttons work for every move, including duplicate moves in both slots.
- Pick any authored ability, held item, or spray, or choose **None** to remove one.
- Edit all five base stats. Inputs accept positive integers up to 1,000,000. The battle-stat preview includes level scaling and equipment bonuses.
- Select levels 1–3 freely. Level 2 upgrades the first move; level 3 upgrades both, following normal combat rules.
- **Edit Teams** returns to the saved setup during or after combat. **Restart Test / Replay Test** launches the same teams in a newly generated arena.
- Sandbox teams are held in memory until the application exits. Tests do not change run rosters, opponent rosters, wins/losses, or perks. Existing run perks do not apply in sandbox battles.

The resource catalog includes all creature, move, ability, and item assets. The editor refreshes it when scripts load, before entering Play Mode, and before a build.

## Play Mode checks

1. Open the main menu and sandbox. Confirm both teams appear and all seven current creatures, eight moves, two abilities, and ten equipment assets are available in their respective pickers.
2. Assign a move outside a creature's pool to each slot. Confirm only native moves are green and assignments are retained when collapsing the list.
3. Set a custom HP value, level 3, an item, a spray, and an ability on both sides. Confirm the battle-stat preview includes bonuses and both creatures use the configured moves.
4. Add five creatures per team, remove one, and run the resulting four-versus-five battle.
5. Return through **Edit Teams** before combat ends, then after a result. Confirm selections survive both paths. Replay a completed battle.
6. Set an input to zero or a negative value; confirm it becomes 1. Remove gear and reset base stats; verify the preview updates.
7. Start a normal run after testing and verify normal shop, opponent generation, run perks, and win/loss progression still operate.

Validation performed: runtime and editor scripts compiled with Unity 6000.3.14f1's compiler and assembly references. Interactive Play Mode checks remain to be performed; the batch editor exited before scene execution.
