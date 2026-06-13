# docker-model-runner-lab

Run LLMs locally with Docker Model Runner: a hands-on, copy-paste lab for developers.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Docker Desktop 4.40+](https://img.shields.io/badge/Docker%20Desktop-4.40%2B-2496ED?logo=docker&logoColor=white)](https://www.docker.com/products/docker-desktop/)

<!-- ![Docker Model Runner architecture](assets/dmr-architecture.gif) -->

## What is Docker Model Runner

Docker Model Runner (DMR) pulls and runs LLMs locally as OCI artifacts, the same way you run containers.
It serves an OpenAI-compatible API, so your existing tools and SDKs work unchanged.
Models live on Docker Hub under the `ai/` namespace and run on llama.cpp or vLLM with GPU acceleration.
No cloud, no API keys, no data leaving your machine.

## Quickstart in 3 commands

```bash
docker model pull ai/gemma3
docker model run ai/gemma3 "Hello in one sentence"
curl http://localhost:12434/engines/v1/models
```

## Modules

| Module | What you learn |
| ------ | -------------- |
| [01-quickstart](01-quickstart) | Pull, list and run a model with idempotent bash and PowerShell scripts |
| [02-openai-api](02-openai-api) | Call the OpenAI-compatible API with curl and the VS Code REST Client, including streaming |
| [03-dotnet-chat](03-dotnet-chat) | A .NET 10 console app with a streaming chat loop using the official OpenAI SDK |
| [04-compose](04-compose) | Provision the model and an app together with the Compose `models` element |

All examples default to `ai/gemma3` and accept a `MODEL` environment variable to use a different model.

## Endpoints

| Context        | Base URL                                            |
| -------------- | --------------------------------------------------- |
| Host process   | `http://localhost:12434/engines/v1`                 |
| Container      | `http://model-runner.docker.internal/engines/v1`    |

## llama.cpp vs vLLM

DMR supports two inference engines:

- **llama.cpp** runs **GGUF** models. It is the default, lightweight, and great for laptops and CPU or
  mixed CPU/GPU inference across Metal, CUDA and Vulkan. Reach for it for local development and most single-user workloads.
- **vLLM** runs **Safetensors** models. It is optimized for high-throughput, concurrent serving on GPUs.
  Reach for it when you need to serve many requests efficiently.

Rule of thumb: start with llama.cpp for local hacking, move to vLLM when you need throughput at scale.

## Documentation

- Official docs: https://docs.docker.com/ai/model-runner/
- Docker Model Runner on GitHub: https://github.com/docker/model-runner

## Built with Claude Code

Built with [Claude Code](https://claude.com/claude-code) by Pablo Piovano.

LinkedIn: <!-- TODO: add your LinkedIn profile URL -->
