# The .NET Stack Handbook

C#, VB, databases, web and the operations around them.

**Read it at [dotnet-stack-handbook.majorsilence.com](https://dotnet-stack-handbook.majorsilence.com/)**,
or download the [PDF](https://dotnet-stack-handbook.majorsilence.com/dotnet-stack-handbook.pdf).

This started life as [one very long blog post](https://majorsilence.com/posts/2023/04/07/dotnet-development.html).
It is now fifteen chapters and an appendix, published as a website and a PDF, with
an `examples/` tree that CI compiles and runs so the listings in the book are known
to work rather than assumed to.

Everything targets **.NET 10** unless a chapter says otherwise.

## Layout

| Path | What it holds |
| --- | --- |
| `_chapters/` | The book. One Markdown file per chapter; this is the source of truth. |
| `examples/` | A .NET solution containing runnable versions of the listings. |
| `tools/` | The splitter that produced the chapters, the structural checker, and the PDF build. |
| `_layouts/`, `assets/` | The Jekyll site. |
| `.github/workflows/` | Build, test, PDF, and deploy to GitHub Pages. |

## Building

```bash
make help       # every target, with a description
make serve      # the site on http://127.0.0.1:4000
make check      # structural checks on the chapters
make examples   # build the example solution
make test       # run the example test suites
make pdf        # build/dotnet-stack-handbook.pdf
```

`make serve` needs Ruby and `bundle install`. `make pdf` needs pandoc and a LaTeX
install with XeTeX. `make examples` needs the .NET 10 SDK.

## The examples tree

`examples/Examples.slnx` is the reason this repo exists as something other than a
folder of Markdown. Chapters that have a matching project name it in their front
matter, and the site renders a link to it.

| Project | Chapters |
| --- | --- |
| `Examples.Language` | The Language: C# and VB |
| `Examples.Language.Vb` | The same listings in VB |
| `Examples.Concurrency` | Asynchronous Work and Threads |
| `Examples.AppStructure` | Structuring an Application |
| `Examples.Data` | SQLite, Data Access in .NET |
| `Examples.Web` | ASP.NET Core |
| `Examples.Tests` | NUnit, Moq, and in-memory integration tests for the web project |

Two rules keep the tree honest:

- **The code matches the book.** Where a listing is awkward, the book gets fixed,
  not the example. Nullable reference types are on, with nullable warnings
  promoted to errors, so a listing cannot quietly start lying about what can be
  null; the language chapter teaches the feature rather than sidestepping it.
- **Anything needing a server is compiled but not run.** The SQL Server and
  PostgreSQL listings, and the `HttpClient` downloader, are built by CI so an API
  change still breaks the build, but nothing in the test run depends on a network
  or a database being up.

## Regenerating the chapters

`tools/split-post.py` records how the original post was cut into chapters. It is
kept for reference and for re-running the migration if the post changes upstream.
It **overwrites** `_chapters/`, discarding the cross-chapter links, explicit
heading anchors and introductions added since the split, so it is not part of any
normal workflow.

## Licence

The book is [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/); the
code samples, `examples/` and `tools/` are MIT. Copying a listing into your own
project carries no obligation beyond the copyright notice — ShareAlike applies to
the prose, not to the code it prints. See `LICENSE`.
