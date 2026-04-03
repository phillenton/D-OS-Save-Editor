# Save / LSX notes (D:OS EE)

## Example unpacked XML to inspect

`GameSaves/Divinity Original Sin Enhanced Edition/SaveFiles/globals.lsx`

(Path is relative to the **D-OS Game File Editor** workspace folder.)

## Inventory `Item` nodes vs in-game names

Character **inventory** `<node id="Item">` rows usually have **no** `attribute id="DisplayName"`. The friendly name you see in-game for a given `Stats` value (e.g. `SCROLL_Fireball_1`) comes from the **game’s data** (string tables / stats definitions), not from an extra string stored on that save row.

`DisplayName` **does** appear elsewhere in `globals.lsx` (e.g. on **`Template`** nodes in level data), but those are not the same nodes as inventory items.

## Same `Stats` (e.g. three `CONT_Backpack_A`) — what can still differ?

If several items share **`Stats`** and the same **`CurrentTemplate` / `OriginalTemplate`** GUIDs, the save can still differ on fields such as:

| Area | Examples (see your `globals.lsx`) |
|------|-------------------------------------|
| **`Flags`** | Same stats, different numeric `Flags` (e.g. `4227992` vs `33688`) — bit flags for item state. |
| **`ItemMachine`** | Some instances include `Type`, `ItemState` / `TransactionID`; others have an empty `ItemMachine`. |
| **`Key`** | Non-empty if the player renamed the item in-game. |
| **`Inventory` / `Parent` / `Slot` / `owner`** | Which bag or character holds the item (different container instances). |
| **Nested `Generation` / `Stats` / `VariableManager`** | Modifiers, identification, script variables. |

So: there is **not** a second “display name” column on inventory items for the UI text. Differences you see between “backpack” vs “pouch” for the **same** `Stats` string are likely **flags**, **ItemMachine**, **rename (`Key`)**, or **game-side logic** reading template + state—not a missing `DisplayName` attribute in the save.
