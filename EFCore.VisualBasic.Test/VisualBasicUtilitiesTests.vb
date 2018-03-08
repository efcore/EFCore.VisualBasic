Imports Microsoft.EntityFrameworkCore.Design
Imports Xunit

Public Class VisualBasicUtilitiesTests
    <Theory>
    <InlineData(GetType(Integer), "Integer")>
    <InlineData(GetType(Integer?), "Integer?")>
    <InlineData(GetType(Integer()), "Integer()")>
    <InlineData(GetType(Boolean), "Boolean")>
    <InlineData(GetType(Integer), "Integer")>
    <InlineData(GetType(UInteger), "UInteger")>
    <InlineData(GetType(Short), "Short")>
    <InlineData(GetType(UShort), "UShort")>
    <InlineData(GetType(Long), "Long")>
    <InlineData(GetType(ULong), "ULong")>
    <InlineData(GetType(Decimal), "Decimal")>
    <InlineData(GetType(Single), "Single")>
    <InlineData(GetType(String), "String")>
    <InlineData(GetType(SByte), "SByte")>
    <InlineData(GetType(Byte), "Byte")>
    <InlineData(GetType(Char), "Char")>
    <InlineData(GetType(Double), "Double")>
    <InlineData(GetType(Object), "Object")>
    <InlineData(GetType(Dictionary(Of String, List(Of Integer))), "Dictionary(Of String, List(Of Integer))")>
    <InlineData(GetType(List(Of Integer?)), "List(Of Integer?)")>
    <InlineData(GetType(SomeGenericStruct(Of Integer)?), "SomeGenericStruct(Of Integer)?")>
    Public Sub GetTypeName(ByVal type As Type, ByVal typeName As String)
        Assert.Equal(typeName, New VisualBasicUtilities().GetTypeName(type))
    End Sub

    Private Structure SomeGenericStruct(Of T)
    End Structure

    <Theory>
    <InlineData("", """""")>
    <InlineData("SomeValue", """SomeValue""")>
    <InlineData("Contains""QuoteAnd" & vbTab & "Tab", """Contains""""QuoteAnd"" & vbTab & ""Tab""")>
    Public Sub DelimitString(ByVal input As String, ByVal expectedOutput As String)
        Assert.Equal(expectedOutput, New VisualBasicUtilities().DelimitString(input))
    End Sub


    <Fact>
    Public Sub Generate_MethodCallCodeFragment_works()
        Dim method = New MethodCallCodeFragment("Test", True, 42)
        Dim result = New VisualBasicUtilities().Generate(method)
        Assert.Equal(".Test(True, 42)", result)
    End Sub

    <Fact>
    Public Sub Generate_MethodCallCodeFragment_works_when_niladic()
        Dim method = New MethodCallCodeFragment("Test")
        Dim result = New VisualBasicUtilities().Generate(method)
        Assert.Equal(".Test()", result)
    End Sub

    <Fact>
    Public Sub Generate_MethodCallCodeFragment_works_when_chaining()
        Dim method = New MethodCallCodeFragment("Test").Chain("Test")
        Dim result = New VisualBasicUtilities().Generate(method)
        Assert.Equal(".Test().Test()", result)
    End Sub

    <Fact>
    Public Sub Generate_MethodCallCodeFragment_works_when_nested_closure()
        Dim method = New MethodCallCodeFragment("Test", New NestedClosureCodeFragment("x", New MethodCallCodeFragment("Test")))
        Dim result = New VisualBasicUtilities().Generate(method)
        Assert.Equal(".Test(Function(x) x.Test())", result)
    End Sub

End Class
