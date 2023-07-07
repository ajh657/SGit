using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGit
{
    public record GitContext(string[] args, bool Verbose)
    {
        public string GitDirectory = Util.GetGitDirectory();
        public string RepoRootDirectory = Util.GetRepositoryRootDirectory();
    }
}
