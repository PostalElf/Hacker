Public Class Machine
    Public Sub New()
        ActiveDirectory = RootDirectory
        RootDirectory.ParentDirectory = RootDirectory

        Dim etc As Dir = New Dir("etc")
        RootDirectory.Add(etc)
        With etc
            .ReadAccess = ePriv.Power
            .WriteAccess = ePriv.Admin
            .RemoveAccess = ePriv.Impossible

            .Add(New File("passwd"))
            Passwd = GetDirFileFromPath("/etc/passwd").DirFile
            .Add(New File("logs"))
            Logs = GetDirFileFromPath("/etc/logs").DirFile
        End With
    End Sub

    Public Name As String
    Public Type As String
    Public Overrides Function ToString() As String
        Return Type & "-" & Name
    End Function
    Public ReadOnly Property Prompt As String
        Get
            Dim total As String = ActiveUser & "@" & Name & " "
            If ActiveDirectory.Name = "root" Then Return total & "/"
            Return total & ActiveDirectory.Name
        End Get
    End Property
    Private ActiveUser As String
    Private ActiveDirectory As Dir

#Region "Utilities"
    Public Function GetDirFileFromPath(ByVal path As String) As sMachineDirFile
        'strip beginning and end /, then split path into string()
        If path.StartsWith("/") Then path = path.Remove(0, 1)
        If path.EndsWith("/") Then path = path.Remove(path.Length - 1, 1)
        Dim ps As String() = path.Split("/")

        'determine if path is on active machine
        Dim startIndex As Integer = 1
        Dim machine As Machine = Player.GetMount(ps(0))
        If machine Is Nothing Then startIndex = 0 : machine = Me

        Dim current As DirFile = machine.RootDirectory
        For n = startIndex To ps.Length - 1
            Dim temp As DirFile = current.GetDirFile(ps(n))
            If temp Is Nothing Then Return Nothing
            current = temp
        Next
        Return New sMachineDirFile(machine, current)
    End Function
    Public Function CheckPrivillege(ByVal target As ePriv) As Boolean
        Dim uidTable = GetUserTable()
        Dim priv As ePriv = uidTable(ActiveUser)(1)

        If target = ePriv.Impossible Then Console.WriteLine("Error! This is a protected system file and cannot be removed or modified.") : Return False
        If priv >= target Then
            Return True
        Else
            Console.WriteLine("Insufficient user privilleges: " & target.ToString & " user or higher required.")
            Return False
        End If
    End Function
    Private Function GetUserTable() As Dictionary(Of String, String())
        Dim uidFile As File = GetDirFileFromPath("/etc/passwd").DirFile
        Dim uidData As List(Of String) = uidFile.Contents

        Dim uidTable As New Dictionary(Of String, String())
        For Each p In uidData
            Dim ps As String() = p.Split(":")
            Dim userName As String = ps(0)
            Dim pwd As String = ps(1)
            Dim priv As String = ps(2)
            If uidTable.ContainsKey(userName) = False Then uidTable.Add(userName, {"", ""})
            uidTable(userName) = {pwd, priv}
        Next

        Return uidTable
    End Function

    Private RootDirectory As New Dir("root")
    Private Passwd As File
    Private Logs As File
#End Region

