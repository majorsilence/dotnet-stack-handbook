using System.ComponentModel.DataAnnotations;

namespace Examples.Web;

public class TvShow
{
    public long Id { get; set; }

    [Required]
    [StringLength(50)]
    public string ShowName { get; set; }

    public string Episode { get; set; }

    [Range(0, 5)]
    public decimal Rating { get; set; }
}

public interface IShowRepo
{
    Task<IEnumerable<TvShow>> GetShowsAsync();
    Task<TvShow> GetShowAsync(long id);
    Task<long> InsertAsync(TvShow show);
    Task DeleteAsync(long id);
}

// The chapter's ShowRepo talks to a database.  This one keeps a list, so the
// example can be started and exercised without a server behind it.  The shape of
// the interface is what the chapter's listings depend on.
public class InMemoryShowRepo : IShowRepo
{
    private readonly List<TvShow> _shows =
    [
        new TvShow { Id = 1, ShowName = "Star Trek", Episode = "1x12", Rating = 5.0m },
        new TvShow { Id = 2, ShowName = "Friends", Episode = "4x05", Rating = 4.8m },
    ];
    private long _nextId = 3;
    private readonly Lock _lock = new();

    public Task<IEnumerable<TvShow>> GetShowsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult<IEnumerable<TvShow>>(_shows.ToList());
        }
    }

    public Task<TvShow> GetShowAsync(long id)
    {
        lock (_lock)
        {
            return Task.FromResult(_shows.FirstOrDefault(x => x.Id == id));
        }
    }

    public Task<long> InsertAsync(TvShow show)
    {
        lock (_lock)
        {
            show.Id = _nextId++;
            _shows.Add(show);
            return Task.FromResult(show.Id);
        }
    }

    public Task DeleteAsync(long id)
    {
        lock (_lock)
        {
            _shows.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }
    }
}
