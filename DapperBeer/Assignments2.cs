using System.Data;
using Dapper;
using DapperBeer.DTO;
using DapperBeer.Model;
using SqlKata;

namespace DapperBeer;

public class Assignments2
{
    // 2.1 Question
    // !!!Doe dit nooit!!!!!
    // Wees voorzichtig en doe dit in de praktijk nooit!!!!!!
    // We gaan SQL-injectie gebruiken om te laten zien hoe het werkt.
    // Maak de methode die alle bieren teruggeeft, gegeven het land.
    // Normaal gesproken zouden we hiervoor een WHERE Country = @Country gebruiken in de sql.
    // Dit is veilig omdat er een query parameter placeholder wordt gebruikt.
    // Echter, we gaan nu het volgende doen:
    //  string sql = $"SELECT Name FROM ... WHERE Country = '{country}'";
    // Dit is onveilig en zal SQL-injectie veroorzaken!
    // Hetzelfde geldt overigens ook voor:
    //  string sql = "SELECT Name FROM ... WHERE Country = '" + country + "'";
    // Kijk goed naar de test, het tweede deel van de test doet een SQL-injectie en we krijgen dus toch alle bieren terug.
    // Ook al zou normaal gesproken dit niet de intentie zijn.
    // Dit is een voorbeeld van een SQL-injectie, dit geval is niet zo gevaarlijk, maar het kan veel gevaarlijker zijn
    // omdat we op de plek waar de SQL-injectie plaatsvindt, alles kunnen doen wat we willen
    // (DELETE, DROP, SELECT van andere tabellen, etc.)!
    // Met andere woorden gebruik altijd query parameters en nooit string concatenatie om SQL-queries te maken.
    // !!!DOE DIT NOOIT MEER SVP!!!!
    public static List<string> GetBeersByCountryWithSqlInjection(string country)
    {
        using IDbConnection connection = DbHelper.GetConnection();
        string sql = @$"SELECT b.Name AS Beer, br.Country AS brewer
            FROM Beer b 
            JOIN Brewer br ON b.BrewerId = br.BrewerId
            WHERE br.Country = '{country}'"; 
        return connection.Query<string>(sql).ToList();
        
    }
    
    // 2.2 Question
    // Maak een methode die alle bieren teruggeeft, gegeven het land, echter het land kan ook leeg gelaten worden.
    // Sorteer de bieren op naam.
    // Als het land leeg is, dan moeten alle bieren teruggegeven worden.
    // Hiervoor kan je NULL-truc gebruiken in je SQL (zie de sheets).
    //      WHERE @Country IS NULL OR Country = @Country 
    // Het vraagteken bij 'GetAllBeersByCountry(string? country)' geeft aan dat het argument optioneel is (string? country).
    // Dit betekent dus dat country null kan zijn.
    public static List<string> GetAllBeersByCountry(string? country)
    {
        using IDbConnection connection = DbHelper.GetConnection();
        string sql = @"
                     SELECT beer.Name
                     FROM Beer
                     JOIN Brewer ON Beer.BrewerId = Brewer.BrewerId
                     WHERE @Country IS NULL OR Country = @Country
                     ORDER BY beer.Name
                     ";
        return connection.Query<string>(sql, new { Country = country }).ToList();
    }
    
    // 2.3 Question
    // Nu doen we hetzelfde als in de vorige opdracht GetAllBeersByCountry, echter voegen we een extra parameter toe,
    // het minimal alcoholpercentage.
    // Ook het minAlcohol kan leeg gelaten worden (decimal? minAlcohol).
    // Gebruikt <= (kleiner of gelijk aan) voor de vergelijking van het minAlcohol.
    public static List<string> GetAllBeersByCountryAndMinAlcohol(string? country = null, decimal? minAlcohol = null)
    {
        using IDbConnection connection = DbHelper.GetConnection();
        string sql = @"
                     SELECT beer.Name
                     FROM Beer
                     JOIN Brewer ON Beer.BrewerId = Brewer.BrewerId
                     WHERE (@Country IS NULL OR Country = @Country)
                       AND (@MinAlcohol IS NULL OR Alcohol >= @MinAlcohol)
                     ORDER BY beer.Name
                     ";
        return connection.Query<string>(sql, new { Country = country, MinAlcohol = minAlcohol }).ToList();
    }
    
    // 2.4 Question
    // Helaas kan je in SQL bijv. geen parameter gebruiken voor de ORDER BY.
    // Dit kan je oplossen door de SQL te bouwen met een StringBuilder of een SqlBuilder.
    // De SqlBuilder is een handige tool om SQL-queries te bouwen.
    //  Voor uitleg zie: https://github.com/DapperLib/Dapper/blob/main/Dapper.SqlBuilder/Readme.md
    
    // Maak onderstaande methode die bieren teruggeeft (gebruik onderstaande query).
    // SELECT beer.Name
    // FROM Beer beer 
    //     JOIN Brewer brewer ON beer.BrewerId = brewer.BrewerId 
    //     WHERE (@Country IS NULL OR Country = @Country) AND (@MinAlcohol IS NULL OR Alcohol >= @MinAlcohol)
    // ORDER BY beer.Name
    //
    // Echter, vervang de WHERE-clausule door gebruik te maken van de SqlBuilder.
    // 
    // Je krijgt dan dergelijke constructies in je code:
    // if(country != null) {
    //     builder = builder.Where("brewer.Country = @Country", new { Country = country });
    // }
    // De queryBuilder maakt het bijvoorbeeld mogelijk om een WHERE-clausule te bouwen met meerdere voorwaarden.
    // Dit kan handig voor het maken van een datagrid (tabel) waarbij de gebruiker meerdere filters / sorteringen kan gebruiken.
    
