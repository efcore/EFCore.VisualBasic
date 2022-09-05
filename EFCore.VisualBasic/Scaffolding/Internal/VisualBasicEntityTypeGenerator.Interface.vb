Imports System.CodeDom.Compiler
Imports Microsoft.EntityFrameworkCore.Scaffolding.Internal

Namespace Scaffolding.Internal
    Partial Public Class VisualBasicEntityTypeGenerator
        Implements ITextTransformation

        Private Property ITextTransformation_Session As IDictionary(Of String, Object) Implements ITextTransformation.Session
            Get
                Return Me.Session
            End Get
            Set
                Session = Value
            End Set
        End Property

        Private ReadOnly Property ITextTransformation_Errors As CompilerErrorCollection Implements ITextTransformation.Errors
            Get
                Return Errors
            End Get
        End Property

        Private Sub ITextTransformation_Initialize() Implements ITextTransformation.Initialize
            Initialize()
        End Sub

        Private Function ITextTransformation_TransformText() As String Implements ITextTransformation.TransformText
            Return TransformText()
        End Function
    End Class
End Namespace
