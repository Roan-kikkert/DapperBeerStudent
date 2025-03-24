using System.Data;
using Dapper;
using DapperBeer.DTO;
using DapperBeer.Model;
using FluentAssertions;

namespace DapperBeer.Tests;

[TestFixture]
[NonParallelizable]
public class Assignments1Tests : TestHelper
{
    // 1.1 Test
    [Test]
    public  async Task GetAllBrewersTest()
    {
        List<Brewer> brewers = Assignments1.GetAllBrewers();
        
        brewers.Should().HaveCount(677);

        await Verify(brewers.Take(3));
    }
    
    // 1.2 Test
    [Test]
    public async Task GetAllBeersOrderByAlcoholTest()
    {
        List<Beer> beers = Assignments1.GetAllBeersOrderByAlcohol();

        beers.Should().HaveCount(1617);

        await Verify(beers.Take(3));
    }
    
    // 1.3 Test
    [Test]
    public async Task GetAllBeersSortedByNameForCountryTest()
    {
        List<Beer> beers = Assignments1.GetAllBeersSortedByNameForCountry("BEL");

        beers.Should().HaveCount(296);

        await Verify(beers.Take(3));
    }
    
    // 1.4 Test
    [Test]
    public void CountBrewersTest()
    {
        int breweryCount = Assignments1.CountBrewers();

        breweryCount.Should().Be(677);
    }
    
    // 1.5 Test
    [Test]
    public async Task NumberOfBrewersByCountryTest()
    {
        List<NumberOfBrewersByCountry> numberOfBrewersByCountries = Assignments1.NumberOfBrewersByCountry();

        numberOfBrewersByCountries.Should().HaveCount(46);

        await Verify(numberOfBrewersByCountries.Take(3));
    }
    
    // 1.6 Test
    [Test]
    public async Task GetBeerWithMostAlcoholTest()
    {
        Beer beer = Assignments1.GetBeerWithMostAlcohol();

        beer.Name.Should().Be("XIAOYU");

        await Verify(beer);
    }
    
    // 1.7 Test
    [Test]
    public async Task GetBreweryByBrewerIdTest()
    {
        Brewer? brewer = Assignments1.GetBreweryByBrewerId(689);

        brewer.Should().NotBeNull();
        brewer!.Name.Should().Be("AFFLIGEM");
        
        Brewer? brewerNull = Assignments1.GetBreweryByBrewerId(-1);
        brewerNull.Should().BeNull();
        
        await Verify(brewer);
    }
    
    // 1.8 Test
    [Test]
    public async Task GetAllBeersByBreweryIdTest()
    {
        List<Beer> beers = Assignments1.GetAllBeersByBreweryId(689);

        beers.Should().HaveCount(2);

        await Verify(beers);
    }
    
    // 1.9 Test
    [Test]
    public async Task GetCafeBeersTest()
    {
        List<CafeBeer> cafeBeers = Assignments1.GetCafeBeers();

        cafeBeers.Should().HaveCount(754);

        await Verify(cafeBeers.Take(3));
    }
    
    // 1.10 Test
    [Test]
    public void GetBeerRatingTest()
    {
        using var connection = DbHelper.GetConnection();
        decimal rating = Assignments1.GetBeerRating(338);
        rating.Should().Be(4.5m);
    }
    
    // 1.11 Test
    [Test]
    public void InsertReviewTest()
    {
        // in SQL/InsertReview.sql wordt ook al een record toegevoegd.  
        Assignments1.InsertReview(338, 5.0m);
        
        decimal rating = Assignments1.GetBeerRating(338);
        rating.Should().Be(4.75m); // ik moet de expected value aanpassen en dan reverten, anders werkt de test niet.
    }
    
    // 1.12 Test
    [Test]
    public void InsertReviewReturnsReviewIdTest()
    {
        int reviewId = Assignments1.InsertReviewReturnsReviewId(338, 5.0m);
        reviewId.Should().Be(2);
        
        decimal rating = Assignments1.GetBeerRating(338);
        rating.Should().Be(4.75m);
    }
}