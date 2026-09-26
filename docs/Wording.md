# Words on screen that read wrong

Internal. Places where what Sprocket says is awkward, ambiguous or grammatically wrong,
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


### 8. The game's own action names, in the middle of a sentence

Every block offers what it declares, and what it declares is named for a button in the
terminal rather than for a sentence somebody is writing: *Toggle block On/Off*, *Increase
Velocity*, *Stockpile On*. A step reads **open** *Airlock Inner Door* and then, one block
along, **Stockpile On** *Hydrogen Tank*, which is the terminal's voice interrupting Sprocket's.
They are kept verbatim for a good reason - they are the words the player has already read, and
a guess at a better phrasing is a guess made once and wrong on ten thousand modded blocks. The
pass decides whether anything can be done generically: a first letter, a trailing *On* that
reads as an adverb, or nothing at all.


### 9. The screens talk round their own nouns

Sprocket has three words for the things it is made of - system, sequence, trigger - and the
screens say the first two and avoid the third, reaching for a phrase instead: *what should it
do*, *nothing starts this yet*, *runs Cycle*. The phrases are longer than the word, they have
to be re-invented at every site, and some of them are wrong for a trigger that stops a
sequence rather than starting one.
The audience is people who have not programmed, which is not the same as people who cannot
learn a noun; talking round a word they will meet anyway reads as explaining a thing to a
child. The pass decides where the noun is said plainly and where a phrase genuinely reads
better.

### 10. "Made from it"

The blueprint screen heads the logic systems made from the blueprint with *Made from it*
(`BlueprintPage`). The pronoun has nothing on the screen to point at, and the heading reads as
the end of a sentence nobody started.
