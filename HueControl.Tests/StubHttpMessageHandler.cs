// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Net;
using System.Net.Http;
using System.Text;

namespace HueControl.Tests;

/// <summary>Records outgoing requests and returns canned responses for API tests.</summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, string, (HttpStatusCode Status, string Json)> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, string, (HttpStatusCode, string)> responder)
        => _responder = responder;

    public List<RecordedRequest> Requests { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

        Requests.Add(new RecordedRequest(request.Method, request.RequestUri!.PathAndQuery, body));

        (HttpStatusCode status, string json) = _responder(request, body);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    internal sealed record RecordedRequest(HttpMethod Method, string Path, string Body);
}
