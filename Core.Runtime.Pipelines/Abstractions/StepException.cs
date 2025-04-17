using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Runtime.Pipelines
{
    public class StepException : Exception
    {
        /// <inheritdoc />
        public StepException(string message, Exception exception) : base(message, exception)
        {
        }
    }
}
