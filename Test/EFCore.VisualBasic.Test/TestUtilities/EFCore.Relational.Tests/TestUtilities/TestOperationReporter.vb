Imports Microsoft.EntityFrameworkCore.Design.Internal

Namespace TestUtilities
    Public Class TestOperationReporter
        Implements IOperationReporter

        Private ReadOnly _messages As New List(Of String)()
        Public ReadOnly Property Messages As IReadOnlyList(Of String)
            Get
                Return _messages
            End Get
        End Property

        Public Sub Clear()
            Call _messages.Clear()
        End Sub

        Public Sub WriteInformation(message As String) Implements IOperationReporter.WriteInformation
            Call _messages.Add("info: " & message)
        End Sub

        Public Sub WriteVerbose(message As String) Implements IOperationReporter.WriteVerbose
            Call _messages.Add("verbose: " & message)
        End Sub

        Public Sub WriteWarning(message As String) Implements IOperationReporter.WriteWarning
            Call _messages.Add("warn: " & message)
        End Sub

        Public Sub WriteError(message As String) Implements IOperationReporter.WriteError
            Call _messages.Add("error: " & message)
        End Sub
    End Class
End Namespace
