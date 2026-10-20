using System.Reflection;
using System.Runtime.InteropServices;
using Activities.WinUI;
using Xunit;

namespace Activities.WinUI.Tests
{
    public class NativeUiPathProviderTests
    {
        private static DllImportAttribute GetDllImport(string methodName)
        {
            var method = typeof(NativeUiPathProvider)
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            return method.GetCustomAttribute<DllImportAttribute>();
        }

        [Fact]
        public void GetUiXPathNative_MapsToGetUiXPathExport()
        {
            var attribute = GetDllImport("GetUiXPathNative");

            Assert.NotNull(attribute);
            Assert.Equal("UIXPathLib.dll", attribute.Value);
            // Имя метода обёртки (GetUiXPathNative) не совпадает с реальным
            // экспортом DLL (GetUiXPath), поэтому точка входа задана явно.
            Assert.Equal("GetUiXPath", attribute.EntryPoint);
        }

        [Fact]
        public void InitAndShutdown_UseTheirOwnNamesAsEntryPoints()
        {
            // InitUiTreeWalk/UnInitUiTreeWalk экспортируются под теми же именами,
            // что и методы P/Invoke, поэтому EntryPoint совпадает с именем метода.
            Assert.Equal("UIXPathLib.dll", GetDllImport("InitUiTreeWalk").Value);
            Assert.Equal("InitUiTreeWalk", GetDllImport("InitUiTreeWalk").EntryPoint);

            Assert.Equal("UIXPathLib.dll", GetDllImport("UnInitUiTreeWalk").Value);
            Assert.Equal("UnInitUiTreeWalk", GetDllImport("UnInitUiTreeWalk").EntryPoint);
        }
    }
}
