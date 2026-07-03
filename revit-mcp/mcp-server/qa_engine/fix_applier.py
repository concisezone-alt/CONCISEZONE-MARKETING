"""
Apply QA/QC fixes to the Revit model based on issue reports.
Each fix calls revit_client to set/add parameters on the affected element.
"""
import revit_client as rc


def apply_fix(issue: dict, new_value: str = None) -> dict:
    """
    Apply a single fix to the model.
    If new_value is not supplied, uses the suggestion logic to derive one.
    Returns success/failure result.
    """
    elem_id = issue.get("element_id")
    field = issue.get("field")
    category = issue.get("category")

    if not elem_id or not field:
        return {"success": False, "reason": "Issue missing element_id or field — cannot auto-fix."}

    value = new_value or _derive_value(issue)
    if not value:
        return {"success": False, "reason": "No value provided and cannot auto-derive one."}

    try:
        # Try set first (most common)
        result = rc.set_parameter(elem_id, field, value)
        if result.get("success"):
            return {
                "success": True,
                "action": "set_parameter",
                "element_id": elem_id,
                "field": field,
                "new_value": value
            }
    except Exception as set_err:
        # Parameter might not exist yet — try adding it
        if "not found" in str(set_err).lower():
            try:
                result = rc.add_parameter(elem_id, field, param_type="Text", value=value)
                return {
                    "success": True,
                    "action": "add_parameter",
                    "element_id": elem_id,
                    "field": field,
                    "new_value": value
                }
            except Exception as add_err:
                return {"success": False, "reason": f"Set failed: {set_err}. Add failed: {add_err}"}
        return {"success": False, "reason": str(set_err)}


def apply_batch_fixes(issues: list[dict], value_map: dict = None) -> list[dict]:
    """
    Apply fixes to multiple issues.
    value_map: {issue_id: new_value} for overriding auto-derived values.
    """
    results = []
    for issue in issues:
        if not issue.get("fixable", False):
            results.append({
                "issue_id": issue["issue_id"],
                "success": False,
                "reason": "Not auto-fixable"
            })
            continue

        override_value = (value_map or {}).get(issue["issue_id"])
        result = apply_fix(issue, new_value=override_value)
        result["issue_id"] = issue["issue_id"]
        results.append(result)

    return results


def _derive_value(issue: dict) -> str | None:
    """Attempt to derive a sensible default value for an issue."""
    category = issue.get("category")
    field = issue.get("field", "").lower()

    if category == "completeness":
        if "department" in field:
            return "Unassigned"
        if "occupancy" in field:
            return "Unoccupied"
        if "status" in field:
            return "Active"

    if category == "ifc_class":
        return issue.get("required_value") or "IfcSpace"

    # Cannot auto-derive
    return None
