#!/usr/bin/env python3
"""Split the original blog post into the chapters under _chapters/.

The handbook began as a single 5,200 line post on majorsilence.com:

    Dev/_posts/2023-04-07-dotnet-development.md

This script is the record of how that post became a book, and is only useful if
you are re-running the migration.  The Markdown in _chapters/ is the source of
truth now, and running this again will overwrite it.

    ./tools/split-post.py ../Dev/_posts/2023-04-07-dotnet-development.md

Each chapter is a slice of the original, identified by the line numbers of the
headings that opened it.  Inside a chapter the headings are promoted, because the
chapter title lives in the Jekyll front matter rather than in the body -- by how
much depends on how deep that material sat in the post, so the amount is recorded
per range in CHAPTERS below.
"""

import argparse
import os
import re
import sys

# (stem, title, number, part, [(first_line, last_line, shift), ...])
#
# Line numbers are 1-based and inclusive, against the post as it stood when the
# split was made (5,229 lines).  A chapter with more than one range is one that
# gathers material the post had left in two places.
#
# `shift` is how many levels the headings in that range move up.  The post was
# not consistent about depth -- most sections were ### under a ## that has become
# a chapter title, but a few were already top-level ## with ### children -- so
# the amount differs per range rather than per chapter.  The target is always the
# same: a section is ##, a subsection is ###.
CHAPTERS = [
    ("01-language-basics", "The Language: C# and VB", 1, 1, [(14, 782, 1)]),
    ("02-async-and-threads", "Asynchronous Work and Threads", 2, 1,
     [(783, 1043, 1), (1729, 1842, 1)]),
    ("03-structuring-an-application", "Structuring an Application", 3, 1,
     [(1151, 1728, 1)]),

    # Winforms was a ### section; Maui was a top-level ## with ### children, so
    # it is already at the depth this chapter wants and must not move.
    ("04-desktop-uis", "Desktop User Interfaces", 4, 2,
     [(1044, 1150, 1), (4763, 4892, 0)]),
    # Crystal Reports was a ### whose heading is dropped, so its #### children
    # have two levels to climb.
    ("05-crystal-reports", "Crystal Reports", 5, 2, [(1843, 2044, 2)]),

    ("06-sqlite", "SQLite", 6, 3, [(2120, 2255, 1)]),
    ("07-postgresql", "PostgreSQL", 7, 3, [(2256, 2461, 1)]),
    ("08-microsoft-sql-server", "Microsoft SQL Server", 8, 3, [(2462, 2883, 1)]),
    ("09-redis", "Redis", 9, 3, [(2884, 3349, 1)]),
    ("10-data-access", "Data Access in .NET", 10, 3, [(3350, 4103, 1)]),

    ("11-aspnet-core", "ASP.NET Core", 11, 4, [(4104, 4403, 1)]),
    ("12-containers-and-hosting", "Containers, nginx and Kubernetes", 12, 4,
     [(4404, 4571, 1), (4976, 5002, 1)]),
    ("13-javascript", "JavaScript in the Browser", 13, 4, [(4572, 4762, 1)]),

    ("14-monitoring", "Monitoring and Observability", 14, 5, [(4893, 4975, 1)]),
    ("15-build-pipelines", "Build Pipelines", 15, 5, [(5003, None, 1)]),
]

# Back matter: no part, no number.
APPENDICES = [
    ("90-git-clients", "Git Clients", [(2045, 2119, 1)]),
]


# Chapters whose first heading in the post is the section title the chapter is
# now named after.  The chapter title lives in the front matter, so that heading
# would otherwise be printed twice.
DROP_LEADING_HEADING = {
    "05-crystal-reports", "06-sqlite", "07-postgresql",
    "08-microsoft-sql-server", "09-redis", "10-data-access", "11-aspnet-core",
    "13-javascript", "14-monitoring", "15-build-pipelines", "90-git-clients",
}


def drop_first_heading(lines):
    """Remove the leading heading, and any blank lines ahead of it."""
    for index, line in enumerate(lines):
        if not line.strip():
            continue
        if line.startswith("#"):
            return lines[index + 1:]
        return lines
    return lines


def shift_headings(lines, amount):
    """Promote every heading by `amount` levels, leaving fenced code alone.

    Bash and Dockerfile comments start with a single # and must not be touched,
    hence the fence tracking.  Nothing is promoted past ##; a chapter body has no
    business holding an H1, since the chapter title is the H1.
    """
    if amount == 0:
        return list(lines)

    out, fenced = [], False
    for line in lines:
        if line.lstrip().startswith("```"):
            fenced = not fenced
            out.append(line)
            continue
        if not fenced:
            match = re.match(r"^(#{2,6})(\s+\S)", line)
            if match:
                level = max(2, len(match.group(1)) - amount)
                line = "#" * level + match.group(2) + line[match.end():]
        out.append(line)
    return out


def slice_segments(lines, ranges):
    """Cut the ranges out of the post, keeping each one's shift with it."""
    segments = []
    for first, last, shift in ranges:
        stop = len(lines) if last is None else last
        segments.append((lines[first - 1:stop], shift))
    return segments


def write_chapter(outdir, stem, title, segments, *, number=None, part=None,
                  appendix=False):
    front = ["---", "layout: chapter", f'title: "{title}"']
    if number is not None:
        front.append(f"number: {number}")
    if part is not None:
        front.append(f"part: {part}")
    if appendix:
        front.append("appendix: true")
    front += ["---", ""]

    # Drop before shifting, so the heading being discarded cannot collide with
    # the children that are about to take its place.
    if stem in DROP_LEADING_HEADING:
        head, shift = segments[0]
        segments = [(drop_first_heading(head), shift)] + segments[1:]

    body = []
    for lines, shift in segments:
        if body:
            body.append("")
        body.extend(shift_headings(lines, shift))

    text = "\n".join(front + body).rstrip() + "\n"
    # Collapse the runs of blank lines the slicing leaves at the seams.
    text = re.sub(r"\n{3,}", "\n\n", text)
    path = os.path.join(outdir, f"{stem}.md")
    with open(path, "w", encoding="utf-8") as handle:
        handle.write(text)
    return path, text.count("\n")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", help="the original post")
    parser.add_argument("--outdir", default="_chapters")
    args = parser.parse_args()

    with open(args.source, encoding="utf-8") as handle:
        lines = handle.read().split("\n")

    if len(lines) < 5000:
        print(f"warning: {args.source} is {len(lines)} lines; the line numbers "
              "in this script were taken against a 5,229 line post and will "
              "slice in the wrong places.", file=sys.stderr)

    os.makedirs(args.outdir, exist_ok=True)

    for stem, title, number, part, ranges in CHAPTERS:
        path, count = write_chapter(args.outdir, stem, title,
                                    slice_segments(lines, ranges),
                                    number=number, part=part)
        print(f"{path}  ({count} lines)")

    for stem, title, ranges in APPENDICES:
        path, count = write_chapter(args.outdir, stem, title,
                                    slice_segments(lines, ranges), appendix=True)
        print(f"{path}  ({count} lines)")


if __name__ == "__main__":
    main()
