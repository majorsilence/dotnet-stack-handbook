namespace Examples.Language.Interfaces;

// The chapter names the interface TVShow rather than ITVShow, on purpose, to
// make the point that the I prefix is a convention and not a rule.  That does
// mean it collides with the TVShow class from the objects section, which is why
// the two live in different namespaces here.
public interface TVShow
{
    string ShowName { get; init; }
    int ShowLength { get; init; }
    string Summary { get; init; }
    decimal Rating { get; init; }
    string Episode { get; init; }
    string ParentalGuide { get; init; }

    void PrettyPrint(bool includeSummary);
    bool IsGoodRating();
}

public class ComedyShow : TVShow
{
    public required string ShowName { get; init; }
    public int ShowLength { get; init; }
    public required string Summary { get; init; }
    public decimal Rating { get; init; }
    public required string Episode { get; init; }
    public required string ParentalGuide { get; init; }

    // includeSummary is a method parameter
    public void PrettyPrint(bool includeSummary)
    {
        if (includeSummary)
        {
            Console.WriteLine($"Comedy: {ShowName} {Episode} {Rating} {ShowLength} {Summary}");
        }
        else
        {
            Console.WriteLine($"Comedy: {ShowName} {Episode} {Rating} {ShowLength}");
        }
    }

    public bool IsGoodRating()
    {
        return Rating >= 3.0m;
    }
}

public class AdventureShow : TVShow
{
    public required string ShowName { get; init; }
    public int ShowLength { get; init; }
    public required string Summary { get; init; }
    public decimal Rating { get; init; }
    public required string Episode { get; init; }
    public required string ParentalGuide { get; init; }

    // includeSummary is a method parameter
    public void PrettyPrint(bool includeSummary)
    {
        if (includeSummary)
        {
            Console.WriteLine($"Adventure: {ShowName} {Episode} {Rating} {ShowLength} {Summary}");
        }
        else
        {
            Console.WriteLine($"Adventure: {ShowName} {Episode} {Rating} {ShowLength}");
        }
    }

    public bool IsGoodRating()
    {
        return Rating >= 3.5m;
    }
}

public static class Shows
{
    static List<TVShow> _tvShows = new List<TVShow>();

    public static void InsertShow(TVShow show)
    {
        _tvShows.Add(show);
    }

    public static void PrintShows()
    {
        foreach (var show in _tvShows)
        {
            show.PrettyPrint(includeSummary: true);
        }
    }
}
