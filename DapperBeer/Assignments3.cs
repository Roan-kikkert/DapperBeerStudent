using System.Data;
using BenchmarkDotNet.Attributes;
using Dapper;
using DapperBeer.DTO;
using DapperBeer.Model;
using DapperBeer.Tests;

namespace DapperBeer;

public class Assignments3
{
    // 3.1 Question
    // Tip: Kijk in voorbeelden en sheets voor inspiratie.
    // Deze staan in de directory ExampleFromSheets/Relationships.cs. 
    // De sheets kan je vinden op: https://slides.com/jorislops/dapper/
    // Kijk niet te veel naar de voorbeelden van relaties op https://www.learndapper.com/relationships
    // Deze aanpak is niet altijd de manier de gewenst is!
    
    // 1 op 1 relatie (one-to-one relationship)
    // Een brouwmeester heeft altijd 1 adres. Haal alle brouwmeesters op en zorg ervoor dat het address gevuld is.
    // Sorteer op naam.
    // Met andere woorden een brouwmeester heeft altijd een adres (Property Address van type Address), zie de klasse Brewmaster.
    // Je kan dit doen door een JOIN te gebruiken.
    // Je zult de map functie in Query<Brewmaster, Address, Brewmaster>(sql, map: ...) moeten gebruiken om de Address property van Brewmaster te vullen.
    // Kijk in voorbeelden hoe je dit kan doen. Deze staan in de directory ExampleFromSheets/Relationships.cs.
    public static List<Brewmaster> GetAllBrouwmeestersIncludesAddress()
    {
        using var connection = DbHelper.GetConnection();
        string sql = @"
                    SELECT brewmaster.Name, adress.AddressId AS AddressSplit
                    FROM brewmaster
                    INNER JOIN Address adress ON brewmaster.AddressId = adress.addressId
                    ORDER BY brewmaster.Name";
    
        List<Brewmaster> brewmaster = connection.Query<Brewmaster, Address, Brewmaster>(
            sql,
            (brewmaster, address) =>
            {
                brewmaster.Address = address;
                return brewmaster;
            },
            splitOn: "AddressSplit")
            .ToList();
    
        return brewmaster;
    }

    // 3.2 Question
    // 1 op 1 relatie (one-to-one relationship)
    // Haal alle brouwmeesters op en zorg ervoor dat de brouwer (Brewer) gevuld is.
    // Sorteer op naam.
    public static List<Brewmaster> GetAllBrewmastersWithBrewery()
    {
        using var connection = DbHelper.GetConnection();
        string sql = @"
                    SELECT brewmaster.Name, brewer.Name AS BrewerSplit
                    FROM brewmaster
                    INNER JOIN Brewer brewer ON brewmaster.BrewerId = brewer.BrewerId
                    ORDER BY brewmaster.Name";
    
        List<Brewmaster> brewmaster = connection.Query<Brewmaster, Brewer, Brewmaster>(
                sql,
                (brewmaster, brewer) =>
                {
                    brewmaster.Brewer = brewer;
                    return brewmaster;
                },
                splitOn: "BrewerSplit")
            .ToList();
    
        return brewmaster;
    }

    // 3.3 Question
    // 1 op 1 (0..1) (one-to-one relationship) 
    // Geef alle brouwers op en zorg ervoor dat de brouwmeester gevuld is.
    // Sorteer op brouwernaam.
    //
    // Niet alle brouwers hebben een brouwmeester.
    // Let op: gebruik het correcte type JOIN (JOIN, LEFT JOIN, RIGHT JOIN).
    // Dapper snapt niet dat het om een 1 - 0..1 relatie gaat.
    // De Query methode ziet er als volgt uit (let op het vraagteken optioneel):
    // Query<Brewer, Brewmaster?, Brewer>(sql, map: ...)
    // Wat je kan doen is in de map functie een controle toevoegen, je zou dit verwachten:
    // if (brewmaster is not null) { brewer.Brewmaster = brewmaster; }
    // !!echter dit werkt niet!!!!
    // Plaats eens een breakpoint en kijk wat er in de brewmaster variabele staat,
    // hoe moet dan je if worden?
    public static List<Brewer> GetAllBrewersIncludeBrewmaster()
    {
        using var connection = DbHelper.GetConnection();
        string sql = @"
                    SELECT brewer.Name AS Brewer, brewmaster.Name AS BrewmasterSplit
                    FROM brewer
                        LEFT JOIN brewmaster ON brewer.BrewerId = brewmaster.BrewerId
                    ORDER BY brewmaster.Name
        ";
        List<Brewer> brewer = connection.Query<Brewer, Brewmaster?, Brewer>(
                sql,
                (brewer, brewmaster) =>
                {
                    if (brewmaster is not null)
                    {
                        brewer.Brewmaster = brewmaster;
                    }

                    return brewer;
                },
                splitOn: "BrewmasterSplit")
            .ToList();
        return brewer;
    }
    
