const { contextBridge, ipcRenderer } = require("electron");

// Expose only specific safe APIs to the renderer
contextBridge.exposeInMainWorld("electronAPI", {
  minimize: () => ipcRenderer.send("win-minimize"),
  maximize: () => ipcRenderer.send("win-maximize"),
  close:    () => ipcRenderer.send("win-close"),
});
