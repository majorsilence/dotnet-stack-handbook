---
layout: chapter
title: "Preface"
unnumbered: true
---

## What this book is {#what-this-book-is}

A working reference for building .NET software and running it once it is built, written by someone who does that for a living rather than someone documenting features in order.

It began as a single post on majorsilence.com. That post grew past five thousand lines, which is the point at which a post stops being a post. It is now fifteen chapters across five parts: the languages, the user interfaces, four databases and the .NET side of talking to them, the web stack, and the operations that keep the result alive.

## Who it is for {#who-it-is-for}

Developers who already program, and who need the .NET answer to something specific. It does not teach programming from nothing, and it does not assume you have used .NET before.

The chapters are deliberately independent. Read one, take what you need, and leave. Where one chapter genuinely depends on another - and it happens most often with [Structuring an Application](03-structuring-an-application.html), whose repository and dependency injection patterns reappear everywhere afterwards - there is a link rather than a repeated explanation.

## What is checked, and what is not {#what-is-checked}

Every listing targets .NET 10 (`net10.0`) unless a section says otherwise.

The C# and VB code is not typed into the page and hoped for. It lives in an `examples/` solution alongside the text, and every build compiles it, runs the tests, and executes the console examples. When a chapter states what a program prints, that is the output it actually produced. Writing that solution found seven bugs in the prose it was copied from, which is a fair estimate of how many are in any programming book that lacks one.

The rest is not machine checked and should be read with correspondingly more suspicion. SQL run against a live server, Dockerfiles, nginx configuration, Kubernetes manifests and CI pipeline definitions are all reviewed but not executed by the build. Where that distinction matters, the text says so.

Tooling ages faster than anything else in a book like this. Recommendations here are current as of writing and will not stay that way; the underlying commands and configuration outlast the graphical tools that wrap them, which is why the text prefers them.

## Conventions {#conventions}

Examples are Linux first, because that is where most .NET now runs in production. Where Windows differs in a way that matters, both are given.

C# and VB appear side by side in the early chapters. Later chapters are mostly C#, not out of any judgement about VB, but because repeating every listing twice would double the length of the book without teaching anything the first two chapters have not already shown.

Nullable reference types are on, in the examples solution and in the listings, because they are on in every project `dotnet new` creates and a book whose code produces warnings the moment it is pasted somewhere real is not much use. A `string?` in a listing means the value can be null and the surrounding code deals with it. [The Language: C# and VB](01-language-basics.html#nullable-reference-types) covers the feature itself.

## Licence {#licence}

The prose is licensed [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/). The code samples, and everything in the `examples/` and `tools/` directories, are MIT.

That split is deliberate. Copy a listing into your own application freely, with no obligation beyond the copyright notice - a book that exists to be copied from should not force you to relicense your work for taking ten lines from it.
