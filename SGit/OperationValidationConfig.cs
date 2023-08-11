using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SGit.Util;

namespace SGit
{
    internal static class OperationValidationConfig
    {
        internal enum Validations
        {
            Staging,
            RecentBuild,
            Checklist,
            BuildNewerThanChecklist
        }

        internal static List<Validations> GetValidationConfig(Operation operation)
        {
            var validationConfig = new List<Validations>();
            switch (operation)
            {
                case Operation.Commit:
                    validationConfig.AddRange(new Validations[] { Validations.Staging, Validations.BuildNewerThanChecklist } );
                    break;
                case Operation.Push:
                    validationConfig.AddRange(new Validations[] { Validations.Staging, Validations.RecentBuild, Validations.Checklist });
                    break;
                case Operation.Debug:
                    validationConfig.AddRange(new Validations[] { Validations.Staging, Validations.RecentBuild, Validations.Checklist, Validations.BuildNewerThanChecklist });
                    break;

            }
            return validationConfig;
        }
    }
}
