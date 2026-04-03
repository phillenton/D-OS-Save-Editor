# Creates a PR from this fork (feature/nested-containers) into AnthonyZJiang/D-OS-Save-Editor master.
# Requires: GitHub CLI (`winget install GitHub.cli`) and `gh auth login` once.

$ErrorActionPreference = "Stop"
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI (gh) not found. Install with: winget install GitHub.cli"
}

gh auth status 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Not authenticated. Run: gh auth login" -ForegroundColor Yellow
    exit 1
}

$bodyFile = Join-Path $env:TEMP "gh-pr-dos-body.md"
@'
## Summary
Enhancements for **Divinity: Original Sin – Enhanced Edition** saves: full nested inventory support, reliable persistence of inventory edits to `globals.lsx`, and clearer inventory UI/session behavior.

## Key changes
- **Nested items:** BFS inventory walk (same order for parse and write); `ItemXmlNodeIdx` targets the correct XML node for nested containers.
- **Save fix:** `WriteEditsToLsxAsync` reloads `globals.lsx`; item writes use **fresh** `XmlNode`s from a replayed BFS on that document. `WritePlayer` is sequential (no parallel DOM writes).
- **LSX writes:** `SetLsxAttributeValue` sets the `value=` attribute reliably.
- **UI:** Inventory `TreeView`, filters/search, **Apply** + pending labels; `TreeViewItem.Tag` synced after Apply; wider save dialog; equip slot (0–14) for paper doll; compact equipped marker; removed non-functional display-name row.
- **Amount:** `IsAmountEditable()` rules; extended gold names in `DataTable`.
- **Docs:** `CHANGELOG.md`, `SAVE_FORMAT_NOTES.md`.

Fork: `phillenton/d-os-save-file-editor` branch `feature/nested-containers` → upstream `master`.
'@ | Set-Content -Path $bodyFile -Encoding UTF8

gh pr create `
    --repo AnthonyZJiang/D-OS-Save-Editor `
    --base master `
    --head phillenton:feature/nested-containers `
    --title "D:OS EE: nested inventory, item save fixes, inventory UX" `
    --body-file $bodyFile

if ($LASTEXITCODE -eq 0) {
    Write-Host "Done." -ForegroundColor Green
}
