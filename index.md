---
layout: home
title: Contents
---

This is a working reference for building .NET software and running it once it is
built. It covers the C# and VB languages, desktop and web user interfaces, four
databases worth knowing, the .NET side of talking to them, and the containers,
proxies, monitoring and pipelines that stand between a build and a service
somebody depends on.

Every listing targets **.NET {{ site.dotnet_version }}** (`{{ site.target_framework }}`)
unless a section says otherwise. The C# and VB examples live under
[`examples/`]({{ site.repo }}/tree/main/examples) and are compiled on every
build, so the code in those chapters is code that at least still builds. The SQL,
nginx and Kubernetes listings are not machine checked and are marked as such
where it matters.

It began as [a single post on majorsilence.com]({{ site.origin_post }}) that grew
past five thousand lines. This is that post, split into chapters and given a
build that can tell when it has gone stale.
