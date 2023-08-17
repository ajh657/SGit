using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGit
{
    public class GitContext
    {
        public string GitDirectory = Util.GetGitDirectory();
        public string RepoRootDirectory = Util.GetRepositoryRootDirectory();
        public string[] Arguments;
        public bool Verbose;

        public GitContext(string[] args, bool verbose)
        {
            Arguments = args;
            Verbose = verbose;
        }
    }
}
