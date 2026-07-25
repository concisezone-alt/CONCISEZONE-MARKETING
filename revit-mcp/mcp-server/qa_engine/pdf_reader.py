"""
Read DM standard PDFs and extract structured rules for QA/QC checking.
Place your DM standard PDFs in the ./standards/ directory.
"""
import os
import re
import json
from pathlib import Path
import pdfplumber

STANDARDS_DIR = Path(os.getenv("STANDARDS_DIR", "./standards"))


def list_standards() -> list[dict]:
    """Return all PDF standards available in the standards directory."""
    if not STANDARDS_DIR.exists():
        return []
    return [
        {"name": p.stem, "filename": p.name, "size_kb": round(p.stat().st_size / 1024, 1)}
        for p in sorted(STANDARDS_DIR.glob("*.pdf"))
    ]


def read_pdf_text(pdf_path: Path, page_range: tuple[int, int] = None) -> str:
    """Extract full text from a PDF, optionally limited to a page range."""
    text_parts = []
    with pdfplumber.open(pdf_path) as pdf:
        pages = pdf.pages
        if page_range:
            start, end = page_range
            pages = pages[start - 1:end]
        for page in pages:
            t = page.extract_text()
            if t:
                text_parts.append(t)
    return "\n".join(text_parts)


def read_pdf_tables(pdf_path: Path) -> list[dict]:
    """Extract all tables from a PDF as list of dicts."""
    tables = []
    with pdfplumber.open(pdf_path) as pdf:
        for i, page in enumerate(pdf.pages, 1):
            for table in page.extract_tables():
                if not table or len(table) < 2:
                    continue
                headers = [str(h or "").strip() for h in table[0]]
                rows = []
                for row in table[1:]:
                    rows.append({
                        headers[j]: str(cell or "").strip()
                        for j, cell in enumerate(row)
                        if j < len(headers)
                    })
                tables.append({"page": i, "headers": headers, "rows": rows})
    return tables


