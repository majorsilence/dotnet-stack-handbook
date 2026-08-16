namespace Examples.Language.Objects;

// The chapter's running example.  A long-form property with validation, and the
// short form for the rest.
public class TVShow
{
    public TVShow()
    {
    }

    private string _showName;

    // Public properties can be accessed from any function inside the
    // class as well as other classes
    public string ShowName
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
    public string Summary { get; init; }
    public decimal Rating { get; init; }
    public string Episode { get; init; }
}
