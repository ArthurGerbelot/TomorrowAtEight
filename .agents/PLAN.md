# Current plan

This file tracks work that is still to be done, not completed features.
For each task, record its expected outcome, source prompt/day, dependencies if any,
and status: active, pending, completed, abandoned, or superseded.
Update it when scope changes or work finishes; link detailed results to the journal.

The current prompt has priority. An earlier unfinished item becomes active only
when the new prompt resumes it or requires it. A suggestion is not an approved task.
If a new decision replaces an old plan, keep the source and mark the old item superseded.

## Work queue

### Verify the Day 0 foundation in Unity

- **Status:** active; partial verification from the owner's Editor log (see the
  Day 0 journal). Launcher/test navigation and a Windows build were observed;
  visual and Test Runner results remain unverified. The shared daily-action
  state now records foundation preparation completed at `2026-11-13T00:32:31Z`;
  its execution was not independently observed by this task.
- **Expected outcome:** the project compiles with `com.unity.entities` `1.5.0`;
  the launcher and three test scenes work in Play mode; EditMode and PlayMode
  tests pass; `TomorrowAtEight/Run Daily Scripts` completes action
  `2026-11-12/001/project-foundation`. Fix any compile or runtime errors found,
  record the resolved Entities version in ARCH-005, and record owner-reported
  results in the day's journal and JSON.
- **Source:** selected prompt of 2026-11-12 (Day 0); [journal](days/2026-11-12.md).
- **Dependencies:** the owner focusing the Editor and running the steps listed in
  CONTEXT.md; the agent can read `/home/node/editorlogs/Editor.log` afterwards.

### Browser build of the Day 0 foundation

- **Status:** active via owner follow-up on 2026-11-13; a Web player build
  was produced at `Builds/2026-11-12-Web`
  using the owner's explicit day label. The earlier `2026-11-13-Web` build was
  deleted by the owner and has not been recreated.
  Compilation/build succeeded with zero build errors/warnings. Artifact integrity
  checks pass; browser behavior and Test Runner results remain pending.
- **Expected outcome:** a Web player build is available for verification.
  Startup, scenes, restart, and
  repeated navigation are checked in a browser (ARCH-001.d); build results in
  the technical journal are updated with sourced facts. Public JSON retains its
  fixed content-only schema, without build/status fields.
- **Source:** selected prompt of 2026-11-12 (Day 0), resumed by the owner's
  request for a dated web build and reusable local script on 2026-11-13.
- **Dependencies:** the item above; Web Build Support installed for Unity
  `6000.3.27f1`. Open risks to check in the browser: Entities on the web player,
  managed code stripping (`link.xml`), and URP Lit shader variants for materials
  created at runtime. The correct Windows Unity and Web module are now confirmed.
  Player compilation, Web build production and payload integrity checks have
  passed; browser checks are still pending. Private release implementation and
  operating instructions are outside the public project.


### Follow-ups recorded on Day 0 (not approved tasks)

- **Status:** pending suggestions; activate only through a prompt or owner request.
- Review the licence of Unity's built-in UI font or choose a project font
  (`font.core.ui` is `pending` with temporary permission in the rights manifest).
- Replace the Play Game placeholder with a real scene through
  `RegisterLauncherScene` once a prompt creates the game.
- An in-game journal view for daily records (ARCH-004); only the launcher's
  latest-record line exists.
- ARCH-003 retention: after `2026-11-12/001/project-foundation` has run and its
  outputs and state file are committed, its entry in `DailyActions.cs` can be removed.
- Completed on 2026-11-12 and removed from this queue: implementing ARCH-001,
  ARCH-002, ARCH-003, ARCH-004, and ARCH-005 in source (see the journal).
