#!/usr/bin/env bash
set -euo pipefail

export PATH="/Applications/Docker.app/Contents/Resources/bin:${PATH}"
docker compose up -d --build
