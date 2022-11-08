﻿Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.Linq
Imports System.Text
Imports EntityFrameworkCore.VisualBasic.Design
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.Extensions.DependencyInjection
Imports System

'------------------------------------------------------------------------------
'<auto-generated>
'    This code was generated by a tool.
'    Runtime Version: 17.0.0.0
' 
'    Changes to this file may cause incorrect behavior and will be lost if
'    the code is regenerated.
'</auto-generated>
'------------------------------------------------------------------------------
Namespace Scaffolding.Internal
    '''<summary>
    '''Class to produce the template output
    '''</summary>
    <Global.System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")>  _
    Partial Public Class VisualBasicEntityTypeGenerator
        Inherits VisualBasicEntityTypeGeneratorBase
        '''<summary>
        '''Create the template output
        '''</summary>
        Public Overridable Function TransformText() As String

    If EntityType.IsSimpleManyToManyJoinEntityType() Then
        ' Don't scaffold these
        Return ""
    End If

    Dim services = DirectCast(Host, IServiceProvider)
    Dim annotationCodeGenerator = services.GetRequiredService(Of IAnnotationCodeGenerator)()
    Dim code = services.GetRequiredService(Of IVisualBasicHelper)()

    Dim importsList = New List(Of String) From {
        "System",
        "System.Collections.Generic"
    }

    If Options.UseDataAnnotations Then
        importsList.Add("System.ComponentModel.DataAnnotations")
        importsList.Add("System.ComponentModel.DataAnnotations.Schema")
        importsList.Add("Microsoft.EntityFrameworkCore")
    End If

    Dim FileNamespaceIdentifier = code.NamespaceIdentifier(Options.RootNamespace, NamespaceHint)

    If Not String.IsNullOrEmpty(FileNamespaceIdentifier) Then

            Me.Write("Namespace ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(FileNamespaceIdentifier))
            Me.Write(""&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10))

    End If

    If Not String.IsNullOrEmpty(EntityType.GetComment()) Then

            Me.Write("    ''' <summary>"&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10)&"    ''' ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(code.XmlComment(EntityType.GetComment(), indent:= 1)))
            Me.Write(""&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10)&"    ''' </summary>"&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10))

    End If

    If Options.UseDataAnnotations Then
        For Each dataAnnotation in EntityType.GetDataAnnotations(annotationCodeGenerator)

            Me.Write("    ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(code.Fragment(dataAnnotation)))
            Me.Write(""&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10))

        Next
    End If

            Me.Write("    Partial Public Class ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(EntityType.Name))
            Me.Write(""&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10))

    Dim firstProperty = true
    For Each prop In entityType.GetProperties().OrderBy(Function(p) If(p.GetColumnOrder(), -1))
        If Not firstProperty Then
            WriteLine("")
        End If

        If Not String.IsNullOrEmpty(prop.GetComment()) Then

            Me.Write("        ''' <summary>"&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10)&"        ''' ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(code.XmlComment(prop.GetComment(), indent:= 2)))
            Me.Write(""&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10)&"        ''' </summary>"&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10))

        End If

        If Options.UseDataAnnotations Then
            Dim dataAnnotations = prop.GetDataAnnotations(annotationCodeGenerator).
                                        Where(Function(a) Not (a.Type = GetType(RequiredAttribute) AndAlso 
                                                          Options.UseNullableReferenceTypes AndAlso 
                                                          Not prop.ClrType.IsValueType))
            For Each dataAnnotation in dataAnnotations

            Me.Write("        ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(code.Fragment(dataAnnotation)))
            Me.Write(""&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10))

            Next
        End If

        importsList.AddRange(code.GetRequiredImports(prop.ClrType))

            Me.Write("        Public Property ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(code.Identifier(prop.Name)))
            Me.Write(" As ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(code.Reference(prop.ClrType)))
            Me.Write(""&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10))

        firstProperty = false
    Next

    For Each navigation in EntityType.GetNavigations()
        WriteLine("")

        If Options.UseDataAnnotations Then
            For Each dataAnnotation in navigation.GetDataAnnotations(annotationCodeGenerator)

            Me.Write("        ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(code.Fragment(dataAnnotation)))
            Me.Write(""&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10))

            Next
        End If

        Dim targetType = navigation.TargetEntityType.Name
        If navigation.IsCollection Then

            Me.Write("        Public Overridable ReadOnly Property ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(code.Identifier(navigation.Name)))
            Me.Write(" As ICollection(Of ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(targetType))
            Me.Write(") = New List(Of ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(targetType))
            Me.Write(")()"&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10))

        Else

            Me.Write("        Public Overridable Property ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(code.Identifier(navigation.Name)))
            Me.Write(" As ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(targetType))
            Me.Write(""&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10))

        End If
    Next

    For Each skipNavigation in EntityType.GetSkipNavigations()
        WriteLine("")

        If Options.UseDataAnnotations Then
            For Each dataAnnotation in skipNavigation.GetDataAnnotations(annotationCodeGenerator)

            Me.Write("        ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(code.Fragment(dataAnnotation)))
            Me.Write(""&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10))

            Next
        End If

            Me.Write("        Public Overridable ReadOnly Property ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(code.Identifier(skipNavigation.Name)))
            Me.Write(" As ICollection(Of ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(skipNavigation.TargetEntityType.Name))
            Me.Write(") = New List(Of ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(skipNavigation.TargetEntityType.Name))
            Me.Write(")()"&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10))

    Next

            Me.Write("    End Class"&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10))

    If Not String.IsNullOrEmpty(FileNamespaceIdentifier) Then

            Me.Write("End Namespace"&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10))

    End If


    Dim previousOutput = GenerationEnvironment
    GenerationEnvironment = New StringBuilder()

    For Each ns in importsList.Where(Function(x) Not String.IsNullOrWhiteSpace(x)).
                               Distinct().
                               OrderBy(Function(x) x, New NamespaceComparer())

            Me.Write("Imports ")
            Me.Write(Me.ToStringHelper.ToStringWithCulture(ns))
            Me.Write(""&Global.Microsoft.VisualBasic.ChrW(13)&Global.Microsoft.VisualBasic.ChrW(10))

    Next

    WriteLine("")

    GenerationEnvironment.Append(previousOutput)

            Return Me.GenerationEnvironment.ToString
        End Function
        Private hostValue As Global.Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost
        '''<summary>
        '''The current host for the text templating engine
        '''</summary>
        Public Overridable Property Host() As Global.Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost
            Get
                Return Me.hostValue
            End Get
            Set
                Me.hostValue = value
            End Set
        End Property

Private _EntityTypeField As Global.Microsoft.EntityFrameworkCore.Metadata.IEntityType

'''<summary>
'''Access the EntityType parameter of the template.
'''</summary>
Private ReadOnly Property EntityType() As Global.Microsoft.EntityFrameworkCore.Metadata.IEntityType
    Get
        Return Me._EntityTypeField
    End Get
End Property

Private _OptionsField As Global.Microsoft.EntityFrameworkCore.Scaffolding.ModelCodeGenerationOptions

'''<summary>
'''Access the Options parameter of the template.
'''</summary>
Private ReadOnly Property Options() As Global.Microsoft.EntityFrameworkCore.Scaffolding.ModelCodeGenerationOptions
    Get
        Return Me._OptionsField
    End Get
End Property

Private _NamespaceHintField As String

'''<summary>
'''Access the NamespaceHint parameter of the template.
'''</summary>
Private ReadOnly Property NamespaceHint() As String
    Get
        Return Me._NamespaceHintField
    End Get
End Property


'''<summary>
'''Initialize the template
'''</summary>
Public Overridable Sub Initialize()
    If (Me.Errors.HasErrors = false) Then
Dim EntityTypeValueAcquired As Boolean = false
If Me.Session.ContainsKey("EntityType") Then
    Me._EntityTypeField = CType(Me.Session("EntityType"),Global.Microsoft.EntityFrameworkCore.Metadata.IEntityType)
    EntityTypeValueAcquired = true
End If
If (EntityTypeValueAcquired = false) Then
    Dim parameterValue As String = Me.Host.ResolveParameterValue("Property", "PropertyDirectiveProcessor", "EntityType")
    If (String.IsNullOrEmpty(parameterValue) = false) Then
        Dim tc As Global.System.ComponentModel.TypeConverter = Global.System.ComponentModel.TypeDescriptor.GetConverter(GetType(Global.Microsoft.EntityFrameworkCore.Metadata.IEntityType))
        If ((Not (tc) Is Nothing)  _
                    AndAlso tc.CanConvertFrom(GetType(String))) Then
            Me._EntityTypeField = CType(tc.ConvertFrom(parameterValue),Global.Microsoft.EntityFrameworkCore.Metadata.IEntityType)
            EntityTypeValueAcquired = true
        Else
            Me.Error("The type 'Microsoft.EntityFrameworkCore.Metadata.IEntityType' of the parameter 'E"& _ 
                    "ntityType' did not match the type of the data passed to the template.")
        End If
    End If
End If
If (EntityTypeValueAcquired = false) Then
    Dim data As Object = Global.System.Runtime.Remoting.Messaging.CallContext.LogicalGetData("EntityType")
    If (Not (data) Is Nothing) Then
        Me._EntityTypeField = CType(data,Global.Microsoft.EntityFrameworkCore.Metadata.IEntityType)
    End If
End If
Dim OptionsValueAcquired As Boolean = false
If Me.Session.ContainsKey("Options") Then
    Me._OptionsField = CType(Me.Session("Options"),Global.Microsoft.EntityFrameworkCore.Scaffolding.ModelCodeGenerationOptions)
    OptionsValueAcquired = true
End If
If (OptionsValueAcquired = false) Then
    Dim parameterValue As String = Me.Host.ResolveParameterValue("Property", "PropertyDirectiveProcessor", "Options")
    If (String.IsNullOrEmpty(parameterValue) = false) Then
        Dim tc As Global.System.ComponentModel.TypeConverter = Global.System.ComponentModel.TypeDescriptor.GetConverter(GetType(Global.Microsoft.EntityFrameworkCore.Scaffolding.ModelCodeGenerationOptions))
        If ((Not (tc) Is Nothing)  _
                    AndAlso tc.CanConvertFrom(GetType(String))) Then
            Me._OptionsField = CType(tc.ConvertFrom(parameterValue),Global.Microsoft.EntityFrameworkCore.Scaffolding.ModelCodeGenerationOptions)
            OptionsValueAcquired = true
        Else
            Me.Error("The type 'Microsoft.EntityFrameworkCore.Scaffolding.ModelCodeGenerationOptions' o"& _ 
                    "f the parameter 'Options' did not match the type of the data passed to the templ"& _ 
                    "ate.")
        End If
    End If
End If
If (OptionsValueAcquired = false) Then
    Dim data As Object = Global.System.Runtime.Remoting.Messaging.CallContext.LogicalGetData("Options")
    If (Not (data) Is Nothing) Then
        Me._OptionsField = CType(data,Global.Microsoft.EntityFrameworkCore.Scaffolding.ModelCodeGenerationOptions)
    End If
End If
Dim NamespaceHintValueAcquired As Boolean = false
If Me.Session.ContainsKey("NamespaceHint") Then
    Me._NamespaceHintField = CType(Me.Session("NamespaceHint"),String)
    NamespaceHintValueAcquired = true
End If
If (NamespaceHintValueAcquired = false) Then
    Dim parameterValue As String = Me.Host.ResolveParameterValue("Property", "PropertyDirectiveProcessor", "NamespaceHint")
    If (String.IsNullOrEmpty(parameterValue) = false) Then
        Dim tc As Global.System.ComponentModel.TypeConverter = Global.System.ComponentModel.TypeDescriptor.GetConverter(GetType(String))
        If ((Not (tc) Is Nothing)  _
                    AndAlso tc.CanConvertFrom(GetType(String))) Then
            Me._NamespaceHintField = CType(tc.ConvertFrom(parameterValue),String)
            NamespaceHintValueAcquired = true
        Else
            Me.Error("The type 'System.String' of the parameter 'NamespaceHint' did not match the type "& _ 
                    "of the data passed to the template.")
        End If
    End If
End If
If (NamespaceHintValueAcquired = false) Then
    Dim data As Object = Global.System.Runtime.Remoting.Messaging.CallContext.LogicalGetData("NamespaceHint")
    If (Not (data) Is Nothing) Then
        Me._NamespaceHintField = CType(data,String)
    End If
End If


    End If
End Sub


    End Class
    #Region "Base class"
    '''<summary>
    '''Base class for this transformation
    '''</summary>
    <Global.System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")>  _
    Public Class VisualBasicEntityTypeGeneratorBase
        #Region "Fields"
        Private generationEnvironmentField As Global.System.Text.StringBuilder
        Private errorsField As Global.System.CodeDom.Compiler.CompilerErrorCollection
        Private indentLengthsField As Global.System.Collections.Generic.List(Of Integer)
        Private currentIndentField As String = ""
        Private endsWithNewline As Boolean
        Private sessionField As Global.System.Collections.Generic.IDictionary(Of String, Object)
        #End Region
        #Region "Properties"
        '''<summary>
        '''The string builder that generation-time code is using to assemble generated output
        '''</summary>
        Protected Property GenerationEnvironment() As System.Text.StringBuilder
            Get
                If (Me.generationEnvironmentField Is Nothing) Then
                    Me.generationEnvironmentField = New Global.System.Text.StringBuilder()
                End If
                Return Me.generationEnvironmentField
            End Get
            Set
                Me.generationEnvironmentField = value
            End Set
        End Property
        '''<summary>
        '''The error collection for the generation process
        '''</summary>
        Public ReadOnly Property Errors() As System.CodeDom.Compiler.CompilerErrorCollection
            Get
                If (Me.errorsField Is Nothing) Then
                    Me.errorsField = New Global.System.CodeDom.Compiler.CompilerErrorCollection()
                End If
                Return Me.errorsField
            End Get
        End Property
        '''<summary>
        '''A list of the lengths of each indent that was added with PushIndent
        '''</summary>
        Private ReadOnly Property indentLengths() As System.Collections.Generic.List(Of Integer)
            Get
                If (Me.indentLengthsField Is Nothing) Then
                    Me.indentLengthsField = New Global.System.Collections.Generic.List(Of Integer)()
                End If
                Return Me.indentLengthsField
            End Get
        End Property
        '''<summary>
        '''Gets the current indent we use when adding lines to the output
        '''</summary>
        Public ReadOnly Property CurrentIndent() As String
            Get
                Return Me.currentIndentField
            End Get
        End Property
        '''<summary>
        '''Current transformation session
        '''</summary>
        Public Overridable Property Session() As Global.System.Collections.Generic.IDictionary(Of String, Object)
            Get
                Return Me.sessionField
            End Get
            Set
                Me.sessionField = value
            End Set
        End Property
        #End Region
        #Region "Transform-time helpers"
        '''<summary>
        '''Write text directly into the generated output
        '''</summary>
        Public Overloads Sub Write(ByVal textToAppend As String)
            If String.IsNullOrEmpty(textToAppend) Then
                Return
            End If
            'If we're starting off, or if the previous text ended with a newline,
            'we have to append the current indent first.
            If ((Me.GenerationEnvironment.Length = 0)  _
                        OrElse Me.endsWithNewline) Then
                Me.GenerationEnvironment.Append(Me.currentIndentField)
                Me.endsWithNewline = false
            End If
            'Check if the current text ends with a newline
            If textToAppend.EndsWith(Global.System.Environment.NewLine, Global.System.StringComparison.CurrentCulture) Then
                Me.endsWithNewline = true
            End If
            'This is an optimization. If the current indent is "", then we don't have to do any
            'of the more complex stuff further down.
            If (Me.currentIndentField.Length = 0) Then
                Me.GenerationEnvironment.Append(textToAppend)
                Return
            End If
            'Everywhere there is a newline in the text, add an indent after it
            textToAppend = textToAppend.Replace(Global.System.Environment.NewLine, (Global.System.Environment.NewLine + Me.currentIndentField))
            'If the text ends with a newline, then we should strip off the indent added at the very end
            'because the appropriate indent will be added when the next time Write() is called
            If Me.endsWithNewline Then
                Me.GenerationEnvironment.Append(textToAppend, 0, (textToAppend.Length - Me.currentIndentField.Length))
            Else
                Me.GenerationEnvironment.Append(textToAppend)
            End If
        End Sub
        '''<summary>
        '''Write text directly into the generated output
        '''</summary>
        Public Overloads Sub WriteLine(ByVal textToAppend As String)
            Me.Write(textToAppend)
            Me.GenerationEnvironment.AppendLine
            Me.endsWithNewline = true
        End Sub
        '''<summary>
        '''Write formatted text directly into the generated output
        '''</summary>
        Public Overloads Sub Write(ByVal format As String, <System.ParamArrayAttribute()> ByVal args() As Object)
            Me.Write(String.Format(Global.System.Globalization.CultureInfo.CurrentCulture, format, args))
        End Sub
        '''<summary>
        '''Write formatted text directly into the generated output
        '''</summary>
        Public Overloads Sub WriteLine(ByVal format As String, <System.ParamArrayAttribute()> ByVal args() As Object)
            Me.WriteLine(String.Format(Global.System.Globalization.CultureInfo.CurrentCulture, format, args))
        End Sub
        '''<summary>
        '''Raise an error
        '''</summary>
        Public Sub [Error](ByVal message As String)
            Dim [error] As System.CodeDom.Compiler.CompilerError = New Global.System.CodeDom.Compiler.CompilerError()
            [error].ErrorText = message
            Me.Errors.Add([error])
        End Sub
        '''<summary>
        '''Raise a warning
        '''</summary>
        Public Sub Warning(ByVal message As String)
            Dim [error] As System.CodeDom.Compiler.CompilerError = New Global.System.CodeDom.Compiler.CompilerError()
            [error].ErrorText = message
            [error].IsWarning = true
            Me.Errors.Add([error])
        End Sub
        '''<summary>
        '''Increase the indent
        '''</summary>
        Public Sub PushIndent(ByVal indent As String)
            If (indent = Nothing) Then
                Throw New Global.System.ArgumentNullException("indent")
            End If
            Me.currentIndentField = (Me.currentIndentField + indent)
            Me.indentLengths.Add(indent.Length)
        End Sub
        '''<summary>
        '''Remove the last indent that was added with PushIndent
        '''</summary>
        Public Function PopIndent() As String
            Dim returnValue As String = ""
            If (Me.indentLengths.Count > 0) Then
                Dim indentLength As Integer = Me.indentLengths((Me.indentLengths.Count - 1))
                Me.indentLengths.RemoveAt((Me.indentLengths.Count - 1))
                If (indentLength > 0) Then
                    returnValue = Me.currentIndentField.Substring((Me.currentIndentField.Length - indentLength))
                    Me.currentIndentField = Me.currentIndentField.Remove((Me.currentIndentField.Length - indentLength))
                End If
            End If
            Return returnValue
        End Function
        '''<summary>
        '''Remove any indentation
        '''</summary>
        Public Sub ClearIndent()
            Me.indentLengths.Clear
            Me.currentIndentField = ""
        End Sub
        #End Region
        #Region "ToString Helpers"
        '''<summary>
        '''Utility class to produce culture-oriented representation of an object as a string.
        '''</summary>
        Public Class ToStringInstanceHelper
            Private formatProviderField  As System.IFormatProvider = Global.System.Globalization.CultureInfo.InvariantCulture
            '''<summary>
            '''Gets or sets format provider to be used by ToStringWithCulture method.
            '''</summary>
            Public Property FormatProvider() As System.IFormatProvider
                Get
                    Return Me.formatProviderField 
                End Get
                Set
                    If (Not (value) Is Nothing) Then
                        Me.formatProviderField  = value
                    End If
                End Set
            End Property
            '''<summary>
            '''This is called from the compile/run appdomain to convert objects within an expression block to a string
            '''</summary>
            Public Function ToStringWithCulture(ByVal objectToConvert As Object) As String
                If (objectToConvert Is Nothing) Then
                    Throw New Global.System.ArgumentNullException("objectToConvert")
                End If
                Dim t As System.Type = objectToConvert.GetType
                Dim method As System.Reflection.MethodInfo = t.GetMethod("ToString", New System.Type() {GetType(System.IFormatProvider)})
                If (method Is Nothing) Then
                    Return objectToConvert.ToString
                Else
                    Return CType(method.Invoke(objectToConvert, New Object() {Me.formatProviderField }),String)
                End If
            End Function
        End Class
        Private toStringHelperField As ToStringInstanceHelper = New ToStringInstanceHelper()
        '''<summary>
        '''Helper to produce culture-oriented representation of an object as a string
        '''</summary>
        Public ReadOnly Property ToStringHelper() As ToStringInstanceHelper
            Get
                Return Me.toStringHelperField
            End Get
        End Property
        #End Region
    End Class
    #End Region
End Namespace
