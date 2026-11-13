# Daily journals

Each UTC development day has two linked records, following ARCH-004:

- `Assets/TomorrowAtEight/Data/Days/YYYY-MM-DD.json`: public data the game can load.
- `.agents/days/YYYY-MM-DD.md`: working context for future agent tasks.

This guide is maintained by the administrator; daily records are editable by the
development agent. Start both records before implementation and complete them even
when the request fails or remains partial. Administrative edits do not create a game day.

## Public daily record

- The record is content, not metadata: what was asked (the exact selected
  prompt and its author when known), what was done, optional details worth
  sharing, and optional suggestions for next prompts. It covers only the day's
  prompt and what it changed in the project. No build, status, or ID fields.
- Every file has the same structure. Copy [TEMPLATE.json](TEMPLATE.json), keep
  every key and their order, and leave unused values as `""` or `[]`. Allowed
  values are defined in [day.schema.json](day.schema.json). Do not add custom keys;
  a structural change updates the template, the schema, and the data class together.
- Use UTF-8 JSON with a root object. Verify
  that decoded prompt text matches the original, including line breaks. The file
  is the source of truth for these fields; a duplicate SO or Markdown summary is
  not required. Source authoring does not require Unity to be running.

## Agent journal

- **Public record reference:** link the day's JSON and note its relevant source
  revision when available. Read the original prompt and public summary there.
- **Additional technical inputs:** preserve owner instructions and corrections
  needed to understand the work, with their origin; public inputs belong in JSON.
- **Starting context and rules:** copy the relevant current context and creative
  choices before changing them. Reference the code and rules revisions when they
  represent the actual starting files; otherwise preserve the rule text used.
- **Plan:** interpretation, expected result, dependencies, and any explicit identity changes.
- **Decisions and work:** actual changes, content IDs, ARCH IDs, reused systems,
  rationale, migrations, and differences from the requested result.
- **Verification:** checks run and observed results, owner-reported tests, and what remains unverified.
- **Handoff:** copy the useful final context, decisions, and unfinished work so this
  day remains understandable after current memory changes.
- **Build:** actual state, version/reference, and URL if produced or published.

Keep the original selected prompt in JSON and starting snapshots in the journal
unchanged. Resume an open day using the same pair; append distinct inputs with
their origin in the appropriate record. After closure, append dated public
corrections to JSON and technical corrections to Markdown instead of rewriting
the earlier result. Preserve any existing historical records in their original form.

The public post for the next day is drafted separately, when the owner asks,
with the next-post skill. It is based on the JSON and the journal's actual
verification and build facts, and it is a draft for the owner. Do not copy the
whole agent journal into game data or publish anything automatically. A build
contains a snapshot of the public records actually included; later source edits
appear only in a new build. End every public post with the exact closing line
required by RULES.md, after all post content, links, and notes.

## Loading an earlier day

Read the relevant day's journal and its linked JSON for the original prompt,
summary, and public facts. Follow ARCH IDs into ARCHITECTURE.md and the code;
compare the earlier intention and result with the current game before continuing.
Earlier rules remain historical evidence. This restores useful working context,
not the agent's exact old conversation. Reverting code is a separate request.
Report missing evidence instead of inventing it.
