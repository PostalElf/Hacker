Public Class Player
#Region "Mounts"
    Public Shared ActiveMount As Machine
    Private Shared Mounts As New List(Of Machine)

    Public Shared Function Main(ByVal rawsplit As String()) As Boolean
        Select Case rawsplit(0).ToLower
            Case "mount", "cm" : Return ChangeMount(rawsplit)
            Case "mounts", "sm" : Return ShowMounts()
            Case "sbm" : Return ShowBookmarks()
            Case "bm" : Return AddBookmark(rawsplit)

            Case Else : Console.WriteLine("Invalid command.") : Return False
        End Select
    End Function

    Private Shared Function ShowMounts()
        If ActiveMount.CheckPrivillege(ePriv.Power) = False Then Return False

        For Each m In Mounts
            Console.WriteLine(m.NameFull)
        Next
        Return True
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
#End Region

#Region "Grid"
    Private Shared UIDs As New Dictionary(Of String, Machine)
    Private Shared Bookmarks As New Dictionary(Of String, String)
    Private Shared Function ShowBookmarks() As Boolean
        For Each k In Bookmarks.Keys
            Console.WriteLine(k & " -- " & Bookmarks(k))
        Next
        Return True
    End Function
    Private Shared Function AddBookmark(ByVal rawsplit As String()) As Boolean
        Dim UID As String
        Dim bookmarkName As String
        If rawsplit.Length >= 3 Then
            UID = rawsplit(1)
            bookmarkName = rawsplit(2)
        Else
            If ActiveMount.UID <> "0.0.0.0" Then
                'not on local machine;  ask if user wants to bookmark current machine
                Console.WriteLine("Current machine: " & ActiveMount.NameUID)
                If Menu.confirmChoice(0, "Bookmark current machine? ") = True Then
                    UID = ActiveMount.UID
                Else
                    Console.Write("Enter UID: ")
                    UID = Console.ReadLine
                End If
            Else
                'on local machine, prompt for UID
                Console.Write("Enter UID: ")
                UID = Console.ReadLine
            End If

            'prompt for bookmarkName
            Console.Write("Bookmark as? ")
            bookmarkName = Console.ReadLine
        End If

        Dim machine As Machine = GetMachineFromUID(UID)
        If machine Is Nothing Then Console.WriteLine("Invalid UID.") : Return False

        Return AddBookmark(bookmarkName, UID)
    End Function
    Private Shared Function AddBookmark(ByVal bookmarkName As String, ByVal UID As String) As Boolean
        If Bookmarks.ContainsKey(bookmarkName.ToLower) = True Then Console.WriteLine("Bookmark name already taken.") : Return False
        Bookmarks.Add(bookmarkName.ToLower, UID)
        Return True
    End Function

    Public Shared Function GetMachineFromUID(ByVal UID As String) As Machine
        If UID = "0.0.0.0" Then Return Nothing
        If Bookmarks.ContainsKey(UID) Then UID = Bookmarks(UID) 'check for bookmark alias
        If UIDs.ContainsKey(UID) Then Return UIDs(UID)
        Return Nothing
    End Function
    Private Shared Function CheckValidUID(ByVal UID As String) As Boolean
        Dim us As String() = UID.Split(".")
        If us.Length <> 4 Then Return False

        For Each u In us
            Dim h As Integer
            If Int32.TryParse(u, h) = False Then Return False
        Next
        Return True
    End Function
    Public Shared Function AddUID(ByVal machine As Machine) As Boolean
        If UIDs.ContainsKey(machine.UID) Then Return False
        UIDs.Add(machine.UID, machine)
        Return True
    End Function
#End Region
End Class
