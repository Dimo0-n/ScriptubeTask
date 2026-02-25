using Scriptube.Core.Configuration;

namespace Scriptube.Tests.Shared;

public abstract class ScriptubeTestBase
{
    protected ScriptubeSettings Settings { get; private set; } = default!;

    [SetUp]
    public void BaseSetUp()
    {
        Settings = ScriptubeSettingsLoader.Load();
    }
}