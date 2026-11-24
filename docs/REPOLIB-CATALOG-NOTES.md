# REPOLib catalogue validation notes

The R.E.P.O. plugin reads the live REPOLib registries and builds a catalogue snapshot after the game run is ready.

## Registry sources

- REPOLib.Modules.Enemies.AllEnemies exposes the active enemy catalogue.
- REPOLib.Modules.Items.AllItems exposes registered shop/game items.
- REPOLib.Modules.Valuables.AllValuables exposes valuable presets.

## Current unrestricted test mode

Every valid entry from Items.AllItems is currently added to the utility-item catalogue. The previous item-type allow-list, unknown-type rejection, weapon classification rejection and global weapon tag restriction have been removed for this test.

The only item exclusion mechanism is src/Rewards/SpawnWeaponsBlocker.cs. Its exact catalogue-ID list is intentionally empty. When exclusions are added later, use confirmed stable IDs from the live catalogue, not display names or broad item-type tags.

Category enable/weight settings remain remotely configurable. They determine whether the monster, utility-item or valuable category can be selected; they do not add item exclusions.

## Runtime validation still required

- Verify the full item catalogue count against the live game and installed mod profile.
- Test representative ordinary shop items through the Control Center utility-item spawn action.
- Record any items that fail to spawn, with their exact catalogue ID and failure details.
- Only after ordinary items are confirmed working should individual exclusions be added to SpawnWeaponsBlocker.cs.
- Verify spawn placement and host authority in a private multiplayer session.
- Refresh the catalogue after run/level transitions.

Do not guess item IDs or populate the blocker from display names. Keep all actual exclusion IDs in the one blocker file.