#Region "Main"
    Public Function Main(ByVal raw As String) As Boolean
        Dim rawsplit As String() = raw.Split(" ")
        Select Case rawsplit(0).ToLower
            Case "cd" : Return ChangeDirectory(rawsplit)
            Case "cd.." : Return ChangeDirectory({"cd", ".."})
            Case "cls", "clear" : Console.Clear() : Return True
            Case "cp" : Return CopyFile(rawsplit)
            Case "del", "rm" : Return DeleteFile(rawsplit)
            Case "dir", "ls" : Return ListDirectory()
            Case "login", "su" : Return ChangeUser(rawsplit)
            Case "md" : Return MakeDirectory(rawsplit)
            Case "mv"
                If CopyFile(rawsplit) = False Then Return False
                Return DeleteFile(rawsplit)
            Case "path" : Console.WriteLine(ActiveDirectory.Path) : Return True
            Case "run", "r" : Return RunFile(rawsplit)

            Case Else : Return Player.Main(rawsplit)
        End Select
    End Function

    Private Function ListDirectory(Optional ByVal flags As String = "") As Boolean
        If CheckPrivillege(ActiveDirectory.ReadAccess) = False Then Return False

        Dim total As New List(Of String)
        If flags.Contains("-..") = False Then total.Add("[..]")
        Dim files As New List(Of String)
        For Each DirFile In ActiveDirectory.Contents
            If TypeOf DirFile Is Dir Then
                If flags.Contains("-dir") = False Then total.Add("[" & DirFile.Name & "]")
            ElseIf TypeOf DirFile Is File Then
                If flags.Contains("-file") = False Then files.Add(DirFile.Name)
            End If
        Next

        'add files to the end of total for presentation
        total.AddRange(files)

        'actually display total
        For Each f In total
            Console.WriteLine(f)
        Next
        Return True
    End Function
    Private Function ChangeDirectory(ByVal rawsplit As String()) As Boolean
        If rawsplit.Length <> 2 Then Console.WriteLine("Syntax: cd [directory]") : Return False
        Dim adName As String = rawsplit(1)
        If adName = "/" Then
            ActiveDirectory = RootDirectory
        ElseIf adName = ".." Then
            If ActiveDirectory.Equals(RootDirectory) Then Return False 'can't go below root
            ActiveDirectory = ActiveDirectory.ParentDirectory
        Else
            Dim ad As DirFile = ActiveDirectory.GetDirFile(adName)
            If ad Is Nothing Then Console.WriteLine("Directory not found.") : Return False
            If TypeOf ad Is File Then Console.WriteLine("Cannot cd into a file.") : Return False
            ActiveDirectory = ad
        End If

        Return True
    End Function
    Private Function MakeDirectory(ByVal rawsplit As String()) As Boolean
        If CheckPrivillege(ActiveDirectory.WriteAccess) = False Then Return False

        If rawsplit.Length <> 2 Then Console.WriteLine("Syntax: md [directory]") : Return False
        Dim mdName As String = rawsplit(1)
        If mdName.Contains("/") Then Console.WriteLine("Directory name cannot contain special characters.") : Return False

        Dim newDir As New Dir(mdName)
        ActiveDirectory.Add(newDir)
        Console.WriteLine("New directory created: " & newDir.Path)
        Return True
    End Function
    Private Function CopyFile(ByVal rawsplit As String()) As Boolean
        If CheckPrivillege(ActiveDirectory.ReadAccess) = False Then Return False

        Dim targetName As String
        Dim target As File
        Dim destinationName As String

        If rawsplit.Length >= 3 Then
            targetName = rawsplit(1)
            destinationName = rawsplit(2)
            target = ActiveDirectory.GetFile(targetName)
            If target Is Nothing Then Console.WriteLine("File not found.") : Return False
        Else
            ListDirectory("-.. -dir")
            Console.Write("Copy which file? ")
            targetName = Console.ReadLine
            target = ActiveDirectory.GetFile(targetName)
            If target Is Nothing Then Console.WriteLine("File not found.") : Return False
            Console.Write("To which directory? ")
            destinationName = Console.ReadLine
        End If

        Dim path As sMachineDirFile = GetDirFileFromPath(destinationName)
        If path Is Nothing OrElse TypeOf path.DirFile Is Dir = False Then Console.WriteLine("Destination directory not found.") : Return False
        Dim destinationMachine As Machine = path.Machine
        Dim destinationDir As Dir = path.DirFile
        If destinationMachine.CheckPrivillege(destinationDir.WriteAccess) = False Then Return False

        'all checks out, clone file into destination
        destinationDir.Add(target.Clone)
        Return True
    End Function
    Private Function DeleteFile(ByVal rawsplit As String()) As Boolean
        If CheckPrivillege(ActiveDirectory.WriteAccess) = False Then Return False
        If CheckPrivillege(ActiveDirectory.removeAccess) = False Then Return False

        Dim filename As String
        If rawsplit.Length >= 2 Then
            filename = rawsplit(1)
        Else
            Console.Write("Delete which file? ")
            filename = Console.ReadLine
        End If

        Dim path As sMachineDirFile = GetDirFileFromPath(filename)
        If path Is Nothing OrElse TypeOf path.DirFile Is File = False Then Console.WriteLine("File not found.") : Return False
        Dim machine As Machine = path.Machine
        Dim file As File = path.DirFile

        file.ParentDirectory.Remove(file)
        Return True
    End Function
    Private Function ChangeUser(ByVal rawsplit As String()) As Boolean
        Dim userName As String
        Dim password As String

        If rawsplit.Length < 3 Then
            Console.Write("Enter user name: ")
            userName = Console.ReadLine
            Console.Write("Enter password: ")
            password = Console.ReadLine
        Else
            userName = rawsplit(1)
            password = rawsplit(2)
        End If

        Return ChangeUser(userName, password)
    End Function
    Private Function ChangeUser(ByVal user As String, ByVal password As String) As Boolean
        Dim uidTable = GetUserTable()
        If uidTable.ContainsKey(user) = False OrElse uidTable(user)(0) <> password Then
            Console.WriteLine("Invalid user or password!")
            Return False
        End If

        'successful login
        ActiveUser = user
        Return True
    End Function
    Private Function RunFile(ByVal rawsplit As String()) As Boolean
        Dim fileName As String
        If rawsplit.Length >= 2 Then
            fileName = rawsplit(1)
        Else
            Console.Write("Which file? ")
            fileName = Console.ReadLine()
        End If

        Dim file As File = ActiveDirectory.GetFile(fileName)
        If file Is Nothing Then Console.WriteLine("Invalid file name.") : Return False
        If CheckPrivillege(file.ReadAccess) = False Then Return False
    End Function
#End Region

    Public Sub DebugSetupMainframe()
        Name = "mainframe"
        Type = "PC"
        ActiveUser = "admin"

        Passwd.Contents.Add("admin:donkeypuncher123:9")
        passwd.Contents.Add("guest::0")
        passwd.Contents.Add("haxxor:leethax69:5")

        With RootDirectory
            .Add(New Dir("test1"))
            .Add(New Dir("test2"))
            .Add(New Dir("test3"))
        End With
        RootDirectory.GetDir("test1").Add(New File("guests.txt"))
        RootDirectory.GetDir("test2").Add(New Dir("dump"))
        RootDirectory.GetDir("test2").GetDir("dump").Add(New Dir("ster"))
    End Sub
    Public Sub DebugSetupDrone()
        Name = "grugnir"
        Type = "drone"
        ActiveUser = "admin"

        Passwd.Contents.Add("admin:admin:9")

        With RootDirectory
            .Add(New Dir("cmds"))
            .Add(New Dir("gearware"))
            .Add(New Dir("software"))
        End With
    End Sub
End Class
