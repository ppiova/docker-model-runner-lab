# 03 - .NET streaming chat

A minimal .NET 10 console app that holds a streaming chat conversation with a model served by
Docker Model Runner, using the official [`OpenAI`](https://www.nuget.org/packages/OpenAI) NuGet package (2.x).

Because DMR is OpenAI-compatible, the only DMR-specific detail is the base URL.

## Prerequisites

- .NET 10 SDK
- Docker Model Runner enabled, with the model pulled (see [01-quickstart](../01-quickstart)).

## Configuration

| Variable          | Default                                 | Description                    |
| ----------------- | --------------------------------------- | ------------------------------ |
| `OPENAI_BASE_URL` | `http://localhost:12434/engines/v1`     | DMR OpenAI-compatible endpoint |
| `MODEL`           | `ai/gemma3`                             | Model to chat with             |

## Run

```bash
dotnet run

# or target a different model
MODEL=ai/llama3.2 dotnet run
```

```powershell
dotnet run

# or target a different model
$env:MODEL = "ai/llama3.2"; dotnet run
```

## Sample session

```
Docker Model Runner chat. Model: ai/gemma3
Endpoint: http://localhost:12434/engines/v1
Type a message and press Enter. Type /exit to quit.

you> What is Docker Model Runner in one sentence?
ai>  Docker Model Runner lets you pull and run LLMs locally through Docker with an OpenAI-compatible API.

you> /exit
Bye.
```

The response streams in token by token, and the conversation history is kept across turns so
the model has context.

## Next

See [04-compose](../04-compose) to provision the model and the app together with Docker Compose.
