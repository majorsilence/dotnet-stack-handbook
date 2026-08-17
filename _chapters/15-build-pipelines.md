---
layout: chapter
title: "Build Pipelines"
number: 15
part: 5
---

A build pipeline is the machine that turns a commit into something you can install, and its real job is not automation for its own sake. It is answering three questions without argument: does this change build on a machine that is not yours, do the tests still pass, and what exactly is in the artifact that went to production.

Everything in this chapter serves those three. The examples are GitHub Actions and Jenkins, because between them they cover most .NET shops, but the shape - restore, build, test, publish, sign, ship - is the same in Azure DevOps, GitLab CI and TeamCity.

## The shape of a .NET pipeline {#shape}

Six steps, in this order, each depending on the last:

```bash
dotnet restore [YourSolution].slnx
dotnet build [YourSolution].slnx --no-restore -c Release
dotnet test [YourSolution].slnx --no-build -c Release
dotnet publish [YourProject] --no-build -c Release -o out
dotnet pack [YourLibrary] --no-build -c Release -o packages   # if you ship NuGet
```

The `--no-restore` and `--no-build` flags are not micro-optimisation. Without them each command silently redoes the previous one, which doubles the build time and - worse - can build with different arguments than the ones you tested, so the artifact you ship is not the artifact that passed. State the dependency once and let each step consume the previous step's output.

.NET 10 reads `.slnx`, the XML solution format, as well as the old `.sln`. New solutions should use it; it merges without conflicts, which the old format famously does not.

### Make CI stricter than your machine {#strict-ci}

The build on a developer's machine is optimised for iteration speed. The build in CI is optimised for catching what iteration missed, and there are three settings that pay for themselves:

```xml
<!-- Directory.Build.props at the repo root, applying to every project. -->
<Project>
  <PropertyGroup>
    <TreatWarningsAsErrors Condition="'$(ContinuousIntegrationBuild)' == 'true'">true</TreatWarningsAsErrors>
    <ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)' == 'true'">true</ContinuousIntegrationBuild>
    <!-- Deterministic paths in the PDBs, so two builds of one commit match. -->
    <Deterministic>true</Deterministic>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
  </PropertyGroup>
</Project>
```

Warnings as errors is the one people push back on, and the objection is fair for a legacy codebase where the count is in the thousands. The middle path is to promote the categories that matter first - nullable warnings, `CA2007`, obsolete APIs - and add the rest as the count comes down:

```xml
<WarningsAsErrors>$(WarningsAsErrors);Nullable</WarningsAsErrors>
```

Formatting belongs in the pipeline too, as a check rather than a fix, so that review comments are about the code and not the whitespace.

```bash
dotnet format --verify-no-changes --severity warn
```

### Make the build reproducible {#reproducible}

"Works in CI" is worth very little if CI means something different next week. Three files pin it down.

**`global.json`** pins the SDK. Without it, a runner image update moves you to a new SDK mid-sprint and something subtle changes.

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

**`Directory.Packages.props`** puts every package version in one file, and stops the situation where three projects reference three versions of the same library and the winner is decided by build order.

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Dapper" Version="2.1.66" />
    <PackageVersion Include="Microsoft.Data.Sqlite" Version="10.0.0" />
    <PackageVersion Include="Npgsql" Version="10.0.0" />
  </ItemGroup>
</Project>
```

Projects then reference packages without a version:

```xml
<ItemGroup>
  <PackageReference Include="Dapper" />
</ItemGroup>
```

**`packages.lock.json`** pins the transitive graph, the part central versions do not cover. Enable it once, commit the files, and have CI refuse to restore anything the lock file does not name.

```xml
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
```

```bash
dotnet restore --locked-mode
```

That last flag is the point: it fails the build if a dependency resolved differently than when the lock file was written, rather than quietly shipping a version nobody chose.

## GitHub Actions {#github-actions}

Workflows live in `.github/workflows`. The file name does not matter; the `name:` inside it is what appears in the UI.

```yaml
name: .NET

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

# Cancel superseded runs on a branch, but never a run on main.
concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: ${{ github.ref != 'refs/heads/main' }}

