#!/usr/bin/env python3
import argparse
import os
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")


def fail(message: str, code: int = 1) -> None:
    print(message, file=sys.stderr)
    raise SystemExit(code)


def main() -> None:
    parser = argparse.ArgumentParser(description="Download and load a faster-whisper model.")
    parser.add_argument("--model", default=os.getenv("V3RII_WHISPER_MODEL", "small"), help="Whisper model size or path.")
    parser.add_argument("--device", default=os.getenv("V3RII_WHISPER_DEVICE", "cpu"), help="cpu, cuda, or auto.")
    parser.add_argument("--compute-type", default=os.getenv("V3RII_WHISPER_COMPUTE_TYPE", "int8"), help="int8, float16, float32, ...")
    args = parser.parse_args()

    try:
        from faster_whisper import WhisperModel
    except Exception as exc:
        fail(f"faster-whisper import failed: {exc}", 12)

    try:
        WhisperModel(args.model, device=args.device, compute_type=args.compute_type)
    except Exception as exc:
        fail(f"Whisper model warmup failed: {exc}", 20)

    print(f"Whisper model ready: {args.model}")


if __name__ == "__main__":
    main()
