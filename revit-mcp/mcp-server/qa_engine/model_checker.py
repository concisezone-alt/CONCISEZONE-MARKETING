"""
Check a Revit model against extracted DM standard rules.
Produces a list of issues with severity, location, and suggested fix.
"""
import re
from typing import Any
import revit_client as rc


SEVERITY = {"error": 3, "warning": 2, "info": 1}


def run_full_check(rules: list[dict]) -> dict:
    """
    Run all rules against the live Revit model.
    Returns a structured report with issues grouped by severity.
    """
    issues = []

    # Gather model data once
    try:
        rooms = rc.get_rooms()
    except Exception as e:
        return {"error": f"Cannot connect to Revit: {e}"}

    try:
        levels = rc.get_levels()
    except Exception:
        levels = []

    for rule in rules:
        targets = rule.get("targets", ["all"])
        category = rule.get("category")

        if "Rooms" in targets or "all" in targets:
            issues.extend(_check_rooms(rule, rooms))

        if category == "ifc_class" and ("all" in targets or "Rooms" in targets):
            issues.extend(_check_ifc_rooms(rule, rooms))

    # Sort by severity desc, then by room number
    issues.sort(key=lambda i: (-SEVERITY.get(i["severity"], 0), i.get("element_number", "")))

    return {
        "total_issues": len(issues),
        "errors": sum(1 for i in issues if i["severity"] == "error"),
        "warnings": sum(1 for i in issues if i["severity"] == "warning"),
        "infos": sum(1 for i in issues if i["severity"] == "info"),
        "issues": issues
    }


def _check_rooms(rule: dict, rooms: list) -> list[dict]:
    issues = []
    category = rule.get("category")
    field = rule.get("field")
    pattern = rule.get("pattern")
    required_value = rule.get("required_value")

    for room in rooms:
        elem_id = room["id"]
        elem_number = room.get("number", "")
        elem_name = room.get("name", "")

        if category == "required_parameter":
            # Check via detailed parameter call would be expensive for every room;
            # check the common fields we already have first
            val = _get_room_field(room, field)
            if val is None or val.strip() == "":
                issues.append(_issue(
                    rule, "error", elem_id, elem_number, elem_name,
                    f"Missing required parameter '{field}'",
                    f"Set '{field}' to a valid value",
                    field, ""
                ))

        elif category == "naming_convention" and pattern:
            if not _matches_pattern(elem_name, pattern):
                issues.append(_issue(
                    rule, "warning", elem_id, elem_number, elem_name,
                    f"Room name '{elem_name}' does not match convention '{pattern}'",
                    f"Rename room to follow pattern: {pattern}",
                    "Name", elem_name
                ))

        elif category == "room_number_format" and pattern:
            if not _matches_pattern(elem_number, pattern):
                issues.append(_issue(
                    rule, "warning", elem_id, elem_number, elem_name,
                    f"Room number '{elem_number}' does not match format '{pattern}'",
                    f"Update room number to format: {pattern}",
                    "Number", elem_number
                ))

        elif category == "completeness":
            val = _get_room_field(room, field)
            if val is None or val.strip() == "":
                issues.append(_issue(
                    rule, "warning", elem_id, elem_number, elem_name,
                    f"Room missing '{field}'",
                    f"Assign a value to '{field}'",
                    field, ""
                ))

    return issues


def _check_ifc_rooms(rule: dict, rooms: list) -> list[dict]:
    issues = []
    required_class = rule.get("required_value", "")
    for room in rooms:
        ifc_val = room.get("ifc_export_element", "")
        if required_class and ifc_val and not required_class.lower() in ifc_val.lower():
            issues.append(_issue(
                rule, "warning",
                room["id"], room.get("number", ""), room.get("name", ""),
                f"IFC export class '{ifc_val}' should be '{required_class}'",
                f"Change IFC export class to '{required_class}'",
                "IFC Export Class", ifc_val
            ))
    return issues


def _get_room_field(room: dict, field: str) -> str | None:
    if not field:
        return None
    field_lower = field.lower().replace(" ", "_").replace("-", "_")
    # Try direct dict key
    if field_lower in room:
        return str(room[field_lower])
    # Try without underscores
    for key in room:
        if key.lower().replace("_", "") == field_lower.replace("_", ""):
            return str(room[key])
    return None


def _matches_pattern(value: str, pattern: str) -> bool:
    """Try to match value against pattern — supports regex and wildcard."""
    if not value or not pattern:
        return False
    try:
        # Convert simple wildcards to regex
        regex = pattern.replace("*", ".*").replace("?", ".")
        return bool(re.match(f"^{regex}$", value, re.IGNORECASE))
    except re.error:
        return pattern.lower() in value.lower()


def _issue(rule: dict, severity: str, elem_id: int, elem_number: str,
           elem_name: str, description: str, suggestion: str,
           field: str, current_value: str) -> dict:
    return {
        "issue_id": f"ISSUE-{elem_id}-{rule['id']}",
        "rule_id": rule["id"],
        "severity": severity,
        "element_id": elem_id,
        "element_number": elem_number,
        "element_name": elem_name,
        "category": rule.get("category"),
        "description": description,
        "suggestion": suggestion,
        "field": field,
        "current_value": current_value,
        "fixable": field not in (None, "")
    }
