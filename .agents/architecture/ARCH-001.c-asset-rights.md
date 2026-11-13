# ARCH-001.c - Asset rights and replacement tracking

Part of [ARCH-001 - Unified content recipes](../ARCHITECTURE.md#arch-001---unified-content-recipes).
The parent record owns the shared status, dates, origin, and path conventions.

- **Decision/origin:** direct owner administrative request on 2026-11-12. Rights
  and replacement tracking belong to the content/asset contract in ARCH-001.
  [RULES.md](../RULES.md) owns the policy; metadata records the facts for each asset implementation.
  First implementation written on 2026-11-12 (Day 0): the manifest below and
  `Assets/TomorrowAtEight/Editor/Content/AssetRightsValidator.cs`, used by
  `TomorrowAtEight/Run/Validate Content and Asset Rights`, the daily action, and
  `Build Web Player` (scope `public_build`). Not yet executed in Unity.
- **Implemented scope (2026-11-12):** records are required for the asset kinds
  `model`, `vfx`, `mesh`, `material`, `texture`, `shader`, `sfx`, and `font`.
  `entity`, `scene`, and `data` keys are code definitions without a record; their
  dependencies are still followed. Record fields: `contentKey`,
  `providerRevision`, `origin`, `source`, `author`, `ipStatus`, `ipStatusReason`,
  `rightsholder`, `license`, `evidence`, `allowedScopes`, `reviewDate`,
  `replacementRequired`, `replacementReason`, and `replacementTarget`. The
  selected content is the dependency closure of `ContentCatalog.StartupScene`.
- **Source of truth:** plan one versioned UTF-8 JSON manifest at
  `Assets/TomorrowAtEight/Data/Content/AssetMetadata.json`, with a schema version
  and an entries array. Each active asset entry is identified by `contentKey`
  and `providerRevision`. Use the same stable keys as the registry, including
  individual meshes, textures, materials, audio, and other referenced parts.
  Keys identify the content; provider revisions identify the implementation
  whose rights were reviewed.
- **Record:** store provenance/source and author or generator, `ipStatus`, a
  reason for that status, rightsholder and license/assignment references where
  applicable, evidence references, allowed use scopes, and review date. Store
  `replacementRequired`, its reason, and a target day or milestone when applicable.
  Required documentation may be referenced without committing private contracts,
  receipts, or personal information to the public repository.

| Stored PI status | Display label | Meaning |
| --- | --- | --- |
| `not_required` | N/A | No third-party rights need to be acquired and the intended use is permitted; record why. |
| `pending` | To provide | Rights, proof, or review needed for permanent adoption are still outstanding. |
| `cleared` | Done | Required rights and evidence have been verified for the documented use scopes. |
| `blocked` | Blocked | Use is prohibited; exclude the asset. |

- **Provenance and status:** AI generation, procedural generation, a community
  contribution, and a purchase describe an asset's origin, not its PI status.
  An original procedural placeholder can receive `N/A` with its documented basis;
  an AI-generated asset is assessed against its inputs and applicable terms.
  Neither `N/A` nor `Done` automatically asserts exclusive copyright ownership.
  Identify third-party software dependencies as such under [RULES.md](../RULES.md).
- **Temporary content:** replacement is a separate attribute. An asset can be
  cleared yet still be a visual placeholder to replace. An asset with pending
  final paperwork may be used temporarily only where an existing permission
  already allows that use. Track allowed scopes independently, initially
  `private_development`, `public_source`, `public_build`, and `public_media`.
  A pending status alone allows none of these. A prohibited asset cannot become
  usable by marking it temporary or promising a later replacement.
- **Validation and reports:** Editor/build tooling joins metadata to registered
  content and checks the dependency closure of the content actually selected for
  the target. Verify every referenced part rather than inheriting an entity's
  `Done` status. Report missing records, outdated provider revisions, required
  replacements, and missing rights/evidence. Unknown or stale records default to
  pending with no allowed scopes. Block preparation/publication for unsupported
  scopes; correctly documented temporary permissions may satisfy the current
  target without pretending permanent ownership has been resolved.
- **Public repository:** validate raw source publication separately from build
  distribution. An asset authorized only for private development or an embedded
  build must not be committed as raw source. Use content with public-source
  permission in the versioned baseline, and preserve required notices. Generated
  build/credits/site reports may read this manifest; do not maintain a second
  editable status list in the journal, a spreadsheet, or a ScriptableObject.
- **Replacement and lifecycle:** replacing an implementation preserves its public
  key but changes its provider revision and requires a new rights assessment.
  Do not copy the previous implementation's clearance automatically. Git history
  and the relevant development record preserve prior provenance and outcomes.
  Metadata is maintained by authoring/Editor/build tools; it is not per-instance
  ECS state, and private evidence is not bundled into player builds.
- **Initial verification:** exercise missing/stale records, separate part
  dependencies, a cleared placeholder awaiting replacement, temporary permission
  valid for one target but not another, and replacement retaining its content key.
