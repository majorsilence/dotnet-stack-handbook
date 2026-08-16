using Examples.Language.Interfaces;
using Objects = Examples.Language.Objects;

// Runs the listings from "The Language: C# and VB" so that the chapter's claimed
// output is the output you actually get.

// --- Variables -----------------------------------------------------------------
int i = 0;
string name = "Star Trek";
bool watched = false;
decimal rating = 5.0m;
Console.WriteLine($"{i} {name} {watched} {rating}");

// --- Objects -------------------------------------------------------------------
var starTrek = new Objects.TVShow
{
    ShowName = "Star Trek",
    ShowLength = 1380,
    Summary = "Teleport Disaster",
    Rating = 5.0m,
    Episode = "1x12"
};
Console.WriteLine($"{starTrek.ShowName} {starTrek.Episode} {starTrek.Rating}");

// ShowName rejects an empty string, which is the whole reason it is written out
// in long form.
try
{
    starTrek.ShowName = "   ";
    Console.WriteLine("ShowName validation did not fire");
}
catch (Exception ex)
{
    Console.WriteLine($"as expected: {ex.Message}");
}

// --- Interfaces ----------------------------------------------------------------
Shows.InsertShow(new ComedyShow()
{
    ShowName = "Friends",
    ShowLength = 1380,
    Summary = "The friends get coffee.",
    Rating = 4.8m,
    Episode = "4x05",
    ParentalGuide = "PG13"
});
Shows.InsertShow(new AdventureShow()
{
    ShowName = "Rick and morty",
    ShowLength = 760,
    Summary = "A quick 20 minute in and out adventure.",
    Rating = 3.8m,
    Episode = "3x14",
    ParentalGuide = "18A"
});

Shows.PrintShows();
