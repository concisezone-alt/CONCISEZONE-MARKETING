# ConciseZone Revit MCP

Full production MCP server connecting Claude (and other AI) to Autodesk Revit 2024.

## System Overview

```
Claude Desktop / ChatGPT / Gemini
         ↕ MCP protocol
   mcp-server/server.py          ← Python, FastMCP
         ↕ HTTP localhost:7777
   revit-addin/ (C# .NET 4.8)   ← Revit 2024 Add-in
         ↕ Revit API
      Revit 2024
```

---

## Part 1 — Revit Add-in (C#)

### Requirements
- Revit 2024 installed at `C:\Program Files\Autodesk\Revit 2024\`
- Visual Studio 2022 or Rider
- .NET 4.8 SDK

### Build
```bash
cd revit-addin
dotnet restore
dotnet build -c Release
```

### Install
1. Copy `bin/Release/net48/RevitMCP.Addin.dll` to a folder, e.g. `C:\RevitMCP\`
2. Copy `RevitMCP.addin` to:
   ```
   C:\ProgramData\Autodesk\Revit\Addins\2024\RevitMCP.addin
   ```
3. Edit `RevitMCP.addin` — set `<Assembly>` to the full path of the DLL
4. Set the auth token (optional, defaults to `changeme`):
   ```
   REVITMCP_TOKEN=your-secret-token
   ```
   Set this as a Windows environment variable or in a `.env` file beside the DLL.
5. Start Revit 2024 — the add-in loads automatically and starts the HTTP server on port 7777.

### Verify add-in is running
```
curl -H "Authorization: Bearer changeme" http://localhost:7777/health
```
Expected: `{"status":"ok","version":"1.0.0"}`

---

## Part 2 — Python MCP Server

### Requirements
- Python 3.11+
- pip

### Setup
```bash
cd mcp-server
python -m venv .venv
.venv\Scripts\activate        # Windows
pip install -r requirements.txt
cp .env.example .env
# Edit .env — set REVITMCP_TOKEN to match the add-in token
```

### Run (standalone test)
```bash
python server.py
```

---

## Part 3 — Connect to Claude Desktop

1. Open Claude Desktop → Settings → Developer → Edit Config
2. Add the contents of `claude_desktop_config.json` (update the paths)
3. Restart Claude Desktop
4. You should see "revit" in the MCP tools panel

### Test in Claude
> "Check the Revit connection"
> "Show me all rooms on Ground Floor"
> "What are the parameters of room 12345?"
> "Run a QA check against the DM standard"

---

## Part 4 — QA/QC with DM Standards

### Setup
1. Copy your DM standard PDFs to `mcp-server/standards/`
   - Example: `standards/DM_Room_Naming_Standard_2024.pdf`
2. In Claude, say:
   > "List my DM standards"
   > "Load the DM_Room_Naming_Standard_2024 standard"
   > "Run a QA check against DM_Room_Naming_Standard_2024"

### What the QA engine checks automatically
| Rule Type | What it detects |
|-----------|----------------|
| `required_parameter` | Parameters that must be present and populated |
| `naming_convention` | Room names that don't match a pattern |
| `room_number_format` | Room numbers in wrong format |
| `ifc_class` | Elements exported to wrong IFC class |
| `completeness` | Missing Department, Occupancy, etc. |

### Fix workflow
1. Run `run_qa_check("your-standard-name")` → get issue list
2. Review issues with AI explanation
3. Run `apply_all_fixable_issues(issues)` to batch-fix OR
4. Run `apply_qa_fix(element_id, field, new_value)` for individual fixes
5. All fixes go into Revit's undo stack — fully reversible

---

## Available MCP Tools

### Connection
| Tool | Description |
|------|-------------|
| `check_revit_connection` | Ping the Revit add-in |

### Model
| Tool | Description |
|------|-------------|
| `get_model_info` | Project name, number, author, Revit version |
| `get_levels` | All levels with elevation (m + ft) |
| `get_model_warnings` | Revit warnings grouped and counted |

### Elements
| Tool | Description |
|------|-------------|
| `get_elements` | List elements by category and/or level |
| `get_element_parameters` | All parameters of an element |
| `set_element_parameter` | Change a parameter value |
| `add_element_parameter` | Add a new shared parameter and optionally set it |

### Rooms
| Tool | Description |
|------|-------------|
| `get_rooms` | All rooms with area, department, phase, IFC class |
| `get_room_parameters` | All parameters of a specific room |
| `set_room_parameter` | Change a room parameter |

### QA/QC
| Tool | Description |
|------|-------------|
| `list_dm_standards` | List available PDF standards |
| `load_dm_standard` | Parse a PDF and extract rules |
| `search_dm_standard` | Search PDF for a specific term |
| `run_qa_check` | Full model check against a standard |
| `apply_qa_fix` | Fix a single issue |
| `apply_all_fixable_issues` | Batch-fix all auto-fixable issues |

---

## Security

- All requests require `Authorization: Bearer <token>` header
- Server only listens on `localhost` — not accessible from network
- Revit API calls run on the Revit main thread via `ExternalEvent` — no race conditions
- All write operations use Revit Transactions — fully undoable

---

## Logs

Add-in log: `%APPDATA%\RevitMCP\revitmcp.log`

---

## ChatGPT / Gemini

These AI systems don't speak MCP natively. To connect them:
- **ChatGPT**: Wrap `server.py` with a FastAPI OpenAPI layer and use a Custom GPT Action
- **Gemini**: Use Google's function-calling with the same `revit_client.py` functions

Ask ConciseZone for the OpenAPI wrapper if needed.
