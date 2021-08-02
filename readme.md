# JintAsyncAwait
use async/await in jint  

Use typescript to write code, and then compile it into es5 version    
use JavaScriptBridge to Call the async function  

```c#

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
```

ts example
```TypeScript

(() => {

    class Test {

        test() {
            log(new Date());
        }

        async testAsync() {
            await new Promise<any>(resolve => {
                setTimeout(resolve, 1000);
            });
            for (let i = 0; i < 10; i++) {
                await sleep(1000);
                log(`${i}: ${new Date().getTime()}`)
            }
        }

    }

    return new Test();
})();
```

compiled js example
```js
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = (this && this.__generator) || function (thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
    return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (_) try {
            if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [op[0] & 2, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
};
(function () {
    var Test = (function () {
        function Test() {
        }
        Test.prototype.test = function () {
            log(new Date());
        };
        Test.prototype.testAsync = function () {
            return __awaiter(this, void 0, void 0, function () {
                var i;
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0: return [4, new Promise(function (resolve) {
                                setTimeout(resolve, 1000);
                            })];
                        case 1:
                            _a.sent();
                            i = 0;
                            _a.label = 2;
                        case 2:
                            if (!(i < 10)) return [3, 5];
                            return [4, sleep(1000)];
                        case 3:
                            _a.sent();
                            log(i + ": " + new Date().getTime());
                            _a.label = 4;
                        case 4:
                            i++;
                            return [3, 2];
                        case 5: return [2];
                    }
                });
            });
        };
        return Test;
    }());
    return new Test();
})();
```

run the program, the output is as follows:  
```text
2021/8/2 3:16:33
0: 1627874195874
1: 1627874196893
2: 1627874197908
3: 1627874198920
4: 1627874199924
5: 1627874200928
6: 1627874201933
7: 1627874202936
8: 1627874203938
9: 1627874204943
```
