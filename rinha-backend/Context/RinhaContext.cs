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
            return "Server=localhost;Port=5432;Database=rinha;User Id=root;Password=root;" +
                "Pooling=true;MinPoolSize=1;MaxPoolSize=1024;";
        }

        public static string Get(Guid id){
            return @"SELECT * FROM PESSOA WHERE ID = @id"; 
        }

        public static string GetParam(Busca t){
            string query = "SELECT * FROM PESSOA WHERE 1=1 ";
            
            if(!string.IsNullOrWhiteSpace(t?.Nome))
                query += "AND NOME ILIKE @t.Nome";
            else if(!string.IsNullOrWhiteSpace(t?.Apelido))
                query += "AND APELIDO ILIKE @t.Apelido";
            else if(!string.IsNullOrWhiteSpace(t?.Stack))
                query += "AND STACK ILIKE @t.Stack";
            else
                throw new ArgumentNullException();
            
            return query;
        }

        public static string Count(){
            return @"SELECT COUNT(ID) FROM PESSOA";
        }

        public static string Post(Pessoa pessoa){
            return @"INSERT INTO PESSOA (NOME, APELIDO, NASCIMENTO, STACK)" + 
                    @"VALUES (@Nome, @Apelido, @Nascimento, @Stack) RETURNING ID";
        }
    }
}