using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;
using Npgsql;


namespace Model
{
    public class Pessoa
    {
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
        public DateOnly? Nascimento { get; set; }
        
        [Column("STACK")]
        public string Stack { get; set; }

        private IEnumerable<string> stackLista => Stack?.Split(',').Select(s => s.Trim()).AsEnumerable();

        internal static void ValidaStack(Pessoa pessoa){
            if(pessoa.stackLista?.Count() != 0)
                foreach (string valor in pessoa.stackLista)
                    if(valor.Length > 32 || valor.Length == 0)
                        throw new ValidationException();
        }

        public static async Task BatchInsertPg(NpgsqlConnection connection, List<Pessoa> listaPessoa){
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
            catch(Exception ex){
                string jaja = ex.Message;
                trans.Rollback();
            }
            finally{
                await connection.CloseAsync();
                trans.Dispose();
            }
        }
    }
}