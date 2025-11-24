using Asahi.Vision.Handlers.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asahi.Vision.Handlers.Interfaces
{
    public interface IInspectionRunner
    {
        Task<InspectionContext> RunAsync(InspectionContext context);
    }

}
