# 05 - Blazor chat UI

A web chat interface for Docker Model Runner, built with **Blazor Server (.NET 10)**.

The OpenAI SDK runs server-side (the same pattern as [03-dotnet-chat](../03-dotnet-chat) and
[04-compose](../04-compose)), and the model streams its answer token by token to the browser over
the Blazor SignalR connection. No CORS setup, no API key, no data leaving your machine.

<!-- ![Blazor chat screenshot](../assets/blazor-chat.png) -->

## Features

- Streaming responses rendered live, with a typing indicator while the model thinks.
- Conversation history kept across turns so the model has context.
- Model name and endpoint shown in the header (resolved from configuration).
- Light and dark theme, following your system preference.
- Friendly error message if the model endpoint is unreachable.

## Configuration

| Variable          | Default                             | Description                    |
| ----------------- | ----------------------------------- | ------------------------------ |
| `OPENAI_BASE_URL` | `http://localhost:12434/engines/v1` | DMR OpenAI-compatible endpoint |
| `MODEL`           | `ai/gemma3`                         | Model to chat with             |

## Run locally

```bash
dotnet run
```

Then open the URL shown in the console (by default `http://localhost:5000`).

## Run with Docker Compose

This builds the app image and provisions the model together. Open `http://localhost:8080`
once it is up.

```bash
docker compose up --build
```

Compose injects `OPENAI_BASE_URL` and `MODEL` into the container, so the app talks to the model
at `http://model-runner.docker.internal` with no extra configuration.

## Stop

```bash
docker compose down
```
