using LibGit2Sharp;
using static SGit.GitInterop;
using static SGit.Util;

namespace SGit
{
    internal class Debug
    {

        internal enum DebugEnum
        {
            Value1,
            Value2,
            Value3
        }

        internal static void DebugBranchChecklistValidation(GitContext context)
        {
            ValidateChecklistStatus(context);
        }


        internal static void DebugBranchName(GitContext context)
        {
            using (var repo = new Repository(context.GitDirectory))
            {
                var branchName = "";

                branchName = GetBranchName(repo);

                Console.WriteLine(branchName);
            }
        }

        internal static void DebugRecentBuildValidation(GitContext context)
        {
            ValidateRecentBuild(context);
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

        internal static void DebugUseArguments(GitContext context)
        {
            var DebugArgument = Constants.CommandArguments.skipValidation.GetArgument();

            LogDebug($"Arguments were \"{context.Arguments.JoinProgramArgs()}\"");

            LogDebug($"Using argument {DebugArgument}");

            var values = context.Arguments.GetNamedArguments<DebugEnum>(DebugArgument);
            context.Arguments = CleanArguments(context.Arguments,DebugArgument);

            LogDebug($"Got {values.Count} values:");
            foreach (var item in values)
            {
                var name = Enum.GetName(item);
                if (name != null)
                {
                    LogAdditionalData(1, name);
                }
                else
                {
                    LogError("Could not get name of parsed enum name?????");
                }
            }

            LogDebug($"Arguments are now: \"{context.Arguments.JoinProgramArgs()}\"");

        }
    }
}
