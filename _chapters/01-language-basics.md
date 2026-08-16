---
layout: chapter
title: "The Language: C# and VB"
number: 1
part: 1
examples: Examples.Language
---

C# and VB are two languages over one runtime. They compile to the same IL, use the same base class library, and can call each other freely inside one solution. Choosing between them is a matter of taste and of what the team already knows, not of capability.

This chapter covers the three things everything else is built from: variables to hold values, objects to group values with the code that operates on them, and interfaces to describe what an object can do without saying how. Both languages are shown side by side throughout, so a VB developer reading C# code, or the reverse, can see the same idea twice.

## Variables

Variables are the basic working blocks in code. You use variables to hold values. There are several different variable types but in this lesson we will cover only four of them.

To declare a variable you use the language keyword "Dim" used with a name and "As". So if you want a string called "Hello World" named TestVariable you would declare it like this.

```vb
Dim TestVariable As String = "Hello World"
```

```cs
string TestVariable = "Hello World";
```

This example declares a variable and assigns a value at the same time. However you can declare a variable without assigning value. The value can always be assigned later. A good general rule is only declare a variable when it is ready to be used (assigned) when possible.

- Integer - are like whole numbers but can contain negatives
- String - contain multiple characters
- Decimal - numbers with decimals
- Boolean - is true or false

### Integers

Integers are like whole numbers but can contain negatives. So they have negatives, zero, or positives.

For example -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5 are all integer values.

To declare an integer you can do this.

```vb
Dim i As Integer
```

```cs
int i;
```

This example creates a new variable of type Integer name i. It does not assign any value to i. To assign a value to i you can do it like this.

```vb
i=1
```

```cs
i=1;
```

Now the value of i is 1.

```vb
i=2
```

```cs
i=2;
```

Now the value of i is 2.

### Strings

Strings can hold any value. They can have letters, numbers, special characters. They can be long or short

To declare a string you can do this.

```vb
Dim s As String
```

```cs
string s;
```

This example creates an empty string called s. This string has no value

To assign the value "hello world" to the variable s we would do.

```vb
s = "hello world"
```

```cs
s = "hello world";
```

The value of the variable can be reassigned at any time so if we want to change the value to "purple monkey dishwasher" just do the same as above put with the new string.

```vb
s = "purple monkey dishwasher"
```

```cs
s = "purple monkey dishwasher";
```

If we want to see the value of a variable printed to the console we can write.

```vb
System.Console.WriteLine("The value of s is: " & s)
```

```cs
System.Console.WriteLine($"The value of s is: {s}");
```

We see here that the value of s is appended to the string "The value of s is: " and then printed to the console as "The value of s is: purple monkey dishwasher". You can append any string to any other string at any time using the & symbol in vb or the + symbol in c#.

#### StringBuilder

If you are appending to a string again and again or changing its value over and over again this can become very slow. String operations like this can be sped up using the StringBuilder class.

To use a string builder you need to initialize System.Text.StringBuilder.

```vb
Dim builder As New System.Text.StringBuilder
builder.Append("Hello World ")
builder.Append("Peter.  ")
builder.Append("Have a good day.")

System.Console.WriteLine(builder.ToString)
```

```cs
var builder = new System.Text.StringBuilder();
builder.Append("Hello World ");
builder.Append("Peter.  ");
builder.Append("Have a good day.");

System.Console.WriteLine(builder.ToString());
```

This will print "Hello World Peter. Have a good day.".

What this does is keep adding to a buffer and when you call the ToString method it finally creates a string. This is much faster than concatenating the string together like the following.

```vb
System.Console.WriteLine("Hello World " & "Peter.  " & "Have a good day.")
```

```cs
System.Console.WriteLine("Hello World " + "Peter.  " + "Have a good day.");
```

This example probably is not faster since it is so tiny but if you did this with 100 000 strings the string builder would be much faster.

### Decimals

Decimals are used when you need numeric values that contain decimals places. It is essential if you are doing financial calculations that you use decimals and no other data type. Do not use doubles.

For example -5.32, -4.76, -3.7654, -2.1, -1.343, 0.13, 1.786555, 2.2, 3.765, 4.22, 5.3446 are all decimal values.

To declare a decimal you can do this.

```vb
Dim d As Decimal = 4.444D
```

```cs
decimal d = 4.444m;
```

This example creates a new variable of type Decimal with the name d and a value of 4.444d

To assign a new value to d you can do it like this.

```vb
d=5.437D
```

```cs
d = 5.437m;
```

Now the value of d is 5.437.

