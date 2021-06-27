Imports Microsoft.EntityFrameworkCore.Infrastructure

Namespace Design

    ''' <summary>
    '''     The parameter object for a <see cref="IVisualBasicRuntimeAnnotationCodeGenerator" />
    ''' </summary>
    Public Class VisualBasicRuntimeAnnotationCodeGeneratorParameters

        ''' <summary>
        '''     <para>
        '''         Creates the parameter object for a <see cref="IVisualBasicRuntimeAnnotationCodeGenerator" />.
        '''     </para>
        '''     <para>
        '''         Do Not call this constructor directly from either provider Or application code as it may change
        '''         as New parameters are added.
        '''     </para>
        '''     <para>
        '''         This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''         the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''         any release. You should only use it directly in your code with extreme caution And knowing that
        '''         doing so can result in application failures when updating to a New Entity Framework Core release.
        '''     </para>
        ''' </summary>
        <EntityFrameworkInternal>
        Public Sub New(targetName As String,
                       className As String,
                       mainBuilder As IndentedStringBuilder,
                       methodBuilder As IndentedStringBuilder,
                       namespaces As ISet(Of String),
                       scopeVariables As ISet(Of String),
                       Optional annotations As IDictionary(Of String, Object) = Nothing,
                       Optional isRuntime As Boolean = False)

            Me.TargetName = targetName
            Me.ClassName = className
            Me.MainBuilder = mainBuilder
            Me.MethodBuilder = methodBuilder
            Me.Namespaces = namespaces
            Me.ScopeVariables = scopeVariables
            Me.Annotations = annotations
            Me.IsRuntime = isRuntime
        End Sub

        ''' <summary>
        '''     The set of annotations from which to generate fluent API calls.
        ''' </summary>
        Public ReadOnly Property Annotations As IDictionary(Of String, Object)

        ''' <summary>
        '''     The name of the target variable.
        ''' </summary>
        Public ReadOnly Property TargetName As String

        ''' <summary>
        '''     The name of the current class.
        ''' </summary>
        Public ReadOnly Property ClassName As String

        ''' <summary>
        '''     The builder for the code building the metadata item.
        ''' </summary>
        Public ReadOnly Property MainBuilder As IndentedStringBuilder

        ''' <summary>
        '''     The builder that could be used to add members to the current class.
        ''' </summary>
        Public ReadOnly Property MethodBuilder As IndentedStringBuilder

        ''' <summary>
        '''     A collection of namespaces for <see langword="Imports"/> generation.
        ''' </summary>
        Public ReadOnly Property Namespaces As ISet(Of String)

        ''' <summary>
        '''     A collection of variable names in the current scope.
        ''' </summary>
        Public ReadOnly Property ScopeVariables As ISet(Of String)

        ''' <summary>
        '''     Indicates whether the given annotations are runtime annotations.
        ''' </summary>
        Public ReadOnly Property IsRuntime As Boolean

        <EntityFrameworkInternal>
        Public Function Cloner() As VisualBasicRuntimeAnnotationCodeGeneratorParametersCloner
            Return New VisualBasicRuntimeAnnotationCodeGeneratorParametersCloner(Me)
        End Function

        Public Class VisualBasicRuntimeAnnotationCodeGeneratorParametersCloner
            Private _Annotations As IDictionary(Of String, Object)
            Private _TargetName As String
            Private _ClassName As String
            Private _MainBuilder As IndentedStringBuilder
            Private _MethodBuilder As IndentedStringBuilder
            Private _Namespaces As ISet(Of String)
            Private _ScopeVariables As ISet(Of String)
            Private _IsRuntime As Boolean

            Friend Sub New(parametersToClone As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
                Me._Annotations = parametersToClone.Annotations
                Me._TargetName = parametersToClone.TargetName
                Me._ClassName = parametersToClone.ClassName
                Me._MainBuilder = parametersToClone.MainBuilder
                Me._MethodBuilder = parametersToClone.MethodBuilder
                Me._Namespaces = parametersToClone.Namespaces
                Me._ScopeVariables = parametersToClone.ScopeVariables
                Me._IsRuntime = parametersToClone.IsRuntime
            End Sub

            Public Function WithTargetName(targetName As String) As VisualBasicRuntimeAnnotationCodeGeneratorParametersCloner
                _TargetName = targetName
                Return Me
            End Function

            Public Function WithClassName(className As String) As VisualBasicRuntimeAnnotationCodeGeneratorParametersCloner
                _ClassName = className
                Return Me
            End Function

            Public Function WithMainBuilder(mainBuilder As IndentedStringBuilder) As VisualBasicRuntimeAnnotationCodeGeneratorParametersCloner
                _MainBuilder = mainBuilder
                Return Me
            End Function

            Public Function WithMethodBuilder(methodBuilder As IndentedStringBuilder) As VisualBasicRuntimeAnnotationCodeGeneratorParametersCloner
                _MethodBuilder = methodBuilder
                Return Me
            End Function

            Public Function WithNamespaces(namespaces As ISet(Of String)) As VisualBasicRuntimeAnnotationCodeGeneratorParametersCloner
                _Namespaces = namespaces
                Return Me
            End Function

            Public Function WithScopeVariables(scopeVariables As ISet(Of String)) As VisualBasicRuntimeAnnotationCodeGeneratorParametersCloner
                _ScopeVariables = scopeVariables
                Return Me
            End Function

            Public Function WithAnnotations(annotations As IDictionary(Of String, Object)) As VisualBasicRuntimeAnnotationCodeGeneratorParametersCloner
                _Annotations = annotations
                Return Me
            End Function

            Public Function WithIsRuntime(isRuntime As Boolean) As VisualBasicRuntimeAnnotationCodeGeneratorParametersCloner
                _IsRuntime = isRuntime
                Return Me
            End Function

            Public Function Clone() As VisualBasicRuntimeAnnotationCodeGeneratorParameters
                Return New VisualBasicRuntimeAnnotationCodeGeneratorParameters(
                    targetName:=_TargetName,
                    className:=_ClassName,
                    mainBuilder:=_MainBuilder,
                    methodBuilder:=_MethodBuilder,
                    namespaces:=_Namespaces,
                    scopeVariables:=_ScopeVariables,
                    annotations:=_Annotations,
                    isRuntime:=_IsRuntime
                )
            End Function
        End Class
    End Class

End Namespace
