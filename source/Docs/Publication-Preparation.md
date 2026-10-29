# Publication preparation checkpoint

`Tools/Publish-Iteration.ps1` can build and validate an isolated release project while the developer continues modifying the main project. The publisher must supply the fixed snapshot project and an explicit installation destination. The immutable snapshot and mutable Unity working copy are managed by `Automation/publication-queue.mjs`; this script never reads the advancing main project.

```powershell
# From the release snapshot Project directory: no foreground application starts.
.\Tools\Publish-Iteration.ps1 -Version s1-example -PrepareOnly -DestinationPath 'F:\工作项目\率土之滨\games'

# Reuse preparation, then run every existing runtime/UI verification gate.
.\Tools\Publish-Iteration.ps1 -Version s1-example -ValidateOnly -DestinationPath 'F:\工作项目\率土之滨\games'

# Called by publisher only after its read-only screenshot review is approved.
.\Tools\Publish-Iteration.ps1 -Version s1-example -InstallVerified -DestinationPath 'F:\工作项目\率土之滨\games'
```

Without `-DestinationPath`, the existing default remains the project's sibling `games` directory. Use exactly one of `-PrepareOnly`, `-ValidateOnly` or `-InstallVerified`. Calling with no mode preserves the existing validate-and-install behavior; the scheduled publisher uses the explicit modes so screenshot review remains required.

`Validation/Releases/<version>/preparation.json` is written only after Unity preparation, Unity serialization verification, pure core checks, asset validation, the Windows build, and Unity architecture verification all pass. `ProjectTools.VerifySerialization` generates the eight `Validation/serialization-*.json` files from the current snapshot before the CLR compatibility checks run; historical generated JSON files are not copied into a release. The required preparation check list includes `serialization`, so older incomplete checkpoints cannot be reused. It binds the version, project path, complete source file hashes, source fingerprint, helper/test fingerprint, required check list, and complete Windows build file hashes. The helper fingerprint includes all `Tools` files, `Validation` root `.cs`/`.mjs`/`.ps1` inputs, the current card-display fixture `Validation/CardDisplayChecks/ExportCardDisplay.cs`, and optional `.local/unity.json`. This matches the snapshot input allowlist; new permanent fixture locations must be added to both allowlists.

Busy desktop input or a running player does not prevent these headless steps. `-PrepareOnly` returns `release.json.status = prepared`, which means foreground validation and installation are still outstanding. `-ValidateOnly` reuses an intact preparation checkpoint, then checks for an existing player and at least 30 idle seconds before opening any review client. Subsequent review clients also wait for the desktop to settle; each existing focus/click/visual check remains in force. A busy desktop returns `waiting_for_desktop`; an existing player returns `waiting_for_player`. Neither status installs a client.

A changed input, incomplete checkpoint or altered/added/deleted build file invalidates preparation and forces all headless steps to run again. Source or helper changes during preparation are rejected. Source, helper and build bytes are checked again after foreground tests. `validation.json` continues to include all eleven runtime/UI checks, exact source files, save-preservation evidence and build hashes; it now also binds the helper fingerprint. `-InstallVerified` rejects mismatched source, helper or build bytes and checks for players again immediately before installation.

The save comparison starts after the foreground gate. A real player may legitimately save while the independent headless build runs; that activity is not treated as a test mutation. Foreground review still uses the existing isolated/read-only sessions and refuses publication if real save hashes change.

`node Validation/PublicationPreparationChecks.mjs` runs the production orchestration against disposable fixture tools and source/build files. It exercises checkpoint reuse/invalidation, malformed receipts, headless progress while a player is present, foreground/player gates, save changes, and explicit installation binding. The fixture replaces Unity, desktop, player and installer entry points; it never launches a game or touches real saves.
