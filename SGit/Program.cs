namespace SGit
{
    internal class Program
    {
        static int Main(string[] args)
        {
            var context = new GitContext(args, args.Contains("--verbose"));

            try
            {
                if (context.args.Length > 0)
                {
                    switch (context.args[0].ToLower())
                    {
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
                
                return Util.LogError(e);
             
            }
        }

        internal static int DebugCommands(GitContext context)
        {
            switch (context.args[1].ToLower())
            {
                case "status":
                    DebugStatusCommands(context);
                    return 0;
                case "buildtime":
                    GitInterop.DebugRecentBuildValidation(context);
                    return 0;
                default:
                    Util.LogCommandNotFound();
                    return 0;
            }
        }

        internal static int DebugStatusCommands(GitContext context)
        {
            if (context.args.Length >= 3)
            {
                switch (context.args[2].ToLower())
                {
                    case "validation":
                        GitInterop.DebugStatusValidation(context);
                        return 0;
                    default:
                        Util.LogCommandNotFound();
                        return 0;
                }
            }
            else
            {
                GitInterop.DebugStatus(context);
                return 0;
            }
        }
    }
}
