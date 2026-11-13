---
name: next-post
description: "Draft the Reddit post that opens the next Tomorrow at Eight day, from the applied day's public JSON and journal, when the owner asks for it. Not for implementing a prompt."
---

# Draft the next day's post

Use this when the owner asks for the post that follows an applied prompt. It
changes no game code and publishes nothing; the owner posts it.

1. Read RULES.md, then the applied day's record pair following
   `.agents/days/README.md`: the public JSON (prompt, done, details,
   suggestions) and the journal's verification, handoff, and build
   sections. Read PLAN.md and GAME.md for what is still open.
2. Base the post on those records and on owner-reported results. If the owner
   supplies newer facts (tests, build URL, winning comment link, author), save
   them as sourced facts in the record pair first, then use them.
   Ask for the comment link and author if they are missing; never invent them.
3. Write the post in English for the Reddit audience. Days are numbered from
   the foundation build, which is Day 0 (2026-11-12). The post announces the
   build of day X and opens the vote for day X+1, so its number is the applied
   day plus one: the post that follows the Day 0 build is Day 1.
   1. Title line: `Day X+1! <short title>`.
   2. The selected prompt summarized in a few words, linked to the winning
      comment in the previous post. Day 0 had no community thread: describe
      it as the project's starting point, without a link.
   3. What changed, in plain language: what players can now see or try, plus the
      build link if supplied, labelled `Play it here:`. Next to it, add
      `View code here:` linking to
      `https://github.com/ArthurGerbelot/TomorrowAtEight/tree/YYYY-MM-DD`, where
      the date is the applied day's record date and source tag, not the next
      voting day. Confirm the tag exists before presenting it as published.
      With no supplied build URL, omit the play link rather than invent one;
      retain the verified source link. No file names, architecture IDs, or internal
      process notes (compilation, tests, tooling). If part of the prompt is not
      in the build, say so in player terms.
   4. A call for the next prompt, optionally with two or three suggestions drawn
      from open items in PLAN.md, open choices in GAME.md, and the last prompt.
      Present them as ideas, not as decisions.
   5. The voting deadline, written by the agent without asking: the day after
      the current UTC date at 20:00 UTC, as `Voting closes YYYY-MM-DD at 20:00
      UTC (8:00 pm).` Read the current UTC date from the system, do not guess
      it. If the owner says the post goes out on another day, use the day after
      that one. An explicit owner-supplied deadline takes precedence.
      Append `[Your local time](https://timee.io/e/YYYYMMDDT2000?tl=ENCODED_TITLE)`:
      use the deadline's UTC date without hyphens and URL-encode the actual
      post title as the `tl` query value. The displayed date and linked instant
      must agree. Then the closing line required by RULES.md.
4. Return the draft to the owner in the conversation, with a note on anything
   the records could not confirm.

For the owner's Day 1 example after the `2026-11-12` build, the source link and
explicit deadline are:

```markdown
View code here: https://github.com/ArthurGerbelot/TomorrowAtEight/tree/2026-11-12

Voting closes 2026-11-13 at 20:00 UTC (8:00 pm). [Your local time](https://timee.io/e/20261113T2000?tl=Day+1%21+The+foundation+is+in.+So+what+game+are+we+making%3F)

See you Tomorrow at Eight
```

Place the supplied `Play it here:` link directly before `View code here:`.
