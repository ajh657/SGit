using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

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

        internal enum Operation
        {
            Debug,
            Push,
            Commit
        }

        internal static void LogRaw(string message)
        {
            Console.WriteLine(message);
        }

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

        internal static void LogError(Exception exception)
        {
            Log(LogLevel.Error, $"Exception has occurred.{Environment.NewLine} Message:{exception.Message} {Environment.NewLine} Stack trace:{exception.StackTrace}");
        }

        internal static void LogError(string message)
        {
            Log(LogLevel.Error, $"Non fatal error has occured:{Environment.NewLine}{message}");
        }

        internal static void LogDebug(string message)
        {
            Log(LogLevel.Debug, message);
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

        internal static string JoinProgramArgs(this string[] args)
        {
            var argString = "";

            for (var i = 0; i < args.Length; i++)
            {
                if (i != 0)
                    argString += " ";

                if (args[i].Contains(' '))
                    argString += $"\"{args[i]}\"";
                else
                    argString += args[i];
            }

            return argString;
        }

        internal static List<T> GetNamedArguments<T>(this string[] args, string name)
        {
            var index = args.ToList().IndexOf(name.ToLower());

            if (index == -1 || args.Length < index + 1)
                return new List<T>();

            var stringArgument = args[index+1];

            var items = new List<T>();

            foreach (var item in stringArgument.Split(' '))
            {
                if (typeof(T).IsEnum)
                {
                    items.Add((T)Enum.Parse(typeof(T), item));
                }
                else
                {
                    items.Add((T)Convert.ChangeType(item, typeof(T)));
                }
            }
            return items;

        }

        internal static string[] CleanArguments(string[] args, string name)
        {
            var index = args.ToList().IndexOf(name.ToLower());
            return args.Where((x, i) => !(i == index || i == index + 1)).ToArray();
        }

        internal static string GetArgument(this string argument)
        {
            return $"--{argument}";
        }

        internal static string GetCheckListFilePath(string branchName)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CheckLists", $"{branchName}.txt");
        }

        internal static string? GetBranchName(Repository repo)
        {
            var branch = repo.Branches.FirstOrDefault((x) => x.IsCurrentRepositoryHead);
            if (branch != null)
                return branch.FriendlyName;
            return null;
        }

        internal static string GetGitDirectory()
        {
            return Path.Combine(GetRepositoryRootDirectory(), Constants.GIT.gitFolder);
        }

        internal static string GetRepositoryRootDirectory()
        {
            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new DirectoryNotFoundException();

            var directories = Directory.GetDirectories(currentDirectory);

            while (!directories.Contains(Path.Combine(currentDirectory, Constants.GIT.gitFolder)))
            {
                currentDirectory = Path.GetFullPath(Path.Combine(currentDirectory, Constants.GIT.parentDirectory));
                directories = Directory.GetDirectories(currentDirectory);
            }

            return currentDirectory;
        }
    }
}
