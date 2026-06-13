#!/usr/bin/env bash
#
# Call Docker Model Runner over its OpenAI-compatible API with curl.
# Shows a normal chat completion and a streaming one.
#
set -euo pipefail

MODEL="${MODEL:-ai/gemma3}"
BASE_URL="${OPENAI_BASE_URL:-http://localhost:12434/engines/v1}"

echo "==> Checking that the API is reachable at ${BASE_URL}"
if ! curl -fsS -m 5 "${BASE_URL}/models" >/dev/null; then
  echo "    Cannot reach ${BASE_URL}."
  echo "    Make sure Docker Model Runner is enabled with host-side TCP support (port 12434)."
  exit 1
fi
echo "    OK"
echo

echo "==> Example 1: chat completion (non-streaming)"
curl -sS "${BASE_URL}/chat/completions" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "'"${MODEL}"'",
    "messages": [
      { "role": "system", "content": "You are a concise assistant." },
      { "role": "user", "content": "Explain Docker Model Runner in two sentences." }
    ]
  }'
echo
echo

echo "==> Example 2: chat completion (streaming, server-sent events)"
curl -sS -N "${BASE_URL}/chat/completions" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "'"${MODEL}"'",
    "stream": true,
    "messages": [
      { "role": "user", "content": "List three benefits of running models locally." }
    ]
  }'
echo
echo
echo "Done. Streaming responses arrive as 'data:' chunks ending with 'data: [DONE]'."
