using Allure.Net.Commons;

namespace Scriptube.Tests.Shared;

public static class AllureAttachmentHelper
{
    public static void AttachText(string name, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var attachmentsDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "TestResults", "attachments");
        Directory.CreateDirectory(attachmentsDir);

        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var fullPath = Path.Combine(attachmentsDir, $"{safeName}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.txt");

        File.WriteAllText(fullPath, content);
        TestContext.AddTestAttachment(fullPath, name);

        try
        {
            AllureApi.AddAttachment(name, "text/plain", fullPath);
        }
        catch
        {
        }
    }
}