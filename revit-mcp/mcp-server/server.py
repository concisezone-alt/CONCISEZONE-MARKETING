"""
ConciseZone Revit MCP Server
Exposes Revit 2024 model operations as MCP tools for Claude, ChatGPT, and Gemini.
"""
from dotenv import load_dotenv
load_dotenv()

from fastmcp import FastMCP
from typing import Optional
import uuid
import revit_client as rc
import qa_engine as qa

# In-memory cache: check_id → full QA report (survives across tool calls in the session)
_report_cache: dict[str, dict] = {}

mcp = FastMCP(
    name="ConciseZone Revit MCP",
    instructions=(
        "You are connected to a live Revit 2024 model via the ConciseZone MCP server. "
        "You can read elements, rooms, and parameters; modify or add parameters; "
        "read DM standard PDFs; and run QA/QC checks against those standards. "
        "Always confirm destructive changes with the user before applying them. "
        "All parameter changes are recorded in Revit's undo stack."
    )
)


# ── Connection ────────────────────────────────────────────────────────────────

@mcp.tool()
def check_revit_connection() -> dict:
    """Check whether the Revit add-in is running and reachable."""
    return rc.health_check()


# ── Model Info ────────────────────────────────────────────────────────────────

@mcp.tool()
def get_model_info() -> dict:
    """
    Get general information about the open Revit project:
    project name, number, author, Revit version, element count.
    """
    return rc.get_model_info()


@mcp.tool()
def get_levels() -> list:
    """List all levels in the model with their elevations in metres and feet."""
    return rc.get_levels()


@mcp.tool()
def get_model_warnings() -> dict:
    """
    Get all warnings in the Revit model (duplicate marks, overlapping elements, etc.).
    Grouped and counted for easy triage.
    """
    return rc.get_model_warnings()


# ── Elements ──────────────────────────────────────────────────────────────────

@mcp.tool()
def get_elements(
    category: Optional[str] = None,
    level: Optional[str] = None
) -> list:
    """
    List elements in the model.
    category: Walls, Doors, Windows, Floors, Roofs, Ceilings, Columns, Stairs,
              Furniture, MechanicalEquipment, ElectricalEquipment, Plumbing,
              GenericModels, Spaces — leave empty for all.
    level: Filter by level name (e.g. 'Ground Floor').
    """
    return rc.get_elements(category=category, level=level)


@mcp.tool()
def get_element_parameters(element_id: int) -> dict:
    """
    Get all parameters of an element by its Revit integer ID.
    Returns parameter name, group, storage type, value, and read-only status.
    """
    return rc.get_element_parameters(element_id)


@mcp.tool()
def set_element_parameter(element_id: int, parameter_name: str, value: str) -> dict:
    """
    Set a parameter value on an element.
    The change is recorded in Revit's undo stack under 'MCP: Set <param>'.
    value must be a string — numbers are converted automatically.
    """
    return rc.set_parameter(element_id, parameter_name, value)


@mcp.tool()
def add_element_parameter(
    element_id: int,
    parameter_name: str,
    parameter_type: str = "Text",
    group: str = "Identity Data",
    value: Optional[str] = None
) -> dict:
    """
    Add a new shared parameter to an element if it does not already exist, then set it.
    parameter_type: Text, Integer, Number, Length, Area, Volume, URL
    group: The parameter group name (e.g. 'Identity Data', 'Data', 'Constraints')
    value: Optional initial value to set after creating the parameter.
    """
    return rc.add_parameter(element_id, parameter_name, parameter_type, group, value)


# ── Rooms ─────────────────────────────────────────────────────────────────────

@mcp.tool()
def get_rooms(level: Optional[str] = None) -> list:
    """
    Get all placed rooms with: number, name, level, area (sqm + sqft),
    department, occupancy, phase, and IFC export class.
    level: Filter by level name.
    """
    return rc.get_rooms(level=level)


@mcp.tool()
def get_room_parameters(room_id: int) -> dict:
    """
    Get ALL parameters of a specific room by its Revit element ID.
    Useful for deep inspection before QA/QC fixing.
    """
    return rc.get_room_parameters(room_id)


