---
layout: chapter
title: "Build Pipelines"
number: 15
part: 5
---

Build pipelines are automated workflows that compile, test, and deploy code changes systematically. They ensure code quality, streamline development, and enhance reliability, integrating tasks like testing, deployment, and monitoring, resulting in efficient, error-free software delivery.

## GitHub Actions

GitHub actions should go in the .github/workflows directory of a git project. The file type is yml but the name can be anything.

Example dotnet github action named dotnet.yml. This GitHub action builds a self contained dotnet console application, run tests, publishes the tests, and zips and archives the output artifacts for Windows, Linux, and Mac.

```yaml
name: .NET

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  linux-build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - name: Restore dependencies
        run: dotnet restore [YourSolution].sln
      - name: Build
        run: dotnet build [YourSolution].sln --no-restore -c Release
      - name: Test
        run: dotnet test -c Release [YourSolution].sln --verbosity normal --collect:"XPlat Code Coverage" --logger:"trx"
      - name: Test Report Publish
        uses: dorny/test-reporter@v2
        if: success() || failure() # run this step even if previous step failed
        with:
          name: unit tests
          path: "**/TestResults/*.trx"
          reporter: dotnet-trx
      - name: Publish
        run: dotnet publish [YourProject] -c Release -r linux-x64 -p:PublishReadyToRun=true --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
      - name: Archive artifacts
        uses: actions/upload-artifact@v4
        with:
          name: [YourProject]-linux-x64
          path: |
            [YourProject]/bin/Release/net10.0/linux-x64
          retention-days: 1

  windows-build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - name: Restore dependencies
        run: dotnet restore [YourSolution].sln
      - name: Build
        run: dotnet build [YourSolution].sln --no-restore -c Release
      - name: Publish
        run: dotnet publish [YourProject] -c Release -r win-x64 -p:PublishReadyToRun=true --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
      - name: Archive artifacts
        uses: actions/upload-artifact@v4
        with:
          name: [YourProject]-win-x64
          path: |
            [YourProject]/bin/Release/net10.0/win-x64
          retention-days: 1

  mac-build:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - name: Restore dependencies
        run: dotnet restore [YourSolution].sln
      - name: Build
        run: dotnet build [YourSolution].sln --no-restore -c Release
      - name: Publish
        run: dotnet publish [YourProject] -c Release -r osx-x64 -p:PublishReadyToRun=true --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
      - name: Archive artifacts
        uses: actions/upload-artifact@v4
        with:
          name: [YourProject]-osx-x64
          path: |
            [YourProject]/bin/Release/net10.0/osx-x64
          retention-days: 1
```

### GH Action to Create Linux Packages

```yaml
jobs:
  linux-build:
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
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 10.0.x
    - name: Build
      run: |
        dotnet restore ${{ env.SOLUTION_NAME }}.sln
        dotnet build -c Release ${{ env.SOLUTION_NAME }}.sln --no-restore
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
        --maintainer "${{ env.DEVELOPER }}" \
        --license "MIT" \
        --architecture all \
        --deb-no-default-config-files \
        --url "https://github.com/${{ env.DEVELOPER }}/${{ env.PROJECT }}" \
        --maintainer "${{ env.MAINTAINER }}" \
        ./
```

## Jenkins

Find jenkins installation instructions at [https://www.jenkins.io/download/](https://www.jenkins.io/download/).

Ubuntu Jenkins install

```bash
curl -fsSL https://pkg.jenkins.io/debian-stable/jenkins.io.key | sudo tee \
    /usr/share/keyrings/jenkins-keyring.asc > /dev/null

echo deb [signed-by=/usr/share/keyrings/jenkins-keyring.asc] \
    https://pkg.jenkins.io/debian-stable binary/ | sudo tee \
    /etc/apt/sources.list.d/jenkins.list > /dev/null

sudo apt-get update
sudo apt-get install jenkins openjdk-21-jdk-headless docker.io -y
sudo usermod -a -G docker jenkins

# java -jar jenkins-cli.jar -s http://localhost:8080/ install-plugin SOURCE ... [-deploy] [-name VAL] [-restart]

```

### Jenkins Plugin Setup

Install the docker pipelines and git branch source plugins

- [https://plugins.jenkins.io/docker-workflow/](https://plugins.jenkins.io/docker-workflow/)
- [https://plugins.jenkins.io/github-branch-source/](https://plugins.jenkins.io/github-branch-source/)
  - If github is being used

To display test results various Jenkin plugins are required.

- dotnet - [nunit](https://plugins.jenkins.io/nunit/)
- code coverage - [coverage](https://plugins.jenkins.io/coverage/)

### Jenkins Dotnet Pipeline

Example of building and testing a dotnet project that has nunit testing enabled. If there is only one solution in the directory then the solution name does not need to be specified.

Save this file as **Jenkinsfile** in the projects base folder.

```groovy
pipeline {
    agent none
    environment {
        DOTNET_CLI_HOME = "/tmp/DOTNET_CLI_HOME"
    }
    stages {
        stage('build and test') {
            agent {
                docker {
                    image 'mcr.microsoft.com/dotnet/sdk:10.0'
                }
            }
            steps {
                echo "building"
                sh """
                dotnet restore [YourSolution].sln
                dotnet build [YourSolution].sln --no-restore
                dotnet test [YourSolution].sln --logger:"nunit"
                # for code coverage run the next line instead of the previous line
                # dotnet test -c Release [YourSolution].sln --collect:"XPlat Code Coverage" --logger:"nunit"
                """
            }
            post{
                always {
                    nunit testResultsPattern: '**/TestResults/*.xml'
                    recordCoverage(tools: [[parser: 'COBERTURA', pattern: '**/TestResults/**/*cobertura.xml']])
                }
            }
        }
    }
}
```

See [Jenkins and pipelines, Jenkinfile](/posts/2022/02/12/jenkins-jenkinsfile-pipelines.html) for more examples.
