#!/usr/bin/env python3
import argparse
import os
import sys


def fail(message: str, code: int = 1) -> None:
    print(message, file=sys.stderr)
    raise SystemExit(code)


def main() -> None:
    parser = argparse.ArgumentParser(description="Transcribe an audio file with faster-whisper.")
    parser.add_argument("--file", required=True, help="Audio file path.")
    parser.add_argument("--language", default="tr", help="Language hint, for example tr or en.")
    parser.add_argument("--model", default=os.getenv("V3RII_WHISPER_MODEL", "small"), help="Whisper model size or path.")
    parser.add_argument("--device", default=os.getenv("V3RII_WHISPER_DEVICE", "cpu"), help="cpu, cuda, or auto.")
    parser.add_argument("--compute-type", default=os.getenv("V3RII_WHISPER_COMPUTE_TYPE", "int8"), help="int8, float16, float32, ...")
    args = parser.parse_args()

    if not os.path.isfile(args.file):
        fail(f"Audio file not found: {args.file}", 2)

    try:
        from faster_whisper import WhisperModel
    except Exception as exc:
        fail(
            "faster-whisper is not installed. Run Scripts/Voice/install-whisper.ps1 "
            "or Scripts/Voice/install-whisper.sh on the API server. "
            f"Import error: {exc}",
            12,
        )

    language = (args.language or "").lower()
    if language not in {"tr", "en"}:
        language = None

    try:
        model = WhisperModel(args.model, device=args.device, compute_type=args.compute_type)
        segments, _ = model.transcribe(
            args.file,
            language=language,
            beam_size=5,
            vad_filter=True,
            vad_parameters={"min_silence_duration_ms": 450},
        )
        text = " ".join(segment.text.strip() for segment in segments).strip()
    except Exception as exc:
        fail(f"Whisper transcription failed: {exc}", 20)

    print(text)


if __name__ == "__main__":
    main()
