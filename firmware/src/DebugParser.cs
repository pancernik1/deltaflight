using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

/// <summary>
/// Simple HTTP server that parses terminal commands and returns responses.
/// Run with: dotnet script DebugParser.cs  (or compile and run)
/// Listens on http://localhost:5050/
/// </summary>
class DebugParser
{
	static string version = "DEV 1.0";

    static readonly string[] AllowedOrigins = { "null", "http://localhost", "http://127.0.0.1" };

    static void Main(string[] args)
    {
        string url = "http://localhost:5050/";
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();
        Console.WriteLine($"[DebugParser] Listening on {url}");
        Console.WriteLine("[DebugParser] Press Ctrl+C to stop.\n");

        while (true)
        {
            HttpListenerContext ctx = listener.GetContext();
            System.Threading.ThreadPool.QueueUserWorkItem(_ => HandleRequest(ctx));
        }
    }

    static void HandleRequest(HttpListenerContext ctx)
    {
        HttpListenerRequest  req  = ctx.Request;
        HttpListenerResponse res  = ctx.Response;

        // CORS headers so the browser page can reach us
        string origin = req.Headers["Origin"] ?? "*";
        res.Headers.Add("Access-Control-Allow-Origin", "*");
        res.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
        res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        if (req.HttpMethod == "OPTIONS")        // preflight
        {
            res.StatusCode = 204;
            res.Close();
            return;
        }

        if (req.HttpMethod != "POST" || req.Url.AbsolutePath != "/cmd")
        {
            res.StatusCode = 404;
            res.Close();
            return;
        }

        // Read body
        string body = "";
        using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
            body = reader.ReadToEnd();

        string command = "";
        try
        {
            var doc = JsonDocument.Parse(body);
            command = doc.RootElement.GetProperty("command").GetString()?.Trim() ?? "";
        }
        catch
        {
            command = body.Trim();
        }

        Console.WriteLine($"[DebugParser] Received: \"{command}\"");

        string output = ParseCommand(command);

        Console.WriteLine($"[DebugParser] Responding: \"{output}\"\n");

        byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { output }));
        res.ContentType     = "application/json";
        res.ContentLength64 = bytes.Length;
        res.OutputStream.Write(bytes, 0, bytes.Length);
        res.Close();
    }

    /// <summary>
    /// Command parser — add more cases here to extend the terminal.
    /// </summary>
    static string ParseCommand(string cmd)
    {
        return cmd.ToLower() switch
        {
            "version"              => DebugVersion(),
            "help"               => "Available commands: version, help, clear, ping, time, echo <text>",
            "ping"               => "Pong.",
            "time"               => $"Server time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            "clear"              => "__CLEAR__",       // handled client-side
            var c when c.StartsWith("echo ")
                                 => cmd.Substring(5),
            ""                   => "",
            _                    => $"Unknown command: \"{cmd}\". Type 'help' for available commands."
        };
    }
	static string DebugVersion()
	 {
		return $"version - {version}";
	 }	




}
