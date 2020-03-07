using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace R2DSEssentials
{
    internal static class NativeWrapper
    {
        private static bool _alreadyCalled;
        private static string _dllPath;

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string exposedFuncName);
        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);

        private const string NativeRemoveGarbageName = "UnityPlayerRemoveGarbage.dll";
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NativeRemoveGarbage();

        internal static void InjectRemoveGarbage()
        {
            if (!_alreadyCalled)
            {
                try
                {
                    var nativeDllPtr = LoadUnmanagedLibraryFromResource(Assembly.GetExecutingAssembly(),
                        "R2DSEssentials.NativeLibrary." + NativeRemoveGarbageName, NativeRemoveGarbageName);
                    var nativeFuncPtr = GetProcAddress(nativeDllPtr, "?RemoveGarbage@@YAHXZ");

                    var removeGarbage = (NativeRemoveGarbage)Marshal.GetDelegateForFunctionPointer(
                        nativeFuncPtr,
                        typeof(NativeRemoveGarbage));

                    var res = removeGarbage();

                    if (res == 0)
                    {
                        PluginEntry.Log.LogInfo("[R2DSE NativeWrapper] Done.");
                    }
                    else
                    {
                        PluginEntry.Log.LogError("[R2DSE NativeWrapper] NativeRemoveGarbage encountered an error : " + res);
                    }

                    FreeLibrary(nativeDllPtr);
                    File.Delete(_dllPath);
                }
                catch (Exception e)
                {
                    PluginEntry.Log.LogError("[R2DSE NativeWrapper] The Wrapper wasn't successful at doing its job. Please complain to iDeathHD");
                    PluginEntry.Log.LogError(e.ToString());
                }

                _alreadyCalled = true;
            }
            else
            {
                PluginEntry.Log.LogWarning("[R2DSE NativeWrapper] NativeRemoveGarbage has already been called.");
            }
        }

        
        private static IntPtr LoadUnmanagedLibraryFromResource(Assembly assembly, string libraryResourceName, string libraryName)
        {
            // ReSharper disable AssignNullToNotNullAttribute
            var assemblyPath = Path.GetDirectoryName(assembly.Location);
            _dllPath = Path.Combine(assemblyPath, libraryName);

            using (var stream = assembly.GetManifestResourceStream(libraryResourceName))
            {
                byte[] data = new BinaryReader(stream).ReadBytes((int)stream.Length);
                File.WriteAllBytes(_dllPath, data);
            }

            return LoadLibrary(_dllPath);
        }
    }
}