```vb
d=2.55
```

```cs
d = 2.55m;
```

Now the value of d is 2.55. As shown above variables in function can always be reassigned new values

### Booleans

Booleans are variables that can be either True or False. That is all they hold. Booleans default to false.

To declare a boolean you can do this.

```vb
Dim b As Boolean = False
```

```cs
bool b = false;
```

This example creates a new variable of type Boolean named b. It does not assign any value to b. To assign a value to b you can do it like this.

```vb
b=True
```

```cs
b=true;
```

Now the value of b is True.

```vb
b=False
```

```cs
b=false;
```

Now the value of b is False.

There is no other value that a boolean can hold. If you do not set a value a boolean will default to False.

### Chars

Chars are variables that can hold one character and only one character. It can be any character available but only one character at a time.

To declare a char you do this.

```vb
Dim c As Char
```

```cs
char c;
```

This example creates a new variable of type Char named c. It does not assign any value to c. To assign a value to c you can do it like this.

```vb
c="A"c
```

```cs
c='A';
```

Now the value of c is A.

```vb
c="~"c
```

```cs
c='~';
```

Now the value of c is ~.

As is written above any character can be held in a char variable but only one character at a time. Like any other variable type you can print the variable to the console using like this.

```vb
System.Console.WriteLine(c)
```

```cs
System.Console.WriteLine(c);
```

### DateTime

DateTime variables can hold a date and time value. If you just want a date you can also use Date instead of DateTime.

To declare a DateTime you do this.

```vb
Dim t As DateTime
```

```cs
DateTime t;
```

This example creates a new variable of type DateTime named t. It does not assign any value to t. To assign a value to t you can do it like this.

```vb
t = DateTime.Now
```

```cs
t = DateTime.Now;
```

This assigns the current date and time to the variable t.

If you want to assign a specific date such as 01 may 2012, do it like this.

```vb
t = New DateTime(2012, 5, 1)
```

```cs
t = new DateTime(2012, 5, 1);
```

Now the date of t would now be 01 May 2012 with a time of 00:00:00.

If you want to print the date to the console you can use several of its functions to print in different formats.

```vb
System.Console.WriteLine(t.ToString)
System.Console.WriteLine(t.ToShortDateString)
```

```cs
System.Console.WriteLine(t.ToString());
System.Console.WriteLine(t.ToShortDateString());
```

There are several other functions that can be looked up and used but generally I find these are the two that I use most often.

### Doubles

Doubles are variables that hold numeric values with decimal places. They are similar to the decimal variable type but are less accurate and accumulate rounding errors when calculations are performed.

To declare a double you do this.

```vb
Dim d As Double
```

```cs
double d;
```

This example creates a new variable of type Double named d. It does not assign any value to d. To assign a value to d you can do it like this.

```vb
d = 5.555567
```

```cs
d = 5.555567;
```

Now the value of d is 5.555567.

```vb
d = 2.1
```

```cs
d = 2.1;
```

Now the value of d is 2.1.

Like any other variable type you can print the variable to the console using like this.

```vb
System.Console.WriteLine(d)
```

```cs
System.Console.WriteLine(d);
```

### Objects

Objects are a base type that all other objects are derived from. This means that any other variable no matter the type can be assigned to an object.

To declare an object you do this.

```vb
Dim o As Object
```

```cs
Object o;
```

This example creates a new variable of type Object named o. It does not assign any value to o. To assign a value to o you can do it like this.

```vb
o = "A"c
```

```cs
o = 'A';
```

Now the value of o is A. The type is Char stored in the object. If we assign an integer.

```vb
o = 120
```

```cs
o = 120;
```

Now the value of o is 120 and the type of the stored value is an Integer

We can do the same by assigning strings, decimals, doubles, or any other type or object into an object of type Object.

If we print the object when assigned an integer it will print the integer. If we print when it is assigned char it will print the char and so on with the other variable types.

```vb
System.Console.WriteLine(o)
```

```cs
System.Console.WriteLine(o);
```

Generally I suggest avoiding the Object type as it defeats type checking that a compiler does and in my experience causes a lot of run time errors. The runtime errors are caused when code attempts to do an operation on the object that is not supported by the stored variable type. If we declare the type we want to use in code the compiler can do all the checks that are needed when the program is compiled.

## Objects

VB and c# have built in types such as int, bool, string, and others. Now it is time to create types that is more specific to your application

For example if you are writing an application about tv channels/stations you probably do not want to use strings and integers. It will be easier to think about stations and channels. In VB, c#, and other object oriented languages we can define our own types and use them just like the built in types.

