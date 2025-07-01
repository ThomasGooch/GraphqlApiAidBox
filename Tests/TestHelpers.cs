using System;
using System.IO;

namespace GraphqlApiAidBox.Tests
{
    public static class TestHelpers
    {
        public static string CreateTempQueriesDirWithFile(string fileName, string fileContent)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            var filePath = Path.Combine(tempDir, fileName);
            File.WriteAllText(filePath, fileContent);
            return tempDir;
        }
    }
}
