Public MustInherit Class DirFile
    Public Overrides Function ToString() As String
        Return Name
    End Function

    Public Name As String
    Public ParentDirectory As Dir
    Public ReadOnly Property Path As String
        Get
            If ParentDirectory Is Nothing OrElse ParentDirectory.Name = "root" Then
                If Name = "root" Then
                    'root case
                    Return "/"
                End If
            End If
            Return ParentDirectory.Path & Name & "/"
        End Get
    End Property

    Public WriteAccess As ePriv = ePriv.Guest
    Public ReadAccess As ePriv = ePriv.Guest
End Class

Public Class Dir
    Inherits DirFile
    Public Sub New(ByVal _name As String)
        Name = _name
    End Sub

    Public Contents As New List(Of DirFile)
    Public Function GetDir(ByVal target As String) As Dir
        For Each f In Contents
            If TypeOf f Is Dir Then
                If f.Name.ToLower = target.ToLower Then Return f
            End If
        Next
        Return Nothing
    End Function
    Public Function GetFile(ByVal target As String) As File
        For Each f In Contents
            If TypeOf f Is File Then
                If f.Name.ToLower = target.ToLower Then Return f
            End If
        Next
        Return Nothing
    End Function
    Public Function GetDirFile(ByVal target As String) As DirFile
        For Each f In Contents
            If f.Name.ToLower = target.ToLower Then Return f
        Next
        Return Nothing
    End Function

    Public Sub Add(ByVal df As DirFile)
        Contents.Add(df)
        df.ParentDirectory = Me
    End Sub
    Public Sub Remove(ByVal df As DirFile)
        If Contents.Contains(df) = False Then Exit Sub
        Contents.Remove(df)
    End Sub
    Public Sub Remove(ByVal df As String)
        Dim matchFound As DirFile = Nothing
        For Each c In Contents
            If c.Name = df Then matchFound = c : Exit For
        Next

        If matchFound Is Nothing Then Exit Sub
        Remove(matchFound)
    End Sub
End Class

Public Class File
    Inherits DirFile
    Public Sub New(ByVal _name As String)
        Name = _name
    End Sub
    Public Function Clone() As File
        Return New File(Name)
    End Function

    Public Contents As New List(Of String)
End Class