"""Adopt a structurally better page-ordered Markdown extraction safely."""

from __future__ import annotations

import argparse
import re
from pathlib import Path


PAGE_HEADING = re.compile(r"^## Page ([0-9]+)$", re.MULTILINE)


def metrics(text: str) -> tuple[list[int], int, int]:
    pages = [int(number) for number in PAGE_HEADING.findall(text)]
    list_lines = sum(1 for line in text.splitlines() if re.match(r"^(?:[-*+] |[0-9]+[.)] )", line))
    return pages, len(text), list_lines


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("reference", type=Path)
    parser.add_argument("target", type=Path)
    args = parser.parse_args()

    reference = args.reference.resolve()
    target = args.target.resolve()
    reference_text = reference.read_text(encoding="utf-8")
    target_text = target.read_text(encoding="utf-8")
    reference_pages, reference_characters, reference_list_lines = metrics(reference_text)
    target_pages, target_characters, target_list_lines = metrics(target_text)

    if not reference_pages or reference_pages != target_pages:
        raise RuntimeError("Reference and target do not have the same ordered page boundaries.")
    if reference_characters < target_characters * 0.95:
        raise RuntimeError("Reference text coverage is materially lower than the target.")
    if reference_list_lines <= target_list_lines:
        raise RuntimeError("Reference does not preserve more list structure than the target.")

    target.write_text(reference_text, encoding="utf-8", newline="\n")
    print(f"reference_pages={len(reference_pages)}")
    print(f"reference_characters={reference_characters}")
    print(f"reference_list_lines={reference_list_lines}")
    print(f"previous_target_characters={target_characters}")
    print(f"previous_target_list_lines={target_list_lines}")
    print(f"updated={target}")


if __name__ == "__main__":
    main()
