# Words on screen that read wrong

Internal. Places where what Reactor says is awkward, ambiguous or grammatically wrong,
gathered as they are met.

**Gathered rather than fixed one at a time.** A sentence is assembled out of fragments from
several folders, and most of these read wrong because of how the pieces meet rather than
because any one string is badly chosen. Editing a fragment to mend one sentence is how the
next one breaks, so the whole set is looked at together, on the screens themselves, with the
sentence in front of you.

Each entry says what is on screen, where it shows, and what is wrong with it. Nothing here
says what it should become; that is the pass's job, and guessing at it now would bake in an
answer nobody has looked at.

---

### 1. "Running" is the word for a sequence that may run

`SequencePage` holds a sequence open for editing only while it cannot be started, and says so:
*"Running. Stop it to make changes - whatever it has already done stays done."* It says this
whenever the sequence is switched on, including when it is sitting idle, and the line above it
reads *"waiting to happen"* at the same time. Two lines of the same screen disagree about
whether anything is happening.

### 2. Several subjects take a singular verb

An `all` trigger reads *"When Airlock Inner Door and Airlock Outer Door finishes closing"*. The
verb comes from the block's own vocabulary, where it is written for one block - *"finishes
closing"* - and the sentence joins two subjects in front of it. Every `all` trigger over more
than one block has this, and so would any other list of subjects.

### 3. An unfilled slot and no trigger at all read the same

`SentenceText.Empty` is `…`, which is what an unfilled slot shows. A sequence nothing starts
has no trigger to fill, and reads *"… » Seal up"* - the same ellipsis, which says "somebody is
part way through this" about something that is not part way through anything. Its own screen
shows `…` as the heading's first part for the same reason.

### 4. A sequence nothing starts is "waiting to happen"

The status beside it, from `SequenceText.Status`, while the screen also says *"Nothing starts
this yet."* one line below. Waiting to happen is a promise, and nothing is going to happen.

### 5. "something that's gone" inside a sentence

`SentenceText.Missing` reads *"When something that's gone finishes closing"* where a block has
been ground down. It is honest and it is the right fact, but it is a noun phrase standing where
a block's name goes, and the sentence around it carries on as though it were one.

### 6. "skipped" is doing two jobs

The line on a sentence reads *"skipped 3x while busy"* when its trigger fired while it was
already running, and *"skipped 1x while off"* when something called it while it was switched
off, or both joined with a comma. The second is a refusal rather than a skip, and `x` as the
multiplier is terse in a way nothing else on the screen is.

### 7. "When something on this ship says so"

What a required trigger naming no block reads as. Longer than every other trigger lead, and it
is the one a hand-written program or an imported system is most likely to produce.
