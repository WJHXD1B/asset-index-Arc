#!/usr/bin/env python3
"""Print issue counts without publishing private extraction diagnostics."""
import json
from pathlib import Path
import sys

STAGES = (
    "asset", "decode", "evidence", "fatal", "image", "inventory", "metadata", "package", "parser", "presentation",
    "reference", "registry", "schema", "text", "validation", "visual-slot", "other",
)
UNAVAILABLE = "Extraction issue counts unavailable."


def summarize(report):
    if not isinstance(report, dict) or not isinstance(report.get("issues"), list):
        return UNAVAILABLE
    counts = dict.fromkeys(STAGES, 0)
    for issue in report["issues"]:
        if not isinstance(issue, dict) or not isinstance(issue.get("stage"), str):
            return UNAVAILABLE
        stage = issue["stage"]
        counts[stage if stage in counts else "other"] += 1
    summary = ", ".join(f"{stage}={count}" for stage, count in counts.items() if count)
    return "Extraction issue counts: " + (summary or "none") + "."


def main():
    try:
        report = json.loads(Path(sys.argv[1]).read_text())
    except (OSError, UnicodeError, ValueError, RecursionError):
        print(UNAVAILABLE)
        return
    print(summarize(report))


if __name__ == "__main__":
    main()
