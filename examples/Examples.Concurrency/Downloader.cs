namespace Examples.Concurrency;

// The HttpClient listing from the async/await section.  This one is compiled but
// never called: the point of the examples tree is that the book's code builds,
// and a build must not depend on the network being up or on majorsilence.com
// answering.
public class Downloader
{
    public async Task<string> DownloadSiteAsync(HttpClient httpClient,
        string url,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(url)
        };

        // proceed past user agent sniffing
        request.Headers.Add("User-Agent", "Mozilla/5.0 (X11; CrOS x86_64 14541.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36");

        HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}
