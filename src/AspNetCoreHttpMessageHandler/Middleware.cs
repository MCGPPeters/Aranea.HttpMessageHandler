// ReSharper disable once CheckNamespace

using Microsoft.AspNetCore.Http;

namespace System.Net.Http
{
    public delegate RequestDelegate Middleware(RequestDelegate requestDelegate);
}