# Least privilege by default; jobs opt into more.
permissions:
  contents: read

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
          cache: true                      # caches ~/.nuget/packages
          cache-dependency-path: '**/packages.lock.json'

      - name: Restore
        run: dotnet restore [YourSolution].slnx --locked-mode

      - name: Format check
        run: dotnet format --verify-no-changes --severity warn --no-restore

      - name: Build
        run: dotnet build [YourSolution].slnx --no-restore -c Release

      - name: Test
        run: >-
          dotnet test [YourSolution].slnx --no-build -c Release
          --collect:"XPlat Code Coverage" --logger:"trx"

      - name: Publish test results
        uses: dorny/test-reporter@v2
        if: success() || failure()      # report failures too, or it is useless
        with:
          name: unit tests
          path: "**/TestResults/*.trx"
          reporter: dotnet-trx
```

The NuGet cache is the single biggest speed win available and costs one line. `if: success() || failure()` on the reporting step is the second: a test report that only publishes when tests pass tells you nothing you did not already know.

### Building for several platforms {#matrix}

The three near-identical jobs this chapter used to carry are one job with a matrix. Less to keep in sync, and adding `linux-arm64` becomes one line.

```yaml
jobs:
  publish:
    runs-on: ${{ matrix.os }}
    strategy:
      fail-fast: false        # one platform failing should not hide the others
      matrix:
        include:
          - os: ubuntu-latest
            rid: linux-x64
          - os: ubuntu-24.04-arm
            rid: linux-arm64
          - os: windows-latest
            rid: win-x64
          - os: macos-latest
            rid: osx-arm64
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Publish
        run: >-
          dotnet publish [YourProject] -c Release -r ${{ matrix.rid }}
          --self-contained true
          -p:PublishSingleFile=true
          -p:EnableCompressionInSingleFile=true
          -p:PublishReadyToRun=true
          -p:Version=${{ github.ref_type == 'tag' && github.ref_name || '0.0.0-ci' }}
          -o publish/${{ matrix.rid }}

      - uses: actions/upload-artifact@v4
        with:
          name: [YourProject]-${{ matrix.rid }}
          path: publish/${{ matrix.rid }}
          retention-days: 7
```

Cross compiling matters here: a `linux-arm64` build runs on any runner, but `PublishReadyToRun` needs a matching host, which is why arm64 gets its own runner rather than a flag on the x64 one.

**Self contained or framework dependent?** Self contained ships the runtime with the app: about 70MB, no prerequisites, and you own patching it. Framework dependent is a few MB and needs the right runtime installed. For a desktop tool handed to users, self contained. For a container, framework dependent - the base image already has the runtime and gets patched when you rebuild.

### Versioning artifacts {#versioning}

An artifact that cannot be traced to a commit is a liability. The cheap version is the snippet above: tags produce a real version, everything else produces `0.0.0-ci`. The better version derives it from git history with [MinVer](https://github.com/adamralph/minver) or [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning).

```bash
dotnet add package MinVer
```

MinVer walks back to the nearest tag, so `v1.4.0` plus two commits builds as `1.4.1-alpha.0.2`, with no version number in any file to forget to bump. Set `fetch-depth: 0` on the checkout step or it cannot see the tags.

Whichever you use, stamp the commit into the assembly so a support ticket can be answered from the binary:

```xml
<PropertyGroup>
  <SourceRevisionId>$(GITHUB_SHA)</SourceRevisionId>
  <RepositoryUrl>https://github.com/majorsilence/[YourProject]</RepositoryUrl>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>
```

### Publishing a NuGet package {#publish-nuget}

```yaml
  pack:
    needs: build
    if: github.ref_type == 'tag'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0        # MinVer needs the tag history
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - run: dotnet pack [YourLibrary] -c Release -o packages

      - name: Push to nuget.org
        run: >-
          dotnet nuget push "packages/*.nupkg"
          --api-key ${{ secrets.NUGET_API_KEY }}
          --source https://api.nuget.org/v3/index.json
          --skip-duplicate
```

`--skip-duplicate` stops a re-run of the job from failing on a version that is already published. Push the `.snupkg` symbols too; six months from now a stack trace with line numbers is worth the thirty seconds it took.

### Containers {#containers}

The SDK builds a container image without a Dockerfile, which for a plain ASP.NET Core service is enough and removes a file that tends to drift:

```bash
dotnet publish [YourProject] -c Release \
  -p:PublishProfile=DefaultContainer \
  -p:ContainerRegistry=ghcr.io \
  -p:ContainerRepository=majorsilence/shows-api \
  -p:ContainerImageTag=1.4.0
