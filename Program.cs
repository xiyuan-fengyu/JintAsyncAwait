
using System.IO;
using System.Threading.Tasks;
using JintAsyncAwait.JavaScript.cs;

namespace JintAsyncAwait
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var jsContent = File.ReadAllText("js/test/Test.js");
            var bridge = new JavaScriptBridge(jsContent);
            bridge.Call("test");
            ((Task<object>) bridge.Call("testAsync")).Wait();
        }
    }
}