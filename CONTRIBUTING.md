# Contributing

## Requirement

- Windows, with Space Engineers installed.
- The .NET SDK.
- A GitHub account, for the malforge package feed.

[MDK2](https://malforge.github.io/spaceengineers/mdk2/) builds and packs the mods.

## Getting the code

Lumel, the user interface framework, is a submodule at `src/Lumel`, from the private `malware-dev/Lumel` repository.
Clone with `git clone --recursive`, or fetch it into an existing clone with `git submodule update --init`.

## The malforge package feed

The mixin packages, such as `Mal.MdkModMixin.Network`, come from malforge's feed on GitHub, which asks for a GitHub
login. A machine is set up for it once, as
[Setting Up GitHub Packages as a NuGet Source](https://github.com/malforge/mdk2-packages/blob/main/docs/Setting-Up-GitHub-Packages.md)
describes.

## Setting up MDK2

[MDK² Project Configuration Guide](https://malforge.github.io/spaceengineers/mdk2/MDK%C2%B2-Project-Configuration-Guide.html).

## The font mods

The simulator, and so the tests, draw with the font mods Sprocket requires, read from Steam's Workshop folder. Load a
world with Sprocket in the game once and Steam downloads them; without them the simulator stops and says so.

## Building and testing

```
dotnet build src/Sprocket.Host.SpaceEngineers                                # builds and packs into the Mods folder
dotnet build src/Sprocket.Host.SpaceEngineers -p:MdkBuildConfiguration=none  # builds without packing
dotnet test -p:MdkBuildConfiguration=none                                    # every test project
```

## Agents

Agents working on Sprocket read their rules from [AGENTS.md](AGENTS.md).
