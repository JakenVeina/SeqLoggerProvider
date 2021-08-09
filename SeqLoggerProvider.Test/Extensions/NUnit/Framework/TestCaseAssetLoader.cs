using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NUnit.Framework
{
    public static class TestCaseAssetLoader
    {
        public static IReadOnlyList<TestCaseData> LoadAssetTestCases(string assetDirectoryPath)
            => Directory.EnumerateFiles(assetDirectoryPath)
                .Select(assetPath => new TestCaseData(assetPath).SetName($"{{m}}({Path.GetFileName(assetPath)})"))
                .ToArray();

        public static string GetAssetPath(string assetPath, [CallerFilePath] string callerFilePath = default!)
            => Path.Combine(
                Path.GetDirectoryName(callerFilePath)!,
                assetPath);
    }
}
