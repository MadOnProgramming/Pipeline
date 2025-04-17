using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Runtime.Pipelines
{
    public interface IPipeResolver
    {
        string GetServiceConnectionString(string componentName,int ouId);
    }
}
