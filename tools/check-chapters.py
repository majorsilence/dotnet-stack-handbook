#!/usr/bin/env python3
"""Structural checks on the chapters.

None of this looks at whether the prose is any good.  It catches the things that
break silently when a 5,000 line document is edited: an unclosed code fence that
swallows the rest of a chapter, a heading that jumped two levels, a link to an
anchor that no longer exists, a chapter number used twice.

    ./tools/check-chapters.py

Exits non-zero if anything failed, so CI can gate on it.
"""

import glob
import os
import re
import sys

CHAPTER_LINK = re.compile(r"\]\((\d{2}-[a-z0-9-]+)\.html(#[a-zA-Z0-9_-]+)?\)")
LOCAL_ANCHOR = re.compile(r"\]\((#[a-zA-Z0-9_-]+)\)")
EXPLICIT_ID = re.compile(r"\{#([a-zA-Z0-9_-]+)\}\s*$")

# Languages rouge is expected to know.  A typo here means an unhighlighted block
# on the site and a pandoc warning in the PDF.
KNOWN_LANGUAGES = {
    "bash", "sh", "shell", "console", "cs", "csharp", "vb", "vbnet", "sql",
    "xml", "yaml", "yml", "json", "js", "javascript", "html", "css", "ini",
    "dockerfile", "docker", "groovy", "text", "plaintext", "diff", "toml",
    "powershell", "razor", "cshtml", "nginx",
}


def kramdown_id(text):
    """kramdown's generate_id, which is what the site will use for a heading."""
    ident = re.sub(r"^[^a-zA-Z]+", "", text)
    ident = ident.replace(" ", "-").lower()
    return re.sub(r"[^a-z0-9-]", "", ident) or "section"


def parse(path):
    text = open(path, encoding="utf-8").read()
    match = re.match(r"^---\n(.*?)\n---\n", text, re.S)
    if not match:
        return None, text, []
    meta = {}
    for line in match.group(1).splitlines():
        key, sep, value = line.partition(":")
        if sep:
            meta[key.strip()] = value.strip().strip('"')
    return meta, text[match.end():], text[match.end():].split("\n")


def main():
    root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
    os.chdir(root)

    paths = sorted(glob.glob("_chapters/*.md"))
    if not paths:
        print("no chapters found", file=sys.stderr)
        return 1

    failures = []
    stems = {os.path.basename(p)[:-3] for p in paths}
    numbers = {}
    anchors = {}
    fence_total = 0

    # First pass: collect every anchor each chapter offers.
    for path in paths:
        meta, body, lines = parse(path)
        stem = os.path.basename(path)[:-3]
        found, fenced = set(), False
        for line in lines:
            if line.lstrip().startswith("```"):
                fenced = not fenced
                continue
            if fenced:
                continue
            heading = re.match(r"^#{1,6}\s+(.*)$", line)
            if heading:
                title = heading.group(1).strip()
                explicit = EXPLICIT_ID.search(title)
                if explicit:
                    found.add(explicit.group(1))
                    title = EXPLICIT_ID.sub("", title).strip()
                found.add(kramdown_id(title))
        anchors[stem] = found

    # Second pass: the checks.
    for path in paths:
        meta, body, lines = parse(path)
        stem = os.path.basename(path)[:-3]

        def fail(message):
            failures.append(f"{path}: {message}")

        if meta is None:
            fail("no YAML front matter")
            continue
        if "title" not in meta:
            fail("front matter has no title")
        if "appendix" not in meta:
            for key in ("number", "part"):
                if key not in meta:
                    fail(f"front matter has no {key}")
            if "number" in meta:
                number = meta["number"]
                if number in numbers:
                    fail(f"chapter number {number} also used by {numbers[number]}")
                numbers[number] = path

        # The chapter layout renders a link to examples/<project>, so a stale
        # value here would ship a 404 rather than fail anywhere visible.
        if "examples" in meta:
            project = str(meta["examples"]).strip().strip('"').strip("'")
            if not os.path.isdir(os.path.join("examples", project)):
                fail(f"front matter points at examples/{project}, which does not exist")

        fenced, fences, previous, opened_at = False, 0, 2, None
        for index, line in enumerate(lines, 1):
            stripped = line.lstrip()
            if stripped.startswith("```"):
                fences += 1
                if not fenced:
                    opened_at = index
                    language = stripped[3:].strip().lower()
                    if language and language not in KNOWN_LANGUAGES:
                        fail(f"line {index}: unknown code fence language "
                             f"'{language}'")
                fenced = not fenced
                continue
            if fenced:
                continue

            heading = re.match(r"^(#{1,6})\s+(.*)$", line)
            if heading:
                level = len(heading.group(1))
                if level == 1:
                    fail(f"line {index}: H1 in the body; the chapter title is "
                         "the H1")
                if level > previous + 1:
                    fail(f"line {index}: heading jumps from h{previous} to "
                         f"h{level} ('{heading.group(2).strip()}')")
                previous = level

        fence_total += fences
        if fenced:
            fail(f"unclosed code fence opened at line {opened_at}")

        for target, anchor in CHAPTER_LINK.findall(body):
            if target not in stems:
                fail(f"link to {target}.html, which is not a chapter")
            elif anchor and anchor[1:] not in anchors[target]:
                fail(f"link to {target}.html{anchor}, which is not a heading "
                     f"in that chapter")

        for anchor in LOCAL_ANCHOR.findall(body):
            if anchor[1:] not in anchors[stem]:
                fail(f"link to {anchor}, which is not a heading in this chapter")

    print(f"{len(paths)} chapters, {fence_total} code fences, "
          f"{sum(len(a) for a in anchors.values())} anchors")

    if failures:
        print(f"\n{len(failures)} problem(s):", file=sys.stderr)
        for failure in failures:
            print(f"  {failure}", file=sys.stderr)
        return 1

    print("ok")
    return 0


if __name__ == "__main__":
    sys.exit(main())