To use a class you must declare one as you would any other variable type.
I will be using the word class and object interchangeably.

For example you declare an integer like this

```vb
Dim i As Integer = 0
```

```cs
int i = 0;
```

Below we create our own data type using the keyword Class. The
best way to use a class is to think of it as an object.
For the purpose of this example our object is going to
be a tv show. Tv shows have many different aspects to them
so we create an object that represent them.

Included in the class are a new class property (ShowName) as Public
and a Private variable (\_showName) that the property works
with. Never declare a class variable as public. Always
use a property or a function.
I will not explain it here but I do encourage you
to read some books on object oriented design.

Private variables are accessible from any function in the class
but cannot be accessed from other classes.

```vb
Public Class TVShow
    Public Sub New()
        ' constructor
    End Sub

    Private _showName As String
    ' Public properties can be accessed from any function inside the
    ' class as well as other classes
    Public Property ShowName() As String
        Get
            ' Inside the get part the private variable is returned.
            ' You can do anything you want here such as data validation
            ' before returning the data if you need or want.
            Return _showName
        End Get
        Set(ByVal value As String)
            ' Inside the set part the private variable is set.
            ' You can do anything you want here such as data validation
            ' before the data is set.
            If value.Trim = "" Then
                Throw New Exception("ShowName cannot be empty")
            End If
            _showName = value
        End Set
    End Property

    ' The above property is long form.  A shorter form can be done as seen below
    Public Property ShowLength As Integer
    Public Property Summary As String
    Public Property Rating As Decimal
    Public Property Episode As String
End Class
```

```cs
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
    public int ShowLength {get; init;}
    public string Summary {get; init;}
    public decimal Rating {get; init;}
    public string Episode {get; init;}
}
```

You create a new instance of a class the same way you would
with an Integer. You create a new instance like this

```vb
Dim starTrek As New TVShow With {
    .ShowName = "Star Trek",
    .ShowLength = 1380,
    .Summary = "Teleport Disaster",
    .Rating = 5.0D,
    .Episode = "1x12"
}
```

```cs
var starTrek = new TVShow() {
    ShowName = "Star Trek",
    ShowLength = 1380,
    Summary = "Teleport Disaster",
    Rating = 5.0m,
    Episode = "1x12"
};
```

If you want a second object you just declare another one.

```vb
Dim dexter As New TVShow With {
    .ShowName = "Dexter",
    .ShowLength = 1380,
    .Summary = "Dexter kills again.",
    .Rating = 4.8D,
    .Episode = "10x01"
}
```

```cs
var dexter = new TVShow() {
    ShowName = "Dexter",
    ShowLength = 1380,
    Summary = "Dexter kills again.",
    Rating = 4.8m,
    Episode = "10x01"
};
```

### Methods

Methods, also known as functions, are used to break code apart into smaller chunks. Functions should do one task and do it well. Functions can be called again and again. They are used to keep duplicate code from building up. This makes things easier to understand. They can be chained/used together to perform complex tasks.

Functions can return a value or return no value. In vb functions that return a value use the key word **Function** and ones that do not return a value use the keyword **Sub**. In c# functions that return a value have a **type** such as a built-in type or object and functions that do not return a value use the keyword **void**.

```cs
public class TVShow
{
    public string ShowName {get; init;}
    public int ShowLength {get; init;}
    public string Summary {get; init;}
    public decimal Rating {get; init;}
    public string Episode {get; init;}

    // includeSummary is a method parameter
    public void PrettyPrint(bool includeSummary){
        if (includeSummary)
        {
            Console.WriteLine($"{ShowName} {Episode} {Rating} {ShowLength} {Summary}");
        }
        else
        {
            Console.WriteLine($"{ShowName} {Episode} {Rating} {ShowLength}");
        }
    }

    public bool IsGoodRating(){
        return Rating >= 3.0m;
    }
}

var dexter = new TVShow() {
    ShowName = "Dexter",
    ShowLength = 1380,
    Summary = "Dexter kills again.",
    Rating = 4.8m,
    Episode = "10x01"
};

dexter.PrettyPrint(includeSummary: true);

if(dexter.IsGoodRating()){
    Console.WriteLine("Let's watch this episode.");
}
```

Method and function parameters are passed by reference for objects and by value for simple types.

## Interfaces

> [Interfaces - define behavior for multiple types](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces). An interface contains definitions for a group of related functionalities that a non-abstract class or a struct must implement.

Interfaces in c# and vb is a way to specify what an object implements. It provides the ability to have different concrete class implementations and choose different ones at runtime.

