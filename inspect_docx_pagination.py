from pathlib import Path
from zipfile import ZipFile


def main() -> None:
    path = Path(r"C:\Users\Lenovo\Desktop\cs183-2026-report-template-word.docx")
    with ZipFile(path) as archive:
        for name in archive.namelist():
            if not name.startswith("word/") or not name.endswith(".xml"):
                continue
            xml = archive.read(name).decode("utf-8", errors="ignore")
            counts = {
                "page_breaks": xml.count('w:type="page"'),
                "last_rendered_page_breaks": xml.count("w:lastRenderedPageBreak"),
                "section_properties": xml.count("<w:sectPr"),
                "page_break_before": xml.count("w:pageBreakBefore"),
                "keep_next": xml.count("w:keepNext"),
                "keep_lines": xml.count("w:keepLines"),
                "floating_drawings": xml.count("<wp:anchor"),
                "inline_drawings": xml.count("<wp:inline"),
            }
            if any(counts.values()):
                print(name, counts)


if __name__ == "__main__":
    main()
