Imports System.IO

Public Class BuildFileResult
    Public Sub New(targetPath As String)
        Me.TargetPath = targetPath
        Me.TargetDir = Path.GetDirectoryName(targetPath)
        Me.TargetName = Path.GetFileNameWithoutExtension(targetPath)
    End Sub

    Public ReadOnly Property TargetPath As String

    Public ReadOnly Property TargetDir As String

    Public ReadOnly Property TargetName As String
End Class