def extract_rules_from_text(text: str, standard_name: str) -> list[dict]:
    """
    Parse common DM standard patterns and extract rules.
    Looks for: required parameters, naming conventions, IFC classes,
    space classifications, data requirements.
    """
    rules = []
    rule_id = 1

    def add(category, description, targets, field=None, pattern=None, required_value=None):
        nonlocal rule_id
        rules.append({
            "id": f"{standard_name}-{rule_id:03d}",
            "category": category,
            "description": description,
            "targets": targets,
            "field": field,
            "pattern": pattern,
            "required_value": required_value
        })
        rule_id += 1

    # ── Required parameter detection ──────────────────────────────────────────
    param_patterns = [
        r"(?:shall|must|required to) (?:have|include|contain) (?:a |an |the )?['\"]?([A-Za-z][A-Za-z0-9 _\-]{1,60})['\"]? parameter",
        r"parameter[:\s]+['\"]?([A-Za-z][A-Za-z0-9 _\-]{1,60})['\"]?\s+(?:is required|shall be provided|must be)",
        r"['\"]([A-Za-z][A-Za-z0-9_\- ]{2,40})['\"] shall be populated",
    ]
    for pat in param_patterns:
        for m in re.finditer(pat, text, re.IGNORECASE):
            add("required_parameter", f"Parameter '{m.group(1)}' must be present and populated",
                ["all"], field=m.group(1))

    # ── Room naming conventions ───────────────────────────────────────────────
    naming_patterns = [
        r"room (?:name|names) (?:shall|must|should) (?:follow|use|be) ['\"]?([A-Z][A-Z0-9_\- ]{2,40})['\"]?",
        r"(?:naming|name) convention[:\s]+([A-Z][A-Za-z0-9_\- \[\]{}]{3,80})",
        r"spaces? (?:shall|must) be named (?:as )?['\"]?([A-Z][A-Za-z0-9_\- ]{2,40})['\"]?",
    ]
    for pat in naming_patterns:
        for m in re.finditer(pat, text, re.IGNORECASE):
            add("naming_convention", f"Room naming must follow: {m.group(1)}",
                ["Rooms"], field="Name", pattern=m.group(1))

    # ── IFC class requirements ────────────────────────────────────────────────
    ifc_patterns = [
        r"(?:exported?|classified) as (Ifc[A-Za-z]+)",
        r"IFC (?:class|type|entity)[:\s]+(Ifc[A-Za-z]+)",
        r"(Ifc[A-Za-z]+)\s+(?:shall|must) be used for",
    ]
    for pat in ifc_patterns:
        for m in re.finditer(pat, text, re.IGNORECASE):
            add("ifc_class", f"IFC export class: {m.group(1)}",
                ["all"], field="IFC Export Class", required_value=m.group(1))

    # ── Room number format ────────────────────────────────────────────────────
    number_patterns = [
        r"room number[s]? (?:shall|must|should) (?:be )?(?:in )?(?:the )?format[:\s]+['\"]?([A-Z0-9\-\.\[\]]{2,20})['\"]?",
        r"(?:space|room) (?:ID|number|numbering)[:\s]+([A-Z]{0,3}[0-9\-\.]{2,15})",
    ]
    for pat in number_patterns:
        for m in re.finditer(pat, text, re.IGNORECASE):
            add("room_number_format", f"Room number format: {m.group(1)}",
                ["Rooms"], field="Number", pattern=m.group(1))

    # ── Completeness rules ────────────────────────────────────────────────────
    completeness_patterns = [
        r"all (?:rooms?|spaces?) (?:shall|must) have (?:a |an )?['\"]?([A-Za-z][A-Za-z0-9 _\-]{1,60})['\"]?",
        r"(?:department|occupancy|function) (?:shall|must|is required) (?:be )?(?:assigned|populated|provided)",
    ]
    for pat in completeness_patterns:
        for m in re.finditer(pat, text, re.IGNORECASE):
            field = m.group(1) if m.lastindex else "Department"
            add("completeness", f"All rooms must have '{field}' assigned",
                ["Rooms"], field=field)

    return rules


def load_standard(standard_name: str) -> dict:
    """
    Load and parse a DM standard PDF by name (without .pdf extension).
    Returns extracted text, tables, and parsed rules.
    """
    pdf_path = STANDARDS_DIR / f"{standard_name}.pdf"
    if not pdf_path.exists():
        available = [p.stem for p in STANDARDS_DIR.glob("*.pdf")]
        raise FileNotFoundError(
            f"Standard '{standard_name}' not found. Available: {available or ['none — upload PDFs to ./standards/']}"
        )

    text = read_pdf_text(pdf_path)
    tables = read_pdf_tables(pdf_path)
    rules = extract_rules_from_text(text, standard_name)

    return {
        "name": standard_name,
        "page_count": len(list(pdfplumber.open(pdf_path).pages)),
        "text_length": len(text),
        "table_count": len(tables),
        "rule_count": len(rules),
        "rules": rules,
        "tables": tables,
        "text_preview": text[:2000]
    }


def search_standard(standard_name: str, query: str) -> dict:
    """Search a standard PDF for specific terms and return matching excerpts."""
    pdf_path = STANDARDS_DIR / f"{standard_name}.pdf"
    if not pdf_path.exists():
        raise FileNotFoundError(f"Standard '{standard_name}' not found.")

    query_lower = query.lower()
    results = []

    with pdfplumber.open(pdf_path) as pdf:
        for i, page in enumerate(pdf.pages, 1):
            text = page.extract_text() or ""
            if query_lower in text.lower():
                # Extract surrounding context (±200 chars)
                idx = text.lower().find(query_lower)
                start = max(0, idx - 200)
                end = min(len(text), idx + len(query) + 200)
                results.append({
                    "page": i,
                    "excerpt": text[start:end].strip(),
                    "char_position": idx
                })

    return {
        "standard": standard_name,
        "query": query,
        "match_count": len(results),
        "matches": results[:10]  # cap at 10 results
    }