A good example for further self study is the [Microsoft ILogger](https://learn.microsoft.com/en-us/dotnet/core/extensions/custom-logging-provider).

We will build upon the TVShows class. We will define an interface. We will include a new property ParentalGuide.

Much of this will not make sense until [Structuring an Application](03-structuring-an-application.html#ioc), which covers IOC and dependency injection.

The following code example defines an interface named TVShow. It is not necessary or necessarily recommended to prepend the name with an I but it is very common to see such interfaces in the c# and vb world. In code bases that do prepend an I the name would be ITVShow. The following code examples will not follow that pattern.

Imagine we have a large program involving tv shows. We could pass around the instances of TVShow but that will make our program brittle if and when we need to make changes.

```cs
public interface TVShow
{
    string ShowName {get; init;}
    int ShowLength {get; init;}
    string Summary {get; init;}
    decimal Rating {get; init;}
    string Episode {get; init;}
    string ParentalGuide {get; init;}

    void PrettyPrint(bool includeSummary);
    bool IsGoodRating();
}
```

An interface cannot be initialized. If we were to try to do so it would be a compile time error.

```cs
// Will not compile.
var inst = new TVShow();
```

Below a new class called ComedyShow implements TVShow. Notice line one with **: TVShow** after the class name. ComedyShow is a type of TVShow. Next notice that AdventureShow also implements TVShow.

```cs
public class ComedyShow : TVShow
{
    public string ShowName {get; init;}
    public int ShowLength {get; init;}
    public string Summary {get; init;}
    public decimal Rating {get; init;}
    public string Episode {get; init;}
    public string ParentalGuide {get; init;}

    // includeSummary is a method parameter
    public void PrettyPrint(bool includeSummary){
        if (includeSummary)
        {
            Console.WriteLine($"Comedy: {ShowName} {Episode} {Rating} {ShowLength} {Summary}");
        }
        else
        {
            Console.WriteLine($"Comedy: {ShowName} {Episode} {Rating} {ShowLength}");
        }
    }

    public bool IsGoodRating(){
        return Rating >= 3.0m;
    }
}

public class AdventureShow : TVShow
{
    public string ShowName {get; init;}
    public int ShowLength {get; init;}
    public string Summary {get; init;}
    public decimal Rating {get; init;}
    public string Episode {get; init;}
    public string ParentalGuide {get; init;}

    // includeSummary is a method parameter
    public void PrettyPrint(bool includeSummary){
        if (includeSummary)
        {
            Console.WriteLine($"Adventure: {ShowName} {Episode} {Rating} {ShowLength} {Summary}");
        }
        else
        {
            Console.WriteLine($"Adventure: {ShowName} {Episode} {Rating} {ShowLength}");
        }
    }

    public bool IsGoodRating(){
        return Rating >= 3.5m;
    }
}
```

Reviewing the code we can see that while both the ComedyShow and AdventureShow classes are similar they have different implementations of PrettyPrint and IsGoodRating. In addition to different internals to the interface methods they each could have different private helper methods or even other public methods.

Lets assume our application permits users to enter tv show information and as part of that entry they can add the show as a comedy or an adventure show. Let's store that information in a list. Notice how InsertShow has a parameter TVShow but lower in the code when calling the method all objects that implement the TVShow interface can be added and worked on.

```cs

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

public static void Main()
{
    Shows.InsertShow(new ComedyShow() {
        ShowName = "Friends",
        ShowLength = 1380,
        Summary = "The friends get coffee.",
        Rating = 4.8m,
        Episode = "4x05",
        ParentalGuide = "PG13"
    });
    Shows.InsertShow(new AdventureShow() {
        ShowName = "Rick and morty",
        ShowLength = 760,
        Summary = "A quick 20 minute in and out adventure.",
        Rating = 3.8m,
        Episode = "3x14",
        ParentalGuide = "18A"
    });

    Shows.PrintShows();
}
```

The output is

> Comedy: Friends 4x05 4.8 1380 The friends get coffee.
> Adventure: Rick and morty 3x14 3.8 760 A quick 20 minute in and out adventure.

For simplicity the example above is using a static Shows class. I almost always recommend against using static classes. I've shown their use in the above example as it is simple but in general I have found their use often coincides with global variables and long term they cause a maintenance quagmire. Static classes and variables have their place but try to avoid them.

Note: Read up about base classes and abstract bases classes as they are an alternative to using interfaces. Read about [SOLID](https://en.wikipedia.org/wiki/SOLID) development.