```

When you need control over the image - extra native packages, a non-root user, a multi-stage build - use the Dockerfile from [Containers, nginx and Kubernetes](12-containers-and-hosting.html) with buildx, and produce an SBOM and provenance attestation while you are there.

```yaml
  image:
    needs: build
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
      id-token: write          # for attestations
    steps:
      - uses: actions/checkout@v4
      - uses: docker/setup-buildx-action@v3
      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - uses: docker/build-push-action@v6
        with:
          push: true
          tags: ghcr.io/majorsilence/shows-api:${{ github.sha }}
          sbom: true
          provenance: mode=max
          cache-from: type=gha
          cache-to: type=gha,mode=max
```

**Tag with the commit sha, not just `latest`.** A rollback needs a name for the exact image that worked, and `latest` is not a name - it is a moving pointer that makes "which build is running in production" unanswerable.

### Building Linux packages {#linux-packages}

For a tool people install rather than a service you host, `fpm` turns a published folder into a `.deb` or `.rpm`.

```yaml
jobs:
  package:
    runs-on: ubuntu-latest
    env:
      SOLUTION_NAME: "YourSolution"
      DEVELOPER: "majorsilence"
      PROJECT: "Your Project"
      MAIN_EXE: "The Main Exe filename"
      PRODUCT: "product name"
      MAINTAINER: "Your Name <your@example.com>"
      VERSION: "1.0.0"
    steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 10.0.x
    - name: Build
      run: |
        dotnet restore ${{ env.SOLUTION_NAME }}.slnx
        dotnet build -c Release ${{ env.SOLUTION_NAME }}.slnx --no-restore
        dotnet publish -c Release -r linux-x64 --self-contained true
    - name: Prep for fpm
      run: |
        mkdir -p build/linux/opt/${{ env.DEVELOPER }}/${{ env.PROJECT }}
        cp -r ${{ env.PROJECT }}/bin/Release/net10.0/linux-x64/publish/* build/linux/opt/${{ env.DEVELOPER }}/${{ env.PROJECT }}/
        chmod +x build/linux/opt/${{ env.DEVELOPER }}/${{ env.PROJECT }}/${{ env.MAIN_EXE }}
        mkdir -p build/linux/usr/bin
        cat > build/linux/usr/bin/${{ env.DEVELOPER }}-${{ env.PRODUCT }} << 'EOF'
        #!/bin/sh
        /opt/${{ env.DEVELOPER }}/${{ env.PROJECT }}/${{ env.MAIN_EXE }} "$@"
        rc=$?
        exit $rc
        EOF
        chmod +x build/linux/usr/bin/${{ env.DEVELOPER }}-${{ env.PRODUCT }}
    - name: Build deb package
      run: |
        cd build/linux
        fpm -s dir -t deb \
        --name ${{ env.DEVELOPER }}-${{ env.PRODUCT }} \
        --version ${{ env.VERSION }} \
        --description "${{ env.DEVELOPER }} ${{ env.PRODUCT }} tool." \
        --maintainer "${{ env.MAINTAINER }}" \
        --license "MIT" \
        --architecture all \
        --deb-no-default-config-files \
        --url "https://github.com/${{ env.DEVELOPER }}/${{ env.PROJECT }}" \
        ./
```

Swap `-t deb` for `-t rpm` and the same folder produces a Fedora package. Windows installers are a separate problem: WiX or Inno Setup for an MSI, MSIX for the Store, and the binaries need signing - see the notes on code signing certificates in [Desktop User Interfaces](04-desktop-uis.html).

## Tests that need a database {#integration-tests}

Unit tests run anywhere. The tests that catch real bugs usually need PostgreSQL, SQL Server or Redis, and there are two honest ways to give them one.

**Service containers** are the simplest: the runner starts the container, the test connects to it on localhost.

```yaml
    services:
      postgres:
        image: postgres:17
        env:
          POSTGRES_PASSWORD: testing
        ports: ["5432:5432"]
        options: >-
          --health-cmd pg_isready --health-interval 5s
          --health-timeout 5s --health-retries 10
```

The health options matter. Without them the test step starts before PostgreSQL is accepting connections and the failure looks like a flaky test rather than a race.

**[Testcontainers](https://dotnet.testcontainers.org/)** puts the same thing in the test code, which means it also works when a developer runs the suite locally - no docker-compose to remember, no shared database to corrupt.

```bash
dotnet add package Testcontainers.PostgreSql
```

```cs
[TestFixture]
public class ShowRepoTests
{
    private PostgreSqlContainer _db = null!;

    [OneTimeSetUp]
    public async Task StartDatabase()
    {
        _db = new PostgreSqlBuilder()
            .WithImage("postgres:17")
            .Build();

        await _db.StartAsync();
        await Migrate(_db.GetConnectionString());
    }

    [OneTimeTearDown]
    public async Task StopDatabase() => await _db.DisposeAsync();

    [Test]
    public async Task InsertThenGetRoundTrips()
    {
        var repo = new ShowRepo(_db.GetConnectionString());
        var id = await repo.InsertAsync(new TvShow { ShowName = "Star Trek" });

        var show = await repo.GetShowAsync(id);

        Assert.That(show!.ShowName, Is.EqualTo("Star Trek"));
    }
}
```

It needs Docker on the machine running the tests, which every GitHub-hosted Linux runner has. The payoff is that the SQL is tested against the engine it will run on, rather than against SQLite pretending to be PostgreSQL.

### Coverage that means something {#coverage}

`--collect:"XPlat Code Coverage"` writes Cobertura XML. Turning that into a number and a trend takes one more tool.

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool

reportgenerator -reports:"**/coverage.cobertura.xml" \
  -targetdir:coverage -reporttypes:"Html;MarkdownSummaryGithub"

