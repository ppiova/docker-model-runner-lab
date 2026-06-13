#!/usr/bin/env bash
#
# Docker Model Runner quickstart (bash).
# Pulls a model, lists local models, runs a one-shot prompt and prints the API endpoint.
# Idempotent: safe to run multiple times.
#
set -euo pipefail

# Model can be overridden: MODEL=ai/llama3.2 ./quickstart.sh
MODEL="${MODEL:-ai/gemma3}"
HOST_ENDPOINT="http://localhost:12434/engines/v1"

echo "==> 1/5 Checking that Docker Model Runner is enabled"
if ! docker model status >/dev/null 2>&1; then
  echo "    Docker Model Runner is not running."
  echo "    Enable it in Docker Desktop: Settings -> AI -> Enable Docker Model Runner"
  echo "    (also enable host-side TCP support on port 12434)."
  exit 1
fi
docker model status

echo "==> 2/5 Pulling model '${MODEL}' (skipped if already present)"
if docker model inspect "${MODEL}" >/dev/null 2>&1; then
  echo "    '${MODEL}' is already available locally, skipping pull."
else
  docker model pull "${MODEL}"
fi

echo "==> 3/5 Listing local models"
docker model list

echo "==> 4/5 Running a one-shot prompt against '${MODEL}'"
docker model run "${MODEL}" "In one sentence, what is Docker Model Runner?"

echo "==> 5/5 OpenAI-compatible API endpoint (from the host)"
echo "    ${HOST_ENDPOINT}"
echo "    Try it: curl ${HOST_ENDPOINT}/models"
echo
echo "Done. The model is ready to use over the OpenAI-compatible API."
