using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeppermintCommon
{
    //A Lazy class that can be awaited
    // Idea and implementation from Stephen Toub
    //http://blogs.msdn.com/b/pfxteam/archive/2011/01/15/10116210.aspx?utm_source=feedburner&utm_medium=twitter&utm_campaign=Feed%3A+SiteHome+(Microsoft+%7C+Blog+%7C+MSDN)
    public class AwaitableLazy<T> : Lazy<Task<T>>
    {
        public AwaitableLazy(Func<T> valueFactory) :
            base(() => Task.Factory.StartNew(valueFactory)) { }

        public AwaitableLazy(Func<Task<T>> valueFactory ):
            base(()=>Task.Factory.StartNew(valueFactory).Unwrap()){}
    }
}