cat coverage/SummaryGithub.md >> $GITHUB_STEP_SUMMARY
```

Setting a hard coverage gate is a decision to make carefully. A threshold on **new code** in a pull request is useful and rarely gamed. A global "80% or the build fails" on an old codebase mostly produces tests that assert nothing while executing a lot of lines.

## Security in the pipeline {#pipeline-security}

The pipeline has credentials to your registry, your cloud account and your package feed. It is a production system, and these are the cheap controls that matter most.

**Know what you depend on.** One command, no packages to install:

```bash
dotnet list package --vulnerable --include-transitive
dotnet list package --deprecated
```

Run it in CI and fail on anything with a known critical advisory. Add [Dependabot](https://docs.github.com/code-security/dependabot) or [Renovate](https://docs.renovatebot.com/) to open the update PRs, grouped weekly so they are reviewable rather than a hundred separate ones nobody reads.

**Do not store long lived cloud credentials.** GitHub can mint a short lived token per run via OIDC, which the cloud provider trusts. A leaked log line then leaks something that expired an hour ago rather than a key that works until someone notices.

```yaml
    permissions:
      id-token: write
      contents: read
```

**Pin third party actions to a commit sha.** A tag is mutable; `@v2` can become different code tomorrow without your involvement, and supply chain attacks against popular actions have happened.

```yaml
      - uses: dorny/test-reporter@31a54ee7ebcacc03a09ea97a7e5465a47b84aea5   # v2.1.0
```

**Set permissions explicitly.** `permissions: contents: read` at the top of the workflow, and each job adding only what it needs. The default token can otherwise push to your repository.

**Static analysis.** [CodeQL](https://codeql.github.com/) has good C# support and runs as an action; add it on a schedule as well as on pull requests, because new rules find old bugs. `dotnet format --verify-no-changes` and the .NET analyzers above catch a different class of problem earlier and faster.

**Ship an SBOM** with anything you distribute. `docker buildx` produces one for images, as shown above; for a published application, `dotnet CycloneDX` produces one from the project graph. When the next widely used library turns out to be compromised, an SBOM is the difference between answering "are we affected" in minutes and in days.

## Deployment {#deployment}

Getting a tested artifact onto a server is the last step, and the ordering questions are where the outages live.

**Database migrations run before the new code, and must work with the old code.** During any rolling deploy both versions run at once. That forces expand and contract: add the nullable column, deploy code that writes both, backfill, deploy code that reads the new one, drop the old column in a later release. A migration that renames a column in one step will take the site down in the window between the two, no matter how good the pipeline is. See [Data Access in .NET](10-data-access.html) for the migration tooling itself.

**Deploy to an environment with a gate.** GitHub environments carry their own secrets and optional required reviewers, so production credentials do not exist in a pull request build.

```yaml
  deploy:
    needs: [build, image]
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    environment:
      name: production
      url: https://shows.example.com
    steps:
      - name: Roll out
        run: kubectl set image deployment/shows-api api=ghcr.io/majorsilence/shows-api:${{ github.sha }}

      - name: Wait for the rollout
        run: kubectl rollout status deployment/shows-api --timeout=5m

      - name: Smoke test
        run: |
          curl --fail --retry 10 --retry-delay 5 --retry-all-errors \
            https://shows.example.com/healthz/ready
