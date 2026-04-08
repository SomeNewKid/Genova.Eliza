# Genova.Eliza

A .NET 8 implementation of the classic ELIZA chatbot, with a reusable library and console-based runners.

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

## License

GNU General Public License v3.0 (GPL-3.0)
