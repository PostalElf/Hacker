﻿Public Class Machine
    Public Sub New()
        ActiveDirectory = RootDirectory
        RootDirectory.ParentDirectory = RootDirectory

        Dim etc As Dir = New Dir("etc")
        RootDirectory.Add(etc)
        With etc
            .ReadAccess = ePriv.Power
            .WriteAccess = ePriv.Admin
            .RemoveAccess = ePriv.Impossible

            .Add(New FileData("passwd"))
            Passwd = GetDirFileFromPath("/etc/passwd").DirFile
            .Add(New FileData("logs"))
            Logs = GetDirFileFromPath("/etc/logs").DirFile
        End With
    End Sub

    Public Name As String
    Public ReadOnly Property NameFull As String
        Get
            Return Type & "-" & Name
        End Get
    End Property
    Public ReadOnly Property NameUID As String
        Get
            Return Name & "@" & UID
        End Get
    End Property
    Public Type As String
    Public UID As String
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

#Region "Security"
    Private SecurityLevel As Integer
    Private Threat As Integer
    Private Cycles As Integer
    Private Sub CycleCountdown(ByVal activity As Integer)
        If SecurityLevel <= 5 Then Exit Sub


    End Sub
#End Region

#Region "Utilities"
    Public Function GetDirFileFromPath(ByVal path As String) As sMachineDirFile
        'strip beginning and end /, then split path into string()
        If path.StartsWith("/") Then path = path.Remove(0, 1)
        If path.EndsWith("/") Then path = path.Remove(path.Length - 1, 1)
        Dim ps As String() = path.Split("/")

        'determine if path is referencing current active directory
        If ps.Length = 1 Then
            Dim temp As File = ActiveDirectory.GetFile(ps(0))
            If temp Is Nothing = False Then Return New sMachineDirFile(Me, temp) Else Return Nothing
        End If

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
        Dim uidFile As FileData = GetDirFileFromPath("/etc/passwd").DirFile
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
    Private Function GetFileData(ByVal rawpath As String) As sMachineDirFile
        'get machine and file, then check
        Dim path As sMachineDirFile = GetDirFileFromPath(rawpath)
        If path Is Nothing OrElse TypeOf path.DirFile Is File = False Then Console.WriteLine("File not found.") : Return Nothing
        If TypeOf path.DirFile Is FileData = False Then Console.WriteLine(path.DirFile.Name & " is not a data file and cannot be edited.") : Return Nothing
        If path.Machine.CheckPrivillege(path.DirFile.WriteAccess) = False Then Return Nothing

        Return path
    End Function

    Private RootDirectory As New Dir("root")
    Private Passwd As FileData
    Private Logs As FileData
#End Region

