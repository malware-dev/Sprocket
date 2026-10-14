---
name: issues
description: Write and file a GitHub issue for this repository in the form it uses - what is wanted, why, what is open, and when it is done. Use whenever an issue is to be raised, edited or closed, or when deciding whether a piece of work needs one at all.
---

Issues are the planning surface. `docs/` is a guide for people to Sprocket as it is, and holds no
planning.

## Standing rules

Rules that govern everything Sprocket is, rather than any one part of it. Every ticket is judged
against all of them, and a ticket that cannot meet one says so.

**Everything Sprocket is responsible for survives a save and a reload.** Anything a running system
holds and then forgets when a world is reloaded is indistinguishable from a bug in the thing that held
it. This covers the program, what a running system has reached, and anything recorded on the way.

**The first part of a split restricts what can be built, never what can be represented.** Where a job
is done in pieces, the model, the stored form and the evaluation carry the whole shape from the first
piece. The editor offers a subset. A trigger held inside a sequence cannot be shared by two of them,
however the editor is written. Restricting the editor costs nothing; restricting the model costs every
program already saved.

## Whether it needs one at all

**Designed, unbuilt work gets a ticket whatever it is called.** Not feature versus debt - the test is
whether losing it would cost more than an afternoon. A whole job with a settled design, several files
and both hosts gets one. A bug found in play, a rename, a tidy-up does not.

**Work about to be done does not get one.** A ticket exists so something is not lost, and work in
hand is not lost. Writing one for the thing being built this hour is ceremony.

## The form

A title that is a sentence about the outcome: *Offer "all of them" only where it can mean something*,
*A dialog needs a default and a cancel*. Not a noun phrase, not a component name.

Then, in this order:

1. **What it is**, with no heading. A plain statement of the situation, present tense.
2. **Why.** One paragraph. What it costs now, as a fact about the product.
3. **Bolded claim paragraphs** for the substance - each one led by the claim it is making, so the
   body can be skimmed by its first sentences.
4. **Open**, a list, for what is genuinely undecided.
5. **Done when**, a checklist of observable outcomes.

**A `design` label means no Done-when.** Writing acceptance criteria for an open design picks the
answer by accident. Say so by labelling it, never by explaining it in the body.

## How it is written

Every issue is read by somebody who was not in the conversation that produced it.

- No marks of authorship or review, no "as discussed", no "you said".
- No history of the issue itself - it says what is wanted, not how the text got there.
- No second person and no "we". The product does things; a person does things.
- Present tense, and no hedging about scope: what is in, what is not, what it waits on.
- Cross-references go to other tickets, by title. Never a bare number - Mal cannot tell tickets apart
  by number the way a tool can.
- No more words than the requirement needs. One sentence per fact.

## Before filing

**Show Mal the text and wait.** Never create or edit an issue on the repository before he has read
what it says. Approval of the text is approval to file that text.

`gh issue create --title ... --label ... --body` with a heredoc. Labels are the stock set plus three:

- **`design`** - the shape is open, so the ticket carries no Done-when.
- **`tentative`** - in the milestone it sits in on sufferance, and moves out without argument.
- **`dream`** - wanted, may never happen, and belongs in no milestone.

Three milestones, and a ticket that is none of them has none:

- **First test** - enough that a few remote testers can build something and report.
- **Release** - enough that a stranger can do the same with nobody to ask.
- **Stretch** - likely to be built, and not before the first public release.

## Closing

Finishing a ticket and committing it closes it - that is part of finishing, not a thing to ask about.

**Close it with no comment.** The ticket said what was wanted and it was built; a comment restating
that is the commit message in a second place. Comment only where the outcome differs from what the
ticket asked for, and then say only what differs.
