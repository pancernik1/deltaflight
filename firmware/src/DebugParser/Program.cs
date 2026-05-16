using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

/// <summary>
/// ΔF Debug Parser — TCP server on port 5050.
/// Electron's main process spawns this and communicates via net.Socket.
/// Each connection receives a newline-delimited JSON command and responds
/// with a newline-delimited JSON result, then closes.
/// </summary>
class DebugParser
{
    const int PORT = 5050;

    static void Main()
    {
        var listener = new TcpListener(IPAddress.Loopback, PORT);
        listener.Start();
        Console.WriteLine($"[DebugParser] Listening on 127.0.0.1:{PORT}");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            ThreadPool.QueueUserWorkItem(_ => Handle(client));
        }
    }

    static void Handle(TcpClient client)
    {
        using (client)
        using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
        {
            try
            {
                string? line = reader.ReadLine();
                if (line == null) return;

                string command = "";
                try
                {
                    var doc = JsonDocument.Parse(line);
                    command = doc.RootElement.GetProperty("command").GetString()?.Trim() ?? "";
                }
                catch { command = line.Trim(); }

                Console.WriteLine($"[DebugParser] cmd: \"{command}\"");

                string output = Parse(command);
                string response = JsonSerializer.Serialize(new { output });

                Console.WriteLine($"[DebugParser] res: \"{output}\"");
                writer.WriteLine(response);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DebugParser] Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Add your commands here. Return "__CLEAR__" to clear the terminal client-side.
    /// </summary>
    static string Parse(string cmd) => cmd.ToLowerInvariant() switch
    {
        "debug"                              => "Hello World",
        "help"                               => "Commands: debug  help  ping  time  echo <text>  clear",
        "ping"                               => "Pong.",
        "time"                               => $"Server time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
        "clear"                              => "__CLEAR__",
        var c when c.StartsWith("echo ")     => cmd[5..],
        ""                                   => "",
        _                                    => $"Unknown command: \"{cmd}\". Type 'help' for a list.",
    };
}