    // Voeg zelf code toe om minAlcohol te kunnen gebruiken in je WHERE clausule.
    // 
    // Voeg ook een ORDER BY toe, zodat de bieren op verschillende kolommen gesorteerd kunnen worden.
    // Hiervoor is de parameter (string orderBy = "beer.Name") toegevoegd aan de methode. 
    // Deze parameter heeft een default waarde. In de test kan je zien hoe deze methode aangeroepen wordt
    // (en dan met name naar de parameters).
    // Dit worden default parameters genoemd i.c.m. optionele parameters en named arguments. 
    // Om Dapper (en andere bibliotheken) te gebruiken, worden vaak bovenstaande technieken gebruikt voor het aanroepen van methoden.
    // Zo zie je maar dat er ook wat van je C#-skills verwacht wordt :-).
    public static List<string> GetAllBeersByCountryAndMinAlcoholOrderByWithSqlBuilder(
        string? country = null, decimal? minAlcohol = null, string orderBy = "beer.Name")
    {
        using IDbConnection connection = DbHelper.GetConnection();
        string sql = @"
                      SELECT beer.Name
                      FROM Beer beer 
                           JOIN Brewer brewer ON beer.BrewerId = brewer.BrewerId     
                      /**where**/
                      ";
        
        SqlBuilder builder = new SqlBuilder();
        
        if (country != null)
        {
            builder = builder.Where("brewer.Country = @Country", new { Country = country });
        }
        if (minAlcohol != null)
        {
            builder = builder.Where("beer.Alcohol >= @MinAlcohol", new { MinAlcohol = minAlcohol });
        }
        builder.OrderBy(orderBy);
        
        var queryTemplate = builder.AddTemplate(sql);
        return connection.Query<string>(queryTemplate.RawSql, queryTemplate.Parameters).ToList();
    }

    // 2.5 Question
    // Maak een view die de naam van het bier teruggeeft en de naam van de brouwerij
    // en de naam van de brouwmeester als de brouwerij deze heeft (LEFT JOIN).
    // Sorteer de resultaten op bier naam.
    //
    // Gebruik de volgende where-clause: WHERE BrewmasterName IS NOT NULL (in de query niet in de view)
    // Gebruik de klasse BrewerBeerBrewmaster om de resultaten in op te slaan. (directory DTO).
    public static List<BrewerBeerBrewmaster> GetAllBeerNamesWithBreweryAndBrewmaster()
    {
        using IDbConnection connection = DbHelper.GetConnection();
        string sql = @"
                     SELECT beer.Name as BeerName, brewer.Name AS BrewerName, brewmaster.Name AS brewmasterName   
                     FROM brewer brewer
                          LEFT JOIN brewmaster brewmaster ON brewer.BrewerId = brewmaster.BrewerId
                          JOIN beer beer ON brewer.BrewerId = beer.BrewerId
                     WHERE brewmaster.Name IS NOT NULL
                     ORDER BY beer.Name
                     ";
        return connection.Query<BrewerBeerBrewmaster>(sql).ToList();
    }
    
    // 2.6 Question
    // Soms is het onhandig om veel parameters mee te geven aan een methode.
    // Dit kan je oplossen door een klasse te maken die de parameters bevat.
    // De kan je rechtstreeks meegeven aan de Query<T>(sql, param: filter).
    // of aan SqlBuilder.Template? queryTemplate = builder.AddTemplate(sql, filter);
    // Maak een query die bieren teruggeeft, gegeven het land en het type (beide optioneel).
    // Ook willen we een limiet en een offset meegeven in de query.
    // LIMIT @PageSize OFFSET @Offset  
    // Sorteer op OrderBy
    // Zie de klasse BeerFilter.
    public class BeerFilter
    {
        public string? Country { get; set; }
        public string? Type { get; set; }
        public int PageSize { get; set; } = 10;    //default value start at 0
        public int PageIndex { get; set; } = 0;    //default value start at 0
        
        public int Offset => PageSize * (PageIndex+1);
        
        public string OrderBy { get; set; } = "beer.Name";
    }
    public static List<Beer> GetBeersByCountryAndType(BeerFilter filter)
    {
        using IDbConnection connection = DbHelper.GetConnection();
        string sql = $"""
                      SELECT beer.BeerId, beer.Name, beer.Type, beer.Style, beer.Alcohol, beer.BrewerId
                      FROM Beer beer 
                           JOIN Brewer brewer ON beer.BrewerId = brewer.BrewerId
                      /**where**/
                      /**orderby**/
                      LIMIT @PageSize OFFSET @Offset
                      """;
        
        SqlBuilder builder = new SqlBuilder();
    
        if (!string.IsNullOrEmpty(filter.Country))
        {
            builder = builder.Where("brewer.Country = @Country", new { filter.Country });
        }
        if (!string.IsNullOrEmpty(filter.Type))
        {
            builder = builder.Where("beer.Type = @Type", new { filter.Type });
        }
    
        var queryTemplate = builder.AddTemplate(sql, filter);
        return connection.Query<Beer>(queryTemplate.RawSql, queryTemplate.Parameters).ToList();
    }
}