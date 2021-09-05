using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace SeqLoggerProvider.Test
{
    public static class TestAssetLoader
    {
        public static async Task<JsonDocument> LoadJsonAsync(
            string                  relativeAssetFilename,
            [CallerFilePath]string  callerFilePath          = "")
        {
            using var asset = File.OpenRead(Path.Combine(
                Path.GetDirectoryName(callerFilePath)!,
                relativeAssetFilename));

            return await JsonDocument.ParseAsync(asset);
        }
    }
}
