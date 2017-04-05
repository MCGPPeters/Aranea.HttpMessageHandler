using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Aranea.HttpMessageHandler
{
    internal class RequestState : IDisposable
    {
        private readonly HttpRequestMessage _request;
        private readonly ResponseStream _responseStream;
        private readonly TaskCompletionSource<HttpResponseMessage> _responseTcs;
        private readonly ResponseFeature _responseFeature;

        internal RequestState(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _request = request;
            _responseTcs = new TaskCompletionSource<HttpResponseMessage>();

            request.Headers.Host = request.RequestUri.IsDefaultPort
                ? request.RequestUri.Host
                : request.RequestUri.GetComponents(UriComponents.HostAndPort, UriFormat.UriEscaped);

            HttpContext = new DefaultHttpContext
            {
                RequestAborted = cancellationToken
            };
            var httpRequest = HttpContext.Request;
            httpRequest.Protocol = "HTTP/" + request.Version.ToString(2);
            httpRequest.Scheme = request.RequestUri.Scheme;
            httpRequest.Method = request.Method.ToString();
            httpRequest.Path = PathString.FromUriComponent(request.RequestUri);
            httpRequest.PathBase = PathString.Empty;
            httpRequest.QueryString = QueryString.FromUriComponent(request.RequestUri);

            _responseFeature = new ResponseFeature();
            HttpContext.Features.Set<IHttpResponseFeature>(_responseFeature);

            foreach (var header in request.Headers)
                httpRequest.Headers.Add(header.Key, header.Value.ToArray());
            if (request.Content != null)
            {
                // Need to touch the ContentLength property for it to be calculated and added
                // to the request.Content.Headers collection.
                var _ = request.Content.Headers.ContentLength;

                foreach (var header in request.Content.Headers)
                    httpRequest.Headers.Add(header.Key, header.Value.ToArray());
            }

            _responseStream = new ResponseStream(CompleteResponse);
            HttpContext.Response.Body = _responseStream;
            HttpContext.Response.StatusCode = 200;
        }

        public Task<HttpResponseMessage> ResponseTask => _responseTcs.Task;

        internal HttpContext HttpContext { get; }

        public void Dispose()
        {
            _responseStream?.Dispose();
            // Do not dispose the request, that will be disposed by the caller.
        }

        internal void CompleteResponse()
        {
            if (!_responseTcs.Task.IsCompleted)
            {
                var response = GenerateResponse();
                // Dispatch, as TrySetResult will synchronously execute the waiters callback and block our Write.
                Task.Factory.StartNew(async () => _responseTcs.TrySetResult(await response));
            }
        }

        private async Task<HttpResponseMessage> GenerateResponse()
        {
            await _responseFeature.FireOnSendingHeadersAsync();
            
            var response = new HttpResponseMessage
            {
                StatusCode = (HttpStatusCode)HttpContext.Response.StatusCode,
                RequestMessage = _request,
                Content = new StreamContent(_responseStream)
            };

            foreach (var header in HttpContext.Response.Headers)
                if (!response.Headers.TryAddWithoutValidation(header.Key, new[] { header.Value.ToString() }))
                    response.Content.Headers.TryAddWithoutValidation(header.Key, new[] { header.Value.ToString() });
            return response;
        }

        internal void Abort()
        {
            Abort(new OperationCanceledException());
        }

        internal void Abort(Exception exception)
        {
            _responseStream.Abort(exception);
            _responseTcs.TrySetException(exception);
        }
    }
}
