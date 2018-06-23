Public Class Player
    Public Shared ActiveMount As Machine
    Private Shared Mounts As New List(Of Machine)

    Public Shared Function Main(ByVal rawsplit As String()) As Boolean
        Select Case rawsplit(0).ToLower
            Case "mount", "cm" : Return ChangeMount(rawsplit)
            Case "mounts", "sm" : Return ShowMounts()

            Case Else : Console.WriteLine("Invalid command.") : Return False
        End Select
    End Function

    Public Shared Function AddMount(ByVal machine As Machine) As Boolean
        Mounts.Add(machine)
        If Mounts.Count = 1 Then ActiveMount = machine
        Return True
    End Function
    Public Shared Function GetMount(ByVal name As String) As Machine
        For Each m In Mounts
            If m.Name.ToLower = name.ToLower OrElse m.NameFull = name.ToLower Then Return m
        Next
        Return Nothing
    End Function
    Private Shared Function ShowMounts()
        If ActiveMount.CheckPrivillege(ePriv.Power) = False Then Return False

        For Each m In Mounts
            Console.WriteLine(m.NameFull)
        Next
        Return True
    End Function
    Private Shared Function ChangeMount(ByVal rawsplit As String()) As Boolean
        If ActiveMount.CheckPrivillege(ePriv.Admin) = False Then Return False

        Dim targetName As String
        If rawsplit.Length < 2 Then
            ShowMounts()
            Console.Write("Which mount? ")
            targetName = Console.ReadLine
        Else
            targetName = rawsplit(1)
        End If

        Dim target As Machine = GetMount(targetName)
        If target Is Nothing Then Console.WriteLine("Invalid mount name.") : Return False
        If target.Main("login") = False Then Return False

        ActiveMount = target
        Return True
    End Function
End Class
