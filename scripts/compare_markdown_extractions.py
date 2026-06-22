"""Compare Markdown PDF extractions without relying on manual reading."""

from __future__ import annotations

import argparse
import difflib
import re
from dataclasses import dataclass
from pathlib import Path


PAGE_HEADING = re.compile(r"^## Page ([0-9]+)$", re.MULTILINE)
WHITESPACE = re.compile(r"\s+")


@dataclass
class Extraction:
    path: Path
    text: str
    pages: dict[int, str]

    @property
    def normalized(self) -> str:
        return WHITESPACE.sub(" ", self.text).strip()


def load(path: Path) -> Extraction:
    text = path.read_text(encoding="utf-8")
    matches = list(PAGE_HEADING.finditer(text))
    pages: dict[int, str] = {}
    for index, match in enumerate(matches):
        end = matches[index + 1].start() if index + 1 < len(matches) else len(text)
        pages[int(match.group(1))] = text[match.end() : end].strip()
    return Extraction(path, text, pages)


def similarity(left: str, right: str) -> float:
    return difflib.SequenceMatcher(None, WHITESPACE.sub(" ", left).split(), WHITESPACE.sub(" ", right).split(), autojunk=True).ratio()


def token_overlap(left: str, right: str) -> float:
    left_tokens = set(WHITESPACE.sub(" ", left).casefold().split())
    right_tokens = set(WHITESPACE.sub(" ", right).casefold().split())
    return len(left_tokens & right_tokens) / len(left_tokens | right_tokens)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("files", nargs=3, type=Path)
    args = parser.parse_args()

    extractions = [load(path.resolve()) for path in args.files]
    for extraction in extractions:
        print(f"file={extraction.path}")
        print(f"characters={len(extraction.text)}")
        print(f"page_headings={len(extraction.pages)}")
        print(f"lines={extraction.text.count(chr(10)) + 1}")
        print(f"bullet_lines={sum(1 for line in extraction.text.splitlines() if re.match(r'^[-*+] ', line))}")
        print(f"numbered_lines={sum(1 for line in extraction.text.splitlines() if re.match(r'^[0-9]+[.)] ', line))}")
        print(f"table_lines={sum(1 for line in extraction.text.splitlines() if line.startswith('|'))}")

    reference = extractions[0]
    for candidate in extractions[1:]:
        print(f"token_overlap_to_{candidate.path.name}={token_overlap(reference.text, candidate.text):.4f}")

    if reference.pages:
        for candidate in extractions[1:]:
            if not candidate.pages:
                continue
            shared_pages = sorted(set(reference.pages) & set(candidate.pages))
            per_page = [similarity(reference.pages[number], candidate.pages[number]) for number in shared_pages]
            if per_page:
                print(f"shared_pages_with_{candidate.path.name}={len(shared_pages)}")
                print(f"mean_page_similarity_to_{candidate.path.name}={sum(per_page) / len(per_page):.4f}")
                for number, score in zip(shared_pages, per_page):
                    if score < 0.85:
                        print(f"low_similarity_page_with_{candidate.path.name}={number}:{score:.4f}")


if __name__ == "__main__":
    main()
