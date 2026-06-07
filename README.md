# Genova.Eliza

A .NET 8 implementation of the classic ELIZA chatbot, with a reusable library and console-based runners.

> [!WARNING]
> This is an experimental project and should not be considered production-ready. It exists to explore a small AI, ML, agent, or demo idea within the broader Genova ecosystem.

> [!IMPORTANT]
> A fresh public clone of this repository should not be expected to restore or build without additional Genova infrastructure. Many Genova dependencies are distributed through a private authenticated NuGet feed, and the public source does not include feed credentials or a complete public package graph.

## Installation

```bash
dotnet restore
dotnet build
```

## Usage

Run the console chatbot:

```bash
dotnet run --project Eliza.Console
```

The core library exposes an `Eliza` type with a greeting and per-turn reply generation.

## Features

* ELIZA chatbot engine implemented as a class library
* Console application for interactive chat sessions
* Additional chat harness that can script conversations and write a transcript

## Notes

* `Eliza.Chatting` requires the `OPENAI_API_KEY` environment variable.
* `Eliza.Chatting` also expects `appsettings.json` to provide `OpenAI:TextModel` and `OutputDirectory`.
* The library loads its chatbot script from an embedded JSON resource.

## Thanks

* ELIZA / DOCTOR, the original 1966 chatbot concept and script

## Third-Party Notices

This project has direct runtime dependencies on third-party NuGet packages, including `Microsoft.Extensions.*` packages (MIT). See each package's NuGet license metadata for full license and notice terms.

## License

GNU General Public License v3.0. See the `LICENSE` file for details.
