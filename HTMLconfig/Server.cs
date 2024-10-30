using Crestron.SimplSharp;                // For Basic SIMPL# Classes
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace HTMLconfig
{
    public static class Server
    {
        private static HttpListener _httpListener;
        private static ushort _port = 8086;
        private static string _htmlDirectory = "\\html\\config";
        private static string _jsonDirectory = "\\user\\config";

        private static readonly Dictionary<string, string> MimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { ".html", "text/html" },
    { ".js", "application/javascript" },
    { ".css", "text/css" },
    { ".json", "application/json" },
    { ".png", "image/png" },
    { ".jpg", "image/jpeg" },
    { ".jpeg", "image/jpeg" },
    { ".gif", "image/gif" },
    { ".svg", "image/svg+xml" },
    { ".ico", "image/x-icon" }
};

        public static void startServer()
        {
            //_port = port;
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://*:{_port}/");

            try
            {
                _httpListener.Start();
                CrestronConsole.PrintLine($"HTTP Server started on port {_port}");
                Task.Run(() => HandleIncomingConnections());
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine($"Error starting HTTP Server: {ex.Message}");
            }
        }

        public static void stopServer()
        {
            _httpListener.Stop();
            CrestronConsole.PrintLine("HTTP Server stopped");
        }

        private static async Task HandleIncomingConnections()
        {
            while (_httpListener.IsListening)
            {
                var context = await _httpListener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                if (request.HttpMethod == "GET" && request.RawUrl != "/list-files")
                {
                    // Determine the file path based on the request URL
                    string requestedFile = request.Url.AbsolutePath.TrimStart('/');
                    string filePath = Path.Combine(_htmlDirectory, requestedFile);

                    if (requestedFile.EndsWith(".json"))
                    {
                        CrestronConsole.PrintLine("Requesting [JSON] {0}. . .", requestedFile);
                        filePath = Path.Combine(_jsonDirectory, requestedFile);
                        CrestronConsole.PrintLine("FilePath: {0}. . .", filePath);

                    }
                    else
                    {
                        CrestronConsole.PrintLine("Requesting [OTHER] {0}. . .", requestedFile);
                        filePath = Path.Combine(_htmlDirectory, requestedFile);
                        CrestronConsole.PrintLine("FilePath: {0}. . .", filePath);
                    }

                    if (File.Exists(filePath))
                    {
                        ServeFile(response, filePath);
                    }
                    else
                    {
                        // If root path or file not found, serve index.html as a default
                        ServeHtmlPage(response);
                    }
                }
                else if (request.HttpMethod == "PUT")
                {
                    try
                    {
                        CrestronConsole.PrintLine("Trying to save data via PUT");
                        string data;
                        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                        {
                            data = await reader.ReadToEndAsync();
                            CrestronConsole.Print("Data: {0}", data);
                        }

                        // Define the file path to save the JSON data
                        string savePath = _jsonDirectory + "\\ClientConfig.json";
                        CrestronConsole.PrintLine("Saving {0}", savePath);

                        // Attempt to write the data to the file
                        await Task.Run(() => File.WriteAllText(savePath, data));

                        // Respond to the client that the save was successful
                        byte[] buffer = Encoding.UTF8.GetBytes("{\"message\":\"Configuration updated successfully\"}");
                        response.ContentType = "application/json";
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    catch (Exception ex)
                    {
                        // Log the error (optional) and send a failure response
                        Console.WriteLine($"Error saving data: {ex.Message}");

                        byte[] buffer = Encoding.UTF8.GetBytes("{\"message\":\"Error saving configuration\"}");
                        response.ContentType = "application/json";
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    finally
                    {
                        response.OutputStream.Close();
                    }
                }
                else if (request.HttpMethod == "GET" && request.RawUrl == "/list-files")
                {
                    try
                    {
                        // Path to the directory where your files are stored
                        string directoryPath = _jsonDirectory;
                        var files = Directory.GetFiles(directoryPath, "*.json"); // Get all JSON files

                        var fileNames = files.Select(file => Path.GetFileNameWithoutExtension(file)).ToList();

                        // Serialize the list of filenames to JSON
                        string jsonResponse = JsonConvert.SerializeObject(fileNames);

                        byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
                        response.ContentType = "application/json";
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    catch (Exception ex)
                    {
                        // Handle errors (optional)
                        Console.WriteLine($"Error listing files: {ex.Message}");

                        byte[] buffer = Encoding.UTF8.GetBytes("{\"message\":\"Error listing files\"}");
                        response.ContentType = "application/json";
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                }

                    response.Close();
            }
        }

        private static void ServeHtmlPage(HttpListenerResponse response)
        {
            string filePath = Path.Combine(_htmlDirectory, "index.html");
            ServeFile(response, filePath);
        }

        private static void ServeFile(HttpListenerResponse response, string filePath)
        {
            try
            {
                byte[] buffer = File.ReadAllBytes(filePath);

                // Determine the content type based on the file extension
                string contentType = GetContentType(filePath);
                response.ContentType = contentType;
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serving file {filePath}: {ex.Message}");
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }

        private static string GetContentType(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            return MimeTypes.TryGetValue(extension, out var mimeType) ? mimeType : "application/octet-stream";
        }

    }  
}