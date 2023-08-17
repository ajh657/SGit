namespace SGit
{
    internal class Program
    {
        internal static int Main(string[] args)
        {
            var context = new GitContext(args, args.Contains("--verbose"));

            try
            {
                if (context.Arguments.Length > 0)
                {
                    switch (context.Arguments[0].ToLower())
                    {
                        case "push":
                            GitInterop.Push(context);
                            return 0;
                        case "commit":
                            GitInterop.Commit(context);
                            return 0;
                        case "debug":
                            return DebugCommands(context);
                        default:
                            GitInterop.PassToGit(context);
                            return 0;
                    }

                }
                else
                {
                    GitInterop.PassToGit(context);
                    return 0;
                }
            }
            catch (Exception e)
            {
                Util.LogError(e);
                return -1;

            }
        }

        internal static int DebugCommands(GitContext context)
        {
            switch (context.Arguments[1].ToLower())
            {
                case "status":
                    DebugStatusCommands(context);
                    return 0;
                case "buildtime":
                    Debug.DebugRecentBuildValidation(context);
                    return 0;
                case "branch":
                    Debug.DebugBranchName(context);
                    return 0;
                case "checklist":
                    Debug.DebugBranchChecklistValidation(context);
                    return 0;
                case "util":
                    DebugUtils(context);
                    return 0;

                default:
                    Util.LogCommandNotFound();
                    return -1;
            }
        }

        internal static int DebugStatusCommands(GitContext context)
        {
            if (context.Arguments.Length >= 3)
            {
                switch (context.Arguments[2].ToLower())
                {
                    case "validation":
                        Debug.DebugStatusValidation(context);
                        return 0;
                    default:
                        Util.LogCommandNotFound();
                        return 1;
                }
            }
            else
            {
                Debug.DebugStatus(context);
                return 0;
            }
        }

        internal static int DebugUtils(GitContext context)
        {
            if (context.Arguments.Length >= 3)
            {
                switch (context.Arguments[2].ToLower())
                {
                    case "parameter":
                        Debug.DebugUseArguments(context);
                        return 0;

                    default:
                        Util.LogCommandNotFound();
                        return -1;
                }
            }
            else
            {
                Util.LogCommandNotFound();
                return 1;
            }
        }
    }
}
