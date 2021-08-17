using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NUnit.Framework
{
    public static class TestCaseAssetLoader
    {
        public static string GetAssetPath(string assetPath, [CallerFilePath] string callerFilePath = default!)
            => Path.Combine(
                Path.GetDirectoryName(callerFilePath)!,
                assetPath);
    }
}
