Public Class Machine
    Private Name As String
    Public ReadOnly Property Prompt As String
        Get
            Dim total As String = ActiveUser & "@" & Name & " "
            If ActiveDirectory.Name = "root" Then Return total & "/"
            Return total & ActiveDirectory.Name
        End Get
    End Property
    Private ActiveUser As String
    Private ActiveDirectory As Dir
    Private ActiveMount As Machine
    Private RootDirectory As New Dir("root")
    Private Mounts As New List(Of Machine)

    Public Function GetDirFromPath(ByVal path As String) As Dir
        If path.StartsWith("/") Then path = path.Remove(0, 1)
        If path.EndsWith("/") Then path = path.Remove(path.Length - 1, 1)
        Dim ps As String() = path.Split("/")
        Dim current As Dir = RootDirectory
        For n = 0 To ps.Length - 1
            Dim temp As Dir = current.GetDir(ps(n))
            If temp Is Nothing Then Return Nothing
            current = temp
        Next
        Return current
    End Function
    Private Function GetUserTable() As Dictionary(Of String, String())
        Dim uidPath As Dir = GetDirFromPath("/etc/")
        Dim uidFile As File = uidPath.GetFile("passwd")
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
    Private Function CheckPrivillege(ByVal target As ePriv) As Boolean
        Dim uidTable = GetUserTable()
        Dim priv As ePriv = uidTable(ActiveUser)(1)

        If priv >= target Then
            Return True
        Else
            Console.WriteLine("Insufficient user privilleges: " & target.ToString & " user or higher required.")
            Return False
        End If
    End Function

    Public Sub New()
        ActiveDirectory = RootDirectory
        RootDirectory.ParentDirectory = RootDirectory

        Dim etc As Dir = New Dir("etc")
        RootDirectory.Add(etc)
        Dim passwd As File = New File("passwd")
        etc.Add(passwd)
        etc.Add(New File("logs"))
    End Sub
    Public Sub Main(ByVal raw As String)
        Dim rawsplit As String() = raw.Split(" ")
        Select Case rawsplit(0).ToLower
            Case "cd" : ChangeDirectory(rawsplit)
            Case "cls", "clear" : Console.Clear()
            Case "cp" : CopyFile(rawsplit)
            Case "del", "rm" : DeleteFile(rawsplit)
            Case "dir", "ls" : ListDirectory()
            Case "login", "su" : ChangeUser(rawsplit)
            Case "md" : MakeDirectory(rawsplit)
            Case "mount" : ChangeMount(rawsplit)
            Case "mounts", "dev", "devices" : ShowMounts()
            Case "mv" : CopyFile(rawsplit) : deletefile(rawsplit)
            Case "path" : Console.WriteLine(ActiveDirectory.Path)
            Case "cp" : CopyFile(rawsplit)
        End Select
    End Sub
    Private Sub ListDirectory(Optional ByVal flags As String = "")
        Dim total As New List(Of String)
        If flags.Contains("-..") = False Then total.Add("[..]")
        Dim files As New List(Of String)
        For Each DirFile In ActiveDirectory.Contents
            If TypeOf DirFile Is Dir Then
                If flags.Contains("-dir") Then Continue For
                total.Add("[" & DirFile.Name & "]") : Continue For
            ElseIf TypeOf DirFile Is File Then
                If flags.Contains("-file") Then Continue For
                files.Add(DirFile.Name) : Continue For
            End If
        Next

        'add files to the end of total for presentation
        total.AddRange(files)

        'actually display total
        For Each f In total
            Console.WriteLine(f)
        Next
    End Sub
    Private Sub ChangeDirectory(ByVal rawsplit As String())
        If rawsplit.Length <> 2 Then Console.WriteLine("Syntax: cd [directory]") : Exit Sub
        Dim adName As String = rawsplit(1)
        If adName = "/" Then
            ActiveDirectory = RootDirectory
        ElseIf adName = ".." Then
            If ActiveDirectory.Equals(RootDirectory) Then Exit Sub 'can't go below root
            ActiveDirectory = ActiveDirectory.ParentDirectory
        Else
            Dim ad As DirFile = ActiveDirectory.GetDirFile(adName)
            If ad Is Nothing Then Console.WriteLine("Directory not found.") : Exit Sub
            If TypeOf ad Is File Then Console.WriteLine("Cannot cd into a file.") : Exit Sub
            ActiveDirectory = ad
        End If
    End Sub
    Private Sub MakeDirectory(ByVal rawsplit As String())
        If rawsplit.Length <> 2 Then Console.WriteLine("Syntax: md [directory]") : Exit Sub
        Dim mdName As String = rawsplit(1)
        If mdName.Contains("/") Then Console.WriteLine("Directory name cannot contain special characters.")
        Dim newDir As New Dir(mdName)
        ActiveDirectory.Add(newDir)
        Console.WriteLine("New directory created: " & newDir.Path)
    End Sub
    Private Sub CopyFile(ByVal rawsplit As String())
        Dim targetName As String
        Dim destinationName As String

        If rawsplit.Length = 3 Then
            targetName = rawsplit(1)
            destinationName = rawsplit(2)
            Dim target As File = ActiveDirectory.GetFile(targetName)
            If target Is Nothing Then Console.WriteLine("File not found.") : Exit Sub
        Else
            ListDirectory("-.. -dir")
            Console.Write("Copy which file? ")
            targetName = Console.ReadLine
            Dim target As File = ActiveDirectory.GetFile(targetName)
            If target Is Nothing Then Console.WriteLine("File not found.") : Exit Sub
            Console.Write("To which directory? ")
            destinationName = Console.ReadLine
        End If

        Dim destination As Dir = GetDirFromPath(destinationName)
        If destination Is Nothing Then Console.WriteLine("Destination directory not found.") : Exit Sub
        CopyFile(targetName, destinationName)
    End Sub
    Private Sub CopyFile(ByVal targetName As String, ByVal path As String)
        Dim destination As Dir = GetDirFromPath(path)
        Dim target As File = ActiveDirectory.GetFile(targetName)

        destination.Add(target.Clone)
    End Sub
    Private Sub DeleteFile(ByVal rawsplit As String())
        Dim targetName As String
        If rawsplit.Length >= 2 Then
            targetName = rawsplit(1)
        Else
            ListDirectory("-.. -dir")
            Console.Write("Delete which file? ")
            targetName = Console.ReadLine
        End If

        Dim target As File = ActiveDirectory.GetFile(targetName)
        If target Is Nothing Then Console.WriteLine("File not found.") : Exit Sub
        ActiveDirectory.Remove(targetName)
    End Sub
    Private Sub ChangeUser(ByVal rawsplit As String())
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

        ChangeUser(userName, password)
    End Sub
    Private Sub ChangeUser(ByVal user As String, ByVal password As String)
        Dim uidTable = GetUserTable()
        If uidTable.ContainsKey(user) = False OrElse uidTable(user)(0) <> password Then
            Console.WriteLine("Invalid user or password!")
            Exit Sub
        End If

        'successful login
        ActiveUser = user
    End Sub
    Private Sub ShowMounts()
        If CheckPrivillege(ePriv.Power) = False Then Exit Sub

        For Each m In Mounts
            Console.WriteLine(m.Name)
        Next
    End Sub
    Private Sub ChangeMount(ByVal rawsplit As String())
        If CheckPrivillege(ePriv.Admin) = False Then Exit Sub

        Dim targetName As String
        If rawsplit.Length < 2 Then
            ShowMounts()
            Console.Write("Which mount? ")
            targetName = Console.ReadLine
        Else
            targetName = rawsplit(1)
        End If

        Dim target As Machine = Nothing
        For Each m In Mounts
            If m.Name.ToLower = targetName.ToLower Then target = m : Exit For
        Next
        If target Is Nothing Then Console.WriteLine("Invalid mount name.") : Exit Sub

        ActiveMount = target
    End Sub

    Public Sub DebugSetup()
        Name = "mainframe"
        ActiveUser = "guest"

        Dim etc As Dir = GetDirFromPath("/etc/")
        Dim passwd As File = etc.GetFile("passwd")
        passwd.Contents.Add("admin:donkeypuncher123:9")
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
End Class
