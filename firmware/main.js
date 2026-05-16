const { app, BrowserWindow, ipcMain, Menu } = require("electron");
const path   = require("path");
const { spawn, execSync } = require("child_process");

// ── Wayland / HiDPI (Hyprland, sway, etc.) ───────────────────────────────────
// Must be called BEFORE app.whenReady() — command-line switches are locked after.
// ELECTRON_OZONE_PLATFORM_HINT=auto in the env is the cleanest trigger, but we
// also force it here so it works regardless of how the app is launched.
if (process.platform === "linux") {
  app.commandLine.appendSwitch("ozone-platform", "wayland");
  app.commandLine.appendSwitch("enable-features", "UseOzonePlatform,WaylandWindowDecorations");
  // Proper fractional scaling for HiDPI compositors like Hyprland
  app.commandLine.appendSwitch("enable-features", "WaylandFractionalScaleV1");
  app.commandLine.appendSwitch("high-dpi-support", "1");
  app.commandLine.appendSwitch("force-device-scale-factor", "1");
}

let mainWindow = null;
let parserProc = null;

// ── Locate the compiled C# parser ────────────────────────────────────────────
function parserExePath() {
  const resourcesDir = app.isPackaged
    ? path.join(process.resourcesPath, "parser-bin")
    : path.join(__dirname, "parser-bin");

  if (process.platform === "win32") return path.join(resourcesDir, "DebugParser.exe");
  return path.join(resourcesDir, "DebugParser");
}

// ── Spawn the C# HTTP server ──────────────────────────────────────────────────
function startParser() {
  const exe = parserExePath();
  console.log("[main] Starting parser:", exe);

  parserProc = spawn(exe, [], {
    stdio: ["ignore", "pipe", "pipe"],
    detached: false,
  });

  parserProc.stdout.on("data", (d) => process.stdout.write("[parser] " + d));
  parserProc.stderr.on("data", (d) => process.stderr.write("[parser-err] " + d));

  parserProc.on("error", (err) => {
    console.warn("[main] Parser binary not found:", err.message);
    console.warn("[main] Run `npm run build:parser` first, or start manually:");
    console.warn("[main]   cd src && dotnet run");
    // Fall back to dotnet run so the terminal still works without a compiled binary
    startParserFallback();
  });

  parserProc.on("exit", (code, signal) => {
    if (signal !== "SIGTERM") console.log("[main] Parser exited:", code);
    parserProc = null;
  });
}

// ── Fallback: use `dotnet run` when no compiled binary exists ─────────────────
function startParserFallback() {
  const csproj = path.join(__dirname, "src", "DebugParser.csproj");
  console.log("[main] Falling back to `dotnet run`:", csproj);

  parserProc = spawn("dotnet", ["run", "--project", csproj, "--no-launch-profile"], {
    stdio: ["ignore", "pipe", "pipe"],
    detached: false,
  });

  parserProc.stdout.on("data", (d) => process.stdout.write("[parser-dotnet] " + d));
  parserProc.stderr.on("data", (d) => process.stderr.write("[parser-dotnet-err] " + d));

  parserProc.on("error", (err) => {
    console.warn("[main] dotnet fallback also failed:", err.message);
    console.warn("[main] Install .NET 8 SDK: https://dotnet.microsoft.com/download");
    parserProc = null;
  });
}

// ── Kill parser on app quit ───────────────────────────────────────────────────
function stopParser() {
  if (!parserProc) return;
  try {
    if (process.platform === "win32") {
      execSync(`taskkill /pid ${parserProc.pid} /T /F`);
    } else {
      parserProc.kill("SIGTERM");
    }
  } catch (_) {}
  parserProc = null;
}

// ── Create the main window ────────────────────────────────────────────────────
function createWindow() {
  mainWindow = new BrowserWindow({
    width:  1200,
    height: 780,
    minWidth:  800,
    minHeight: 550,
    frame: false,
    backgroundColor: "#0a0a0c",
    webPreferences: {
      preload: path.join(__dirname, "preload.js"),
      contextIsolation: true,
      nodeIntegration:  false,
    },
    show: false,
    titleBarStyle: "hidden",
  });

  Menu.setApplicationMenu(null);
  mainWindow.loadFile(path.join(__dirname, "src", "index.html"));
  mainWindow.once("ready-to-show", () => mainWindow.show());
  mainWindow.on("closed", () => { mainWindow = null; });
}

// ── IPC: window controls ──────────────────────────────────────────────────────
ipcMain.on("win-minimize", () => mainWindow?.minimize());
ipcMain.on("win-maximize", () => {
  if (mainWindow?.isMaximized()) mainWindow.unmaximize();
  else mainWindow?.maximize();
});
ipcMain.on("win-close", () => mainWindow?.close());

// ── App lifecycle ─────────────────────────────────────────────────────────────
app.whenReady().then(() => {
  startParser();
  createWindow();
  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on("window-all-closed", () => {
  stopParser();
  if (process.platform !== "darwin") app.quit();
});

app.on("before-quit", stopParser);