    // 3.4 Question
    // 1 op veel relatie (one-to-many relationship)
    // Geef een overzicht van alle bieren. Zorg ervoor dat de property Brewer gevuld is.
    // Sorteer op biernaam en beerId!!!!
    // Zorg ervoor dat bieren van dezelfde brouwerij naar dezelfde instantie van Brouwer verwijzen.
    // Dit kan je doen door een Dictionary<int, Brouwer> te gebruiken.
    // Kijk in voorbeelden hoe je dit kan doen. Deze staan in de directory ExampleFromSheets/Relationships.cs.
    public static List<Beer> GetAllBeersIncludeBrewery()
    {
        using var connection = DbHelper.GetConnection();
        string sql = @"
            SELECT beer.BeerId, beer.Name AS BeerName, beer.Type, beer.alcohol, 
                   brewer.BrewerId, brewer.Name AS BrewerName, brewer.Country
            FROM Beer AS beer
            INNER JOIN Brewer AS brewer ON beer.BrewerId = brewer.BrewerId
            ORDER BY beer.Name, beer.BeerId";
    
        var breweryLookup = new Dictionary<int, Brewer>();
        List<Beer> beers = connection.Query<Beer, Brewer, Beer>(
            sql,
            (beer, brewer) =>
            {
                if (!breweryLookup.TryGetValue(brewer.BrewerId, out var existingBrewer))
                {
                    existingBrewer = brewer;
                    breweryLookup.Add(brewer.BrewerId, existingBrewer);
                }
                beer.Brewer = existingBrewer;
                return beer;
            },
            splitOn: "BrewerId")
            .ToList();
    
        return beers;
    }
    
    // 3.5 Question
    // N+1 probleem (1-to-many relationship)
    // Geef een overzicht van alle brouwerijen en hun bieren. Sorteer op brouwerijnaam en daarna op biernaam.
    // Doe dit door eerst een Query<Brewer>(...) te doen die alle brouwerijen ophaalt. (Dit is 1)
    // Loop (foreach) daarna door de brouwerijen en doe voor elke brouwerij een Query<Beer>(...)
    // die de bieren ophaalt voor die brouwerij. (Dit is N)
    // Dit is een N+1 probleem. Hoe los je dit op? Dat zien we in de volgende vragen.
    // Als N groot is (veel brouwerijen) dan kan dit een performance probleem zijn of worden. Probeer dit te voorkomen!
    public static List<Brewer> GetAllBrewersIncludingBeersNPlus1()
    {
        using var connection = DbHelper.GetConnection();
    
        // Fetch all brewers (1 query)
        string fetchBrewersSql = @"
            SELECT brewer.BrewerId, brewer.Name, brewer.Country
            FROM Brewer AS brewer
            ORDER BY brewer.Name";
    
        List<Brewer> brewers = connection.Query<Brewer>(fetchBrewersSql).ToList();
    
        // Fetch beers for each brewer (N queries)
        string fetchBeersSql = @"
            SELECT beer.BeerId, beer.Name AS BeerName, beer.Type, beer.Alcohol, beer.BrewerId
            FROM Beer AS beer
            WHERE beer.BrewerId = @BrewerId
            ORDER BY beer.Name";
    
        foreach (Brewer brewer in brewers)
        {
            List<Beer> beers = connection.Query<Beer>(fetchBeersSql, new { BrewerId = brewer.BrewerId }).ToList();
            brewer.Beers = beers;
        }
    
        return brewers;
    }
    
