// ReSharper disable once CheckNamespace
namespace System.Net.Http
{
    using Microsoft.AspNetCore.Http;

    public delegate RequestDelegate Middleware(RequestDelegate requestDelegate);
}