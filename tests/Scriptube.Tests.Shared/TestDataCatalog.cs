namespace Scriptube.Tests.Shared;

public static class TestDataCatalog
{
    public static class SuccessVideos
    {
        public const string EnglishManual = "https://www.youtube.com/watch?v=tstENMAN001";
        public const string EnglishAuto = "https://www.youtube.com/watch?v=tstENAUT001";
        public const string KoreanOnly = "https://www.youtube.com/watch?v=tstKOONL001";
        public const string SpanishOnly = "https://www.youtube.com/watch?v=tstESAUT001";
        public const string MultiLanguage = "https://www.youtube.com/watch?v=tstMULTI001";
    }

    public static class ErrorVideos
    {
        public const string PrivateVideo = "https://www.youtube.com/watch?v=tstPRIVT001";
        public const string TimeoutVideo = "https://www.youtube.com/watch?v=tstTIMEO001";
    }

    public static class Playlists
    {
        public const string SuccessPlaylist = "https://www.youtube.com/playlist?list=PLtstOK00001";
        public const string MixedPlaylist = "https://www.youtube.com/playlist?list=PLtstMIX0001";
    }
}