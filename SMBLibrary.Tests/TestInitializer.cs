using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SMBLibrary.Tests
{
    [TestClass]
    public class TestInitializer
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}
