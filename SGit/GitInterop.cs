using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace SGit
{
    internal class GitInterop
    {

        private enum Operation
        {
            Debug,
            Push,
            Commit
        }

        internal static void Push(GitContext context)
        {
            ValidatedGitOperation(context, Operation.Push);
        }

        internal static void Commit(GitContext context)
        {
            ValidatedGitOperation(context, Operation.Commit);
        }

        #region Debug

        internal static void DebugBranchName(GitContext context)
        {
            using (var repo = new Repository(context.GitDirectory))
            {
                var branchName = "";

                branchName = GetBranchName(repo);

                Console.WriteLine(branchName);
            }
        }

        internal static void DebugBranchChecklistValidation(GitContext context)
        {
            ValidateChecklistStatus(context);
        }

        internal static void DebugStatus(GitContext context)
        {
            using (var repo = new Repository(context.GitDirectory))
            {
                foreach (var item in repo.RetrieveStatus())
                {
                    Console.WriteLine(item.State);
                }
            }
        }

        internal static void DebugStatusValidation(GitContext context)
        {
            ValidateStagingStatus(context);
        }

        internal static void DebugRecentBuildValidation(GitContext context)
        {
            ValidateRecentBuild(context);
        }

        #endregion

        private static void ValidatedGitOperation(GitContext context, Operation operation)
        {

            var fullValidation = true;

            if (!ValidateStagingStatus(context))
                fullValidation = false;
            else
                Util.Log(Util.LogLevel.Info, $"Staging was ok");

            if (!ValidateRecentBuild(context))
                fullValidation = false;
            else
                Util.Log(Util.LogLevel.Info, $"Build was recent enough");

            if (!ValidateChecklistStatus(context))
                fullValidation = false;
            else
                Util.Log(Util.LogLevel.Info, $"All checklist items were completed");

            if (fullValidation)
            {
                PassToGit(context);
            }
            else
            {
                Util.Log(Util.LogLevel.Error, $"Abortting {Enum.GetName(operation)} because of validation fali");
            }
        }

        #region ValidateStaging

        private static bool ValidateStagingStatus(GitContext context)
        {
            using (var repo = new Repository(context.GitDirectory))
            {
                var stageValidation = CheckStagingStatus(repo.RetrieveStatus(new StatusOptions()));

                if (stageValidation != null)
                {
                    if (!stageValidation.Validated)
                    {
                        LogMissedStagingValidationFiles(stageValidation);
                        return false;
                    }
                }
                else
                {
                    Util.Log(Util.LogLevel.Error, "How? This shoud not be possible. stageValidation");
                }
            }

            return true;
        }

        private static GitValidationResult CheckStagingStatus (RepositoryStatus status)
        {

            var ValidationList = new List<string>();

            foreach (var item in status)
            {
                if (item.State is FileStatus.NewInWorkdir or FileStatus.ModifiedInWorkdir or FileStatus.DeletedFromWorkdir or FileStatus.TypeChangeInWorkdir or FileStatus.RenamedInWorkdir)
                {
                    ValidationList.Add(item.FilePath);
                }
            }

            return new GitValidationResult(ValidationList, GitValidationResult.ValidationType.UnStagedFileNames);

        }

        private static void LogMissedStagingValidationFiles(GitValidationResult validationResult)
        {
            Util.Log(Util.LogLevel.Error, $"Files were still unstaged:");
            if (validationResult.UnStagedFileNames != null)
            {
                foreach (var item in validationResult.UnStagedFileNames)
                {
                    Util.LogAdditionalData(1, item.RemoveStringOccurence(Util.GetRepositoryRootDirectory()));
                }
            }
        }

        #endregion

        #region ValidateRecentBuild

        static bool ValidateRecentBuild(GitContext context)
        {
            var recentValidation = CheckRecentBuildStatus(context);

            if (recentValidation != null)
            {
                if (!recentValidation.Validated)
                {
                    LogOldRecentBuildValidation(recentValidation);
                    return false;
                }
            }

            return true;
        }

        private static GitValidationResult CheckRecentBuildStatus(GitContext context)
        {
            var BuildDirectories = Directory.GetDirectories(context.RepoRootDirectory, "bin", SearchOption.AllDirectories);
            var extensions = new List<string>() { ".dll", ".exe" };
            var buildTimes = new List<DateTime>();

            var fileSearchStopWatch = new Stopwatch();

            if (context.Verbose)
            {
                fileSearchStopWatch.Start();
            }

            foreach (var buildDirectory in BuildDirectories)
            {
                buildTimes.AddRange(Directory.GetFiles(buildDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(f => extensions.IndexOf(Path.GetExtension(f)) >= 0)
                    .Select(File.GetLastWriteTime).ToList());
            }

            if (context.Verbose)
            {
                fileSearchStopWatch.Stop();
                Util.Log(Util.LogLevel.Verbose, $"Build file search took {fileSearchStopWatch.ElapsedMilliseconds} ms");
            }

            return new GitValidationResult(buildTimes.OrderDescending().First(),GitValidationResult.ValidationType.RecentBuildTimeStamp);
        }

        private static void LogOldRecentBuildValidation(GitValidationResult validationResult)
        {
            Util.Log(Util.LogLevel.Error, "The most recent build was too old");
            if (validationResult.RecentBuildTimeStamp != null)
            {
                Util.LogAdditionalData(1,$"Oldest build was {validationResult.RecentBuildTimeStamp.Value.ToString("g")}");
            }
            else
            {
                Util.LogAdditionalData(1, "Somehow the timestamp was null");
            }
        }

        #endregion

        #region ValidateChecklist

        private static bool ValidateChecklistStatus(GitContext context)
        {
            try
            {
                var validationResult = CheckCheckListItems(context);

                if (validationResult != null)
                {
                    if (!validationResult.Validated)
                    {
                        LogMissedChecklistItems(validationResult);
                        return false;
                    }
                }
                else
                {
                    Util.Log(Util.LogLevel.Error, "How? This shoud not be possible. ValidateChecklistStatus");
                }

                return true;
            }
            catch (FileNotFoundException fe)
            {
                Util.Log(Util.LogLevel.Error, "Checklist file was not found");
                Util.LogError(fe);
                return false;
            }
        }

        private static void LogMissedChecklistItems(GitValidationResult validationResult)
        {
            Util.Log(Util.LogLevel.Error, $"Some checklist items were maked uncomplete:");
            if (validationResult.MissedCheckListItems != null)
            {
                foreach (var item in validationResult.MissedCheckListItems)
                {
                    Util.LogAdditionalData(1, item.RemoveStringOccurence(Util.GetRepositoryRootDirectory()));
                }
            }
        }

        private static GitValidationResult CheckCheckListItems(GitContext context)
        {
            using (var repo = new Repository(context.GitDirectory))
            {
                var missedItems = new List<string>();
                var branchName = GetBranchName(repo);

                if (branchName != null)
                {
                    var lines = File.ReadAllLines(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CheckLists", $"{branchName}.txt"));
                    missedItems.AddRange(lines.Where((x) => !x.EndsWith('-')));
                }
                else
                {
                    throw new FileNotFoundException();
                }

                return new GitValidationResult(missedItems, GitValidationResult.ValidationType.MissedCheckListItems);
            }
        }

        private static string? GetBranchName(Repository repo)
        {
            var branch = repo.Branches.FirstOrDefault((x) => x.IsCurrentRepositoryHead);
            if (branch != null)
                return branch.FriendlyName;
            return null;
        }

        #endregion

        internal static void PassToGit(GitContext context)
        {
            using (var gitProcess = new Process())
            {
                var args = Util.JoinProgramArgs(context.args);

                if (context.Verbose)
                    Util.Log(Util.LogLevel.Verbose, $"Starting with arguments: {args}");

                var startInfo = gitProcess.StartInfo;

                if (context.args.Length > 0)
                {
                    startInfo.Arguments = args;
                }
                startInfo.FileName = "git";
                gitProcess.StartInfo = startInfo;

                var processStopwatch = new Stopwatch();

                if (context.Verbose)
                {
                    processStopwatch.Start();
                }

                gitProcess.Start();

                gitProcess.WaitForExit();

                if (context.Verbose)
                {
                    processStopwatch.Stop();
                    Util.Log(Util.LogLevel.Verbose, $"Git process took: {processStopwatch.ElapsedMilliseconds} ms");
                }
            }
        }
    }
}
