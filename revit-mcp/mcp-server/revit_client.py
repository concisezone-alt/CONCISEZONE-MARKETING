import httpx
import os
from typing import Any

_HOST = os.getenv("REVITMCP_HOST", "http://localhost:7777")
_TOKEN = os.getenv("REVITMCP_TOKEN", "changeme")
_HEADERS = {"Authorization": f"Bearer {_TOKEN}", "Content-Type": "application/json"}


def _client() -> httpx.Client:
    return httpx.Client(base_url=_HOST, headers=_HEADERS, timeout=30.0)


def get(path: str, params: dict = None) -> Any:
    with _client() as c:
        r = c.get(path, params=params)
        r.raise_for_status()
        return r.json()


def post(path: str, body: dict) -> Any:
    with _client() as c:
        r = c.post(path, json=body)
        r.raise_for_status()
        return r.json()


def health_check() -> dict:
    try:
        return get("/health")
    except Exception as e:
        return {"status": "error", "detail": str(e)}


# ── element queries ──────────────────────────────────────────────────────────

def get_elements(category: str = None, level: str = None) -> list:
    return get("/elements", params={k: v for k, v in [("category", category), ("level", level)] if v})


def get_element_parameters(element_id: int) -> dict:
    return get(f"/element/{element_id}/parameters")


def set_parameter(element_id: int, name: str, value: str) -> dict:
    return post(f"/element/{element_id}/set-parameter", {"name": name, "value": value})


def add_parameter(element_id: int, name: str, param_type: str = "Text",
                  group: str = "Identity Data", value: str = None,
                  shared_param_file: str = None) -> dict:
    body = {"name": name, "type": param_type, "group": group}
    if value is not None:
        body["value"] = value
    if shared_param_file:
        body["shared_param_file"] = shared_param_file
    return post(f"/element/{element_id}/add-parameter", body)


# ── room queries ─────────────────────────────────────────────────────────────

def get_rooms(level: str = None) -> list:
    return get("/rooms", params={"level": level} if level else None)


def get_room_parameters(room_id: int) -> dict:
    return get(f"/room/{room_id}/parameters")


def set_room_parameter(room_id: int, name: str, value: str) -> dict:
    return post(f"/room/{room_id}/set-parameter", {"name": name, "value": value})


# ── model queries ─────────────────────────────────────────────────────────────

def get_model_info() -> dict:
    return get("/model/info")


def get_levels() -> list:
    return get("/model/levels")


def get_model_warnings() -> dict:
    return get("/model/warnings")