#Region "Main"
    Public Function Main(ByVal raw As String) As Boolean
        Dim rawsplit As String() = raw.Split(" ")
        Select Case rawsplit(0).ToLower
            Case "cd" : Return ChangeDirectory(rawsplit)
            Case "cd.." : Return ChangeDirectory({"cd", ".."})
            Case "cls", "clear" : Console.Clear() : Return True
            Case "connect" : Return ConnectUID(rawsplit)
            Case "cp" : Return CopyFile(rawsplit)
            Case "del", "rm" : Return DeleteFile(rawsplit)
            Case "dir", "ls" : Return ListDirectory()
            Case "edit", "vim", "vi" : Return EditFile(rawsplit)
            Case "read" : Return ReadFile(rawsplit)
            Case "su" : Return ChangeUser(rawsplit)
            Case "login" : Return LoginUser(rawsplit)
            Case "md" : Return MakeDirectory(rawsplit)
            Case "mv"
                If CopyFile(rawsplit) = False Then Return False
                Return DeleteFile(rawsplit)
            Case "path" : Console.WriteLine(ActiveDirectory.Path) : Return True
            Case "run", "r", "exe" : Return RunFile(rawsplit)

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
        CycleCountdown(0)
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

        CycleCountdown(0)
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
        CycleCountdown(0)
        Return True
    End Function
    Private Function CopyFile(ByVal rawsplit As String()) As Boolean
        If CheckPrivillege(ActiveDirectory.ReadAccess) = False Then Return False

        'get targetName and destinationName
        Dim targetName As String
        Dim destinationName As String
        If rawsplit.Length >= 3 Then
            targetName = rawsplit(1)
            destinationName = rawsplit(2)
        Else
            ListDirectory("-.. -dir")
            Console.Write("Copy which file? ")
            targetName = Console.ReadLine
            Console.Write("To which directory? ")
            destinationName = Console.ReadLine
        End If

        'get targetPath
        Dim targetPath As sMachineDirFile = GetDirFileFromPath(destinationName)
        If targetPath Is Nothing Then Console.Write("Target file not found.") : Return False
        If TypeOf targetPath.DirFile Is File = False Then Console.WriteLine("Invalid target file.") : Return False
        Dim target As File = targetPath.DirFile

        'get destinationPath
        Dim destinationPath As sMachineDirFile = GetDirFileFromPath(destinationName)
        If destinationPath Is Nothing OrElse TypeOf destinationPath.DirFile Is Dir = False Then Console.WriteLine("Destination directory not found.") : Return False
        Dim destinationMachine As Machine = destinationPath.Machine
        Dim destinationDir As Dir = destinationPath.DirFile
        If destinationMachine.CheckPrivillege(destinationDir.WriteAccess) = False Then Return False

        'all checks out, clone file into destination
        destinationDir.Add(target.Clone)
        targetPath.Machine.CycleCountdown(5)
        If destinationMachine.Equals(targetPath.Machine) = False Then destinationMachine.CycleCountdown(5)
        Return True
    End Function
    Private Function ReadFile(ByVal rawsplit As String()) As Boolean
        'get parameters
        Dim filename As String
        If rawsplit.Length >= 2 Then
            filename = rawsplit(1)
        Else
            Console.Write("Edit which file? ")
            filename = Console.ReadLine
        End If
        Dim path As sMachineDirFile = GetFileData(filename)

        Dim dFile As FileData = CType(path.DirFile, FileData)
        Console.WriteLine(dFile.Name)
        dFile.Display()
        path.Machine.CycleCountdown(2)
        Return True
    End Function
    Private Function EditFile(ByVal rawsplit As String()) As Boolean
        'display file
        If ReadFile(rawsplit) = False Then Return False

        'get parameters
        Dim filename As String
        If rawsplit.Length >= 2 Then
            filename = rawsplit(1)
        Else
            Console.Write("Read which file? ")
            filename = Console.ReadLine
        End If
        Dim path As sMachineDirFile = GetFileData(filename)
        Dim dFile As FileData = CType(path.DirFile, FileData)

        'get linenumber
        Dim lineNumber As Integer
        While True
            Console.WriteLine()
            Console.Write("Edit which line? ")
            Dim lineNumberStr As String = Console.ReadLine
            If lineNumberStr = "-1" Then Return False
            If Int32.TryParse(lineNumberStr, lineNumber) AndAlso lineNumber >= 0 Then Exit While
        End While

        'get content and write content
        Dim lineContent As String
        If lineNumber >= dFile.Contents.Count Then
            Console.Write("New line? ")
            lineContent = Console.ReadLine
            dFile.Contents.Add(lineContent)
        Else
            Console.Write("Line #" & lineNumber & "? ")
            lineContent = Console.ReadLine
            dFile.Contents(lineNumber) = lineContent
        End If
        path.Machine.CycleCountdown(2)
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
        machine.CycleCountdown(5)
        Return True
    End Function
    Private Function LoginUser(ByVal rawsplit As String()) As Boolean
        If ActiveUser = "" Then
            'no loggedin user
            Return ChangeUser(rawsplit)
        Else
            'has loggedin user
            'check to see if activemachine is the same
            If Player.ActiveMount.Equals(Me) Then
                'same machine, run as alias of su
                Return ChangeUser(rawsplit)
            Else
                'different machine, login command does nothing so just return true
                'most likely being run as part of cm
                Return True
            End If
        End If
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
            CycleCountdown(5)
            Return False
        End If

        'successful login
        ActiveUser = user
        CycleCountdown(0)
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

        Dim path As sMachineDirFile = GetDirFileFromPath(fileName)
        If path Is Nothing Then Console.WriteLine(fileName & ": file not found.") : Return False
        Dim machine As Machine = path.Machine
        Dim file As File = path.DirFile
        If TypeOf file Is FileExecutable = False Then Console.WriteLine(file.Name & " is not an executable file and cannot be run.") : Return False
        Dim efile As FileExecutable = CType(file, FileExecutable)
        If machine.CheckPrivillege(efile.ReadAccess) = False Then Return False

        Select Case efile.Type
            Case eExecutable.Dictionary : Return RunDictionary(rawsplit, efile)

            Case eExecutable.Null : Console.WriteLine(file.Name & " is not an executable file and cannot be run.") : Return False
            Case Else : Throw New Exception("Unrecognised eExecutable type.")
        End Select
    End Function

    Private Function RunDictionary(ByVal rawsplit As String(), ByVal exe As FileExecutable) As Boolean
        If exe Is Nothing Then Return False

        'get target
        Dim machineName As String
        If rawsplit.Length >= 2 Then
            machineName = rawsplit(1)
        Else
            Console.Write("Machine UID? ")
            machineName = Console.ReadLine
        End If
        Dim machine As Machine = Player.GetMachineFromUID(machineName)
        If machine Is Nothing Then Console.WriteLine("Invalid UID.") : Return False

        'calculate dictionary cost

    End Function
#End Region

    Public Sub DebugSetupMainframe()
        Name = "mainframe"
        Type = "PC"
        UID = "0.0.0.0"
        ActiveUser = "admin"

        Passwd.Contents.Add("admin:donkeypuncher123:9")
        passwd.Contents.Add("guest::0")
        passwd.Contents.Add("haxxor:leethax69:5")

        With RootDirectory
            .Add(New Dir("test1"))
            .Add(New Dir("test2"))
            .Add(New Dir("test3"))

            .GetDir("test1").Add(New FileData("guests.txt"))
            .GetDir("test2").Add(New Dir("dump"))
            .GetDir("test2").GetDir("dump").Add(New Dir("ster"))
        End With
    End Sub
    Public Sub DebugSetupDrone()
        Name = "grugnir"
        Type = "drone"
        UID = "0.0.0.0"

        Passwd.Contents.Add("admin:admin:9")

        With RootDirectory
            .Add(New Dir("cmds"))
            .Add(New Dir("gearware"))
            .Add(New Dir("software"))
        End With
    End Sub
    Public Sub DebugSetupTestMachine()
        Name = "spirax"
        Type = "server"
        UID = "5519.9918.5010.2047"

        Passwd.Contents.Add("admin:085fN27:9")
        Passwd.Contents.Add("power:manmachine47:5")
        Passwd.Contents.Add("guest:guest:0")

        With RootDirectory
            Dim users As New Dir("users")
            users.Add(New Dir("admin"))
            users.Add(New Dir("power"))
            users.Add(New Dir("guest"))
            .Add(users)
        End With
    End Sub
End Class
