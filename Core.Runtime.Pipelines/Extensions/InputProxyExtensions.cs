using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Runtime.Pipelines
{
    public static class InputProxyExtensions
    {
        public static PipeContext GetContext(this object inputProxy)
        {
            var mi = inputProxy.GetType().GetMethods().First(m => m.Name == "Get");
            var pc = (PipeContext)mi.MakeGenericMethod(typeof(PipeContext)).Invoke(inputProxy, new[] { "pc" });
            return pc;
        }
        public static PipeData GetData(this object inputProxy)
        {
            var mi = inputProxy.GetType().GetMethods().First(m => m.Name == "Get");
            var pd = (PipeData)mi.MakeGenericMethod(typeof(PipeData)).Invoke(inputProxy, new[] { "pd" });
            return pd;
        }
        public static PipeResolver GetResolver(this object inputProxy)
        {
            var mi = inputProxy.GetType().GetMethods().First(m => m.Name == "Get");
            var pr = (PipeResolver)mi.MakeGenericMethod(typeof(PipeResolver)).Invoke(inputProxy, new[] { "pr" });
            return pr;
        }
    }
}
