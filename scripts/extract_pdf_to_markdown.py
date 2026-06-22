"""Extract a PDF into page-ordered Markdown using the embedded text layer."""

from __future__ import annotations

import argparse
from pathlib import Path

import pdfplumber
from pypdf import PdfReader


def extract_page_text(pdf_page: pdfplumber.page.Page, fallback_page, preserve_layout: bool) -> str:
    """Prefer pdfplumber's position-ordered text, then fall back to pypdf."""
    text = pdf_page.extract_text(layout=preserve_layout, x_tolerance=1, y_tolerance=3) or ""
    if not text.strip():
        text = fallback_page.extract_text(extraction_mode="layout") or ""
    return text.replace("\r\n", "\n").replace("\r", "\n").strip()


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input_pdf", type=Path)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--preserve-layout", action="store_true", help="Preserve source line placement where possible.")
    args = parser.parse_args()

    input_pdf = args.input_pdf.resolve()
    output_md = args.out.resolve()
    if not input_pdf.is_file():
        raise FileNotFoundError(f"Input PDF was not found: {input_pdf}")

    reader = PdfReader(str(input_pdf))
    output_md.parent.mkdir(parents=True, exist_ok=True)
    pages_with_text = 0
    extracted_characters = 0
    markdown = [
        f"# {input_pdf.stem}",
        "",
        f"Source PDF: `{input_pdf.name}`",
        f"Pages: {len(reader.pages)}",
        "",
        "The following text is extracted in PDF page order. Page headings preserve the source-page boundary.",
    ]

    with pdfplumber.open(input_pdf) as document:
        if len(document.pages) != len(reader.pages):
            raise RuntimeError("PDF readers disagree on the number of pages.")

        for page_number, (pdf_page, fallback_page) in enumerate(zip(document.pages, reader.pages), start=1):
            text = extract_page_text(pdf_page, fallback_page, args.preserve_layout)
            if text:
                pages_with_text += 1
                extracted_characters += len(text)
            else:
                text = "[No embedded text was extracted from this page.]"

            markdown.extend(["", "---", "", f"## Page {page_number}", "", text])

    output_md.write_text("\n".join(markdown) + "\n", encoding="utf-8")
    print(f"pages={len(reader.pages)}")
    print(f"pages_with_text={pages_with_text}")
    print(f"extracted_characters={extracted_characters}")
    print(f"output={output_md}")


if __name__ == "__main__":
    main()
