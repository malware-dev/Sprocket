# Text

Where every word Sprocket shows comes from: the texts and their translations, the sentences built from steps and
triggers, markdown-lite, and the codex. The model holds ids and never words, as [The model](Model.md) sets out, so
every word is found by the screen that shows it.

## The texts

The mod's texts are in `Content/Data/Localization`: `MyTexts.resx` in English, and a translation beside it for each
language, named for its culture, such as `MyTexts.no.resx` for Norwegian.

- **A key** is `Sprocket.` and then the area and the name, as in `Sprocket.Chrome.Back`. The game keeps every mod's
  texts in one table, so the prefix keeps Sprocket's apart; the project sets it as `<LocalizationPrefix>`.
- **Every English text carries a comment** for its translator: which screen it is on, which element it is, and what
  each placeholder is filled with. A comment stands alone, since a translator, or a translation tool, sees one text at
  a time.
- **A placeholder** is a name in braces, such as `{name}` or `{count}`, filled by `Lumel/Text/Template.cs`:
  `Template.Fill` into plain words, or `Template.Read` into a document. `{{` and `}}` are a brace itself.
- **A text may hold markdown-lite**, such as a strong word.

## The generator

`src/Sprocket.Localization.Generator` reads `MyTexts.resx` as the mod builds and writes the `Texts` class: one property
for each key, named for the key without its prefix and its dots, so `Sprocket.Chrome.Back` is `Texts.ChromeBack`. Each
property's documentation is its English text and its translator's comment.

It reports what would go wrong in the game:

| Id | Reported |
|---|---|
| LOC001 to LOC004 | Errors: texts that cannot be read, a key without the prefix, two keys making one property, a key making no property name |
| LOC005, LOC006 | A translation with a text English lacks, or lacking one English has |
| LOC007 to LOC015 | The codex: a page without its title, a page without its tooltip or the reverse, a link to no page, a box never closed, a link in a tooltip, a missing index, an id the index lists with no page or lists twice, a page the index leaves out |
| LOC016 to LOC019 | A comment that is missing, leans on the texts beside it, or leaves a placeholder unexplained; a last sentence with no period |
| LOC020 | A text nothing uses, counting the keys the game's own definitions name |

Every language with a `MyTexts.<culture>.resx` is expected in the codex too, and the codex check compares each
language's pages with English.

## At run time

Sprocket reads its texts itself as a world loads (`SprocketSession.Speak`), so a changed text shows after reloading
the world, without restarting the game. `Localization/GameTexts.cs` loads them through the game in the player's
language, English first and the language over it, so a text a translation lacks is said in English.
`Localization/Phrasebook.cs` keeps the one string for each text. A change of language in the game's options rereads
them, and every screen is worded again.

A screen reads a text as a property of its `Texts`, as in `texts.ChromeClose`. A dedicated server shows nothing, and
loads no texts.

## Sentences

A step or a trigger reads as a sentence, built in `Features/Sentences`: `SentenceText` composes it out of slots, each
a part a player can press, and decides its word order and its capitals, while `SequenceText` gives the shorter labels:
list rows, headings, captions.

The words in those sentences come from three places: the texts, the names a player gave blocks and sequences, and
each kind of block, which names its own actions, values and changes where it is declared (see [Blocks](Blocks.md)).
Some of the words joining a sentence together are written in the code, in English, and the
block words are English in every language. Saying them in the player's language, with each language's own word order,
is the subject of issue #63.

## Markdown-lite

The rich text Sprocket shows is markdown-lite, read by `Lumel/Text/Markdown.cs` and `Lumel/Text/Inlines.cs`: headings,
rules, one level of lists and of quotes, boxes, tables, strong and emphasized words, code, pictures, links and escapes,
with a single newline as a line break. The codex, the descriptions a player writes, tooltips and confirmations are all
markdown-lite, and so is the text of an LCD running the Rich Text app from Rich Text Panels. The codex's own Rich text
page shows every piece of it beside what it gives.

## The codex

The codex is in `Content/Data/Codex`:

- `index` lists the terms, one id a line, in the order the contents show them; `#` starts a comment, and `>` starts a
  section headed by that term.
- Each language has a folder, `en` and `no`, holding each term's page, `<id>.md`, and its tooltip, `<id>.tldr.md`.

`Localization/GameCodex.cs` reads a page from the mod's folder each time it is shown, in the player's language, or in
English where a translation lacks it. `Features/Codex/Terms.cs` reads the index, and each screen's question mark opens
the term the screen names as its `Help`.

How a page is written, and how the wiki is written from the codex, is in [Codex](Codex.md).

## Adding a text

1. Add the key to `MyTexts.resx`, with its English and its comment.
2. Add the same key to every translation.
3. Build: the generator gives `Texts` its property.
4. Read it on the screen, filling its placeholders with `Template` where it has any.

A missing translation, a missing or leaning comment, and a text nothing uses each show as a build warning.
