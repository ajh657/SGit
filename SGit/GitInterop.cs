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

        internal static void DebugStatus(GitContext context)
        {
            using (var repo = new Repository(Util.GetGitDirectory()))
            {
                foreach (var item in repo.RetrieveStatus())
                {
                    Console.WriteLine(item.State);
                }
            }
        }

        internal static void DebugStatusValidation(GitContext context)
        {
            using (var repo = new Repository(context.GitDirectory))
            {
                var validation = ValidateStagingStatus(repo.RetrieveStatus(new StatusOptions()));

                if (validation != null && validation.unStagedFileNames != null)
                {
                    if (!validation.Validated)
                    {
                        Util.Log(Util.LogLevel.Debug, "Found Unstaged files printting error message:");
                        LogMissedStagingValidationFiles(validation, Operation.Push);
                    }
                    else
                    {
                        Util.Log(Util.LogLevel.Debug, "All files are ok");
                    }
                }
                else
                {
                    Util.Log(Util.LogLevel.Error, "How? This shoud not be possible. debugStatusValidation");
                }
            }
        }

        internal static void DebugRecentBuildValidation(GitContext context)
        {
            using (var repo = new Repository(context.RepoRootDirectory))
            {
                var validation = ValidateRecentBuild(context);
                if (validation != null && validation.recentBuildTimeStamp != null)
                {
                    if (!validation.Validated)
                    {
                        Util.Log(Util.LogLevel.Debug, "Recent build is too old. Printing error message:");
                        LogOldRecentBuildValidation(validation, Operation.Debug);
                    }
                    else
                    {
                        Util.Log(Util.LogLevel.Debug, "Recent build is new enough");
                    }
                }
                else
                {
                    Util.Log(Util.LogLevel.Error, "How? This shoud not be possible. debugRecentBuildValidation");
                }
            }
        }

        internal static void Push(GitContext context)
        {
            ValidatedGitOperation(context,Operation.Push);
        }

        internal static void Commit(GitContext context)
        {
            ValidatedGitOperation(context, Operation.Commit);
        }

        private static void ValidatedGitOperation(GitContext context, Operation operation)
        {

            var fullValidation = true;

            using (var repo = new Repository(Util.GetGitDirectory()))
            {
                var stageValidation = ValidateStagingStatus(repo.RetrieveStatus(new StatusOptions()));

                if (stageValidation != null)
                {
                    if (!stageValidation.Validated)
                    {
                        LogMissedStagingValidationFiles(stageValidation, operation);
                        fullValidation = false;
                    }
                }
                else
                {
                    Util.Log(Util.LogLevel.Error, "How? This shoud not be possible. stageValidation");
                }
            }

            var recentValidation = ValidateRecentBuild(context);

            if (recentValidation != null)
            {
                if (!recentValidation.Validated)
                {
                    LogMissedStagingValidationFiles(recentValidation, operation);
                    fullValidation = false;
                }
            }

            if (fullValidation)
            {
                PassToGit(context);
            }
        }

        #region ValidateStaging

        private static GitValidationResult ValidateStagingStatus (RepositoryStatus status)
        {

            var ValidationList = new List<string>();

            foreach (var item in status)
            {
                if (item.State is FileStatus.NewInWorkdir or FileStatus.ModifiedInWorkdir or FileStatus.DeletedFromWorkdir or FileStatus.TypeChangeInWorkdir or FileStatus.RenamedInWorkdir)
                {
                    ValidationList.Add(item.FilePath);
                }
            }

            return new GitValidationResult(ValidationList);

        }

        private static void LogMissedStagingValidationFiles(GitValidationResult validationResult, Operation operation)
        {
            Util.Log(Util.LogLevel.Error, $"Files were still unstaged:");
            if (validationResult.unStagedFileNames != null)
            {
                foreach (var item in validationResult.unStagedFileNames)
                {
                    Util.LogAdditionalData(1, item.RemoveStringOccurence(Util.GetRepositoryRootDirectory()));
                }
            }
        }

        #endregion

        #region ValidateRecentBuild

        private static GitValidationResult ValidateRecentBuild(GitContext context)
        {
            var BuildDirectories = Directory.GetDirectories(context.RepoRootDirectory, "bin", SearchOption.AllDirectories);
            var extensions = new List<string>() { ".dll", ".exe" };
            var buildTimes = new List<DateTime>();

            foreach (var buildDirectory in BuildDirectories)
            {
                buildTimes.AddRange(Directory.GetFiles(buildDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(f => extensions.IndexOf(Path.GetExtension(f)) >= 0)
                    .Select(File.GetLastWriteTime).ToList());
            }

            return new GitValidationResult(buildTimes.OrderDescending().First());
        }

        private static void LogOldRecentBuildValidation(GitValidationResult validationResult, Operation operation)
        {
            Util.Log(Util.LogLevel.Error, "The most recent build was too old");
            if (validationResult.recentBuildTimeStamp != null)
            {
                Util.LogAdditionalData(1,$"Oldest build was {validationResult.recentBuildTimeStamp.Value.ToString("g")}");
            }
            else
            {
                Util.LogAdditionalData(1, "Somehow the timestamp was null");
            }
        }

        #endregion

        internal static void PassToGit(GitContext context)
        {
            using (var gitProcess = new Process())
            {
                var args = string.Join(" ", context.args);

                if (context.Verbose)
                    Util.Log(Util.LogLevel.Verbose, $"Starting with arguments: {args}");

                var startInfo = gitProcess.StartInfo;

                if (context.args.Length > 0)
                {
                    startInfo.Arguments = args;
                }
                startInfo.FileName = "git";
                gitProcess.StartInfo = startInfo;

                gitProcess.Start();

                gitProcess.WaitForExit();
            }
        }
    }
}
