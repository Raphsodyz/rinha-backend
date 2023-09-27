using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;


namespace Model
{
    public class Pessoa
    {
        [Key]
        [Required]
        [JsonIgnore]
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
    }
}