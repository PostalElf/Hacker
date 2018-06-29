Public Class Menu
    Private Shared Function vbSpace(Optional ByVal i As Integer = 1) As String
        Const space As String = " "
        Dim total As String = ""
        For n = 1 To i
            total &= space
        Next
        Return total
    End Function
    Friend Shared Function getListChoice(Of T)(ByVal objList As List(Of T), ByVal indent As Integer, Optional ByVal str As String = "", Optional ByVal prompt As String = "> ") As T
        If objList.Count = 0 Then Return Nothing
        If objList.Count = 1 Then Return objList(0)

        Dim ind As String = vbSpace(indent)
        If str <> "" Then
            Console.WriteLine(str)
        End If

        For n = 0 To objList.Count - 1
            Dim obj As Object = objList(n)
            Console.WriteLine(ind & n + 1 & ") " & obj.ToString)
        Next

        While True
            Console.WriteLine()
            Console.Write(ind & prompt)
            Dim input As String = Console.ReadLine

            If IsNumeric(input) = True Then
                Dim num As Integer = CInt(input) - 1
                If num > objList.Count OrElse num < -1 Then
                    Console.WriteLine("Invalid input!")
                ElseIf num = -1 Then
                    'escape clause when user input 0
                    Return Nothing
                Else
                    Return objList(num)
                End If
            Else
                Console.WriteLine("Invalid input!")
            End If
        End While

        Return Nothing
    End Function
    Friend Shared Function getListChoice(ByVal objList As Dictionary(Of Integer, String), ByVal indent As Integer, Optional ByVal str As String = "", Optional ByVal prompt As String = "> ") As Integer
        If objList.Count = 0 Then Return Nothing

        Dim ind As String = vbSpace(indent)
        If str <> "" Then Console.WriteLine(str)

        For Each kvp In objList
            Console.WriteLine(ind & kvp.Key & ") " & kvp.Value)
        Next

        While True
            Console.WriteLine()
            Console.Write(ind & prompt)
            Dim input As String = Console.ReadLine
            If input = Nothing Then Return Nothing
            Dim inputNum As Integer = CInt(input)

            If objList.ContainsKey(inputNum) OrElse inputNum = 0 Then
                Return inputNum
            Else
                Console.WriteLine()
                Console.WriteLine("Invalid input!")
            End If
        End While

        Return Nothing
    End Function
    Friend Shared Function getListChoice(ByVal objList As Dictionary(Of Char, String), ByVal indent As Integer, Optional ByVal str As String = "", Optional ByVal prompt As String = "> ") As Char
        If objList.Count = 0 Then Return Nothing

        Dim ind As String = vbSpace(indent)
        If str <> "" Then Console.WriteLine(str)

        For Each kvp In objList
            Console.WriteLine(ind & kvp.Key & ") " & kvp.Value)
        Next

        While True
            Console.WriteLine()
            Console.Write(ind & prompt)
            Dim input As ConsoleKeyInfo = Console.ReadKey

            If objList.ContainsKey(input.KeyChar) OrElse objList.ContainsKey(Char.ToUpperInvariant(input.KeyChar)) _
                OrElse input.Key = ConsoleKey.Spacebar OrElse input.Key = ConsoleKey.Enter _
                Then
                Return Char.ToLowerInvariant(input.KeyChar)
            Else
                Console.WriteLine()
                Console.WriteLine("Invalid input!")
            End If
        End While

        Return Nothing
    End Function
    Friend Shared Function getListChoiceIndex(Of T)(ByVal objList As List(Of T), ByVal indent As Integer, Optional ByVal str As String = "", Optional ByVal prompt As String = "> ") As Integer
        If objList.Count = 0 Then Return Nothing
        If objList.Count = 1 Then Return 0

        Dim ind As String = vbSpace(indent)
        If str <> "" Then Console.WriteLine(str)

        For n = 0 To objList.Count - 1
            Console.WriteLine(ind & n + 1 & ") " & objList(n).ToString)
        Next

        While True
            Console.WriteLine()
            Console.Write(ind & prompt)
            Dim input As String = Console.ReadLine
            If IsNumeric(input) = False Then Console.WriteLine(vbCrLf & "Invalid input!") : Continue While
            If input = Nothing Then Return -1
            Dim inputNum As Integer = CInt(input)

            Dim realNum As Integer = inputNum - 1
            If realNum > objList.Count - 1 Then Console.WriteLine(vbCrLf & "Invalid input!") : Continue While
            Return realNum
        End While
        Return -1
    End Function

    Friend Shared Function confirmChoice(ByVal indent As Integer, Optional ByVal str As String = "Are you sure? ") As Boolean
        Dim ind As String = vbSpace(indent)

        Console.Write(ind & str)
        Dim input As ConsoleKeyInfo = Console.ReadKey()
        If input.Key = ConsoleKey.Y Then Return True Else Return False
    End Function
    Friend Shared Function getNumInput(ByVal indent As Integer, ByVal min As Integer, ByVal max As Integer, ByVal str As String) As Integer
        Dim input As String = ""
        Dim ind As String = vbSpace(indent)

        While True
            Console.Write(ind & str)
            input = Console.ReadLine
            If IsNumeric(input) = True Then
                Dim num As Integer = CInt(input)
                If num >= min AndAlso num <= max Then Return num Else Console.WriteLine(ind & "Number must be between " & min & " and " & max & ".")
            Else
                Console.WriteLine(ind & "Numbers only please!")
            End If
        End While
        Return Nothing
    End Function
    Friend Shared Function getCharInput(ByVal objList As List(Of Char), ByVal str As String) As Char
        While True
            Console.Write(str)
            Dim input As ConsoleKeyInfo = Console.ReadKey

            If input.Key = ConsoleKey.Escape Then
                Return Nothing
            ElseIf objList.Contains(Char.ToLower(input.KeyChar)) OrElse objList.Contains(Char.ToUpper(input.KeyChar)) Then
                Return input.KeyChar
            Else
                Console.WriteLine()
                Console.WriteLine("Invalid input.")
            End If
        End While
        Return Nothing
    End Function
End Class