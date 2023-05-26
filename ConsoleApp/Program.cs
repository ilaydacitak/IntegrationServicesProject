using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // MSSQL bağlantı bilgileri
            string connectionString = "data source=ILAYDCTK;initial catalog=SQLTurkiye;integrated security=True; TrustServerCertificate=True";

            // SQL betiğini oluşturmak için StringBuilder kullanma
            StringBuilder sqlScriptBuilder = new StringBuilder();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Tabloları okuma
                DataTable tablesSchema = connection.GetSchema("Tables");

                foreach (DataRow tableRow in tablesSchema.Rows)
                {
                    string tableName = tableRow["TABLE_NAME"].ToString();

                    // Tablo bilgilerini okuma
                    string query = @"
                        SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = @TableName";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@TableName", tableName);

                    DataTable columnsSchema = new DataTable();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        columnsSchema.Load(reader);
                    }

                    // Tabloya ait primary key sütunlarını al
                    List<string> primaryKeyColumns = new List<string>();
                    query = @"
                        SELECT COLUMN_NAME
                        FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                        WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1
                        AND TABLE_NAME = @TableName";

                    command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@TableName", tableName);


                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string primaryKeyColumn = reader["COLUMN_NAME"].ToString();
                            primaryKeyColumns.Add(primaryKeyColumn);
                        }
                    }

                    // CREATE TABLE ifadesini oluşturma
                    sqlScriptBuilder.AppendLine($"-- {tableName} tablosunu oluşturma");
                    sqlScriptBuilder.AppendLine($"CREATE TABLE {tableName} (");

                    // Sütunları dolaşarak SQL ifadesini oluşturma
                    foreach (DataRow columnRow in columnsSchema.Rows)
                    {
                        string columnName = columnRow["COLUMN_NAME"].ToString();
                        string dataType = columnRow["DATA_TYPE"].ToString();
                        string isNullable = columnRow["IS_NULLABLE"].ToString() == "YES" ? "NULL" : "NOT NULL";

                        // Sütunun primary key olup olmadığını kontrol et
                        string primaryKeyConstraint = primaryKeyColumns.Contains(columnName) ? "PRIMARY KEY" : "";

                        sqlScriptBuilder.AppendLine($"{columnName} {dataType} {isNullable} {primaryKeyConstraint},");
                    }

                    // Son sütundaki virgülü kaldırma
                    sqlScriptBuilder.Length -= 3;

                    // Tablonun kapanış parantezini ekleme
                    sqlScriptBuilder.AppendLine(");");

                    // FOREIGN KEY ilişkilerini okuma
                    /*query = @"
                        SELECT CONSTRAINT_NAME, FKCOLUMN_NAME, PKTABLE_NAME, PKCOLUMN_NAME
                        FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS
                        WHERE CONSTRAINT_SCHEMA = 'dbo' AND TABLE_NAME = @TableName";


                    command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@TableName", tableName);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string constraintName = reader["CONSTRAINT_NAME"].ToString();
                            string foreignKeyColumn = reader["FKCOLUMN_NAME"].ToString();
                            string referencedTable = reader["PKTABLE_NAME"].ToString();
                            string referencedColumnName = reader["PKCOLUMN_NAME"].ToString();

                            sqlScriptBuilder.AppendLine($"ALTER TABLE {tableName}");
                            sqlScriptBuilder.AppendLine($"ADD CONSTRAINT {constraintName}");
                            sqlScriptBuilder.AppendLine($"FOREIGN KEY ({foreignKeyColumn})");
                            sqlScriptBuilder.AppendLine($"REFERENCES {referencedTable} ({referencedColumnName});");
                        }
                    }*/
                }
            }

            // SQL betiğini yazdırma
            string sqlScript = sqlScriptBuilder.ToString();
            Console.WriteLine(sqlScript);

            Console.WriteLine("SQL betiği başarıyla oluşturuldu.");
            Console.Read();
        }
    }
}