    // 3.6 Question
    // 1 op n relatie (one-to-many relationship)
    // Schrijf een query die een overzicht geeft van alle brouwerijen. Vul per brouwerij de property Beers (List<Beer>) met de bieren van die brouwerij.
    // Sorteer op brouwerijnaam en daarna op biernaam.
    // Gebruik de methode Query<Brewer, Beer, Brewer>(sql, map: ...)
    // Het is belangrijk dat je de map functie gebruikt om de bieren te vullen.
    // De query geeft per brouwerij meerdere bieren terug. Dit is een 1 op veel relatie.
    // Om ervoor te zorgen dat de bieren van dezelfde brouwerij naar dezelfde instantie van Brewer verwijzen,
    // moet je een Dictionary<int, Brewer> gebruiken.
    // Dit is een veel voorkomend patroon in Dapper.
    // Vergeet de Distinct() methode te gebruiken om dubbel brouwerijen (Brewer) te voorkomen.
    //  Query<...>(...).Distinct().ToList().
    
    public static List<Brewer> GetAllBrewersIncludeBeers()
    {
        using var connection = DbHelper.GetConnection();
        string sql = @"
            SELECT brewer.BrewerId, brewer.Name AS BrewerName, brewer.Country, 
                   beer.BeerId, beer.Name AS BeerName, beer.Type, beer.Alcohol
            FROM Brewer AS brewer
            LEFT JOIN Beer AS beer ON brewer.BrewerId = beer.BrewerId
            ORDER BY brewer.Name, beer.Name";
    
        var brewersLookup = new Dictionary<int, Brewer>();
        List<Brewer> brewers = connection.Query<Brewer, Beer, Brewer>(
            sql,
            (brewer, beer) =>
            {
                if (!brewersLookup.TryGetValue(brewer.BrewerId, out var existingBrewer))
                {
                    existingBrewer = brewer;
                    existingBrewer.Beers = new List<Beer>();
                    brewersLookup[brewer.BrewerId] = existingBrewer;
                }
    
                if (beer != null)
                {
                    existingBrewer.Beers.Add(beer);
                }
    
                return existingBrewer;
            },
            splitOn: "BeerId"
        ).Distinct().ToList();
    
        return brewers;
    }
    
    // 3.7 Question
    // Optioneel:
    // Dezelfde vraag als hiervoor, echter kan je nu ook de Beers property van Brewer vullen met de bieren?
    // Hiervoor moet je wat extra logica in map methode schrijven.
    // Let op dat er geen dubbelingen komen in de Beers property van Beer!
    public static List<Beer> GetAllBeersIncludeBreweryAndIncludeBeersInBrewery()
    {
        throw new NotImplementedException();
    }
    
    // 3.8 Question
    // n op n relatie (many-to-many relationship)
    // Geef een overzicht van alle cafés en welke bieren ze schenken.
    // Let op een café kan meerdere bieren schenken. En een bier wordt vaak in meerdere cafe's geschonken. Dit is een n op n relatie.
    // Sommige cafés schenken geen bier. Dus gebruik LEFT JOINS in je query.
    // Bij n op n relaties is er altijd spraken van een tussen-tabel (JOIN-table, associate-table), in dit geval is dat de tabel Sells.
    // Gebruikt de multi-mapper Query<Cafe, Beer, Cafe>("query", splitOn: "splitCol1, splitCol2").
    // Gebruik de klassen Cafe en Beer.
    // De bieren worden opgeslagen in een de property Beers (List<Beer>) van de klasse Cafe.
    // Sorteer op cafénaam en daarna op biernaam.
    
