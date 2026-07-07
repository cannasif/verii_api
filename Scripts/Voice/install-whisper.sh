#!/usr/bin/env bash
set -euo pipefail

PYTHON_BIN="${PYTHON_BIN:-python3}"
MODEL="${V3RII_WHISPER_MODEL:-small}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
VENV_PATH="$REPO_ROOT/.venv"

echo "Creating Python virtual environment at $VENV_PATH"
"$PYTHON_BIN" -m venv "$VENV_PATH"

PYTHON_EXE="$VENV_PATH/bin/python"

echo "Installing faster-whisper dependencies"
"$PYTHON_EXE" -m pip install --upgrade pip
"$PYTHON_EXE" -m pip install -r "$SCRIPT_DIR/requirements.txt"

echo ""
echo "Use these appsettings values on the API server:"
echo "Voice:TranscriptionEnabled=true"
echo "Voice:TranscriptionExecutablePath=$PYTHON_EXE"
echo "Voice:TranscriptionArgumentsTemplate=\"Scripts/Voice/transcribe_whisper.py\" --language {language} --file \"{input}\" --model $MODEL"
