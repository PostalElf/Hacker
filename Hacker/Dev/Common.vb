<DebuggerStepThrough()>
Public Module Common
    Public Rng As New Random(5)

    Public Function vbTab(ByVal word As String, ByVal totalLength As Integer)
        Dim spaceCount As Integer = totalLength - word.Length
        Dim spaces As String = ""
        For n = 1 To spaceCount
            spaces &= " "
        Next
        Return word & spaces
    End Function

    Public Function FormatCommaList(Of T)(ByVal list As List(Of T)) As String
        Dim total As String = ""
        For n = 0 To list.Count - 1
            total &= list(n).ToString
            If n <> list.Count - 1 Then total &= ", "
        Next
        Return total
    End Function
    Public Function UnformatCommaList(ByVal value As String) As List(Of String)
        Dim total As New List(Of String)
        Dim values As String() = value.Split(",")
        For Each s In values
            Dim v As String = s.Trim
            total.Add(v)
        Next
        Return total
    End Function

    Public Function GetRandom(Of T)(ByRef list As List(Of T)) As T
        Dim roll As Integer = Rng.Next(list.Count)
        GetRandom = list(roll)
    End Function
    Public Function GrabRandom(Of T)(ByRef list As List(Of T)) As T
        Dim roll As Integer = Rng.Next(list.Count)
        GrabRandom = list(roll)
        list.RemoveAt(roll)
    End Function
    Public Function GetProbability(ByVal probabilities As Integer()) As Integer
        'returns index of probabilities
        Dim roll As Integer = Rng.Next(1, 101)
        Dim count As Integer = probabilities(0)
        For n = 0 To probabilities.Count - 1
            If roll <= count Then Return n
            If n = probabilities.Count - 1 Then Return -1
            count += probabilities(n + 1)
        Next
        Return -1
    End Function

    Public Function String2Enum(Of T)(ByVal value As String) As T
        For Each dt In [Enum].GetValues(GetType(T))
            If dt.ToString = value Then Return dt
        Next
        Return Nothing
    End Function
    Public Function StringPossessive(ByVal value As String) As String
        Dim lastChar As Char = value(value.Length - 1)
        If lastChar = "s"c OrElse lastChar = "S"c Then Return value & "'" Else Return value & "'s"
    End Function
End Module

Public Class AutoIncrementer
    Private Counter As Integer = 0
    Public Function N() As Integer
        N = Counter
        Counter += 1
    End Function
End Class