    // Kan je ook uitleggen wat het verschil is tussen de verschillende JOIN's en wat voor gevolg dit heeft voor het resultaat?
    // Het is belangrijk om te weten wat de verschillen zijn tussen de verschillende JOIN's!!!! Als je dit niet weet, zoek het op!
    // Als je dit namelijk verkeerd doet, kan dit grote gevolgen hebben voor je resultaat (je krijgt dan misschien een verkeerde aantal records).
    public static List<Cafe> OverzichtBierenPerKroegLijstMultiMapper()
    {
        using var connection = DbHelper.GetConnection();
        string sql = @"
            SELECT cafe.CafeId, cafe.Name AS CafeName, cafe.Address,
                   beer.BeerId, beer.Name AS BeerName, beer.Type, beer.Alcohol
            FROM Cafe AS cafe
            LEFT JOIN Sells AS sells ON cafe.CafeId = sells.CafeId
            LEFT JOIN Beer AS beer ON sells.BeerId = beer.BeerId
            ORDER BY cafe.Name, beer.Name";
    
        var cafeLookup = new Dictionary<int, Cafe>();
        List<Cafe> cafes = connection.Query<Cafe, Beer, Cafe>(
            sql,
            (cafe, beer) =>
            {
                if (!cafeLookup.TryGetValue(cafe.CafeId, out var existingCafe))
                {
                    existingCafe = cafe;
                    existingCafe.Beers = new List<Beer>();
                    cafeLookup[cafe.CafeId] = existingCafe;
                }
    
                if (beer != null)
                {
                    existingCafe.Beers.Add(beer);
                }
    
                return existingCafe;
            },
            splitOn: "BeerId"
        ).Distinct().ToList();
    
        return cafes;
    }

    // 3.9 Question
    // We gaan nu nog een niveau dieper. Geef een overzicht van alle brouwerijen, met daarin de bieren die ze verkopen,
    // met daarin in welke cafés ze verkocht worden.
    // Sorteer op brouwerijnaam, biernaam en cafenaam. 
    // Gebruik (vul) de class Brewer, Beer en Cafe.
    // Gebruik de methode Query<Brewer, Beer, Cafe, Brewer>(...) met daarin de juiste JOIN's in de query en splitOn parameter.
    // Je zult twee dictionaries moeten gebruiken. Een voor de brouwerijen en een voor de bieren.
    public static List<Brewer> GetAllBrewersIncludeBeersThenIncludeCafes()
    {
        using var connection = DbHelper.GetConnection();
        string sql = @"
            SELECT brewer.BrewerId, brewer.Name AS BrewerName, brewer.Country, 
                   beer.BeerId, beer.Name AS BeerName, beer.Type, beer.Alcohol, 
                   cafe.CafeId, cafe.Name AS CafeName, cafe.Address
            FROM Brewer AS brewer
            LEFT JOIN Beer AS beer ON brewer.BrewerId = beer.BrewerId
            LEFT JOIN Sells AS sells ON beer.BeerId = sells.BeerId
            LEFT JOIN Cafe AS cafe ON sells.CafeId = cafe.CafeId
            ORDER BY brewer.Name, beer.Name, cafe.Name";
    
        var brewerLookup = new Dictionary<int, Brewer>();
        var beerLookup = new Dictionary<int, Beer>();
    
        List<Brewer> brewers = connection.Query<Brewer, Beer, Cafe, Brewer>(
            sql,
            (brewer, beer, cafe) =>
            {
                if (!brewerLookup.TryGetValue(brewer.BrewerId, out var existingBrewer))
                {
                    existingBrewer = brewer;
                    existingBrewer.Beers = new List<Beer>();
                    brewerLookup[brewer.BrewerId] = existingBrewer;
                }
    
                if (beer != null)
                {
                    if (!beerLookup.TryGetValue(beer.BeerId, out var existingBeer))
                    {
                        existingBeer = beer;
                        existingBeer.Cafes = new List<Cafe>();
                        existingBrewer.Beers.Add(existingBeer);
                        beerLookup[beer.BeerId] = existingBeer;
                    }
    
                    if (cafe != null)
                    {
                        existingBeer.Cafes.Add(cafe);
                    }
                }
    
                return existingBrewer;
            },
            splitOn: "BeerId,CafeId"
        ).Distinct().ToList();
    
        return brewers;
    }
    
    // 3.10 Question - Er is geen test voor deze vraag
    // Optioneel: Geef een overzicht van alle bieren en hun de bijbehorende brouwerij.
    // Sorteer op brouwerijnaam, biernaam.
    // Gebruik hiervoor een View BeerAndBrewer (maak deze zelf). Deze view bevat alle informatie die je nodig hebt gebruikt join om de tabellen Beer, Brewer.
    // Let op de kolomnamen in de view moeten uniek zijn. Dit moet je dan herstellen in de query waarin je view gebruik zodat Dapper het snap
    // (SELECT BeerId, BeerName as Name, Type, ...). Zie BeerName als voorbeeld hiervan.
    public static List<Beer> GetBeerAndBrewersByView()
    {
        throw new NotImplementedException();
    }
}