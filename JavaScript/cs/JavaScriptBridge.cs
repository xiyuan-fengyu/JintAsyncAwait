using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Jint;
using Jint.Native;
using Jint.Native.Error;
using Jint.Runtime;
using Jint.Runtime.Interop;

namespace JintAsyncAwait.JavaScript.cs
{
    public class JavaScriptBridge
    {
        
        private static readonly Engine jsEngine;

        static JavaScriptBridge()
        {
            jsEngine = new Engine(cfg => cfg
                .AllowClr(
                    typeof(Object).Assembly, 
                    typeof(JavaScriptBridge).Assembly
                )
                .AddExtensionMethods(
                    typeof(Enumerable),
                    typeof(Queryable)
                )
            );
            
            var importType = new Func<string, TypeReference>(fullName => TypeReference.CreateTypeReference(jsEngine, GetType(fullName)));
            jsEngine.SetValue("type", importType);
            jsEngine.SetValue("require", importType);
            jsEngine.SetValue("log", new Action<object[]>(Log));
            jsEngine.SetValue("createPRR", jsEngine.Evaluate(@"
                () => {
                    const prr = [null, null, null];
                    prr[0] = new Promise((resolve, reject) => {
                        prr[1] = resolve;
                        prr[2] = reject;
                    });
                    return prr;
                }
            "));
            jsEngine.SetValue("setTimeout", new Action<Action, int>(SetTimeout));
            jsEngine.SetValue("sleep", new Func<int, object>(Sleep));
        }
        
        #region jsEngineExtention

        private class PromiseResolveReject
        {
            public JsValue Promise;
            public ICallable Resolve;
            public ICallable Reject;
            public PromiseResolveReject()
            {
                var prr = jsEngine.GetValue("createPRR").Invoke().AsArray();
                Promise = prr[0];
                Resolve = prr[1] as ICallable;
                Reject = prr[2] as ICallable;
            }
        }
        
        private static void Log(params object[] args)
        {
            Console.WriteLine(string.Join(", ", args));
        }
        
        private static void SetTimeout(Action action, int ms = 0)
        {
            Task.Delay(ms).ContinueWith(task => action());
        }
        
        private static object Sleep(int ms = 0)
        {
            var prr = new PromiseResolveReject();
            Task.Delay(ms).ContinueWith(task =>
            {
                prr.Resolve.Call(JsValue.Null, new []{ JsValue.Undefined });
            });
            return prr.Promise;
        }
        
        private static Type GetType(string typeName)
        {
            // Try Type.GetType() first. This will work with types defined
            // by the Mono runtime, in the same assembly as the caller, etc.
            var type = Type.GetType( typeName );
 
            // If it worked, then we're done here
            if( type != null )
                return type;
 
            // If the TypeName is a full name, then we can try loading the defining assembly directly
            if( typeName.Contains( "." ) )
            {
                // Get the name of the assembly (Assumption is that we are using 
                // fully-qualified type names)
                var assemblyName = typeName.Substring( 0, typeName.IndexOf( '.' ) );
                // Attempt to load the indicated Assembly
                var assembly = Assembly.Load( assemblyName );
                if( assembly == null )
                    return null;
                // Ask that assembly to return the proper Type
                type = assembly.GetType( typeName );
                if( type != null )
                    return type;
            }
 
            // If we still haven't found the proper type, we can enumerate all of the 
            // loaded assemblies and see if any of them define the type
            var currentAssembly = Assembly.GetExecutingAssembly();
            var referencedAssemblies = currentAssembly.GetReferencedAssemblies();
            foreach( var assemblyName in referencedAssemblies )
            {
                // Load the referenced assembly
                var assembly = Assembly.Load( assemblyName );
                if( assembly != null )
                {
                    // See if that assembly defines the named type
                    type = assembly.GetType( typeName );
                    if( type != null )
                        return type;
                }
            }
            // The type just couldn't be found...
            return null;
 
        }
        
        private static object GetField(object typeOrInstance, string fieldName)
        {
            var type = typeOrInstance is Type ? typeOrInstance as Type : typeOrInstance.GetType();
            var binding = typeOrInstance is Type
                ? BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                : BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic;
            var field = type.GetField(fieldName, binding);
            if (field != null)
            {
                return field.GetValue(typeOrInstance);
            }
                
            var prop = type.GetProperty(fieldName, binding);
            if (prop != null && prop.CanRead)
            {
                return prop.GetValue(typeOrInstance);
            }
            
            throw new Exception($"cannot find field/property: {typeOrInstance}.{fieldName}");
        }
        
        private static object TryCallJsControllerMethod(JavaScriptBridge bridge, string method, bool ignoreMethodMissing, params JsValue[] args)
        {
            var jsController = bridge.jsController;
            var methodObj = jsController.Get(method);
            if (methodObj is ICallable call)
            {
                var res = call.Call(jsController, args);
                var resType = res.GetType();
                if (resType.FullName == "Jint.Native.Promise.PromiseInstance")
                {
                    var taskRes = new TaskCompletionSource<object>();
                    var asyncTask = new Action(async () =>
                    {
                        while (true)
                        {
                            await Task.Yield();
                            var state = GetField(res, "State").ToString();
                            if (state == "Pending")
                            {
                                // async/await 编译为 es5 后的支持
                                // 栈空时执行一条空语句使得Promise执行完成
                                var callStack = GetField(jsEngine, "CallStack");
                                var count = (int)GetField(callStack, "Count");
                                if (count == 0)
                                {
                                    try
                                    {
                                        jsEngine.Execute("");
                                    }
                                    catch (Exception e)
                                    {
                                        jsEngine.ResetCallStack();
                                        Console.Error.WriteLine(e);
                                    }
                                }
                            }
                            else if (state == "Fulfilled")
                            {
                                taskRes.SetResult(GetField(res, "Value"));
                                break;
                            }
                            else if (state == "Rejected")
                            {
                                var ex = new JavaScriptException((ErrorInstance) GetField(res, "Value"));
                                Console.Error.WriteLine(ex);
                                taskRes.SetException(ex);
                                break;
                            }
                        }
                    });
                    asyncTask();
                    return taskRes.Task;
                }
                return res;
                // Debug.Log($"{method}({string.Join(", ", args.ToList())}) => {res}");
            }

            if (ignoreMethodMissing)
            {
                return null;
            }
            throw new Exception($"{method} is not found in {bridge}.jsController");
        }
        #endregion

        private JsValue jsController;
        
        public JavaScriptBridge(string jsContent)
        {
            jsController = jsEngine.Evaluate(jsContent);
        }

        public object Call(string method)
        {
            return TryCallJsControllerMethod(this, method, false);
        }
        
        public object Call(string method, params JsValue[] args)
        {
            return TryCallJsControllerMethod(this, method, false, args);
        }
        
    }
}