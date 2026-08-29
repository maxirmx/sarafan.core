// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

using Sarafan.Core;

namespace Sarafan.Core.Tests;

[TestFixture]
public sealed class StatusEndpointTests
{
    private WebApplicationFactory<Program> _application = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        _application = new WebApplicationFactory<Program>();
        _client = _application.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _application.Dispose();
    }

    [Test]
    public async Task Root_RedirectsToStatusEndpoint()
    {
        using var response = await _client.GetAsync("/");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
            Assert.That(response.Headers.Location?.OriginalString, Is.EqualTo("/api/status/status"));
        }
    }

    [Test]
    public async Task Status_ReturnsServiceStatus()
    {
        using var response = await _client.GetAsync("/api/status/status");
        await using var responseStream = await response.Content.ReadAsStreamAsync();
        using var payload = await JsonDocument.ParseAsync(responseStream);
        var root = payload.RootElement;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
            Assert.That(root.GetProperty("service").GetString(), Is.EqualTo("Sarafan.Core"));
            Assert.That(root.GetProperty("status").GetString(), Is.EqualTo("ok"));
            Assert.That(root.GetProperty("appVersion").GetString(), Is.EqualTo(VersionInfo.AppVersion));
        }
    }
}
