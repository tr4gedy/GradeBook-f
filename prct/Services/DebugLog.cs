using System;
using System.IO;
using System.Text.Json;

namespace GradeBook.Services
{
    internal static class DebugLog
    {
        private const string SessionId = "f477e0";
        private const string RunId = "pre-fix";
        private const string LogPath = @"d:\Downlads\prct\debug-f477e0.log";
        private static readonly object Gate = new();

        public static void Write(string hypothesisId, string location, string message, object? data = null)
        {
            try
            {
                var payload = new
                {
                    sessionId = SessionId,
                    runId = RunId,
                    hypothesisId,
                    location,
                    message,
                    data,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                lock (Gate)
                {
                    File.AppendAllText(LogPath, JsonSerializer.Serialize(payload) + Environment.NewLine);
                }
            }
            catch
            {
                // Debug logging must never change application behavior.
            }
        }
    }
}
