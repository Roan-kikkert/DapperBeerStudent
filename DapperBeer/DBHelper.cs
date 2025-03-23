using System.Data;
using Dapper;
using DapperBeer.DTO;
using MySqlConnector;

namespace DapperBeer;

public static class DbHelper
{
    public static IDbConnection GetConnection()
    {
        return new MySqlConnection("server=localhost;database=DapperBeer;user=DapperBeer;password=test1234;AllowUserVariables=True");
    }

    public static void CreateTablesAndInsertData()
    {
        using var connection = GetConnection();

        string[] tables = ["Brewer", "Beer", "Cafe", "Sells", "Address", "Brewmaster", "Review"];

        bool allExists = tables.All(table => connection.QuerySingleOrDefault<bool>(
            $"""
                 SELECT COUNT(1) = 1
                 FROM   information_schema.tables
                 WHERE      table_schema = 'DapperBeer' 
                            AND table_name = '{table}'
                 LIMIT 1; 
             """));
    
        if(allExists && CorrectRecordCountInTables())
            return;
        
        var createTables = File.ReadAllText("SQL/CreateTables.sql");
        connection.Execute(createTables);
        
        foreach (var table in tables)
        {
            var insertBrewer = File.ReadAllText($"SQL/Insert{table}.sql");
            connection.Execute(insertBrewer);
        }

        bool CorrectRecordCountInTables()
        {
            var tableCount = connection.QuerySingleOrDefault<TableCount>(
                """
                    SELECT 
                        (SELECT COUNT(1) FROM Beer) as BeerCount,
                        (SELECT COUNT(1) FROM Brewer) as BrewerCount,
                        (SELECT COUNT(1) FROM Cafe) as CafeCount,
                        (SELECT COUNT(1) FROM Sells) as SellsCount,
                        (SELECT COUNT(1) FROM Address) as AddressCount,
                        (SELECT COUNT(1) FROM Brewmaster) as BrewmasterCount
                """);

            return tableCount != null && 
                   tableCount.BeerCount == 1617 && tableCount.BrewerCount == 677 && 
                   tableCount.CafeCount == 345 && tableCount.SellsCount == 754 &&
                   tableCount.AddressCount == 201 && tableCount.BrewmasterCount == 201;
        }
    }
    
    private class TableCount
    {
        public int BeerCount { get; set; }
        public int BrewerCount { get; set; }
        public int CafeCount { get; set; }
        public int SellsCount { get; set; }
        public int AddressCount { get; set; }
        public int BrewmasterCount { get; set; }
    }

    public static void DropAndCreateTableReviews()
    {
        string dropCreateReviewTable = 
            """
            DROP TABLE IF EXISTS Review;
            CREATE TABLE Review (
                ReviewId INT PRIMARY KEY AUTO_INCREMENT,
                BeerId INT REFERENCES Beer(BeerId),
                Score DECIMAL(4, 2)
            );
            """;
        
        using IDbConnection connection = DbHelper.GetConnection();
        connection.Execute(dropCreateReviewTable);
    }
}