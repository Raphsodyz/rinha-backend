using Model;
using Npgsql;

namespace rinha_backend.Model
{
    public class Job
    {
        public Guid Id { get; set; }
        public Pessoa Pessoa { get; set; }

        public static async Task BatchInsertPg(NpgsqlConnection connection, List<Job> jobs){
            try{
                List<string> dadosFormatados = new();
                foreach(var job in jobs){
                    string jobFormatado = $"{job.Pessoa}";
                    dadosFormatados.Add(jobFormatado);
                }
                _ = string.Join("\n", dadosFormatados);

                await connection.OpenAsync();
                using var leitor = connection.BeginTextImport("COPY PESSOA FROM stdin");
                leitor.Write(dadosFormatados);
            }
            finally{
                await connection.CloseAsync();
            }
        }
    }
}