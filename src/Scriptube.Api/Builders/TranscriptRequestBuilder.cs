using Scriptube.Api.Contracts;

namespace Scriptube.Api.Builders;

public sealed class TranscriptRequestBuilder
{
    private readonly List<string> _urls = [];
    private bool _translateToEnglish;
    private bool _useByok;

    public TranscriptRequestBuilder WithUrl(string url)
    {
        _urls.Add(url);
        return this;
    }

    public TranscriptRequestBuilder WithUrls(IEnumerable<string> urls)
    {
        _urls.AddRange(urls);
        return this;
    }

    public TranscriptRequestBuilder TranslateToEnglish(bool value = true)
    {
        _translateToEnglish = value;
        return this;
    }

    public TranscriptRequestBuilder UseByok(bool value = true)
    {
        _useByok = value;
        return this;
    }

    public TranscriptSubmitRequest BuildSubmit()
    {
        return new TranscriptSubmitRequest
        {
            Urls = [.. _urls],
            TranslateToEnglish = _translateToEnglish,
            UseByok = _useByok
        };
    }

    public CreditsPrecheckRequest BuildPrecheck()
    {
        return new CreditsPrecheckRequest
        {
            Urls = [.. _urls],
            TranslateToEnglish = _translateToEnglish,
            UseByok = _useByok
        };
    }

    public CreditsEstimateRequest BuildEstimateByVideoIds(IEnumerable<string> videoIds)
    {
        return new CreditsEstimateRequest
        {
            VideoIds = [.. videoIds],
            TranslateToEnglish = _translateToEnglish,
            UseByok = _useByok
        };
    }
}