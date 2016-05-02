using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Test
{
    public class Class1
    {
        [Fact()]
        public void Test()
        {
            Assert.False(true);
        }
        

        [Fact(Skip ="Bar")]
        public void Test2()
        {
            Assert.False(true);
        }

        [Fact()]
        public void Test3()
        {
            Assert.False(false);
        }
    }
}
