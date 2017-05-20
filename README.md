# Aranea.HttpMessageHandler

A port of Damian Hickey's [OwinHttpMessageHandler](https://github.com/damianh/OwinHttpMessageHandler) so that the concept can be used for ASP .NET core as well. The readme below shows this as it is also a port of the associated readme :) .

[![NuGet Latest Stable https://www.nuget.org/packages/Aranea.HttpMessageHandler](https://img.shields.io/nuget/v/Aranea.HttpMessageHandler.svg)](https://www.nuget.org/packages/Aranea.HttpMessageHandler)
[![Build status](https://ci.appveyor.com/api/projects/status/efb11scf69h28i33?svg=true)](https://ci.appveyor.com/project/MCGPPeters/aranea-httpmessagehandler)

An implementation of System.Net.Http.HttpMessageHandler that translates an HttpRequestMessage into an ASP.NET Core compatible HttpContext, calls the supplied RequestDelegate and translates the result to an HttpResponseMessage. This allows you to call an ASP.NET Core application (RequestDelegate) / Middleware using an HttpClient without actually hitting the network stack. Useful for testing and embedded scenarios.

Install via NuGet : [![NuGet Latest Stable https://www.nuget.org/packages/Aranea.HttpMessageHandler](https://img.shields.io/nuget/v/Aranea.HttpMessageHandler.svg)](https://www.nuget.org/packages/Aranea.HttpMessageHandler)

## Using

```C#
var handler = new AspNetCoreHttpMessageHandler(appFunc) // Alternatively you can pass in Middleware
{
    UseCookies = true, // Will send cookies on subsequent requests. Default is false.
    AllowAutoRedirect = true // The handler will auto follow 301/302
}
var httpClient = new HttpClient(handler)
{
    BaseAddress = new Uri("http://localhost")
}

var response = await httpClient.GetAsync("/");

```

By default, the HttpContext is defined to look as though the source of the request is local. You can adjust the  HttpContext via a delegate:

```C#

var httpClient = new HttpClient(new AspNetCoreHttpMessageHandler(context =>
{
    if (context.Request.Path == $"/{requestPath}")
        context.Response.StatusCode = 200;

        ...
}));

```

More information on [Http Message Handlers](http://www.asp.net/web-api/overview/working-with-http/http-message-handlers)

