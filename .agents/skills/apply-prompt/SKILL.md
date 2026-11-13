---
name: apply-prompt
description: "Implement a selected Tomorrow at Eight development prompt, recording its day, reusing architecture, updating memory, and handing off to the owner. Does not draft the public post (see next-post). Not for standalone consultation or administration."
---

# Apply a prompt

This skill defines the complete daily development cycle for a prompt supplied by
the owner. It does not select a winner or publish anything.

1. Reread the rules and memory linked from AGENTS.md. Consult existing architecture
   and relevant code before adding a system. For an earlier day referenced by the
   request, read its agent journal and linked public JSON as needed.
2. Create or resume the UTC record pair following `.agents/days/README.md` and
   ARCH-004, copying `.agents/days/TEMPLATE.json` for the public JSON without
   adding or removing keys. Preserve the exact selected input in public JSON and starting agent
   context in the Markdown journal before implementation. Citing an old date is
   a context lookup, not an instruction to backdate this run.
3. Record the interpretation, expected result, and plan. Work within current permissions;
   make adopted creative changes only when clearly requested.
4. Implement and verify. Reuse existing recipes and entry points; document justified
   architectural changes in ARCHITECTURE.md and place ARCH IDs, dates, and journal
   references near their code.
5. Synchronize only the memory that changed: GAME.md for adopted creative choices,
   ARCHITECTURE.md for technical decisions and entry points, CONTEXT.md for actual
   current state, and PLAN.md for unfinished work and task status.
6. Complete the public JSON and the journal's technical handoff without duplicating
   public fields. Distinguish JSON written from Unity import, runtime loading, and
   UI behavior actually verified. Then reply with the owner handoff below. Do not
   draft the public post in this reply.

## Owner handoff (reply that ends the implementation run)

This reply is addressed only to the owner, in the conversation's language. It is
a working note, not public text. Keep it short and practical:

- what was implemented, in a few lines, and both record paths;
- what the owner must do in the Unity Editor, in order (menu commands, Play,
  tests, modules to install, build command), and what result to expect;
- what is missing, unverified, or blocked, including compile or build state;
- anything that needs the owner's validation, and open questions.

Technical caveats such as "not compiled yet" or "tests not run" belong here and
in the journal, never in the public post.

## After the handoff

Stay in conversation with the owner: fix what their Editor reports, record their
tests and build results as sourced facts, and answer their questions. The public
post for the next day is a separate request handled by the
[next-post](../next-post/SKILL.md) skill; do not draft it here.

A later request to update memory or summarize a date can use the same record pair
without starting implementation. Record new owner tests or build URLs as sourced
facts in the appropriate record, preserve earlier results, and update any drafted post
through next-post. Follow the journal guide for dated corrections to closed days.
