using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
        public DateTime Nascimento { get; set; }
        
        [StringLength(32)]
        [Column("STACK")]
        public ICollection<string> Stack { get; set; }
    }
}