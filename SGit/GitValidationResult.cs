using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SGit
{
    public class GitValidationResult
    {
        public enum ValidationType
        {
            UnStagedFileNames,
            RecentBuildTimeStamp,
            MissedCheckListItems,
            BuildAftherChecklistCheck
        }


        public bool Validated;
        public List<string>? UnStagedFileNames;
        public DateTime? BuildTimeStamp;
        public List<string>? MissedCheckListItems;

        public GitValidationResult(List<string> stringList, ValidationType type)
        {
            switch (type)
            {
                case ValidationType.UnStagedFileNames:
                    Validated = stringList.Count == 0;
                    UnStagedFileNames = stringList;
                    break;
                case ValidationType.MissedCheckListItems:
                    Validated = stringList.Count == 0;
                    MissedCheckListItems = stringList;
                    break;
                default: throw new ArgumentException();
            }
        }

        public GitValidationResult(DateTime timeStamp, ValidationType type)
        {
            switch (type)
            {
                case ValidationType.RecentBuildTimeStamp:
                    Validated = (DateTime.Now - timeStamp).TotalMinutes < 5;
                    BuildTimeStamp = timeStamp;
                    break;
                default: throw new ArgumentException();
            }
        }

        public GitValidationResult(DateTime checkListUpdateTime, DateTime buildTime)
        {
            Validated = checkListUpdateTime < buildTime;
            BuildTimeStamp = buildTime;
        }
    }
}
