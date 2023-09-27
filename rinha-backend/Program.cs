using System.ComponentModel.DataAnnotations;
using System.Data;
using Model;
using Npgsql;
using rinha_backend.Context;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<RinhaContext>();
var app = builder.Build();

app.MapPost("pessoas", async (Pessoa pessoa) => {
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

        var location = new Uri($"/pessoas/{(Guid)novaPessoa}", UriKind.Relative);
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

app.MapGet("/pessoas", async (string? t) => {
    using var connection = new NpgsqlConnection(RinhaContext.ConnectionString());
    
    if(string.IsNullOrWhiteSpace(t))
        return Results.StatusCode(StatusCodes.Status400BadRequest);

    try{
        await connection.OpenAsync();
        using var cmd = connection.CreateCommand();
        cmd.Parameters.AddWithValue("@t", $"%{t}%");

        cmd.CommandText = RinhaContext.GetParam(t);
        var reader = await cmd.ExecuteReaderAsync();

        List<Pessoa> pessoas = new();
        while(reader.Read())
            pessoas.Add(new Pessoa(){
                Id = reader.GetGuid(0),
                Nome = reader.GetString(1),
                Apelido =reader.GetString(2),
                Nascimento = reader.IsDBNull(3) ? new DateOnly() : reader.GetFieldValue<DateOnly>(3),
                Stack = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
            });
                
        return Results.Ok(pessoas);
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
