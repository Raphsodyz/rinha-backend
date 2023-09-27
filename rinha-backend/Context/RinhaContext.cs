using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Model;

namespace rinha_backend.Context
{
    public class RinhaContext
    {
        public static string ConnectionString(){
            return "Host=localhost;Port=5432;Database=rinha;User ID=postgres;Password=;" +
                "Pooling=true;MinPoolSize=1;MaxPoolSize=1024;";
        }

        public static string Get(Guid id){
            return "SELECT * FROM PESSOA WHERE ID = @id"; 
        }

        public static string GetParam(string t){
            return "SELECT ID, NOME, APELIDO, NASCIMENTO, STACK FROM PESSOA WHERE BUSCA ILIKE @t LIMIT 50";
        }

        public static string Count(){
            return "SELECT COUNT(1) FROM PESSOA";
        }

        public static string Post(Pessoa pessoa){
            return @"INSERT INTO PESSOA (ID, NOME, APELIDO, NASCIMENTO, STACK) VALUES (@Id, @Nome, @Apelido, @Nascimento, @Stack) RETURNING ID";
        }
    }
}