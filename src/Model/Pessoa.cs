using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;
using Npgsql;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;


namespace Model
{
    public class Pessoa
    {
        public Pessoa(Guid id, string nome, string apelido, DateOnly nascimento, string stack)
        {
            Id = id;
            Nome = nome;
            Apelido = apelido;
            Nascimento = nascimento;
            Stack = stack;
        }

        [Key]
        [Required]
        [Column("ID")]
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(100)]
        [Column("NOME")]
        public string Nome { get; set; }
        
        [Required]
        [StringLength(32)]
        [Column("APELIDO")]
        public string Apelido { get; set; }
        
        [Column("NASCIMENTO")]
        public DateOnly Nascimento { get; set; }
        
        [Column("STACK")]
        public string Stack { get; set; }

        private IEnumerable<string> stackLista => Stack?.Split(',').Select(s => s.Trim()).AsEnumerable();

        internal static void ValidaStack(Pessoa pessoa){
            if(pessoa.stackLista?.Count() != 0)
                foreach (string valor in pessoa.stackLista)
                    if(valor.Length > 32 || valor.Length == 0)
                        throw new ValidationException();
        }

        internal static async Task BatchInsertPg(NpgsqlConnection connection, List<Pessoa> listaPessoa){
            await connection.OpenAsync();
            using var trans = connection.BeginTransaction();
            
            try{
                using var cmd = connection.CreateCommand();
                
                var sqlQuery = new StringBuilder();
                sqlQuery.AppendFormat("INSERT INTO PESSOA (ID, NOME, APELIDO, NASCIMENTO, STACK) VALUES");
                int counter = 0;

                foreach(var val in listaPessoa){
                    sqlQuery.AppendFormat("(@Id{0}, @Nome{0}, @Apelido{0}, @Nascimento{0}, @Stack{0}),", counter);
                    cmd.Parameters.AddWithValue("@Id" + counter.ToString(), val.Id);
                    cmd.Parameters.AddWithValue("@Nome" + counter.ToString(), val.Nome);
                    cmd.Parameters.AddWithValue("@Apelido" + counter.ToString(), val.Apelido);
                    cmd.Parameters.AddWithValue("@Nascimento" + counter.ToString(), val.Nascimento);
                    cmd.Parameters.AddWithValue("@Stack" + counter.ToString(), val.Stack);
                    counter++;
                }
                sqlQuery.Remove(sqlQuery.Length - 1, 1);
                cmd.CommandText = sqlQuery.ToString();
                await cmd.ExecuteNonQueryAsync();

                trans.Commit();
            }
            catch(Exception){
                trans.Rollback();
            }
            finally{
                await connection.CloseAsync();
                trans.Dispose();
            }
        }
        internal static async Task<Pessoa> FindByIdRedis(RedisClient client, Guid id){
            IRedisTypedClient<Pessoa> redis = client.As<Pessoa>();
            var redisList = redis.Lists["Pessoas"];
            var pessoa = redisList.FirstOrDefault(p => p.Id == id);

            return pessoa;
        }
        internal static async Task<Pessoa> FindByIdPostgres(NpgsqlConnection connection, Guid id){
            await connection.OpenAsync();
            using var cmd = connection.CreateCommand();
            cmd.Parameters.AddWithValue("@id", id);
            cmd.CommandText = "SELECT * FROM PESSOA WHERE ID = @id";
            using var reader = await cmd.ExecuteReaderAsync();
            while(await reader.ReadAsync()){
                Pessoa pessoa = GetPessoa(reader);
                return pessoa;
            }
            return null;
        }
        internal static Pessoa GetPessoa(NpgsqlDataReader reader){
            Guid? Id = reader["Id"] as Guid?;
            string Nome = reader["Nome"] as string;
            string Apelido = reader["Apelido"] as string;
            DateOnly? Nascimento = reader["Nascimento"] as DateOnly?;
            string Stack = reader["Stack"] as string;

            Pessoa pessoa = new(Id ?? Guid.Empty, Nome, Apelido, Nascimento ?? new DateOnly(), Stack);
            return pessoa;
        }
    }
}