using System.ComponentModel.DataAnnotations;
using System.Data;
using Model;
using Npgsql;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;

const int PG_MAX_INSERT_VALUE = 999;
string connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
RedisEndpoint config = new(){
    Host = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING"),
    Port = 6379
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
        IRedisTypedClient<Pessoa> job = redisClient.As<Pessoa>();
        var jobList = job.Lists["Pessoas"];
        pessoa.Id = Guid.NewGuid();
        jobList.Add(pessoa);

        if(jobList.Count >= PG_MAX_INSERT_VALUE){
            var listPessoas = jobList.ToList();
            await Pessoa.BatchInsertPg(connection, listPessoas);
            jobList.RemoveAll();
        }
        
        var location = new Uri($"/pessoas/{pessoa.Id}", UriKind.Relative);
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
    RedisClient client = new(config);
    using var redisClient = new RedisClient(config);
    using var connection = new NpgsqlConnection(connectionString);
    try{
        var pessoa = Pessoa.FindByIdRedis(redisClient, id) ?? Pessoa.FindByIdPostgres(connection, id);
        if (pessoa == null)
            return Results.StatusCode(StatusCodes.Status404NotFound);
        
        return Results.Ok(pessoa.Result);
    }
    catch(ValidationException){
        return Results.StatusCode(StatusCodes.Status422UnprocessableEntity);
    }
    catch(Exception){
        return Results.StatusCode(StatusCodes.Status400BadRequest);
    }
    finally{
        redisClient.Dispose();
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
            pessoas.Add(Pessoa.GetPessoa(reader));
                
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