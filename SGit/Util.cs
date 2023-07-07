using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SGit
{
    internal static partial class Util
    {
        internal enum LogLevel
        {
            Info,
            Verbose,
            Error,
            Warning,
            Debug
        }

        internal static void LogRaw(string message) => Console.WriteLine(message);

        internal static void Log(LogLevel level, string message)
        {
            LogRaw($"[{Enum.GetName(level)}] {message}");
        }

        internal static void LogAdditionalData(int indentationLevel, string message)
        {
            var indentedMessage = "";

            for (var i = 0; i < indentationLevel; i++)
            {
                indentedMessage += "\t";
            }

            indentedMessage += message;

            LogRaw(indentedMessage);
        }

        internal static int LogError(Exception exception)
        {
            Log(LogLevel.Error, $"Exception has occurred.{Environment.NewLine} Message:{exception.Message} {Environment.NewLine} Stack trace:{exception.StackTrace}");
            return 1;
        }

        internal static void LogCommandNotFound()
        {
            Log(LogLevel.Warning, "Command not found");
        }

        internal static string RemoveStringOccurence(this string str,string removal)
        {
            var index = str.IndexOf(removal);
            return (index < 0) ? str : str.Remove(index, removal.Length);
        }
        
        internal static string GetGitDirectory()
        {
            return Path.Combine(GetRepositoryRootDirectory(), Constants.gitFolder);
        }

        internal static string GetRepositoryRootDirectory()
        {
            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new DirectoryNotFoundException();

            var directories = Directory.GetDirectories(currentDirectory);

            while (!directories.Contains(Path.Combine(currentDirectory, Constants.gitFolder)))
            {
                currentDirectory = Path.GetFullPath(Path.Combine(currentDirectory, Constants.parentDirectory));
                directories = Directory.GetDirectories(currentDirectory);
            }

            return currentDirectory;
        }
    }
}
