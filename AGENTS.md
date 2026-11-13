# Tomorrow at Eight - agent entry point

This file routes the agent to the project's working instructions.
The repository has one Git working tree rooted here, including the Unity project
and agent framework; follow ARCH-006 for repository boundaries and future site exports.
Read [RULES](.agents/RULES.md) and the public [README](README.md) from disk
at each new task. For development, read [CONTEXT](.agents/CONTEXT.md),
[GAME](.agents/GAME.md), [ARCHITECTURE](.agents/ARCHITECTURE.md), and [PLAN](.agents/PLAN.md).
Follow the architecture index into the detailed parts relevant to the task before
implementing or extending their systems.

Local tools may be available in ignored `automation/`. If `automation/AGENTS.md`
exists, read it before using or modifying those tools. Its absence must not
block work on the public project.

Use [apply-prompt](.agents/skills/apply-prompt/SKILL.md) for a selected development
prompt. It covers implementation, memory updates, the daily records, and a private
handoff to the owner. Use [next-post](.agents/skills/next-post/SKILL.md) only when
the owner asks for the public post that opens the next day.

Read only the historical agent journals and paired public daily records relevant
to the request, following [days/README](.agents/days/README.md). A consultation
does not start implementation or create a daily record. Administrative edits
do not create a development day. Build uploads and public posts are handled
manually by the owner.

## Where information lives

- [README](README.md): public project presentation and participation rules.
- [RULES](.agents/RULES.md): agent permissions and working agreements.
- [GAME](.agents/GAME.md): adopted creative direction and how it may change.
- [ARCHITECTURE](.agents/ARCHITECTURE.md): technical decision index, summaries, and code references.
- [architecture/](.agents/architecture/): detailed contracts such as ARCH-001.a
  through ARCH-001.d; load the relevant parts through the index.
- [CONTEXT](.agents/CONTEXT.md): current state to load when resuming.
- [PLAN](.agents/PLAN.md): active and unfinished work.
- [apply-prompt](.agents/skills/apply-prompt/SKILL.md): selected development prompt workflow.
- [next-post](.agents/skills/next-post/SKILL.md): draft of the next day's public post.
- [days/](.agents/days/README.md): technical agent journals and how to reconstruct
  an earlier context using the paired public records.
- `TomorrowAtEight/`: Unity project root; paths starting with `Assets/`, `Packages/`,
  or `ProjectSettings/` in agent memory are relative to this directory.
- [.gitattributes](.gitattributes): repository-wide Unity text and Git LFS rules.
- [.gitignore](.gitignore): shared exclusions; Unity also has its own
  [.gitignore](TomorrowAtEight/.gitignore) inside its project directory.
- `web/`: planned public website in this same repository (ARCH-006).
- `TomorrowAtEight/Assets/TomorrowAtEight/Data/Days/`: public daily JSON for the game's prompt
  and journal views (ARCH-004).

All project content is English; private conversation may remain French.
