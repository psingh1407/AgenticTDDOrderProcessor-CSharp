// ***************************************************************************
// Copyright (c) 2026, Industrial Logic, Inc., All Rights Reserved.
//
// This code is the exclusive property of Industrial Logic, Inc. It may ONLY be
// used by students during Industrial Logic's workshops or by individuals
// who are being coached by Industrial Logic on a project.
//
// This code may NOT be copied or used for any other purpose without the prior
// written consent of Industrial Logic, Inc.
// ****************************************************************************

using Microsoft.Extensions.FileProviders;
using System.Text.Json.Serialization;
using OrderProcessor.Domain;
using OrderProcessor.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IOrderRepository>(_ => new JsonOrderRepository("orders.json"));
builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

var frontendPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "frontend"));
if (Directory.Exists(frontendPath))
{
    app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(frontendPath) });
    app.MapGet("/", () => Results.File(Path.Combine(frontendPath, "index.html"), "text/html"));
}

app.MapPost("/api/orders", (IOrderRepository repo) =>
{
    var order = repo.Create();
    return Results.Created($"/api/orders/{order.Id}", order);
});

app.MapGet("/api/orders", (IOrderRepository repo) =>
    Results.Ok(repo.GetAll()));

app.MapPost("/api/orders/{id}/products", (Guid id, Product product, IOrderRepository repo) =>
{
    var order = repo.GetById(id);
    if (order is null) return Results.NotFound();
    order.Products.Add(product);
    repo.Save(order);
    return Results.Created($"/api/orders/{id}", order);
});

app.MapDelete("/api/orders", (IOrderRepository repo) =>
{
    repo.Clear();
    return Results.NoContent();
});

app.MapPost("/api/orders/{id}/confirm", (Guid id, IOrderRepository repo) =>
{
    var order = repo.GetById(id);
    if (order is null) return Results.NotFound();
    try
    {
        order.Confirm();
        repo.Save(order);
        return Results.Ok(order);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

app.Run();

public partial class Program { }
