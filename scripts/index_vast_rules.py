"""Create a page-indexed rule-candidate report from the Vast Markdown source."""

from __future__ import annotations

import argparse
import re
from pathlib import Path


PAGE_HEADING = re.compile(r"^## Page ([0-9]+)$", re.MULTILINE)
RULE_SIGNAL = re.compile(
    r"\b(?:must|may|roll|each|when|if|unless|gain|lose|restore|damage|travel|rest|turn|round|day|hour|check|save|attack|defend|move|encounter)\b",
    re.IGNORECASE,
)


def pages_from_markdown(text: str) -> list[tuple[int, str]]:
    matches = list(PAGE_HEADING.finditer(text))
    pages: list[tuple[int, str]] = []
    for index, match in enumerate(matches):
        end = matches[index + 1].start() if index + 1 < len(matches) else len(text)
        pages.append((int(match.group(1)), text[match.end() : end].strip()))
    return pages


def heading_candidates(lines: list[str]) -> list[str]:
    candidates: list[str] = []
    for line in lines:
        compact = line.strip()
        words = compact.split()
        if not compact or len(compact) > 72 or len(words) > 10 or compact.endswith((".", ",", ";", ":")):
            continue
        uppercase = sum(character.isupper() for character in compact)
        letters = sum(character.isalpha() for character in compact)
        title_words = sum(word[:1].isupper() for word in words)
        if (letters and uppercase / letters > 0.5) or title_words >= max(2, len(words) - 1):
            candidates.append(compact)
    return candidates[:8]


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("--out", required=True, type=Path)
    args = parser.parse_args()

    source = args.source.resolve()
    output = args.out.resolve()
    pages = pages_from_markdown(source.read_text(encoding="utf-8"))
    if [number for number, _ in pages] != list(range(1, len(pages) + 1)):
        raise RuntimeError("Source page headings are missing or out of order.")

    report = ["# Vast Rules: Mechanical Page Index", "", f"Source: `{source.name}`", f"Pages: {len(pages)}"]
    for page_number, text in pages:
        lines = [line for line in text.splitlines() if line.strip()]
        rule_lines = [line.strip() for line in lines if RULE_SIGNAL.search(line)]
        report.extend(["", f"## Page {page_number}", f"- Non-empty lines: {len(lines)}", f"- Rule-signal lines: {len(rule_lines)}"])
        for candidate in heading_candidates(lines):
            report.append(f"- Heading candidate: {candidate}")
        for rule_line in rule_lines[:12]:
            report.append(f"- Signal: {rule_line}")

    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text("\n".join(report) + "\n", encoding="utf-8")
    print(f"pages={len(pages)}")
    print(f"output={output}")


if __name__ == "__main__":
    main()
