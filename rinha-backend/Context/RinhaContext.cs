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
            return "Host=localhost;Port=5432;Database=jajaj;User ID=postgres;Password=root;" +
                "Pooling=true;MinPoolSize=1;MaxPoolSize=1024;";
        }

        public static string Get(Guid id){
            return "SELECT * FROM PESSOA WHERE ID = @id"; 
        }

        public static string GetParam(string nome, string apelido, string stack){
            string query = "SELECT * FROM PESSOA WHERE 1=1 ";
            
            if(!string.IsNullOrWhiteSpace(nome))
                query += "AND NOME ILIKE @nome ";
            else if(!string.IsNullOrWhiteSpace(apelido))
                query += "AND APELIDO ILIKE @apelido ";
            else if(!string.IsNullOrWhiteSpace(stack))
                query += "AND STACK ILIKE @stack ";
            else
                throw new ArgumentNullException();
            
            query += "LIMIT 50";
            return query;
        }

        public static string Count(){
            return "SELECT COUNT(1) FROM PESSOA";
        }

        public static string Post(Pessoa pessoa){
            return @"INSERT INTO PESSOA (ID, NOME, APELIDO, NASCIMENTO, STACK)
                    VALUES (@Id, @Nome, @Apelido, @Nascimento, @Stack) RETURNING ID";
        }
    }
}