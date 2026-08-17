using System.Net;
using System.Net.Http.Json;
using Claims.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Claims.Tests
{
    public class ClaimsControllerTests
    {
        [Fact]
        public async Task Get_Claims_ReturnsOkWithJsonArray()
        {
            var application = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(_ => { });

            var client = application.CreateClient();

            var response = await client.GetAsync("/Claims", TestContext.Current.CancellationToken);

            response.EnsureSuccessStatusCode();
            var claims = await response.Content.ReadFromJsonAsync<IEnumerable<ClaimResponse>>(TestContext.Current.CancellationToken);
            Assert.NotNull(claims);
        }

        [Fact]
        public async Task Get_UnknownClaim_ReturnsNotFound()
        {
            var application = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(_ => { });

            var client = application.CreateClient();

            var response = await client.GetAsync("/Claims/does-not-exist", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
