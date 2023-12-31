using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using static SGit.Util;
using static SGit.OperationValidationConfig;

namespace SGit
{
    internal class GitInterop : Debug
    {
        internal static void Push(GitContext context)
        {
            ValidatedGitOperation(context, Operation.Push);
        }

        internal static void Commit(GitContext context)
        {
            ValidatedGitOperation(context, Operation.Commit);
        }

        private static void ValidatedGitOperation(GitContext context, Operation operation)
        {

            var fullValidation = true;
            var operationValidations = GetValidationConfig(operation);
            var skippedValidations = context.Arguments.GetNamedArguments<Validations>(Constants.CommandArguments.skipValidation.GetArgument());
            context.Arguments = CleanArguments(context.Arguments, Constants.CommandArguments.skipValidation.GetArgument());


            if (operationValidations != null)
            {

                if (operationValidations.Contains(Validations.Staging) && !skippedValidations.Contains(Validations.Staging))
                {
                    if (!ValidateStagingStatus(context))
                        fullValidation = false;
                    else
                        Log(Util.LogLevel.Info, $"Staging was ok");
                }

                if (operationValidations.Contains(Validations.RecentBuild) && !skippedValidations.Contains(Validations.RecentBuild))
                {
                    if (!ValidateRecentBuild(context))
                        fullValidation = false;
                    else
                        Log(Util.LogLevel.Info, $"Build was recent enough");
                }

                if (operationValidations.Contains(Validations.Checklist) && !skippedValidations.Contains(Validations.Checklist))
                {
                    if (!ValidateChecklistStatus(context) && !skippedValidations.Contains(Validations.Checklist))
                        fullValidation = false;
                    else
                        Log(Util.LogLevel.Info, $"All checklist items were completed");
                }

                if (operationValidations.Contains(Validations.BuildNewerThanChecklist) && !skippedValidations.Contains(Validations.BuildNewerThanChecklist))
                {
                    if (!ValidateBuildAfterChecklist(context))
                        fullValidation = false;
                    else
                        Log(Util.LogLevel.Info, $"Build was newer than last checklist update");
                }

                if (fullValidation)
                {
                    PassToGit(context);
                }
                else
                {
                    Log(Util.LogLevel.Error, $"Abortting {Enum.GetName(operation)} because of validation fali");
                }
            }



        }

        #region ValidateStaging

        internal static bool ValidateStagingStatus(GitContext context)
        {
            using (var repo = new Repository(context.GitDirectory))
            {
                var stageValidation = CheckStagingStatus(repo.RetrieveStatus(new StatusOptions()));

                if (!stageValidation.Validated)
                {
                    LogMissedStagingValidationFiles(stageValidation);
                    return false;
                }
            }

            return true;
        }

        private static GitValidationResult CheckStagingStatus(RepositoryStatus status)
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
            Log(Util.LogLevel.Error, $"Files were still unstaged:");
            if (validationResult.UnStagedFileNames != null)
            {
                foreach (var item in validationResult.UnStagedFileNames)
                {
                    LogAdditionalData(1, item.RemoveStringOccurence(GetRepositoryRootDirectory()));
                }
            }
        }

        #endregion

        #region ValidateRecentBuild

        internal static bool ValidateRecentBuild(GitContext context)
        {
            var recentValidation = CheckRecentBuildStatus(context);

            if (!recentValidation.Validated)
            {
                LogOldRecentBuildValidation(recentValidation);
                return false;
            }

            return true;
        }

        private static GitValidationResult CheckRecentBuildStatus(GitContext context)
        {
            var fileSearchStopWatch = new Stopwatch();

            if (context.Verbose)
            {
                fileSearchStopWatch.Start();
            }

            var lastBuildTime = GetLastBuildTime(context);

            if (context.Verbose)
            {
                fileSearchStopWatch.Stop();
                Log(Util.LogLevel.Verbose, $"Build file search took {fileSearchStopWatch.ElapsedMilliseconds} ms");
            }

            return new GitValidationResult(lastBuildTime, GitValidationResult.ValidationType.RecentBuildTimeStamp);
        }

