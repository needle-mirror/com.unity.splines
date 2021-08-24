using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Coding.Editor.ApiScraping;

class ApiFilesGenerated
{
    [Test]
    public void APIScraping_AllFilesUpToDate()
    {
        var files = new List<string>();
        var allScraped = ApiScraping.ValidateAllFilesScraped(files);

        Assert.IsTrue(allScraped, "Some .api files have not been generated. Please make sure to run " +
            "\"Window/Internal/Build API Files\" or configure the com.unity.coding package to your project in order " +
            "to regenerate api files. Here are the api files that need to be generated:\n" +
            string.Join("\n\t", files));
    }
}