```

**A deploy that does not verify itself is not finished.** `kubectl rollout status` fails if the new pods never become ready, and the smoke test against `/healthz/ready` from [Monitoring and Observability](14-monitoring.html) catches the case where they are ready but wrong. Both are two lines, and together they turn a silent bad deploy into a red build.

**Rollback is `kubectl rollout undo`, or redeploying the previous sha.** Both need the previous image to still exist, which is the practical argument against a registry retention policy that deletes anything from last week.

## Jenkins {#jenkins}

Jenkins remains common where builds must run inside a network that GitHub's runners cannot reach, or on hardware nobody is going to move. Installation instructions are at [jenkins.io/download](https://www.jenkins.io/download/).

```bash
curl -fsSL https://pkg.jenkins.io/debian-stable/jenkins.io.key | sudo tee \
    /usr/share/keyrings/jenkins-keyring.asc > /dev/null

echo deb [signed-by=/usr/share/keyrings/jenkins-keyring.asc] \
    https://pkg.jenkins.io/debian-stable binary/ | sudo tee \
    /etc/apt/sources.list.d/jenkins.list > /dev/null

sudo apt-get update
sudo apt-get install jenkins openjdk-21-jdk-headless docker.io -y
sudo usermod -a -G docker jenkins
```

### Plugins {#jenkins-plugins}

- [docker-workflow](https://plugins.jenkins.io/docker-workflow/) - run build steps inside a container
- [github-branch-source](https://plugins.jenkins.io/github-branch-source/) - multibranch pipelines from a repository
- [nunit](https://plugins.jenkins.io/nunit/) - test results
- [coverage](https://plugins.jenkins.io/coverage/) - Cobertura coverage reports

### A .NET pipeline {#jenkins-dotnet}

Save this as `Jenkinsfile` in the root of the repository. Running the build inside the official SDK image means the agent needs Docker and nothing else - no .NET installs to keep in step across a fleet of build machines.

```groovy
pipeline {
    agent none
    options {
        timeout(time: 30, unit: 'MINUTES')
        buildDiscarder(logRotator(numToKeepStr: '30'))
    }
    environment {
        DOTNET_CLI_HOME = "/tmp/DOTNET_CLI_HOME"
        DOTNET_CLI_TELEMETRY_OPTOUT = "1"
        DOTNET_NOLOGO = "1"
    }
    stages {
        stage('build and test') {
            agent {
                docker {
                    image 'mcr.microsoft.com/dotnet/sdk:10.0'
                    // Reuse the NuGet cache between builds on this agent.
                    args '-v $HOME/.nuget/packages:/tmp/nuget'
                }
            }
            environment {
                NUGET_PACKAGES = "/tmp/nuget"
            }
            steps {
                sh '''
                dotnet restore [YourSolution].slnx --locked-mode
                dotnet build [YourSolution].slnx --no-restore -c Release
                dotnet test [YourSolution].slnx --no-build -c Release \
                    --collect:"XPlat Code Coverage" --logger:"nunit"
                '''
            }
            post {
                always {
                    nunit testResultsPattern: '**/TestResults/*.xml'
                    recordCoverage(tools: [[parser: 'COBERTURA',
                        pattern: '**/TestResults/**/*cobertura.xml']])
                }
            }
        }
    }
}
```

More examples in [Jenkins and pipelines, Jenkinsfile](/posts/2022/02/12/jenkins-jenkinsfile-pipelines.html).

## What good looks like {#checklist}

A pipeline worth having, in the order to build it:

1. Every push builds and tests, and a failure blocks the merge.
2. Restore is locked and the SDK is pinned, so the build means the same thing next month.
3. Tests that need a database get a real one, from a container.
4. Test results and coverage are published where a reviewer sees them without digging through logs.
5. Artifacts are versioned from git and tagged with the commit sha.
6. Dependencies are scanned, and updates arrive as reviewable pull requests.
7. Deployment is gated by an environment, verified by a health check, and reversible by name.
8. The whole thing finishes in under ten minutes, because a pipeline slower than a developer's patience gets worked around.
