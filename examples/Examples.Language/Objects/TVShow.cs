namespace Examples.Language.Objects;

// The chapter's running example.  A long-form property with validation, and the
// short form for the rest.
public class TVShow
{
    public TVShow()
    {
    }

    // Assigned so the compiler knows it is never null before ShowName's setter
    // runs.  With nullable reference types on, a non-nullable field has to hold
    // something by the time the constructor exits.
    private string _showName = string.Empty;

    // Public properties can be accessed from any function inside the
    // class as well as other classes.  required means an object initializer has
    // to set it, so the type cannot be constructed half built.
    public required string ShowName
    {
        get
        {
            // Inside the get part the private variable is returned.
            // You can do anything you want here such as data validation
            // before returning the data if you need or want.
            return _showName;
        }
        set
        {
            // Inside the set part the private variable is set.
            // You can do anything you want here such as data validation
            // before the data is set.
            if (value.Trim() == "")
                throw new Exception("ShowName cannot be empty");
            _showName = value;
        }
    }

    // The above property is long form.  A shorter form can be done as seen below
    public int ShowLength { get; init; }
    public required string Summary { get; init; }
    public decimal Rating { get; init; }
    public required string Episode { get; init; }
}
