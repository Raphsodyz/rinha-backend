using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Model;
using Npgsql;
using rinha_backend.Context;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<RinhaContext>();
var app = builder.Build();

app.MapPost("pessoas", async ([FromBody]Pessoa pessoa) => {
    if(pessoa == null)
        Results.StatusCode(StatusCodes.Status400BadRequest);

    Pessoa.ValidaStack(pessoa);
    using var connection = new NpgsqlConnection(RinhaContext.ConnectionString());
    
    try{
        await connection.OpenAsync();
        using var cmd = connection.CreateCommand();
        
        cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("@Nome", pessoa.Nome);
        cmd.Parameters.AddWithValue("@Apelido", pessoa.Apelido);
        cmd.Parameters.AddWithValue("@Nascimento", pessoa.Nascimento);
        cmd.Parameters.AddWithValue("@Stack", pessoa.Stack);
        cmd.CommandText = RinhaContext.Post(pessoa);
        
        var novaPessoa = await cmd.ExecuteScalarAsync();
        if(novaPessoa == null)
            return Results.StatusCode(StatusCodes.Status500InternalServerError);

        var location = new Uri($"/pessoas/{novaPessoa}", UriKind.Relative);
        return Results.Created(location, StatusCodes.Status201Created);
    }
    catch(ValidationException){
        return Results.StatusCode(StatusCodes.Status400BadRequest);
    }
    catch(Exception){
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
    finally{
        await connection.CloseAsync();
    }
});

app.MapGet("pessoas/{id}", async (Guid id) => {
    using var connection = new NpgsqlConnection(RinhaContext.ConnectionString());
    
    try{
        await connection.OpenAsync();
        using var cmd = connection.CreateCommand();
        cmd.Parameters.AddWithValue("@id", id);
        cmd.CommandText = RinhaContext.Get(id);
        using var reader = await cmd.ExecuteReaderAsync();

        DataTable table = new();
        table.Load(reader);

        if(table.Rows.Count == 0)
            return Results.StatusCode(StatusCodes.Status404NotFound);
        
        return Results.Ok(table);
    }
    catch(ValidationException){
        return Results.StatusCode(StatusCodes.Status422UnprocessableEntity);
    }
    catch(Exception){
        return Results.StatusCode(StatusCodes.Status400BadRequest);
    }
    finally{
        await connection.CloseAsync();
    }
});

app.MapGet("/pessoas", async (string? nome, string? apelido, string? stack) => {
    using var connection = new NpgsqlConnection(RinhaContext.ConnectionString());
    
    if(string.IsNullOrWhiteSpace(nome) && string.IsNullOrWhiteSpace(apelido) && string.IsNullOrWhiteSpace(stack))
        return Results.StatusCode(StatusCodes.Status400BadRequest);

    try{
        await connection.OpenAsync();
        using var cmd = connection.CreateCommand();

        if(!string.IsNullOrWhiteSpace(nome))
            cmd.Parameters.AddWithValue("@nome", nome);
        else if(!string.IsNullOrWhiteSpace(apelido))
            cmd.Parameters.AddWithValue("@apelido", apelido);
        else if(!string.IsNullOrWhiteSpace(stack))
            cmd.Parameters.AddWithValue("@stack", stack);

        cmd.CommandText = RinhaContext.GetParam(nome, apelido, stack);
        using var reader = await cmd.ExecuteReaderAsync();

        DataTable table = new();
        table.Load(reader);
        
        return Results.Ok(table);
    }
    finally{
        await connection.CloseAsync();
    }
});

app.MapGet("/contagem-pessoas", async () => {
    using var connection = new NpgsqlConnection(RinhaContext.ConnectionString());

    try{
        await connection.OpenAsync();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = RinhaContext.Count();
        var reader = await cmd.ExecuteScalarAsync();
        
        return Results.Ok(reader);
    }
    finally{
        await connection.CloseAsync();
    }
});

app.Run();
