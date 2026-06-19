using FluentAssertions;
using InventoryShared.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace InventoryTests;

public class OrderConcurrencyTests
{
    [Fact]
    public async Task Test_Stock()
    {
        // arrange
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var request1 = new CreateOrderRequestDto
        {
            CustomerId = 1,
            ShippingAddress = "Bandung",
            Items =
            [
                new CreateOrderItemDto
                {
                    ProductId = 1,
                    Quantity = 10
                }
            ]
        };

        var request2 = new CreateOrderRequestDto
        {
            CustomerId = 2,
            ShippingAddress = "Jakarta",
            Items =
            [
                new CreateOrderItemDto
                {
                    ProductId = 1,
                    Quantity = 10
                }
            ]
        };

        // act
        var task1 = SendOrderAsync(client, request1, "test-concurrent-001");
        var task2 = SendOrderAsync(client, request2, "test-concurrent-002");

        var responses = await Task.WhenAll(task1, task2);

        // assert
        responses.Count(x => x.StatusCode == HttpStatusCode.Created)
            .Should().Be(1);

        responses.Count(x => x.StatusCode == HttpStatusCode.Conflict)
            .Should().Be(1);
    }

    private static Task<HttpResponseMessage> SendOrderAsync(
        HttpClient client,
        CreateOrderRequestDto request,
        string idempotencyKey)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(request)
        };

        httpRequest.Headers.Add("Idempotency-Key", idempotencyKey);

        return client.SendAsync(httpRequest);
    }
}