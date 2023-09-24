using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Model;
using Npgsql;
using rinha_backend.Context;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<RinhaContext>();
var app = builder.Build();

app.MapGet("pessoas/{id}", async (Guid id) => {
    
    using var connection = new NpgsqlConnection(RinhaContext.ConnectionString());
    
    try{
        await connection.OpenAsync();
        using var cmd = new NpgsqlCommand(RinhaContext.Get(id));
        cmd.Parameters.AddWithValue("@id", id);
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

app.Map("/pessoas", ([FromQuery]Busca t) => {
    
});

app.Run();