@mcp.tool()
def set_room_parameter(room_id: int, parameter_name: str, value: str) -> dict:
    """
    Set a parameter value on a room element.
    Common parameters: Name, Number, Department, Occupancy, Comments.
    """
    return rc.set_room_parameter(room_id, parameter_name, value)


# ── DM Standards / QA/QC ─────────────────────────────────────────────────────

@mcp.tool()
def list_dm_standards() -> list:
    """
    List all DM standard PDFs available in the standards directory.
    Place your PDFs in the mcp-server/standards/ folder.
    """
    return qa.list_standards()


@mcp.tool()
def load_dm_standard(standard_name: str) -> dict:
    """
    Load and parse a DM standard PDF by filename (without .pdf extension).
    Extracts: text preview, tables, and automatically parsed rules
    (required parameters, naming conventions, IFC classes, completeness rules).
    """
    return qa.load_standard(standard_name)


@mcp.tool()
def search_dm_standard(standard_name: str, query: str) -> dict:
    """
    Search a DM standard PDF for specific terms.
    Returns matching excerpts with page numbers and surrounding context.
    Useful for finding specific clauses before running a check.
    """
    return qa.search_standard(standard_name, query)


@mcp.tool()
def run_qa_check(standard_name: str) -> dict:
    """
    Run a full QA/QC check of the live Revit model against a DM standard.
    Steps:
      1. Loads and parses the PDF rules
      2. Reads rooms and elements from Revit
      3. Checks each rule against the model data
      4. Returns a report of errors, warnings, and suggested fixes
    Each issue includes: element ID, field, current value, and suggestion.
    """
    standard = qa.load_standard(standard_name)
    rules = standard.get("rules", [])
    if not rules:
        return {
            "status": "no_rules_extracted",
            "message": (
                "No rules were automatically extracted from this PDF. "
                "Try search_dm_standard to find relevant clauses, then apply fixes manually."
            ),
            "text_preview": standard.get("text_preview", "")
        }
    report = qa.run_full_check(rules)
    report["standard"] = standard_name
    report["rules_checked"] = len(rules)

    # Cache the report so apply_qa_fix / apply_all_fixable_issues can reference it
    check_id = str(uuid.uuid4())[:8]
    report["check_id"] = check_id
    _report_cache[check_id] = report
    return report


@mcp.tool()
def apply_qa_fix(
    element_id: int,
    field: str,
    new_value: str,
    rule_id: Optional[str] = None
) -> dict:
    """
    Apply a single QA/QC fix to an element: set or add a parameter.
    element_id: Revit element integer ID from the QA report
    field: Parameter name to fix
    new_value: The correct value to set
    rule_id: Optional — for reference/tracking only
    """
    issue = {
        "element_id": element_id,
        "field": field,
        "fixable": True,
        "category": "manual_fix",
        "issue_id": f"MANUAL-{element_id}-{field}",
        "rule_id": rule_id or "manual"
    }
    return qa.apply_fix(issue, new_value=new_value)


@mcp.tool()
def get_qa_report(check_id: str) -> dict:
    """
    Retrieve a previously run QA report by its check_id.
    check_id is returned in the run_qa_check response.
    Useful for resuming a fix session without re-running the full check.
    """
    report = _report_cache.get(check_id)
    if not report:
        available = list(_report_cache.keys())
        return {"error": f"No report found for check_id '{check_id}'.", "available": available}
    return report


@mcp.tool()
def apply_all_fixable_issues(issues: list, value_overrides: Optional[dict] = None) -> list:
    """
    Batch-apply all auto-fixable issues from a QA report.
    issues: The 'issues' list from run_qa_check output (only fixable=true items are processed)
    value_overrides: Optional {issue_id: value} to override auto-derived values
    Returns per-issue success/failure results.
    """
    fixable = [i for i in issues if i.get("fixable")]
    return qa.apply_batch_fixes(fixable, value_map=value_overrides or {})


# ── Entry point ───────────────────────────────────────────────────────────────

if __name__ == "__main__":
    mcp.run(transport="stdio")
