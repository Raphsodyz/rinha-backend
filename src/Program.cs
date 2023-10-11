using System.ComponentModel.DataAnnotations;
using System.Data;
using Model;
using Npgsql;
using rinha_backend.Model;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;

string connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
RedisEndpoint config = new(){
    Host = $"{Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")}",
    Port = 6379,
};

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("pessoas", async (Pessoa pessoa) => {
    if(pessoa == null)
        Results.StatusCode(StatusCodes.Status400BadRequest);

    Pessoa.ValidaStack(pessoa);
    
    RedisClient client = new(config);
    using var redisClient = new RedisClient(config);
    using var connection = new NpgsqlConnection(connectionString);
    try{
        IRedisTypedClient<Job> job = redisClient.As<Job>();
        var jobList = job.Lists["Jobs"];
        Guid id = Guid.NewGuid();
        pessoa.Id = id;
        Job novoJob = new() { Id = id, Pessoa = pessoa };
        jobList.Add(novoJob);

        if(jobList.Count >= 1000){
            var listJobs = jobList.ToList();
            await Job.BatchInsertPg(connection, listJobs);
            jobList.RemoveAll();
        }
        
        var location = new Uri($"/pessoas/{id}", UriKind.Relative);
        return Results.Created(location, StatusCodes.Status201Created);
    }
    catch(ValidationException){
        return Results.StatusCode(StatusCodes.Status400BadRequest);
    }
    catch(Exception){
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
    finally{
        redisClient.Dispose();
        await connection.CloseAsync();
    }
});

app.MapGet("pessoas/{id}", async (Guid id) => {
    using var connection = new NpgsqlConnection(connectionString);
    
    try{
        await connection.OpenAsync();
        using var cmd = connection.CreateCommand();
        cmd.Parameters.AddWithValue("@id", id);
        cmd.CommandText = "SELECT * FROM PESSOA WHERE ID = @id";
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
    using var connection = new NpgsqlConnection(connectionString);
    
    if(string.IsNullOrWhiteSpace(t))
        return Results.StatusCode(StatusCodes.Status400BadRequest);

    try{
        await connection.OpenAsync();
        using var cmd = connection.CreateCommand();
        cmd.Parameters.AddWithValue("@t", $"%{t}%");

        cmd.CommandText = "SELECT * FROM PESSOA WHERE BUSCA ILIKE @t LIMIT 50";
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
    using var connection = new NpgsqlConnection(connectionString);

    try{
        await connection.OpenAsync();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM PESSOA";
        var reader = await cmd.ExecuteScalarAsync();
        
        return Results.Ok(reader);
    }
    finally{
        await connection.CloseAsync();
    }
});

app.Run();