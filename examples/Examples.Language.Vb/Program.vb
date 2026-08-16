Imports Examples.Language.Vb.Objects

' Runs the VB listings from "The Language: C# and VB" and the VB lock example
' from "Asynchronous Work and Threads".
Module Program

    Sub Main()
        RunObjects()
        RunLockExample().GetAwaiter().GetResult()
    End Sub

    Private Sub RunObjects()
        Dim i As Integer = 0
        Dim showName As String = "Star Trek"
        Dim watched As Boolean = False
        Dim rating As Decimal = 5.0D
        System.Console.WriteLine($"{i} {showName} {watched} {rating}")

        Dim starTrek As New TVShow With {
            .ShowName = "Star Trek",
            .ShowLength = 1380,
            .Summary = "Teleport Disaster",
            .Rating = 5.0D,
            .Episode = "1x12"
        }
        System.Console.WriteLine($"{starTrek.ShowName} {starTrek.Episode} {starTrek.Rating}")

        Try
            starTrek.ShowName = "   "
            System.Console.WriteLine("ShowName validation did not fire")
        Catch ex As Exception
            System.Console.WriteLine($"as expected: {ex.Message}")
        End Try
    End Sub

    ' Ten tasks each adding 1 a thousand times.  Without SyncLock the total comes
    ' out under 10000 often enough to notice.
    Private Async Function RunLockExample() As Task
        Dim tasks As New List(Of Task)
        Dim lockObject As New Object()

        Dim count As Integer = 0

        For i As Integer = 0 To 9
            tasks.Add(Task.Factory.StartNew(Sub()
                                                For j As Integer = 0 To 999
                                                    SyncLock lockObject
                                                        count = count + 1
                                                    End SyncLock
                                                Next
                                            End Sub))
        Next

        For Each t In tasks
            Await t
        Next

        System.Console.WriteLine(count)
    End Function

End Module
