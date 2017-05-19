namespace Aranea.HttpMessageHandler
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Eru;
    using Microsoft.AspNetCore.Http;

    public class AspNetCoreHttpMessageHandler : HttpMessageHandler
    {
        private readonly RequestDelegate _application;
        private bool _disposed;
        private bool _operationStarted;
        private bool _useCookies;

        /// <summary>
        /// Create a handler using a request delegate
        /// </summary>
        /// <param name="application"></param>
        public AspNetCoreHttpMessageHandler(RequestDelegate application) => _application =
            application ?? throw new ArgumentNullException(nameof(application));

        /// <summary>
        /// Create a handler using a Middleware delegate
        /// </summary>
        /// <param name="middleware">A middleware function that will terminate with 404 response</param>
        public AspNetCoreHttpMessageHandler(Middleware middleware) =>
            _application = middleware?.Invoke(context =>
            {
                context.Response.StatusCode = 404;
                return Task.FromResult(0);
            }) ?? throw new ArgumentNullException(nameof(middleware));

        private static Predicate<bool> False => _ => _ == false;

        public int AutoRedirectLimit { private get; set; } = 20;

        public bool AllowAutoRedirect { private get; set; }

        public CookieContainer CookieContainer { get; } = new CookieContainer();

        public Either<Unit, InvalidOperationException> UseCookies(bool useCookies)
        {
            return _disposed
                .Check(False, "It is not permitted to change cookie usage after disposing")
                .Bind(_ => _operationStarted.Check(False,
                    "It is not permitted to change cookie usage after initial operation"))
                .Map(_ =>
                {
                    _useCookies = useCookies;
                    return Unit.Instance;
                })
                .MapOtherwise(
                    error => new InvalidOperationException(error.Messages.Aggregate((s, s1) => s + " " + s1)));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _operationStarted = true;

            var response = await SendAsyncCore(request, cancellationToken).ConfigureAwait(false);

            var redirectCount = 0;
            var statusCode = (int) response.StatusCode;

            while (AllowAutoRedirect &&
                   (IsRedirectToGet(statusCode) ||
                    IsBodylessRequest(request) && statusCode == 307))
            {
                if (redirectCount >= AutoRedirectLimit)
                {
                    var httpProblemDetails = new HttpProblemDetails
                    {
                        Detail = $"The number of details exceeded the maximum allowed number of {AutoRedirectLimit}",
                        Title = "Too many redirects"
                    };
                    response.Content =
                        new StringContent(SimpleJson.SerializeObject(httpProblemDetails,
                            new CamelCasingSerializerStrategy()));
                    response.Content.Headers.ContentType = HttpProblemDetails.MediaTypeWithQualityHeaderValue;
                    return response;
                }
                var location = response.Headers.Location;
                if (!location.IsAbsoluteUri)
                    location = new Uri(response.RequestMessage.RequestUri, location);

                var redirectMethod = IsRedirectToGet(statusCode) ? HttpMethod.Get : request.Method;
                request.RequestUri = location;
                request.Method = redirectMethod;
                request.Headers.Authorization = null;
                CheckSetCookie(request, response);

                response = await SendAsyncCore(request, cancellationToken).ConfigureAwait(false);

                statusCode = (int) response.StatusCode;
                redirectCount++;
            }

            return response;
        }

        private static bool IsRedirectToGet(int code) => code == 301 || code == 302 || code == 303;

        private static bool IsBodylessRequest(HttpRequestMessage req) => req.Method == HttpMethod.Get ||
                                                                         req.Method == HttpMethod.Head || req.Method ==
                                                                         HttpMethod.Delete;

        private async Task<HttpResponseMessage> SendAsyncCore(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_useCookies)
            {
                var cookieHeader = CookieContainer.GetCookieHeader(request.RequestUri);
                if (!string.IsNullOrEmpty(cookieHeader))
                    request.Headers.Add("Cookie", cookieHeader);
            }

            var requestState = new RequestState(request, cancellationToken);
            var requestContent = request.Content ?? new StreamContent(Stream.Null);
            var body = await requestContent.ReadAsStreamAsync().ConfigureAwait(false);
            if (body.CanSeek)
                body.Seek(0, SeekOrigin.Begin);
            requestState.HttpContext.Request.Body = body;

            var registration = cancellationToken.Register(requestState.Abort);

            // Async offload, don't let the test code block the caller.
            var _ = Task.Run(async () =>
            {
                try
                {
                    await _application(requestState.HttpContext).ConfigureAwait(false);
                    requestState.CompleteResponse();
                }
                catch (Exception ex)
                {
                    requestState.Abort(ex);
                }
                finally
                {
                    registration.Dispose();
                    requestState.Dispose();
                }
            }, cancellationToken);

            var response = await requestState.ResponseTask.ConfigureAwait(false);
            CheckSetCookie(request, response);
            return response;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _disposed = true;
        }

        private void CheckSetCookie(HttpRequestMessage request, HttpResponseMessage response)
        {
            if (!_useCookies || !response.Headers.Contains("Set-Cookie")) return;

            var cookieHeader = string.Join(",", response.Headers.GetValues("Set-Cookie"));
            CookieContainer.SetCookies(request.RequestUri, cookieHeader);
        }
    }
}