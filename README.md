# Tomorrow at Eight

Tomorrow at Eight is a collaborative game project. Each day, the community chooses
one prompt, and an AI coding agent uses it to evolve the same game. The project
owner runs the selection process and publishes the builds and daily updates.

The first creative prompts will establish the game's identity: its genre, world,
mechanics, and visual direction are still open. Later prompts will build on those
choices. The target is a game made with Unity and playable in a web browser.

The title refers to **20:00 UTC**. Each official voting thread announces its exact
deadline with a calendar date and a 24-hour UTC time.

## How to participate

1. Post a detailed proposal in English as a top-level comment in the official
   Reddit thread. Replies are for discussion; only proposals in the official
   thread count.
2. Vote before the announced deadline. The comment with the highest observed
   Reddit score at closure wins. Ties go to the oldest comment.
3. The owner gives the exact selected text to the agent, which implements it
   against the current game and documents the result. If there is no proposal,
   no winner is invented.

Prompts can introduce something new or develop an existing idea. Use documented
content IDs to target a particular entity or one of its visual or audio parts.
You can refer to an earlier day, but its idea will be continued in the current
game. An established creative direction changes only when a selected prompt
explicitly requests it or the owner makes a documented decision. Technical
demonstrations remain provisional.

## What each day records

Each development day preserves the original selected prompt, its source when
available, the decisions made, the work completed, checks performed, remaining
work, and a public summary. Partial results and failures are reported too.
Implementation, verification, and publication are tracked separately; the owner
publishes builds and public posts.

The owner may also request administration, corrections, completion of unfinished
work, or help unblocking the agent outside the community vote. Development
interventions and their outcomes are recorded as owner inputs in the relevant
daily record, separate from the community-selected prompt. Administrative edits
do not create a development day.

## Participation rules

Proposals must be good-faith contributions to the game. The owner may reject
attempts to redirect the agent to unrelated work, misuse project resources,
introduce or exploit security vulnerabilities, bypass the rules, or abuse the
process.

The owner maintains these participation rules and the project's agent instructions
and may revise them at any time. Community prompts cannot rewrite them or grant
themselves administrative permissions. External assets must retain their provenance
and terms.

The repository is intended to remain public for transparency. The owner alone
controls publication of changes to the official repository, builds, and posts.
Community participation does not grant write access.

Assets, code, and packages may be created, purchased, or proposed by the owner or
the community. They can be included only when the project's rights to use and
distribute them are documented. Contributors must have the right to supply their
material; copyright, licenses, required credits, and distribution restrictions
must be respected.

## Current status

Day 0 (2026-11-12) added the shared foundation: a content registry with stable
keys, an entity factory, an ECS session bootstrap, asset rights metadata, editor
automation, and a development launcher with three provisional test scenes. This
work has since run in the Unity Editor and produced a Windows standalone build.
A Web build was successfully generated on 2026-11-13; browser behavior remains
unverified. The game's genre, world, and visual direction are still open.
