# Agent Bridge: MCP for Unity

Development-only bridge that lets an AI agent drive the open Unity Editor (inspect scenes, run menu items, read the console, run tests). See ADR-009.

Nothing here ships in the game binary, and no `Convergence.*` runtime assembly may reference it.

## Prerequisites

| Requirement | How to get it |
|---|---|
| Unity 6000.6.0f1 with this project open | Unity Hub |
| `uv` (launches the Python MCP server) | `winget install --id astral-sh.uv -e` |
| Package `com.coplaydev.unity-mcp` | Already pinned in `Packages/manifest.json` (tag `v9.7.3`) |

## Setup

1. Open the project. Package Manager resolves `com.coplaydev.unity-mcp` from the git URL.
2. In Unity: **Window > MCP for Unity**. If it reports "uv Not Found", choose `%LOCALAPPDATA%\Microsoft\WinGet\Links\uv.exe`.
3. Start the server from that window (HTTP transport, port 8080).
4. Cursor reads `%USERPROFILE%\.cursor\mcp.json`:

```json
{
  "mcpServers": {
    "unityMCP": { "url": "http://localhost:8080/mcp" }
  }
}
```

5. Enable `unityMCP` in Cursor Settings > MCP, or restart Cursor.

## Failure modes

- **Cursor shows the server but it never connects**: the Unity-side server is not started. Start it from **Window > MCP for Unity**.
- **Port 8080 in use**: change the port in the MCP for Unity window and in `mcp.json` together.
- **Batchmode scripts fail with "project already open"**: expected while the Editor is running. Use the MCP bridge or close the Editor before running `Tools/Build-Level01Scene.ps1`.
