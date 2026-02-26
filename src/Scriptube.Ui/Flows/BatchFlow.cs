using Scriptube.Ui.Pages;

namespace Scriptube.Ui.Flows;

public sealed class BatchFlow
{
    private readonly DashboardPage _dashboardPage;
    private readonly BatchDetailsPage _batchDetailsPage;

    public BatchFlow(DashboardPage dashboardPage, BatchDetailsPage batchDetailsPage)
    {
        _dashboardPage = dashboardPage;
        _batchDetailsPage = batchDetailsPage;
    }

    public async Task SubmitAndOpenDetailsAsync(IEnumerable<string> urls)
    {
        await _dashboardPage.SubmitBatchAsync(urls);

        var hasSignal = await _dashboardPage.HasBatchCreatedSignalAsync();
        if (!hasSignal)
        {
            throw new InvalidOperationException("Batch submission signal was not detected on dashboard.");
        }

        await _batchDetailsPage.TryOpenFromDashboardAsync();
    }
}
