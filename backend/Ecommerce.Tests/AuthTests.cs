using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Core.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ecommerce.Tests.Integration;

public class AuthTests(TestFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Register_Login_Me_Work()
    {
        var email = NewEmail();
        var reg = await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password!123", "Ada", "Lovelace"));
        reg.StatusCode.Should().Be(HttpStatusCode.Created);
        reg.Headers.Location.Should().NotBeNull();
        var auth = await reg.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.User.Email.Should().Be(email);

        var login = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password!123"));
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var authed = CreateAuthed(Client, auth.AccessToken);
        var me = await authed.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        var meBody = await me.Content.ReadFromJsonAsync<UserDto>();
        meBody!.Email.Should().Be(email);
    }

    [Fact]
    public async Task Register_DuplicateEmail_409()
    {
        var email = NewEmail();
        (await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password!123", "A", "B"))).StatusCode.Should().Be(HttpStatusCode.Created);
        var dup = await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password!123", "A", "B"));
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_InvalidDto_400()
    {
        var bad = await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("not-an-email", "short", "", ""));
        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WrongPassword_401()
    {
        var email = NewEmail();
        await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password!123", "A", "B"));
        var bad = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Wrong!999"));
        bad.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithoutToken_401()
    {
        var me = await Client.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Rotates_And_OldToken_Rejected()
    {
        var email = NewEmail();
        var reg = await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password!123", "A", "B"));
        var first = await reg.Content.ReadFromJsonAsync<AuthResponse>();
        first.Should().NotBeNull();

        // Extract refresh cookie
        reg.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var refreshCookie = cookies!.First(c => c.StartsWith("refresh_token="));
        var raw = refreshCookie.Split(';')[0]["refresh_token=".Length..];

        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        req1.Headers.Add("Cookie", $"refresh_token={raw}");
        var r1 = await Client.SendAsync(req1);
        r1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Reuse of rotated token must fail
        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        req2.Headers.Add("Cookie", $"refresh_token={raw}");
        var r2 = await Client.SendAsync(req2);
        r2.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_InvalidToken_401()
    {
        var r = await Client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest("bogus-token"));
        r.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Revokes_RefreshToken()
    {
        var email = NewEmail();
        var reg = await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password!123", "A", "B"));
        var auth = await reg.Content.ReadFromJsonAsync<AuthResponse>();
        reg.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var raw = cookies!.First(c => c.StartsWith("refresh_token=")).Split(';')[0]["refresh_token=".Length..];

        var authed = CreateAuthed(Client, auth!.AccessToken);
        var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logout.Headers.Add("Cookie", $"refresh_token={raw}");
        (await authed.SendAsync(logout)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var retry = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        retry.Headers.Add("Cookie", $"refresh_token={raw}");
        (await Client.SendAsync(retry)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateAuthed(HttpClient baseClient, string token)
    {
        var c = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }
}
