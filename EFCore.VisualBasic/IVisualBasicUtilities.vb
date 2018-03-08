Imports System
Imports System.Collections.Generic
Imports JetBrains.Annotations
Imports Microsoft.EntityFrameworkCore.Design


Public Interface IVisualBasicUtilities

    Function DelimitString(value As String) As String

    Function EscapeString(str As String) As String

    ' Function EscapeVerbatimString(str As String) As String

    Function GenerateCSharpIdentifier(identifier As String, existingIdentifiers As ICollection(Of String), singularizePluralizer As Func(Of String, String)) As String

    Function GenerateCSharpIdentifier(identifier As String, existingIdentifiers As ICollection(Of String), singularizePluralizer As Func(Of String, String), uniquifier As Func(Of String, ICollection(Of String), String)) As String

    Function GenerateLiteral(value As Boolean) As String

    Function GenerateLiteral(value As Byte()) As String

    Function GenerateLiteral(value As DateTime) As String

    Function GenerateLiteral(value As DateTimeOffset) As String

    Function GenerateLiteral(value As Decimal) As String

    Function GenerateLiteral(value As Double) As String

    Function GenerateLiteral(value As Single) As String

    Function GenerateLiteral(value As Guid) As String

    Function GenerateLiteral(value As Integer) As String

    Function GenerateLiteral(value As Long) As String

    Function GenerateLiteral(value As Object) As String

    Function GenerateLiteral(value As String) As String

    Function GenerateLiteral(value As TimeSpan) As String

    'Function GenerateVerbatimStringLiteral(value As String) As String

    Function GetTypeName(type As Type) As String

    Function IsCSharpKeyword(identifier As String) As Boolean

    Function IsValidIdentifier(name As String) As Boolean

    Function Uniquifier(proposedIdentifier As String, existingIdentifiers As ICollection(Of String)) As String

    Function Generate(methodCallCodeFragment As MethodCallCodeFragment) As String

End Interface
