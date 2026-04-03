# Changelog

All notable changes in **[phillenton/D-OS-Save-Editor](https://github.com/phillenton/D-OS-Save-Editor)** (this repository) are documented here. The original open-source project by Anthony Jiang is **[AnthonyZJiang/D-OS-Save-Editor](https://github.com/AnthonyZJiang/D-OS-Save-Editor)**.

## Post-fork improvements (master)

### Session and save UX

- **Dirty state:** Window title `*` and “Not saved to file” when there are in-memory changes not written to disk; close prompt; cleared on successful Save and Reset.
- **Unapplied edits:** Character **Apply** and inventory **Apply changes** enable when pending edits exist; orange labels (“Unapplied edits”) with **Apply** before the label so the button does not jump; close warns if character or inventory edits are still unapplied.
- **Tabs:** Stats, Abilities, Traits, Talents report pending edits so Apply state stays accurate.

### Inventory

- **Saving item edits:** `WriteEditsToLsxAsync` loads a fresh `globals.lsx` document; inventory changes must target **that** tree. Previously, `ItemXmlNodes` from the initial parse pointed at the old DOM, so amount and other item edits did not persist to disk while character attributes (written via fresh XPath) still worked. Writes now replay the same BFS inventory walk on the loaded document. `WritePlayer` also uses a sequential loop instead of `Parallel.For` so multiple players do not concurrently mutate one `XmlDocument`.
- **Amount (stack count):** Centralized rules via `IsAmountEditable()`. Amount is editable for stackable-style items (potions, loot, skill books, arrows, etc.) and **not** for equipment-style rows (weapons, armor, furniture), quest items, keys, **Unique** rarity, or stats ids starting with `arm_` (armor), matching game behavior better than the old allow-list (potion/gold/grenade/scroll/food only).
- **Gold:** `DataTable.GoldNames` extended (e.g. `larger_gold`) so gold is categorized and filtered correctly.
- **Layout:** Inventory pending label aligned with character bar (Apply left, message right).

### Build

- **SortGenerationList** retargeted to **.NET Framework 4.6.2** so the solution builds with the same targeting pack as the main WPF app (`App.config` / `csproj` updated).
- **Main app:** SDK-style `D-OS Save Editor.csproj` targeting **net462**, **`PlatformTarget` x86** (matches **LSLib** / `LSLibNative.dll`). `WPF-UI` **4.2.0** — **`ThemesDictionary` only** in `App.xaml` (do **not** merge **`ControlsDictionary`** globally: it caused `BadImageFormatException` when updating standard `TextBlock`s). **Visual:** `FluentWindow` + **`TitleBar`**, **`ApplicationBackgroundBrush`**, **`WindowBackdropType="Mica"`** (Windows 11), update strip uses theme caution brush; **SaveEditor** window uses the same background brush. Build: `dotnet build`. Output: `bin\Release\net462\`. Windows API Code Pack: `packages\winapicp.1.1` via `packages.config` (`winapicp`).

### Other

- **wishlist.md** — backlog and done reference for future work (nested containers, skills/story tabs, D:OS 2, etc.).
