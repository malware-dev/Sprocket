"""Reminds that docs/Language.md is the script language's spec and moves with the code.

Run as a PostToolUse hook on Write|Edit. Reads the hook payload on stdin and, when the file that
was written is one the language's shape is decided in, prints the reminder as additional context.
Anything unexpected is silent: a hook that fails must never interrupt the work.
"""

import json
import sys

# The files a language rule can change in: the lexer's tokens, the grammar, the keyword table, what
# the writer spells, and what the reader accepts.
WATCHED = (
    "sprocket.host.spaceengineers/core/storage/",
    "sprocket.host.spaceengineers/core/sequences/naming.cs",
)

REMINDER = (
    "docs/Language.md is the script language's spec and holds its current truth. "
    "If this change adds, removes or alters a rule, keyword, construct or spelling, update "
    "docs/Language.md in this same change: facts only, no history or rationale, and delete the "
    "wording that is no longer true."
)


def watched(path):
    flat = path.replace("\\", "/").lower()
    return any(part in flat for part in WATCHED)


def main():
    try:
        payload = json.load(sys.stdin)
    except Exception:
        return

    tool = payload.get("tool_input") or {}
    response = payload.get("tool_response") or {}
    path = response.get("filePath") or tool.get("file_path") or ""

    if not isinstance(path, str) or not watched(path):
        return

    json.dump(
        {
            "hookSpecificOutput": {
                "hookEventName": "PostToolUse",
                "additionalContext": REMINDER,
            }
        },
        sys.stdout,
    )


if __name__ == "__main__":
    main()
