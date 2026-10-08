# Errors from reading a script

Errors from ProgramText, the parser and Trouble are read by somebody editing the script by hand. Mal:
"We need to remember the context we're in. *Here* our target group isn't John Normal. It's Dave
Developer."

They are written like a compiler's: the line, then what was found or what was expected, and nothing taught.

```
Line 5: unexpected 'garbage'.
Line 17: expected 'to' after the parts.
Line 23: no block called 'door9'.
```

No "Read as:" prefix, no sentence explaining the language. Mal on the first attempt, "'garbage' does not
belong at the end of this line": "You're really not good at making error messages."

**Keywords are quoted in examples.** Each part of a qualified name is its own identifier and is judged
alone: `ext.'on'`, never `ext.on`. Before an example script goes anywhere, check every word beside a dot
against the keywords (on, off, run, stop, every, when, all, any, then, to, with, set, act, block,
sequence, system).
