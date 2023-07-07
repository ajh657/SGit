using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGit
{
    public class GitValidationResult
    {
        public bool Validated;
        public List<string>? unStagedFileNames;
        public DateTime? recentBuildTimeStamp;

        public GitValidationResult(List<string> files)
        {
            Validated = files.Count == 0;
            unStagedFileNames = files;
        }

        public GitValidationResult(DateTime BuildTimeStamp)
        {
            Validated = (DateTime.Now - BuildTimeStamp).TotalMinutes < 5;
            recentBuildTimeStamp = BuildTimeStamp;
        }
    }
}
