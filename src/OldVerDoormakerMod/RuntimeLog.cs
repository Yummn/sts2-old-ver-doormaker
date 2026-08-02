using System;
using System.IO;

namespace OldVerDoormakerMod;

internal static class RuntimeLog
{
	private static readonly object Sync = new object();

	private static readonly string LogPath = BuildLogPath();

	private static string BuildLogPath()
	{
		try
		{
			string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			string text = Path.Combine(baseDirectory, "mods", "OldVerDoormaker");
			Directory.CreateDirectory(text);
			return Path.Combine(text, "OldVerDoormaker_runtime.log");
		}
		catch
		{
			return Path.Combine(Path.GetTempPath(), "OldVerDoormaker_runtime.log");
		}
	}

	public static void Write(string message)
	{
		lock (Sync)
		{
			try
			{
				string contents = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
				File.AppendAllText(LogPath, contents);
			}
			catch
			{
			}
		}
	}
}
