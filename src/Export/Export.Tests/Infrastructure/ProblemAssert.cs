using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Export.Tests.Infrastructure;

public static class ProblemAssert
{
    public static async Task<ProblemDetails> IsProblemAsync(HttpResponseMessage response, HttpStatusCode status, string? detailContains = null)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)status, problem.Status);
        if (detailContains is not null)
        {
            Assert.Contains(detailContains, problem.Detail);
        }

        return problem;
    }
}