---
layout: chapter
title: "Git Clients"
appendix: true
---

Github new repo example.

```bash
mkdir your_repo
cd your_repo
echo "" >> README.md
git init
git add README.md
git commit -m "first commit"
git branch -M main
git remote add origin git@github.com:YOUR_USERNAME/YOUR_REPO.git
git push -u origin main
```

Push an existing local repo to a new github repo.

```bash
git remote add origin git@github.com:YOUR_USERNAME/YOUR_REPO.git
git branch -M main
git push -u origin main
```

Git, show current branch.

```bash
git branch --show-current
```

Git, show remotes.

```bash
git branch --remotes
```

Git commit changes.

```bash
git commit -m "hello world"
```

Git pull/rebase from

```bash
git pull --rebase
```

Git pull from remote and branch.

```bash
git pull --rebase upstream main
```

## Git Visual Studio

[About Git in Visual Studio](https://learn.microsoft.com/en-us/visualstudio/version-control/git-with-visual-studio?view=vs-2022)

## Git Rider

[How to efficiently use Git integration in JetBrains Rider](https://www.jetbrains.com/help/rider/Using_Git_Integration.html)

## Tortoise Git

> The Power of Git – in a Windows Shell

[Tortoise Git](https://tortoisegit.org/) - windows shell git integration

## Github Desktop

> Experience Git without the struggle

[GitHub Desktop](https://github.com/apps/desktop)