        private static DateTime GetLastBuildTime(GitContext context)
        {
            var BuildDirectories = Directory.GetDirectories(context.RepoRootDirectory, "bin", SearchOption.AllDirectories);
            var buildTimes = new List<DateTime>();
            var extensions = new List<string>() { ".dll", ".exe" };

            foreach (var buildDirectory in BuildDirectories)
            {
                buildTimes.AddRange(Directory.GetFiles(buildDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(f => extensions.IndexOf(Path.GetExtension(f)) >= 0)
                    .Select(File.GetLastWriteTime).ToList());
            }

            return buildTimes.OrderDescending().First();
        }

        private static void LogOldRecentBuildValidation(GitValidationResult validationResult)
        {
            Log(Util.LogLevel.Error, "The most recent build was too old");
            if (validationResult.BuildTimeStamp != null)
            {
                LogAdditionalData(1, $"Oldest build was {validationResult.BuildTimeStamp.Value.ToString("g")}");
            }
            else
            {
                LogAdditionalData(1, "Somehow the timestamp was null");
            }
        }

        #endregion

        #region ValidateChecklist

        internal static bool ValidateChecklistStatus(GitContext context)
        {
            try
            {
                var validationResult = CheckCheckListItems(context);

                if (!validationResult.Validated)
                {
                    LogMissedChecklistItems(validationResult);
                    return false;
                }

                return true;
            }
            catch (FileNotFoundException fe)
            {
                Log(Util.LogLevel.Error, "Checklist file was not found");
                LogError(fe);
                return false;
            }
        }

        private static void LogMissedChecklistItems(GitValidationResult validationResult)
        {
            Log(Util.LogLevel.Error, $"Some checklist items were maked uncomplete:");
            if (validationResult.MissedCheckListItems != null)
            {
                foreach (var item in validationResult.MissedCheckListItems)
                {
                    LogAdditionalData(1, item.RemoveStringOccurence(GetRepositoryRootDirectory()));
                }
            }
        }

        private static GitValidationResult CheckCheckListItems(GitContext context)
        {
            using (var repo = new Repository(context.GitDirectory))
            {
                var missedItems = new List<string>();
                var branchName = GetBranchName(repo) ?? throw new FileNotFoundException();

                var lines = File.ReadAllLines(GetCheckListFilePath(branchName));
                missedItems.AddRange(lines.Where((x) => !x.EndsWith('-')));

                return new GitValidationResult(missedItems, GitValidationResult.ValidationType.MissedCheckListItems);
            }
        }

        #endregion

        #region ValidateBuildAfterChecklist

        internal static bool ValidateBuildAfterChecklist(GitContext context)
        {
            var validationResult = CheckIfBuildAfterChecklist(context);
            if (!validationResult.Validated)
            {
                LogLastBuildTime(validationResult);
                return false;
            }

            return true;
        }

        private static void LogLastBuildTime(GitValidationResult validationResult)
        {
            Log(Util.LogLevel.Error, $"Last build was before checklist update: {validationResult.BuildTimeStamp}");
        }

        private static GitValidationResult CheckIfBuildAfterChecklist(GitContext context)
        {
            using (var repo = new Repository(context.GitDirectory))
            {
                var branchName = GetBranchName(repo) ?? throw new FileNotFoundException();

                var fileInfo = new FileInfo(GetCheckListFilePath(branchName)) ?? throw new FileNotFoundException();

                var checkListLastUpdated = fileInfo.LastWriteTime;
                var buildLastBuildTime = GetLastBuildTime(context);
                return new GitValidationResult(checkListLastUpdated,buildLastBuildTime);
            }
        }

        #endregion

        internal static void PassToGit(GitContext context)
        {
            using (var gitProcess = new Process())
            {
                var args = context.Arguments.JoinProgramArgs();

                if (context.Verbose)
                    Log(Util.LogLevel.Verbose, $"Starting with arguments: {args}");

                var startInfo = gitProcess.StartInfo;

                if (context.Arguments.Length > 0)
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
                    Log(Util.LogLevel.Verbose, $"Git process took: {processStopwatch.ElapsedMilliseconds} ms");
                }
            }
        }
    }
}
