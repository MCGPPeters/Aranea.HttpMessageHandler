using System.Collections.Generic;

namespace Aranea.HttpMessageHandler
{
    public class HttpProblemDetailsEqualityComparer : IEqualityComparer<HttpProblemDetails>
    {
        public bool Equals(HttpProblemDetails x, HttpProblemDetails y)
        {
            return x.Status == y.Status &&
                   x.Detail == y.Detail &&
                   x.Title == y.Title &&
                   x.Type == y.Type &&
                   x.Instance == y.Instance;
        }

        public int GetHashCode(HttpProblemDetails obj)
        {
            return obj.GetHashCode();
        }
    }
}