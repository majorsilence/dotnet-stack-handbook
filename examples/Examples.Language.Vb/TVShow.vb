Namespace Objects

    ' The VB half of the objects section.  Note that these are Property and not
    ' ReadOnly Property: a `With {}` initializer assigns to them, and a ReadOnly
    ' auto-property has no setter for it to assign through.
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

End Namespace
