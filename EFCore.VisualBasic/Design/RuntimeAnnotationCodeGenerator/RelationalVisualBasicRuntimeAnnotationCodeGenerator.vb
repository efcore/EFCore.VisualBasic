Imports System.Reflection
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.ChangeTracking
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Migrations
Imports Microsoft.EntityFrameworkCore.Storage

Namespace Design.AnnotationCodeGeneratorProvider

    ''' <summary>
    '''     Base class to be used by relational database providers when implementing an <see cref="IVisualBasicRuntimeAnnotationCodeGenerator" />
    ''' </summary>
    Public Class RelationalVisualBasicRuntimeAnnotationCodeGenerator
        Inherits VisualBasicRuntimeAnnotationCodeGenerator

        ''' <summary>
        '''     Initializes a New instance of this class.
        ''' </summary>
        ''' <param name="vbHelper">The Visual Basic helper.</param>
        Public Sub New(vbHelper As IVisualBasicHelper)
            MyBase.New(vbHelper)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate(model As IModel, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim annotations = parameters.Annotations
            If parameters.IsRuntime Then
                annotations.Remove(RelationalAnnotationNames.ModelDependencies)

                Dim relationalModel As RelationalModel = Nothing
                If annotations.TryGetAndRemove(RelationalAnnotationNames.RelationalModel,
                                               relationalModel) Then

                    GenerateSimpleAnnotation(RelationalAnnotationNames.RelationalModel, "CreateRelationalModel()", parameters)

                    Dim MethodBuilder As New IndentedStringBuilder()
                    Create(
                        relationalModel,
                            parameters.Cloner.
                                        WithMainBuilder(parameters.MethodBuilder).
                                        WithMethodBuilder(MethodBuilder).
                                        WithScopeVariables(New HashSet(Of String)).
                                        Clone)

                    Dim methods = MethodBuilder.ToString()
                    If Not String.IsNullOrEmpty(methods) Then
                        parameters.
                            MethodBuilder.
                            AppendLine().
                            AppendLines(methods)
                    End If
                End If
            Else
                annotations.Remove(RelationalAnnotationNames.Collation)

                Dim functions As SortedDictionary(Of String, IDbFunction) = Nothing

                If TryGetAndRemove(annotations, RelationalAnnotationNames.DbFunctions, functions) Then
                    parameters.Namespaces.Add(GetType(SortedDictionary(Of,)).Namespace)
                    parameters.Namespaces.Add(GetType(BindingFlags).Namespace)
                    Dim functionsVariable = VBCode.Identifier("functions", parameters.ScopeVariables, capitalize:=False)
                    parameters.
                            MainBuilder.
                            Append("Dim ").
                            Append(functionsVariable).
                            AppendLine(" As New SortedDictionary(Of String, IDbFunction)()")

                    For Each func In functions.Values
                        Create(func, functionsVariable, parameters)
                    Next

                    GenerateSimpleAnnotation(RelationalAnnotationNames.DbFunctions, functionsVariable, parameters)
                End If

                Dim sequences As SortedDictionary(Of (String, String), ISequence) = Nothing

                If TryGetAndRemove(annotations, RelationalAnnotationNames.Sequences, sequences) Then
                    parameters.Namespaces.Add(GetType(SortedDictionary(Of,)).Namespace)
                    Dim sequencesVariable = VBCode.Identifier("sequences", parameters.ScopeVariables, capitalize:=False)
                    Dim mainBuilder = parameters.MainBuilder

                    mainBuilder.
                        Append("Dim ").
                        Append(sequencesVariable).
                        AppendLine(" As New SortedDictionary(Of (String, String), ISequence)()")

                    For Each sequencePair In sequences
                        Create(sequencePair.Value, sequencesVariable, parameters)
                    Next

                    GenerateSimpleAnnotation(RelationalAnnotationNames.Sequences, sequencesVariable, parameters)
                End If
            End If

            MyBase.Generate(model, parameters)
        End Sub

        Private Overloads Sub Create(model As IRelationalModel,
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.AppendLine("Private Function CreateRelationalModel() As IRelationalModel")

            Using mainBuilder.Indent()
                parameters.Namespaces.Add(GetType(RelationalModel).Namespace)
                parameters.Namespaces.Add(GetType(Microsoft.EntityFrameworkCore.RelationalModelExtensions).Namespace)
                Dim relationalModelVariable = VBCode.Identifier("relationalModel", parameters.ScopeVariables, capitalize:=False)

                mainBuilder.AppendLine($"Dim {relationalModelVariable} As New RelationalModel({parameters.TargetName})")

                Dim metadataVariables = New Dictionary(Of IAnnotatable, String)()

                Dim relationalModelParameters = parameters.Cloner.
                                                           WithTargetName(relationalModelVariable).
                                                           Clone

                AddNamespace(GetType(List(Of TableMapping)), parameters.Namespaces)

                For Each entityType In model.Model.GetEntityTypes()
                    CreateMappings(entityType, declaringVariable:=Nothing, metadataVariables, relationalModelParameters)
                Next

                For Each table In model.Tables
                    For Each foreignKey In table.ForeignKeyConstraints
                        Create(foreignKey, metadataVariables, parameters.Cloner.WithTargetName(metadataVariables(table)).Clone())
                    Next
                Next

                For Each dbFunction In model.Model.GetDbFunctions()
                    If Not dbFunction.IsScalar Then Continue For
                    GetOrCreate(dbFunction.StoreFunction, metadataVariables, relationalModelParameters)
                Next

                CreateAnnotations(
                    model,
                    AddressOf Generate,
                    relationalModelParameters)

                mainBuilder.
                    AppendLine($"Return {relationalModelVariable}.MakeReadOnly()")
            End Using

            mainBuilder.
                AppendLine("End Function")
        End Sub

        Private Sub CreateMappings(typeBase As ITypeBase,
                                   declaringVariable As String,
                                   metadataVariables As Dictionary(Of IAnnotatable, String),
                                   parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim mainBuilder = parameters.MainBuilder

            Dim typeBaseVariable = VBCode.Identifier(typeBase.ShortName(), parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(typeBase, typeBaseVariable)
            If TypeOf typeBase Is IComplexType Then
                Dim complexType = DirectCast(typeBase, IComplexType)

                mainBuilder.
                    AppendLine().
                    Append($"Dim {typeBaseVariable} = ").
                    AppendLine($"{declaringVariable}.FindComplexProperty({VBCode.Literal(complexType.ComplexProperty.Name)}).ComplexType")
            Else
                mainBuilder.
                    AppendLine().
                    AppendLine($"Dim {typeBaseVariable} = FindEntityType({VBCode.Literal(typeBase.Name)})")
            End If

            ' All the mappings below are added in a way that preserves the order
            For Each mapping In typeBase.GetDefaultMappings()
                Dim tableMappingsVariable = VBCode.Identifier("defaultTableMappings", parameters.ScopeVariables, capitalize:=False)
                mainBuilder.
                    AppendLine().
                    AppendLine($"Dim {tableMappingsVariable} As New List(Of TableMappingBase(Of ColumnMappingBase))()").
                    Append($"{typeBaseVariable}.SetRuntimeAnnotation(").
                    AppendLine($"{VBCode.Literal(RelationalAnnotationNames.DefaultMappings)}, {tableMappingsVariable})")
                Create(mapping, tableMappingsVariable, metadataVariables, parameters)
            Next

            If typeBase.GetTableMappings().Any() Then
                Dim tableMappingsVariable = VBCode.Identifier("tableMappings", parameters.ScopeVariables, capitalize:=False)
                mainBuilder.
                    AppendLine().
                    AppendLine($"Dim {tableMappingsVariable} As New List(Of TableMapping)()").
                    Append($"{typeBaseVariable}.SetRuntimeAnnotation(").
                    AppendLine($"{VBCode.Literal(RelationalAnnotationNames.TableMappings)}, {tableMappingsVariable})")
                For Each mapping In typeBase.GetTableMappings()
                    Create(mapping, tableMappingsVariable, metadataVariables, parameters)
                Next
            End If

            If typeBase.GetViewMappings().Any() Then
                Dim viewMappingsVariable = VBCode.Identifier("viewMappings", parameters.ScopeVariables, capitalize:=False)
                mainBuilder.
                    AppendLine().
                    AppendLine($"Dim {viewMappingsVariable} As New List(Of ViewMapping)()").
                    Append($"{typeBaseVariable}.SetRuntimeAnnotation(").
                    AppendLine($"{VBCode.Literal(RelationalAnnotationNames.ViewMappings)}, {viewMappingsVariable})")
                For Each mapping In typeBase.GetViewMappings()
                    Create(mapping, viewMappingsVariable, metadataVariables, parameters)
                Next
            End If

            If typeBase.GetSqlQueryMappings().Any() Then
                Dim sqlQueryMappingsVariable = VBCode.Identifier("sqlQueryMappings", parameters.ScopeVariables, capitalize:=False)
                mainBuilder.
                    AppendLine().
                    AppendLine($"Dim {sqlQueryMappingsVariable} As New List(Of SqlQueryMapping)()").
                    Append($"{typeBaseVariable}.SetRuntimeAnnotation(").
                    AppendLine($"{VBCode.Literal(RelationalAnnotationNames.SqlQueryMappings)}, {sqlQueryMappingsVariable})")
                For Each mapping In typeBase.GetSqlQueryMappings()
                    Create(mapping, sqlQueryMappingsVariable, metadataVariables, parameters)
                Next
            End If

            If typeBase.GetFunctionMappings().Any() Then
                Dim functionMappingsVariable = VBCode.Identifier("functionMappings", parameters.ScopeVariables, capitalize:=False)
                mainBuilder.
                    AppendLine().
                    AppendLine($"Dim {functionMappingsVariable} As New List(Of FunctionMapping)()").
                    Append($"{typeBaseVariable}.SetRuntimeAnnotation(").
                    AppendLine($"{VBCode.Literal(RelationalAnnotationNames.FunctionMappings)}, {functionMappingsVariable})")
                For Each mapping In typeBase.GetFunctionMappings()
                    Create(mapping, functionMappingsVariable, metadataVariables, parameters)
                Next
            End If

            If typeBase.GetDeleteStoredProcedureMappings().Any() Then
                Dim deleteSprocMappingsVariable = VBCode.Identifier("deleteSprocMappings", parameters.ScopeVariables, capitalize:=False)
                mainBuilder.
                    AppendLine().
                    AppendLine($"Dim {deleteSprocMappingsVariable} As New List(Of StoredProcedureMapping)()").
                    Append($"{typeBaseVariable}.SetRuntimeAnnotation(").
                    AppendLine(
                        $"{VBCode.Literal(RelationalAnnotationNames.DeleteStoredProcedureMappings)}, {deleteSprocMappingsVariable})")
                For Each mapping In typeBase.GetDeleteStoredProcedureMappings()
                    Create(
                        mapping,
                        deleteSprocMappingsVariable,
                        StoreObjectType.DeleteStoredProcedure,
                        metadataVariables,
                        parameters)
                Next
            End If

            If typeBase.GetInsertStoredProcedureMappings().Any() Then
                Dim insertSprocMappingsVariable = VBCode.Identifier("insertSprocMappings", parameters.ScopeVariables, capitalize:=False)
                mainBuilder.
                    AppendLine().
                    AppendLine($"Dim {insertSprocMappingsVariable} As New List(Of StoredProcedureMapping)()").
                    Append($"{typeBaseVariable}.SetRuntimeAnnotation(").
                    AppendLine(
                        $"{VBCode.Literal(RelationalAnnotationNames.InsertStoredProcedureMappings)}, {insertSprocMappingsVariable})")
                For Each mapping In typeBase.GetInsertStoredProcedureMappings()
                    Create(
                        mapping,
                        insertSprocMappingsVariable,
                        StoreObjectType.InsertStoredProcedure,
                        metadataVariables,
                        parameters)
                Next
            End If

            If typeBase.GetUpdateStoredProcedureMappings().Any() Then
                Dim updateSprocMappingsVariable = VBCode.Identifier("updateSprocMappings", parameters.ScopeVariables, capitalize:=False)
                mainBuilder.
                    AppendLine().
                    AppendLine($"Dim {updateSprocMappingsVariable} As New List(Of StoredProcedureMapping)()").
                    Append($"{typeBaseVariable}.SetRuntimeAnnotation(").
                    AppendLine(
                        $"{VBCode.Literal(RelationalAnnotationNames.UpdateStoredProcedureMappings)}, {updateSprocMappingsVariable})")
                For Each mapping In typeBase.GetUpdateStoredProcedureMappings()
                    Create(
                        mapping,
                        updateSprocMappingsVariable,
                        StoreObjectType.UpdateStoredProcedure,
                        metadataVariables,
                        parameters)
                Next
            End If

            For Each complexProperty In typeBase.GetDeclaredComplexProperties()
                CreateMappings(complexProperty.ComplexType, typeBaseVariable, metadataVariables, parameters)
            Next
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="model">The relational model to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(model As IRelationalModel, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Function GetOrCreate(table As ITableBase,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) As String

            Dim tableVariable As String = Nothing
            If metadataVariables.TryGetValue(table, tableVariable) Then
                Return tableVariable
            End If

            Dim code = VBCode
            tableVariable = code.Identifier(table.Name & "TableBase", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(table, tableVariable)
            Dim mainBuilder = parameters.MainBuilder

            mainBuilder.
                Append($"Dim {tableVariable} As New TableBase({code.Literal(table.Name)}, {code.Literal(table.Schema)}, ").
                AppendLine($"{parameters.TargetName})")

            Dim tableParameters = parameters.Cloner.
                                             WithTargetName(tableVariable).
                                             Clone

            For Each column In table.Columns
                Create(column, metadataVariables, tableParameters)
            Next

            CreateAnnotations(
                table,
                AddressOf Generate,
                tableParameters)

            mainBuilder.
                AppendLine($"{parameters.TargetName}.DefaultTables.Add({code.Literal(table.Name)}, {tableVariable})")

            Return tableVariable
        End Function

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="table">The table to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(table As ITableBase, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Function GetOrCreate(table As ITable,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) As String

            Dim tableVariable As String = Nothing
            If metadataVariables.TryGetValue(table, tableVariable) Then
                Return tableVariable
            End If

            Dim code = VBCode

            tableVariable = code.Identifier(table.Name & "Table", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(table, tableVariable)

            Dim mainBuilder = parameters.MainBuilder

            mainBuilder.
                Append($"Dim {tableVariable} As New Table({code.Literal(table.Name)}, {code.Literal(table.Schema)}, ").
                AppendLine($"{parameters.TargetName})")

            Dim tableParameters = parameters.Cloner.
                                             WithTargetName(tableVariable).
                                             Clone

            For Each column In table.Columns
                Create(column, metadataVariables, tableParameters)
            Next

            For Each uniqueConstraint In table.UniqueConstraints
                Create(uniqueConstraint, uniqueConstraint.Columns.Select(Function(c) metadataVariables(c)), tableParameters)
            Next

            For Each index In table.Indexes
                Create(index, index.Columns.Select(Function(c) metadataVariables(c)), tableParameters)
            Next

            For Each trigger In table.Triggers
                Dim entityTypeVariable = metadataVariables(trigger.EntityType)

                Dim triggerName = trigger.GetDatabaseName(StoreObjectIdentifier.Table(table.Name, table.Schema))

                mainBuilder.
                    Append($"{tableVariable}.Triggers.Add({code.Literal(triggerName)}, ").
                    AppendLine($"{entityTypeVariable}.FindDeclaredTrigger({code.Literal(trigger.ModelName)}))")
            Next

            CreateAnnotations(
                table,
                AddressOf Generate,
                tableParameters)

            mainBuilder.
                Append($"{parameters.TargetName}.Tables.Add((").
                AppendLine($"{code.Literal(table.Name)}, {code.Literal(table.Schema)}), {tableVariable})")

            Return tableVariable
        End Function

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="table">The table to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(table As ITable, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Function GetOrCreate(view As IView,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) As String

            Dim viewVariable As String = Nothing
            If metadataVariables.TryGetValue(view, viewVariable) Then
                Return viewVariable
            End If

            Dim code = VBCode

            viewVariable = code.Identifier(view.Name & "View", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(view, viewVariable)
            Dim mainBuilder = parameters.MainBuilder

            mainBuilder.
                Append($"Dim {viewVariable} As New View({code.Literal(view.Name)}, {code.Literal(view.Schema)}, ").
                AppendLine($"{parameters.TargetName})")

            Dim viewParameters = parameters.Cloner.
                                            WithTargetName(viewVariable).
                                            Clone

            For Each column In view.Columns
                Create(column, metadataVariables, viewParameters)
            Next

            CreateAnnotations(
                view,
                AddressOf Generate,
                viewParameters)

            mainBuilder.
                Append($"{parameters.TargetName}.Views.Add((").
                AppendLine($"{code.Literal(view.Name)}, {code.Literal(view.Schema)}), {viewVariable})")

            Return viewVariable
        End Function

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="view">The view to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(view As IView, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Function GetOrCreate(sqlQuery As ISqlQuery,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) As String

            Dim sqlQueryVariable As String = Nothing
            If metadataVariables.TryGetValue(sqlQuery, sqlQueryVariable) Then
                Return sqlQueryVariable
            End If

            sqlQueryVariable = VBCode.Identifier(sqlQuery.Name & "SqlQuery", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(sqlQuery, sqlQueryVariable)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.
                Append($"Dim {sqlQueryVariable} As New SqlQuery({VBCode.Literal(sqlQuery.Name)}, {parameters.TargetName}, ").
                AppendLine($"{VBCode.Literal(sqlQuery.Sql)})")

            Dim sqlQueryParameters = parameters.Cloner.
                                                WithTargetName(sqlQueryVariable).
                                                Clone

            For Each column In sqlQuery.Columns
                Create(column, metadataVariables, sqlQueryParameters)
            Next

            CreateAnnotations(
                sqlQuery,
                AddressOf Generate,
                sqlQueryParameters)

            mainBuilder.
                Append($"{parameters.TargetName}.Queries.Add(").
                AppendLine($"{VBCode.Literal(sqlQuery.Name)}, {sqlQueryVariable})")

            Return sqlQueryVariable
        End Function

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="sqlQuery">The SQL query to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(sqlQuery As ISqlQuery, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Function GetOrCreate([function] As IStoreFunction,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) As String

            Dim functionVariable As String = Nothing
            If metadataVariables.TryGetValue([function], functionVariable) Then
                Return functionVariable
            End If

            Dim mainDbFunctionVariable = GetOrCreate([function].DbFunctions.First(), metadataVariables, parameters)
            functionVariable = VBCode.Identifier([function].Name & "Function", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add([function], functionVariable)

            Dim mainBuilder = parameters.MainBuilder

            mainBuilder.
                AppendLine($"Dim {functionVariable} As New StoreFunction({mainDbFunctionVariable}, {parameters.TargetName})")

            Dim functionParameters = parameters.Cloner.
                                                WithTargetName(functionVariable).
                                                Clone

            For Each dbFunction In [function].DbFunctions.Skip(1)

                Dim dbFunctionVariable = GetOrCreate(dbFunction, metadataVariables, parameters)

                mainBuilder.
                    AppendLine($"{dbFunctionVariable}.StoreFunction = {functionVariable}").
                    AppendLine($"{functionVariable}.DbFunctions.Add({VBCode.Literal(dbFunction.ModelName)}, {dbFunctionVariable})")
            Next

            For Each parameter In [function].Parameters

                Dim parameterVariable = VBCode.Identifier(parameter.Name & "FunctionParameter", parameters.ScopeVariables, capitalize:=False)
                metadataVariables.Add(parameter, parameterVariable)
                mainBuilder.AppendLine($"Dim {parameterVariable} = {functionVariable}.FindParameter({VBCode.Literal(parameter.Name)})")

                CreateAnnotations(
                    parameter,
                    AddressOf Generate,
                    parameters.Cloner.
                                WithTargetName(parameterVariable).
                                Clone)
            Next

            For Each column In [function].Columns
                Create(column, metadataVariables, functionParameters)
            Next

            CreateAnnotations(
                [function],
                AddressOf Generate,
                functionParameters)

            mainBuilder.
                AppendLine($"{parameters.TargetName}.Functions.Add(").
                IncrementIndent().
                Append($"({VBCode.Literal([function].Name)}, {VBCode.Literal([function].Schema)}, ").
                AppendLine($"{VBCode.Literal([function].DbFunctions.First().Parameters.Select(Function(p) p.StoreType).ToArray())}),").
                AppendLine($"{functionVariable})").
                DecrementIndent()

            Return functionVariable
        End Function

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="function">The function to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate([function] As IStoreFunction, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Function GetOrCreate([function] As IDbFunction,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) As String

            Dim functionVariable As String = Nothing
            If metadataVariables.TryGetValue([function], functionVariable) Then
                Return functionVariable
            End If

            Dim code = VBCode
            functionVariable = code.Identifier([function].Name, parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add([function], functionVariable)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.AppendLine($"Dim {functionVariable} = DirectCast(Me.FindDbFunction({code.Literal([function].ModelName)}), IRuntimeDbFunction)")

            Return functionVariable
        End Function

        Private Function GetOrCreate(storeStoredProcedure As IStoreStoredProcedure,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) As String

            Dim storedProcedureVariable As String = Nothing
            If metadataVariables.TryGetValue(storeStoredProcedure, storedProcedureVariable) Then
                Return storedProcedureVariable
            End If

            Dim code = VBCode
            storedProcedureVariable = code.Identifier(storeStoredProcedure.Name & "StoreSproc", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(storeStoredProcedure, storedProcedureVariable)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.
                Append($"Dim {storedProcedureVariable} As New StoreStoredProcedure(").
                Append($"{code.Literal(storeStoredProcedure.Name)}, {code.Literal(storeStoredProcedure.Schema)}").
                AppendLine($", {parameters.TargetName})")

            Dim sprocParameters = parameters.Cloner.
                                             WithTargetName(storedProcedureVariable).
                                             Clone

            Dim returnValue = storeStoredProcedure.ReturnValue

            If returnValue IsNot Nothing Then
                mainBuilder.
                    Append($"{storedProcedureVariable}.ReturnValue = New StoreStoredProcedureReturnValue(").
                    AppendLine($""""", {code.Literal(returnValue.StoreType)}, {storedProcedureVariable})")
            End If

            For Each parameter In storeStoredProcedure.Parameters
                Create(parameter, metadataVariables, sprocParameters)
            Next

            For Each column In storeStoredProcedure.ResultColumns
                Create(column, metadataVariables, sprocParameters)
            Next

            For Each storedProcedure In storeStoredProcedure.StoredProcedures
                mainBuilder.
                    Append($"{storedProcedureVariable}.AddStoredProcedure(").
                    Append(CreateFindSnippet(storedProcedure, metadataVariables)).
                    AppendLine(")"c)
            Next

            CreateAnnotations(
                storeStoredProcedure,
                AddressOf Generate,
                sprocParameters)

            mainBuilder.
            Append($"{parameters.TargetName}.StoredProcedures.Add(").
            AppendLine(
                $"({code.Literal(storeStoredProcedure.Name)}, {code.Literal(storeStoredProcedure.Schema)}), {storedProcedureVariable})")

            Return storedProcedureVariable
        End Function

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="storedProcedure">The stored procedure to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(storedProcedure As IStoreStoredProcedure, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Function CreateFindSnippet(storedProcedure As IStoredProcedure,
                                           metadataVariables As Dictionary(Of IAnnotatable, String)) As String

            Dim storedProcedureVariable As String = Nothing
            If metadataVariables.TryGetValue(storedProcedure, storedProcedureVariable) Then
                Return storedProcedureVariable
            End If

            Dim entityTypeVariable = metadataVariables(storedProcedure.EntityType)

            Dim StoreObjectType = storedProcedure.GetStoreIdentifier().StoreObjectType
            Dim methodName As String
            Select Case StoreObjectType
                Case StoreObjectType.InsertStoredProcedure : methodName = "GetInsertStoredProcedure"
                Case StoreObjectType.DeleteStoredProcedure : methodName = "GetDeleteStoredProcedure"
                Case StoreObjectType.UpdateStoredProcedure : methodName = "GetUpdateStoredProcedure"
                Case Else : Throw New Exception("Unexpected stored procedure type: " & StoreObjectType)
            End Select

            Return $"DirectCast({entityTypeVariable}.{methodName}(), IRuntimeStoredProcedure)"
        End Function

        Private Overloads Sub Create(column As IColumnBase,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode

            Dim columnVariable = code.Identifier(column.Name & "ColumnBase", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(column, columnVariable)
            Dim mainBuilder = parameters.MainBuilder
            Dim columnType = If(TypeOf column Is JsonColumnBase, "JsonColumnBase", "ColumnBase(Of ColumnMappingBase)")

            mainBuilder.
                Append($"Dim {columnVariable} As New {columnType}(").
                Append($"{code.Literal(column.Name)}, {code.Literal(column.StoreType)}, {parameters.TargetName})")

            GenerateIsNullableInitializer(column.IsNullable, mainBuilder, code).
                AppendLine().
                AppendLine($"{parameters.TargetName}.Columns.Add({code.Literal(column.Name)}, {columnVariable})")

            CreateAnnotations(
                column,
                AddressOf Generate,
                parameters.Cloner.
                           WithTargetName(columnVariable).
                           Clone)
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="column">The column to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(column As IColumnBase, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(column As IColumn,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode

            Dim columnVariable = code.Identifier(column.Name & "Column", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(column, columnVariable)

            Dim mainBuilder = parameters.MainBuilder
            Dim columnType = If(TypeOf column Is JsonColumn, "JsonColumn", "Column")

            mainBuilder.
                Append($"Dim {columnVariable} As New {columnType}(").
                Append($"{code.Literal(column.Name)}, {code.Literal(column.StoreType)}, {parameters.TargetName})")

            GenerateIsNullableInitializer(column.IsNullable, mainBuilder, code).
                 AppendLine().
                 AppendLine($"{parameters.TargetName}.Columns.Add({code.Literal(column.Name)}, {columnVariable})")

            CreateAnnotations(
                column,
                AddressOf Generate,
                parameters.Cloner.
                           WithTargetName(columnVariable).
                           Clone)
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="column">The column to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(column As IColumn, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(column As IViewColumn,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim columnVariable = code.Identifier(column.Name & "ViewColumn", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(column, columnVariable)
            Dim mainBuilder = parameters.MainBuilder

            Dim columnType = If(TypeOf column Is JsonViewColumn, "JsonViewColumn", "ViewColumn")
            mainBuilder.
                Append($"Dim {columnVariable} As New {columnType}(").
                Append($"{code.Literal(column.Name)}, {code.Literal(column.StoreType)}, {parameters.TargetName})")

            GenerateIsNullableInitializer(column.IsNullable, mainBuilder, code).
                AppendLine().
                AppendLine($"{parameters.TargetName}.Columns.Add({code.Literal(column.Name)}, {columnVariable})")

            CreateAnnotations(
                column,
                AddressOf Generate,
                parameters.Cloner.
                           WithTargetName(columnVariable).
                           Clone)
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="column">The column to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(column As IViewColumn, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(column As ISqlQueryColumn,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode

            Dim columnVariable = code.Identifier(column.Name & "SqlQueryColumn", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(column, columnVariable)

            Dim mainBuilder = parameters.MainBuilder

            mainBuilder.
            Append($"Dim {columnVariable} As New SqlQueryColumn(").
            Append($"{code.Literal(column.Name)}, {code.Literal(column.StoreType)}, {parameters.TargetName})")

            GenerateIsNullableInitializer(column.IsNullable, mainBuilder, code).
                AppendLine().
                AppendLine($"{parameters.TargetName}.Columns.Add({code.Literal(column.Name)}, {columnVariable})")

            CreateAnnotations(
                column,
                AddressOf Generate,
                parameters.Cloner.
                           WithTargetName(columnVariable).
                           Clone)
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="column">The column to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(column As ISqlQueryColumn, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(column As IFunctionColumn,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim columnVariable = code.Identifier(column.Name & "FunctionColumn", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(column, columnVariable)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.
                Append($"Dim {columnVariable} As New FunctionColumn(").
                Append($"{code.Literal(column.Name)}, {code.Literal(column.StoreType)}, {parameters.TargetName})")

            GenerateIsNullableInitializer(column.IsNullable, mainBuilder, code).
                AppendLine().
                AppendLine($"{parameters.TargetName}.Columns.Add({code.Literal(column.Name)}, {columnVariable})")

            CreateAnnotations(
                column,
                AddressOf Generate,
                parameters.Cloner.
                           WithTargetName(columnVariable).
                           Clone)
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="column">The column to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(column As IFunctionColumn, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="parameter">The parameter to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(parameter As IStoreFunctionParameter, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(column As IStoreStoredProcedureResultColumn,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim columnVariable = code.Identifier(column.Name & "FunctionColumn", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(column, columnVariable)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.
                Append($"Dim {columnVariable} As New StoreStoredProcedureResultColumn({code.Literal(column.Name)}, ").
                Append($"{code.Literal(column.StoreType)}, {code.Literal(column.Position)}, {parameters.TargetName})")

            GenerateIsNullableInitializer(column.IsNullable, mainBuilder, code).
                AppendLine().
                AppendLine($"{parameters.TargetName}.AddResultColumn({columnVariable})")

            CreateAnnotations(
                column,
                AddressOf Generate,
                parameters.Cloner.
                           WithTargetName(columnVariable).
                           Clone)
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="column">The column to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(column As IStoreStoredProcedureResultColumn, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(parameter As IStoreStoredProcedureParameter,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim parameterVariable = code.Identifier(parameter.Name & "Parameter", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(parameter, parameterVariable)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.
                Append($"Dim {parameterVariable} As New StoreStoredProcedureParameter({code.Literal(parameter.Name)}, ").
                Append($"{code.Literal(parameter.StoreType)}, {code.Literal(parameter.Position)}, {parameters.TargetName}").
                Append($", {code.Literal(parameter.Direction, fullName:=True)})")

            GenerateIsNullableInitializer(parameter.IsNullable, mainBuilder, code).
                AppendLine().
                AppendLine($"{parameters.TargetName}.AddParameter({parameterVariable})")

            CreateAnnotations(
                parameter,
                AddressOf Generate,
                parameters.Cloner.
                           WithTargetName(parameterVariable).
                           Clone)
        End Sub

        Private Shared Function GenerateIsNullableInitializer(isNullable As Boolean,
                                                              mainBuilder As IndentedStringBuilder,
                                                              code As IVisualBasicHelper) As IndentedStringBuilder

            If isNullable Then
                mainBuilder.
                    AppendLine(" With {").
                    IncrementIndent().
                    AppendLine($".IsNullable = {code.Literal(isNullable)}").
                    DecrementIndent().
                    Append("}"c)
            End If

            Return mainBuilder
        End Function

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="parameter">The parameter to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(parameter As IStoreStoredProcedureParameter, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(uniqueConstraint As IUniqueConstraint,
                                     columns As IEnumerable(Of String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim uniqueConstraintVariable = code.Identifier(uniqueConstraint.Name, parameters.ScopeVariables, capitalize:=False)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.
                Append("Dim ").Append(uniqueConstraintVariable).
                Append(" As New ").
                Append("UniqueConstraint").
                Append("("c).
                Append(code.Literal(uniqueConstraint.Name)).
                Append(", ").
                Append(parameters.TargetName).
                Append(", ").
                Append("{"c).
                AppendJoin(columns).
                AppendLine("})")

            If uniqueConstraint.GetIsPrimaryKey() Then
                mainBuilder.
                    Append(parameters.TargetName).
                    Append(".PrimaryKey = ").
                    AppendLine(uniqueConstraintVariable)
            End If

            CreateAnnotations(
                uniqueConstraint,
                AddressOf Generate,
                parameters.Cloner.
                           WithTargetName(uniqueConstraintVariable).
                           Clone)

            For Each mappedForeignKey In uniqueConstraint.MappedKeys
                Dim keyVariable = code.Identifier(uniqueConstraintVariable & "Uc", parameters.ScopeVariables, capitalize:=False)

                mainBuilder.
                    AppendLine($"Dim {keyVariable} = RelationalModel.GetKey(Me,").
                    IncrementIndent().
                    AppendLine($"{code.Literal(mappedForeignKey.DeclaringEntityType.Name)},").
                    AppendLine($"{code.Literal(mappedForeignKey.Properties.Select(Function(p) p.Name).ToArray())})").
                    DecrementIndent()

                mainBuilder.AppendLine($"{uniqueConstraintVariable}.MappedKeys.Add({keyVariable})")
                mainBuilder.AppendLine($"RelationalModel.GetOrCreateUniqueConstraints({keyVariable}).Add({uniqueConstraintVariable})")
            Next

            mainBuilder.
                Append($"{parameters.TargetName}.UniqueConstraints.Add({code.Literal(uniqueConstraint.Name)}, ").
                AppendLine($"{uniqueConstraintVariable})")
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="uniqueConstraint">The unique constraint to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(uniqueConstraint As IUniqueConstraint, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(index As ITableIndex,
                                     columns As IEnumerable(Of String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim indexVariable = code.Identifier(index.Name, parameters.ScopeVariables, capitalize:=False)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.
                Append("Dim ").Append(indexVariable).Append(" As New ").Append("TableIndex").AppendLine("("c).
                Append(code.Literal(index.Name)).Append(", ").
                Append(parameters.TargetName).Append(", ").
                Append("{"c).AppendJoin(columns).Append("}, ").
                Append(code.Literal(index.IsUnique)).AppendLine(")"c)

            CreateAnnotations(
                index,
                AddressOf Generate,
                parameters.Cloner.
                           WithTargetName(indexVariable).
                           Clone)

            For Each mappedIndex In index.MappedIndexes
                Dim tableIndexVariable = code.Identifier(indexVariable & "Ix", parameters.ScopeVariables, capitalize:=False)

                mainBuilder.
                    AppendLine($"Dim {tableIndexVariable} = RelationalModel.GetIndex(Me,").
                    IncrementIndent().
                    AppendLine($"{code.Literal(mappedIndex.DeclaringEntityType.Name)},").
                    AppendLine(
                        $"{If(mappedIndex.Name Is Nothing,
                            code.Literal(mappedIndex.Properties.Select(Function(p) p.Name).ToArray()),
                            code.Literal(mappedIndex.Name))})").
                    DecrementIndent()

                mainBuilder.
                    AppendLine($"{indexVariable}.MappedIndexes.Add({tableIndexVariable})").
                    AppendLine($"RelationalModel.GetOrCreateTableIndexes({tableIndexVariable}).Add({indexVariable})")
            Next

            mainBuilder.
                 AppendLine($"{parameters.TargetName}.Indexes.Add({code.Literal(index.Name)}, {indexVariable})")
        End Sub


        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="index">The unique constraint to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(index As ITableIndex, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(foreignKey As IForeignKeyConstraint,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim principalTableVariable = metadataVariables(foreignKey.PrincipalTable)
            Dim foreignKeyConstraintVariable = code.Identifier(foreignKey.Name, parameters.ScopeVariables, capitalize:=False)

            AddNamespace(GetType(ReferentialAction), parameters.Namespaces)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.
                AppendLine($"Dim {foreignKeyConstraintVariable} As New ForeignKeyConstraint(").
                IncrementIndent().
                AppendLine($"{code.Literal(foreignKey.Name)}, {parameters.TargetName}, {principalTableVariable},").
                Append("{"c).AppendJoin(foreignKey.Columns.Select(Function(c) metadataVariables(c))).
                AppendLine("},").
                Append($"{principalTableVariable}.FindUniqueConstraint({code.Literal(foreignKey.PrincipalUniqueConstraint.Name)}), ").
                Append(code.Literal(DirectCast(foreignKey.OnDeleteAction, [Enum]))).
                AppendLine(")"c).
                DecrementIndent()

            CreateAnnotations(
                foreignKey,
                AddressOf Generate,
                parameters.Cloner.
                           WithTargetName(foreignKeyConstraintVariable).
                           Clone)

            For Each mappedForeignKey In foreignKey.MappedForeignKeys

                Dim foreignKeyVariable = code.Identifier(foreignKeyConstraintVariable & "Fk", parameters.ScopeVariables, capitalize:=False)

                mainBuilder.
                    AppendLine($"Dim {foreignKeyVariable} = RelationalModel.GetForeignKey(Me,").IncrementIndent().
                    AppendLine($"{code.Literal(mappedForeignKey.DeclaringEntityType.Name)},").
                    AppendLine($"{code.Literal(mappedForeignKey.Properties.Select(Function(p) p.Name).ToArray())},").
                    AppendLine($"{code.Literal(mappedForeignKey.PrincipalEntityType.Name)},").
                    AppendLine($"{code.Literal(mappedForeignKey.PrincipalKey.Properties.Select(Function(p) p.Name).ToArray())})").
                    DecrementIndent()

                mainBuilder.
                    AppendLine($"{foreignKeyConstraintVariable}.MappedForeignKeys.Add({foreignKeyVariable})").
                    AppendLine(
                        $"RelationalModel.GetOrCreateForeignKeyConstraints({foreignKeyVariable}).Add({foreignKeyConstraintVariable})")
            Next

            mainBuilder.
                AppendLine($"{metadataVariables(foreignKey.Table)}.ForeignKeyConstraints.Add({foreignKeyConstraintVariable})").
                AppendLine($"{principalTableVariable}.ReferencingForeignKeyConstraints.Add({foreignKeyConstraintVariable})")
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="foreignKey">The foreign key to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(foreignKey As IForeignKeyConstraint, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(tableMapping As ITableMappingBase,
                                     tableMappingsVariable As String,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
        parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            Dim typeBase = tableMapping.TypeBase
            Dim typeBaseVariable = metadataVariables(typeBase)

            Dim table = tableMapping.Table
            Dim tableVariable = GetOrCreate(table, metadataVariables, parameters)
            Dim tableMappingVariable = code.Identifier(table.Name & "MappingBase", parameters.ScopeVariables, capitalize:=False)

            GenerateAddMapping(
                tableMapping,
                tableVariable,
                typeBaseVariable,
                tableMappingsVariable,
                tableMappingVariable,
                "TableMappingBase(Of ColumnMappingBase)",
                parameters)

            CreateAnnotations(
                tableMapping,
                AddressOf Generate,
                parameters.Cloner.
                           WithTargetName(tableMappingVariable).
                           Clone)

            For Each columnMapping In tableMapping.ColumnMappings
                mainBuilder.
                    Append("RelationalModel.CreateColumnMapping(").
                    Append($"DirectCast({metadataVariables(columnMapping.Column)}, ColumnBase(Of ColumnMappingBase)), ").
                    Append($"{typeBaseVariable}.FindProperty({code.Literal(columnMapping.Property.Name)}), ").
                    Append(tableMappingVariable).AppendLine(")"c)
            Next
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="tableMapping">The table mapping to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(tableMapping As ITableMappingBase, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(tableMapping As ITableMapping,
                                     tableMappingsVariable As String,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            Dim typeBase = tableMapping.TypeBase
            Dim typeBaseVariable = metadataVariables(typeBase)

            Dim table = tableMapping.Table
            Dim tableVariable = GetOrCreate(table, metadataVariables, parameters)
            Dim tableMappingVariable = code.Identifier(table.Name & "TableMapping", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(tableMapping, tableMappingVariable)

            GenerateAddMapping(
                tableMapping,
                tableVariable,
                typeBaseVariable,
                tableMappingsVariable,
                tableMappingVariable,
                "TableMapping",
                parameters)

            CreateAnnotations(
                tableMapping,
                AddressOf Generate,
                parameters.Cloner.
                           WithTargetName(tableMappingVariable).
                           Clone)

            For Each columnMapping In tableMapping.ColumnMappings
                mainBuilder.
                    Append($"RelationalModel.CreateColumnMapping({metadataVariables(columnMapping.Column)}, ").
                    Append($"{typeBaseVariable}.FindProperty({code.Literal(columnMapping.Property.Name)}), ").
                    Append(tableMappingVariable).AppendLine(")"c)
            Next
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="tableMapping">The table mapping to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(tableMapping As ITableMapping, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(viewMapping As IViewMapping,
                                     viewMappingsVariable As String,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            Dim typeBase = viewMapping.TypeBase
            Dim typeBaseVariable = metadataVariables(typeBase)

            Dim view = viewMapping.View
            Dim viewVariable = GetOrCreate(view, metadataVariables, parameters)
            Dim viewMappingVariable = code.Identifier(view.Name & "ViewMapping", parameters.ScopeVariables, capitalize:=False)

            GenerateAddMapping(
                viewMapping,
                viewVariable,
                typeBaseVariable,
                viewMappingsVariable,
                viewMappingVariable,
                "ViewMapping",
                parameters)

            CreateAnnotations(
                viewMapping,
                AddressOf Generate,
                parameters.Cloner.
                           WithTargetName(viewMappingVariable).
                           Clone)

            For Each columnMapping In viewMapping.ColumnMappings
                mainBuilder.
                    Append($"RelationalModel.CreateViewColumnMapping({metadataVariables(columnMapping.Column)}, ").
                    Append($"{typeBaseVariable}.FindProperty({code.Literal(columnMapping.Property.Name)}), ").
                    Append(viewMappingVariable).AppendLine(")"c)
            Next
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="viewMapping">The view mapping to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(viewMapping As IViewMapping, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(sqlQueryMapping As ISqlQueryMapping,
                                     sqlQueryMappingsVariable As String,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            Dim typeBase = sqlQueryMapping.TypeBase
            Dim typeBaseVariable = metadataVariables(typeBase)

            Dim sqlQuery = sqlQueryMapping.SqlQuery
            Dim sqlQueryVariable = GetOrCreate(sqlQuery, metadataVariables, parameters)
            Dim sqlQueryMappingVariable = code.Identifier(sqlQuery.Name & "SqlQueryMapping", parameters.ScopeVariables, capitalize:=False)

            GenerateAddMapping(
                sqlQueryMapping,
                sqlQueryVariable,
                typeBaseVariable,
                sqlQueryMappingsVariable,
                sqlQueryMappingVariable,
                "SqlQueryMapping",
                parameters)

            If sqlQueryMapping.IsDefaultSqlQueryMapping Then
                mainBuilder.
                   AppendLine($"{sqlQueryMappingVariable}.IsDefaultSqlQueryMapping = {code.Literal(True)}")
            End If

            CreateAnnotations(
                sqlQueryMapping,
                AddressOf Generate,
                parameters.Cloner.
                           WithTargetName(sqlQueryMappingVariable).
                           Clone)

            For Each columnMapping In sqlQueryMapping.ColumnMappings
                mainBuilder.
                    Append($"RelationalModel.CreateSqlQueryColumnMapping({metadataVariables(columnMapping.Column)}, ").
                    Append($"{typeBaseVariable}.FindProperty({code.Literal(columnMapping.Property.Name)}), ").
                    Append(sqlQueryMappingVariable).
                    AppendLine(")"c)
            Next
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="sqlQueryMapping">The SQL query mapping to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(sqlQueryMapping As ISqlQueryMapping, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(functionMapping As IFunctionMapping,
                                     functionMappingsVariable As String,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            Dim typeBase = functionMapping.TypeBase
            Dim typeBaseVariable = metadataVariables(typeBase)

            Dim storeFunction = functionMapping.StoreFunction
            Dim functionVariable = GetOrCreate(storeFunction, metadataVariables, parameters)
            Dim dbFunctionVariable = metadataVariables(functionMapping.DbFunction)
            Dim functionMappingVariable = code.Identifier(storeFunction.Name & "FunctionMapping", parameters.ScopeVariables, capitalize:=False)

            GenerateAddMapping(
                functionMapping,
                functionVariable,
                typeBaseVariable,
                functionMappingsVariable,
                functionMappingVariable,
                "FunctionMapping",
                parameters,
                $"{dbFunctionVariable}, ")

            If functionMapping.IsDefaultFunctionMapping Then
                mainBuilder.
                    AppendLine($"{functionMappingVariable}.IsDefaultFunctionMapping = {code.Literal(True)}")
            End If

            CreateAnnotations(
                functionMapping,
                AddressOf Generate,
                parameters.Cloner.
                           WithTargetName(functionMappingVariable).
                           Clone)

            For Each columnMapping In functionMapping.ColumnMappings
                mainBuilder.
                    Append($"RelationalModel.CreateFunctionColumnMapping({metadataVariables(columnMapping.Column)}, ").
                    Append($"{typeBaseVariable}.FindProperty({code.Literal(columnMapping.Property.Name)}), ").
                    Append(functionMappingVariable).AppendLine(")"c)
            Next
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="functionMapping">The function mapping to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(functionMapping As IFunctionMapping, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(sprocMapping As IStoredProcedureMapping,
                                     sprocMappingsVariable As String,
                                     storeObjectType As StoreObjectType,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            Dim typeBase = sprocMapping.TypeBase
            Dim typeBaseVariable = metadataVariables(typeBase)

            Dim storeSproc = sprocMapping.StoreStoredProcedure
            Dim storeSprocVariable = GetOrCreate(storeSproc, metadataVariables, parameters)

            Dim sprocMappingName As String
            Select Case storeObjectType
                Case StoreObjectType.InsertStoredProcedure : sprocMappingName = "InsertStoredProcedureMapping"
                Case StoreObjectType.DeleteStoredProcedure : sprocMappingName = "DeleteStoredProcedureMapping"
                Case StoreObjectType.UpdateStoredProcedure : sprocMappingName = "UpdateStoredProcedureMapping"
                Case Else : Throw New Exception("Unexpected stored procedure type: " & storeObjectType)
            End Select

            Dim sprocSnippet = CreateFindSnippet(sprocMapping.StoredProcedure, metadataVariables)
            Dim sprocVariable = code.Identifier(storeSproc.Name & sprocMappingName(0) & "Sproc", parameters.ScopeVariables, capitalize:=False)
            mainBuilder.
                AppendLine($"Dim {sprocVariable} = {CreateFindSnippet(sprocMapping.StoredProcedure, metadataVariables)}")

            Dim sprocMappingVariable = code.Identifier(storeSproc.Name & "SprocMapping", parameters.ScopeVariables, capitalize:=False)
            Dim tableMappingVariable = If(sprocMapping.TableMapping IsNot Nothing, metadataVariables(sprocMapping.TableMapping), Nothing)

            GenerateAddMapping(
                sprocMapping,
                storeSprocVariable,
                typeBaseVariable,
                sprocMappingsVariable,
                sprocMappingVariable,
                "StoredProcedureMapping",
                parameters,
                $"{sprocSnippet}, {If(tableMappingVariable, "Nothing")}, ")

            If tableMappingVariable IsNot Nothing Then
                mainBuilder.
                AppendLine($"{tableMappingVariable}.{sprocMappingName} = {sprocMappingVariable}")
            End If

            CreateAnnotations(
                sprocMapping,
                AddressOf Generate,
                parameters.Cloner.
                           WithTargetName(sprocMappingVariable).
                           Clone)

            For Each parameterMapping In sprocMapping.ParameterMappings
                mainBuilder.
                    Append($"RelationalModel.CreateStoredProcedureParameterMapping({metadataVariables(parameterMapping.StoreParameter)}, ").
                    Append($"{sprocVariable}.FindParameter({code.Literal(parameterMapping.Parameter.Name)}), ").
                    Append($"{typeBaseVariable}.FindProperty({code.Literal(parameterMapping.Property.Name)}), ").
                    Append(sprocMappingVariable).AppendLine(")"c)
            Next

            For Each columnMapping In sprocMapping.ResultColumnMappings
                mainBuilder.
                    Append($"RelationalModel.CreateStoredProcedureResultColumnMapping({metadataVariables(columnMapping.StoreResultColumn)}, ").
                    Append($"{sprocVariable}.FindResultColumn({code.Literal(columnMapping.ResultColumn.Name)}), ").
                    Append($"{typeBaseVariable}.FindProperty({code.Literal(columnMapping.Property.Name)}), ").
                    Append(sprocMappingVariable).AppendLine(")"c)
            Next
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="sprocMapping">The stored procedure mapping to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(sprocMapping As IStoredProcedureMapping, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Sub GenerateAddMapping(tableMapping As ITableMappingBase,
                                       tableVariable As String,
                                       entityTypeVariable As String,
                                       tableMappingsVariable As String,
                                       tableMappingVariable As String,
                                       mappingType As String,
                                       parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters,
                                       Optional additionalParameter As String = Nothing)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            Dim typeBase = tableMapping.TypeBase

            mainBuilder.
                Append($"Dim {tableMappingVariable} As New {mappingType}({entityTypeVariable}, ").
                Append($"{tableVariable}, {If(additionalParameter, "")}{code.Literal(tableMapping.IncludesDerivedTypes)}")

            If tableMapping.IsSharedTablePrincipal.HasValue OrElse
               tableMapping.IsSplitEntityTypePrincipal.HasValue Then

                mainBuilder.
                    Append(")"c).
                    AppendLine(" With {").
                    IncrementIndent()

                Dim AddComma = False

                If tableMapping.IsSharedTablePrincipal.HasValue Then
                    mainBuilder.
                        Append(".IsSharedTablePrincipal = ").
                        Append(code.Literal(tableMapping.IsSharedTablePrincipal))
                    AddComma = True
                End If

                If tableMapping.IsSplitEntityTypePrincipal.HasValue Then
                    If AddComma Then mainBuilder.AppendLine(","c)

                    mainBuilder.
                        Append(".IsSplitEntityTypePrincipal = ").
                        Append(code.Literal(tableMapping.IsSplitEntityTypePrincipal))
                End If

                mainBuilder.
                    AppendLine().
                    DecrementIndent().
                    AppendLine("}"c)
            Else
                mainBuilder.AppendLine(")"c)
            End If

            Dim table = tableMapping.Table
            Dim isOptional = table.IsOptional(typeBase)

            mainBuilder.
                AppendLine($"{tableVariable}.AddTypeMapping({tableMappingVariable}, {code.Literal(isOptional)})").
                AppendLine($"{tableMappingsVariable}.Add({tableMappingVariable})")

            If TypeOf typeBase Is IEntityType Then
                Dim entityType = DirectCast(typeBase, IEntityType)
                For Each internalForeignKey In table.GetRowInternalForeignKeys(entityType)
                    mainBuilder.
                        Append(tableVariable).Append($".AddRowInternalForeignKey({entityTypeVariable}, ").
                        AppendLine("RelationalModel.GetForeignKey(Me,").
                        IncrementIndent().
                        AppendLine($"{code.Literal(internalForeignKey.DeclaringEntityType.Name)},").
                        AppendLine($"{code.Literal(internalForeignKey.Properties.Select(Function(p) p.Name).ToArray())},").
                        AppendLine($"{code.Literal(internalForeignKey.PrincipalEntityType.Name)},").
                        AppendLine($"{code.Literal(internalForeignKey.PrincipalKey.Properties.Select(Function(p) p.Name).ToArray())}))").
                        DecrementIndent()
                Next
            End If
        End Sub

        Private Overloads Sub Create([function] As IDbFunction,
                                     functionsVariable As String,
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            If [function].Translation IsNot Nothing Then
                Throw New InvalidOperationException(RelationalStrings.CompiledModelFunctionTranslation([function].Name))
            End If

            AddNamespace([function].ReturnType, parameters.Namespaces)

            Dim code = VBCode

            Dim functionVariable = code.Identifier(
            If([function].MethodInfo?.Name, [function].Name), parameters.ScopeVariables, capitalize:=False)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.
                Append("Dim ").Append(functionVariable).AppendLine(" As New RuntimeDbFunction(").IncrementIndent().
                Append(code.Literal([function].ModelName)).AppendLine(","c).
                Append(parameters.TargetName).AppendLine(","c).
                Append(code.Literal([function].ReturnType)).AppendLine(","c).
                Append(code.Literal([function].Name))

            If [function].Schema IsNot Nothing Then
                mainBuilder.
                    AppendLine(","c).
                    Append("schema:=").
                    Append(code.Literal([function].Schema))
            End If

            If [function].StoreType IsNot Nothing Then
                mainBuilder.
                    AppendLine(","c).
                    Append("storeType:=").
                    Append(code.Literal([function].StoreType))
            End If

            If [function].MethodInfo IsNot Nothing Then
                Dim method = [function].MethodInfo
                AddNamespace(method.DeclaringType, parameters.Namespaces)

                mainBuilder.
                    AppendLine(","c).
                    AppendLine($"methodInfo:={code.Literal(method.DeclaringType)}.GetMethod(").IncrementIndent().
                    Append(code.Literal(method.Name)).AppendLine(","c).
                    Append(If(method.IsPublic, "BindingFlags.Public", "BindingFlags.NonPublic")).
                    Append(If(method.IsStatic, " Or BindingFlags.Static", " Or BindingFlags.Instance")).
                    AppendLine(" Or BindingFlags.DeclaredOnly,").
                    AppendLine("Nothing,").
                    Append("{"c).Append(String.Join(", ", method.GetParameters().Select(Function(p) code.Literal(p.ParameterType)))).AppendLine("},").
                    Append("Nothing)").DecrementIndent()
            End If

            If [function].IsScalar Then
                mainBuilder.
                    AppendLine(","c).
                    Append("scalar:=").
                    Append(code.Literal([function].IsScalar))
            End If

            If [function].IsAggregate Then
                mainBuilder.
                    AppendLine(","c).
                    Append("aggregate:=").
                    Append(code.Literal([function].IsAggregate))
            End If

            If [function].IsNullable Then
                mainBuilder.AppendLine(","c).Append("nullable:=").Append(code.Literal([function].IsNullable))
            End If

            If [function].IsBuiltIn Then
                mainBuilder.
                    AppendLine(","c).
                    Append("builtIn:=").
                    Append(code.Literal([function].IsBuiltIn))
            End If

            mainBuilder.
                AppendLine(")"c).
                DecrementIndent().
                AppendLine()

            parameters = parameters.Cloner.
                                    WithTargetName(functionVariable).
                                    Clone()

            For Each parameter In [function].Parameters
                Create(parameter, parameters)
            Next

            If [function].TypeMapping IsNot Nothing Then
                mainBuilder.Append(functionVariable).Append(".TypeMapping = ")
                Create(
                    [function].TypeMapping,
                    parameters.
                        Cloner.
                        WithTargetName(functionVariable).
                        Clone)
                mainBuilder.AppendLine()
            End If

            CreateAnnotations(
                [function],
                AddressOf Generate,
                parameters)

            mainBuilder.
                Append(functionsVariable).
                Append("("c).
                Append(code.Literal([function].ModelName)).
                Append(") = ").
                AppendLine(functionVariable).
                AppendLine()
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="function">The function to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate([Function] As IDbFunction, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(parameter As IDbFunctionParameter, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            AddNamespace(parameter.ClrType, parameters.Namespaces)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            Dim parameterVariable = code.Identifier(parameter.Name, parameters.ScopeVariables, capitalize:=False)
            mainBuilder.
                Append("Dim ").
                Append(parameterVariable).
                Append(" = ").
                Append(parameters.TargetName).
                AppendLine(".AddParameter(").
                IncrementIndent().
                Append(code.Literal(parameter.Name)).
                AppendLine(","c).
                Append(code.Literal(parameter.ClrType)).
                AppendLine(","c).
                Append(code.Literal(parameter.PropagatesNullability)).
                AppendLine(","c).
                Append(code.Literal(parameter.StoreType)).
                AppendLine(")"c).
                DecrementIndent()

            mainBuilder.Append(parameterVariable).Append(".TypeMapping = ")
            Create(
                parameter.TypeMapping,
                parameters.
                    Cloner.
                    WithTargetName(parameterVariable).
                    Clone)

            mainBuilder.AppendLine()

            CreateAnnotations(
                parameter,
                AddressOf Generate,
                parameters.Cloner.
                            WithTargetName(parameterVariable).
                            Clone())

            mainBuilder.AppendLine()
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="functionParameter">The function parameter to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(functionParameter As IDbFunctionParameter, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(aSequence As ISequence, sequencesVariable As String, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            Dim code = VBCode
            Dim sequenceVariable = code.Identifier(aSequence.Name, parameters.ScopeVariables, capitalize:=False)
            Dim mainBuilder = parameters.MainBuilder

            mainBuilder.
                Append("Dim ").
                Append(sequenceVariable).
                AppendLine(" As New RuntimeSequence(").
                IncrementIndent().
                Append(code.Literal(aSequence.Name)).
                AppendLine(","c).
                Append(parameters.TargetName).
                AppendLine(","c).
                Append(code.Literal(aSequence.Type))

            If aSequence.Schema IsNot Nothing Then
                mainBuilder.
                    AppendLine(","c).
                    Append("schema:=").
                    Append(code.Literal(aSequence.Schema))
            End If

            If aSequence.StartValue <> Sequence.DefaultStartValue Then
                mainBuilder.
                    AppendLine(","c).
                    Append("startValue:=").
                    Append(code.Literal(aSequence.StartValue))
            End If

            If aSequence.IncrementBy <> Sequence.DefaultIncrementBy Then
                mainBuilder.
                    AppendLine(","c).
                    Append("incrementBy:=").
                    Append(code.Literal(aSequence.IncrementBy))
            End If

            If aSequence.IsCyclic Then
                mainBuilder.
                    AppendLine(","c).
                    Append("cyclic:=").
                    Append(code.Literal(aSequence.IsCyclic))
            End If

            If aSequence.MinValue IsNot Nothing Then
                mainBuilder.
                    AppendLine(","c).
                    Append("minValue:=").
                    Append(code.Literal(aSequence.MinValue))
            End If

            If aSequence.MaxValue IsNot Nothing Then
                mainBuilder.
                    AppendLine(","c).
                    Append("maxValue:=").
                    Append(code.Literal(aSequence.MaxValue))
            End If

            If aSequence.ModelSchema Is Nothing AndAlso aSequence.Schema IsNot Nothing Then
                mainBuilder.
                    AppendLine(","c).
                    Append("modelSchemaIsNull:=").
                    Append(code.Literal(True))
            End If

            mainBuilder.
                AppendLine(")"c).
                DecrementIndent().
                AppendLine()

            CreateAnnotations(
                aSequence,
                AddressOf Generate,
                parameters.Cloner.
                            WithTargetName(sequenceVariable).
                            Clone())

            mainBuilder.
                Append(sequencesVariable).
                Append("((").Append(code.Literal(aSequence.Name)).Append(", ").
                Append(code.Literal(aSequence.ModelSchema)).Append(")) = ").
                AppendLine(sequenceVariable).
                AppendLine()
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="sequence">The sequence to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(sequence As ISequence, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate(entityType As IEntityType, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            Dim annotations = parameters.Annotations

            If parameters.IsRuntime Then
                annotations.Remove(RelationalAnnotationNames.TableMappings)
                annotations.Remove(RelationalAnnotationNames.ViewMappings)
                annotations.Remove(RelationalAnnotationNames.SqlQueryMappings)
                annotations.Remove(RelationalAnnotationNames.FunctionMappings)
                annotations.Remove(RelationalAnnotationNames.InsertStoredProcedureMappings)
                annotations.Remove(RelationalAnnotationNames.DeleteStoredProcedureMappings)
                annotations.Remove(RelationalAnnotationNames.UpdateStoredProcedureMappings)
                annotations.Remove(RelationalAnnotationNames.DefaultMappings)
            Else
                annotations.Remove(RelationalAnnotationNames.CheckConstraints)
                annotations.Remove(RelationalAnnotationNames.Comment)
                annotations.Remove(RelationalAnnotationNames.IsTableExcludedFromMigrations)

                'These need to be set explicitly to prevent default values from being generated
                annotations(RelationalAnnotationNames.TableName) = entityType.GetTableName()
                annotations(RelationalAnnotationNames.Schema) = entityType.GetSchema()
                annotations(RelationalAnnotationNames.ViewName) = entityType.GetViewName()
                annotations(RelationalAnnotationNames.ViewSchema) = entityType.GetViewSchema()
                annotations(RelationalAnnotationNames.SqlQuery) = entityType.GetSqlQuery()
                annotations(RelationalAnnotationNames.FunctionName) = entityType.GetFunctionName()

                Dim fragments As IReadOnlyStoreObjectDictionary(Of IEntityTypeMappingFragment) = Nothing

                If annotations.TryGetAndRemove(RelationalAnnotationNames.MappingFragments,
                                               fragments) Then

                    AddNamespace(GetType(StoreObjectDictionary(Of RuntimeEntityTypeMappingFragment)), parameters.Namespaces)
                    AddNamespace(GetType(StoreObjectIdentifier), parameters.Namespaces)

                    Dim fragmentsVariable = VBCode.Identifier("fragments", parameters.ScopeVariables, capitalize:=False)

                    parameters.
                        MainBuilder.
                        Append("Dim ").Append(fragmentsVariable).AppendLine(" As New StoreObjectDictionary(Of RuntimeEntityTypeMappingFragment)()")

                    For Each fragment In fragments.GetValues()
                        Create(fragment, fragmentsVariable, parameters)
                    Next

                    GenerateSimpleAnnotation(RelationalAnnotationNames.MappingFragments, fragmentsVariable, parameters)
                End If

                Dim insertStoredProcedure As StoredProcedure = Nothing
                If annotations.TryGetAndRemove(
                   RelationalAnnotationNames.InsertStoredProcedure, insertStoredProcedure) Then

                    Dim sprocVariable = VBCode.Identifier("insertSproc", parameters.ScopeVariables, capitalize:=False)

                    Create(insertStoredProcedure, sprocVariable, parameters)

                    GenerateSimpleAnnotation(RelationalAnnotationNames.InsertStoredProcedure, sprocVariable, parameters)
                    parameters.MainBuilder.AppendLine()
                End If

                Dim deleteStoredProcedure As StoredProcedure = Nothing
                If annotations.TryGetAndRemove(
                    RelationalAnnotationNames.DeleteStoredProcedure, deleteStoredProcedure) Then

                    Dim sprocVariable = VBCode.Identifier("deleteSproc", parameters.ScopeVariables, capitalize:=False)

                    Create(deleteStoredProcedure, sprocVariable, parameters)

                    GenerateSimpleAnnotation(RelationalAnnotationNames.DeleteStoredProcedure, sprocVariable, parameters)
                    parameters.MainBuilder.AppendLine()
                End If

                Dim updateStoredProcedure As StoredProcedure = Nothing
                If annotations.TryGetAndRemove(
                    RelationalAnnotationNames.UpdateStoredProcedure, updateStoredProcedure) Then
                    Dim sprocVariable = VBCode.Identifier("updateSproc", parameters.ScopeVariables, capitalize:=False)

                    Create(updateStoredProcedure, sprocVariable, parameters)

                    GenerateSimpleAnnotation(RelationalAnnotationNames.UpdateStoredProcedure, sprocVariable, parameters)
                    parameters.MainBuilder.AppendLine()
                End If
            End If

            MyBase.Generate(entityType, parameters)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate(complexType As IComplexType, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            Dim annotations = parameters.Annotations
            If parameters.IsRuntime Then
                annotations.Remove(RelationalAnnotationNames.TableMappings)
                annotations.Remove(RelationalAnnotationNames.ViewMappings)
                annotations.Remove(RelationalAnnotationNames.SqlQueryMappings)
                annotations.Remove(RelationalAnnotationNames.FunctionMappings)
                annotations.Remove(RelationalAnnotationNames.InsertStoredProcedureMappings)
                annotations.Remove(RelationalAnnotationNames.DeleteStoredProcedureMappings)
                annotations.Remove(RelationalAnnotationNames.UpdateStoredProcedureMappings)
                annotations.Remove(RelationalAnnotationNames.DefaultMappings)
            Else
                ' These need to be set explicitly to prevent default values from being generated
                annotations(RelationalAnnotationNames.TableName) = complexType.GetTableName()
                annotations(RelationalAnnotationNames.Schema) = complexType.GetSchema()
                annotations(RelationalAnnotationNames.ViewName) = complexType.GetViewName()
                annotations(RelationalAnnotationNames.ViewSchema) = complexType.GetViewSchema()
                annotations(RelationalAnnotationNames.SqlQuery) = complexType.GetSqlQuery()
                annotations(RelationalAnnotationNames.FunctionName) = complexType.GetFunctionName()
            End If

            MyBase.Generate(complexType, parameters)
        End Sub

        Private Overloads Sub Create(fragment As IEntityTypeMappingFragment,
                                     fragmentsVariable As String,
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim storeObject = fragment.StoreObject
            Dim code = VBCode
            Dim overrideVariable = code.Identifier(storeObject.Name & "Fragment", parameters.ScopeVariables, capitalize:=False)
            Dim mainBuilder = parameters.MainBuilder

            mainBuilder.
                Append("Dim ").
                Append(overrideVariable).
                AppendLine(" As New RuntimeEntityTypeMappingFragment(").
                IncrementIndent().
                Append(parameters.TargetName).
                AppendLine(","c)

            AppendLiteral(storeObject, mainBuilder, code)

            mainBuilder.
                AppendLine(","c).
                Append(code.Literal(fragment.IsTableExcludedFromMigrations)).
                AppendLine(")"c).
                DecrementIndent()

            CreateAnnotations(
                fragment,
                AddressOf Generate,
                parameters.Cloner.
                            WithTargetName(overrideVariable).
                            Clone)

            ' Using reflection because VB currently doesn't seem to be able to call a method that has
            ' an 'in' parameter if it's virtual.
            mainBuilder.
                Append(fragmentsVariable).
                Append($".GetType().GetMethod(""Add"").Invoke({fragmentsVariable}, {{")

            AppendLiteral(storeObject, mainBuilder, code)

            mainBuilder.
                Append(", ").
                Append(overrideVariable).AppendLine("})")
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="fragment">The fragment to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(fragment As IEntityTypeMappingFragment, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(storedProcedure As IStoredProcedure, sprocVariable As String, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            AddNamespace(GetType(RuntimeStoredProcedure), parameters.Namespaces)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder

            mainBuilder.
            Append("Dim ").Append(sprocVariable).AppendLine(" As New RuntimeStoredProcedure(").
                IncrementIndent().
                Append(parameters.TargetName).AppendLine(","c).
                Append(code.Literal(storedProcedure.Name)).AppendLine(","c).
                Append(code.Literal(storedProcedure.Schema)).AppendLine(","c).
                Append(code.Literal(storedProcedure.IsRowsAffectedReturned)).
                AppendLine(")"c).
                DecrementIndent().
                AppendLine()

            parameters = parameters.Cloner.
                                    WithTargetName(sprocVariable).
                                    Clone

            For Each parameter In storedProcedure.Parameters
                Create(parameter, parameters)
            Next

            For Each resultColumn In storedProcedure.ResultColumns
                Create(resultColumn, parameters)
            Next

            CreateAnnotations(
                storedProcedure,
                AddressOf Generate,
                parameters)
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="storedProcedure">The stored procedure to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(storedProcedure As IStoredProcedure, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(parameter As IStoredProcedureParameter, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            Dim parameterVariable = code.Identifier(If(parameter.PropertyName, parameter.Name), parameters.ScopeVariables, capitalize:=False)

            mainBuilder.
                Append("Dim ").Append(parameterVariable).Append(" = ").
                Append(parameters.TargetName).AppendLine(".AddParameter(").IncrementIndent().
                Append(code.Literal(parameter.Name)).Append(", ").
                Append(code.Literal(DirectCast(parameter.Direction, [Enum]), fullName:=True)).Append(", ").
                Append(code.Literal(parameter.ForRowsAffected)).Append(", ").
                Append(code.Literal(parameter.PropertyName)).Append(", ").
                Append(code.Literal(parameter.ForOriginalValue)).
                AppendLine(")"c).DecrementIndent()

            CreateAnnotations(
                parameter,
                AddressOf Generate,
                parameters.Cloner.
                            WithTargetName(parameterVariable).
                            Clone)
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="storedProcedure">The stored procedure to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(storedProcedure As IStoredProcedureParameter, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Overloads Sub Create(resultColumn As IStoredProcedureResultColumn, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            Dim resultColumnVariable = code.Identifier(resultColumn.Name, parameters.ScopeVariables, capitalize:=False)

            mainBuilder.
                Append("Dim ").Append(resultColumnVariable).Append(" = ").
                Append(parameters.TargetName).AppendLine(".AddResultColumn(").IncrementIndent().
                Append(code.Literal(resultColumn.Name)).Append(", ").
                Append(code.Literal(resultColumn.ForRowsAffected)).Append(", ").
                Append(code.Literal(resultColumn.PropertyName)).
                AppendLine(")"c).DecrementIndent()

            CreateAnnotations(
                resultColumn,
                AddressOf Generate,
                parameters.Cloner.
                            WithTargetName(resultColumnVariable).
                            Clone)
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="storedProcedure">The stored procedure to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(storedProcedure As IStoredProcedureResultColumn, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="constraint">The check constraint to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(constraint As ICheckConstraint, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        '' <inheritdoc />
        Public Overrides Sub Generate([Property] As IProperty, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim annotations = parameters.Annotations
            If parameters.IsRuntime Then
                annotations.Remove(RelationalAnnotationNames.TableColumnMappings)
                annotations.Remove(RelationalAnnotationNames.ViewColumnMappings)
                annotations.Remove(RelationalAnnotationNames.SqlQueryColumnMappings)
                annotations.Remove(RelationalAnnotationNames.FunctionColumnMappings)
                annotations.Remove(RelationalAnnotationNames.InsertStoredProcedureParameterMappings)
                annotations.Remove(RelationalAnnotationNames.InsertStoredProcedureResultColumnMappings)
                annotations.Remove(RelationalAnnotationNames.DeleteStoredProcedureParameterMappings)
                annotations.Remove(RelationalAnnotationNames.UpdateStoredProcedureParameterMappings)
                annotations.Remove(RelationalAnnotationNames.UpdateStoredProcedureResultColumnMappings)
                annotations.Remove(RelationalAnnotationNames.DefaultColumnMappings)
            Else
                annotations.Remove(RelationalAnnotationNames.ColumnOrder)
                annotations.Remove(RelationalAnnotationNames.Comment)
                annotations.Remove(RelationalAnnotationNames.Collation)

                Dim tableOverrides As IReadOnlyStoreObjectDictionary(Of IRelationalPropertyOverrides) = Nothing

                If TryGetAndRemove(annotations, RelationalAnnotationNames.RelationalOverrides, tableOverrides) Then
                    AddNamespace(GetType(StoreObjectDictionary(Of RuntimeRelationalPropertyOverrides)), parameters.Namespaces)
                    AddNamespace(GetType(StoreObjectIdentifier), parameters.Namespaces)

                    Dim overridesVariable = VBCode.Identifier("overrides", parameters.ScopeVariables, capitalize:=False)
                    parameters.MainBuilder.
                        AppendLine().
                        Append("Dim ").
                        Append(overridesVariable).
                        AppendLine(" As New StoreObjectDictionary(Of RuntimeRelationalPropertyOverrides)()")

                    For Each [overrides] In tableOverrides.GetValues()
                        Create([overrides], overridesVariable, parameters)
                    Next

                    GenerateSimpleAnnotation(RelationalAnnotationNames.RelationalOverrides, overridesVariable, parameters)
                    parameters.MainBuilder.AppendLine()
                End If
            End If

            MyBase.Generate([Property], parameters)
        End Sub

        Private Overloads Sub Create([overrides] As IRelationalPropertyOverrides,
                                     overridesVariable As String,
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim storeObject = [overrides].StoreObject

            Dim code = VBCode

            Dim overrideVariable =
                code.Identifier(parameters.TargetName & Capitalize(storeObject.Name), parameters.ScopeVariables, capitalize:=False)

            Dim mainBuilder = parameters.MainBuilder

            mainBuilder.
                Append("Dim ").
                Append(overrideVariable).
                AppendLine(" As New RuntimeRelationalPropertyOverrides(").
                IncrementIndent().
                Append(parameters.TargetName).AppendLine(","c)

            AppendLiteral(storeObject, mainBuilder, code)

            mainBuilder.
                AppendLine(","c).
                Append(code.Literal([overrides].IsColumnNameOverridden)).
                AppendLine(","c).
                Append(code.Literal([overrides].ColumnName)).
                AppendLine(")"c).
                DecrementIndent()

            CreateAnnotations(
                [overrides], AddressOf Generate,
                parameters.Cloner.
                            WithTargetName(overrideVariable).
                            Clone())


            ' Using reflection because VB currently doesn't seem to be able to call a method that has
            ' an 'in' parameter if it's virtual.
            mainBuilder.
                Append(overridesVariable).
                Append($".GetType().GetMethod(""Add"").Invoke({overridesVariable}, {{")

            AppendLiteral(storeObject, mainBuilder, code)

            mainBuilder.
                Append(", ").
                Append(overrideVariable).
                AppendLines("})")
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="overrides">The property overrides to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate([Overrides] As IRelationalPropertyOverrides, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate(key As IKey, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            If parameters.IsRuntime Then
                parameters.Annotations.Remove(RelationalAnnotationNames.UniqueConstraintMappings)
            End If

            MyBase.Generate(key, parameters)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate(foreignKey As IForeignKey, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            If parameters.IsRuntime Then
                parameters.Annotations.Remove(RelationalAnnotationNames.ForeignKeyMappings)
            End If

            MyBase.Generate(foreignKey, parameters)
        End Sub

        '''<inheritdoc />
        Public Overrides Sub Generate(index As IIndex, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            Dim annotations = parameters.Annotations
            If parameters.IsRuntime Then
                annotations.Remove(RelationalAnnotationNames.TableIndexMappings)
            Else
                annotations.Remove(RelationalAnnotationNames.Filter)
            End If

            MyBase.Generate(index, parameters)
        End Sub

        ''' <inheritdoc />
        Public Overrides Function Create(
            typeMapping As CoreTypeMapping,
            parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters,
            Optional valueComparer As ValueComparer = Nothing,
            Optional keyValueComparer As ValueComparer = Nothing,
            Optional providerValueComparer As ValueComparer = Nothing) As Boolean

            Dim relationalTypeMapping = TryCast(typeMapping, RelationalTypeMapping)
            If relationalTypeMapping Is Nothing Then
                Return MyBase.Create(typeMapping, parameters, valueComparer, keyValueComparer, providerValueComparer)
            End If

            Dim mainBuilder = parameters.MainBuilder
            Dim code = VBCode

            If IsSpatial(relationalTypeMapping) Then
                ' Spatial mappings are Not supported in the compiled model
                mainBuilder.Append(code.UnknownLiteral(Nothing))
                Return False
            End If

            Dim defaultInstance = DirectCast(CreateDefaultTypeMapping(relationalTypeMapping, parameters), RelationalTypeMapping)
            If defaultInstance Is Nothing Then
                Return True
            End If

            parameters.Namespaces.Add(GetType(Type).Namespace)
            parameters.Namespaces.Add(GetType(BindingFlags).Namespace)

            Dim cloneMethod = GetCloneMethod(typeMapping.GetType(), {
                "comparer",
                "keyComparer",
                "providerValueComparer",
                "mappingInfo",
                "converter",
                "clrType",
                "storeTypePostfix",
                "jsonValueReaderWriter",
                "elementMapping"
            })

            parameters.Namespaces.Add(cloneMethod.DeclaringType.Namespace)

            mainBuilder.
                AppendLine($"DirectCast(GetType({code.Reference(cloneMethod.DeclaringType)}).").
                IncrementIndent().
                AppendLine($"GetMethod(""Clone"", BindingFlags.Public Or BindingFlags.Instance Or BindingFlags.DeclaredOnly).").
                AppendLine($"Invoke({code.Reference(typeMapping.GetType())}.Default, {{").
                IncrementIndent()

            Dim first = True
            For Each p In cloneMethod.GetParameters()
                If first Then
                    first = False
                Else
                    mainBuilder.AppendLine(","c)
                End If

                Select Case p.Name
                    Case "comparer"
                        Create(If(valueComparer, relationalTypeMapping.Comparer), parameters, code)

                    Case "keyComparer"
                        Create(If(keyValueComparer, relationalTypeMapping.KeyComparer), parameters, code)

                    Case "providerValueComparer"
                        Create(If(providerValueComparer, relationalTypeMapping.ProviderValueComparer), parameters, code)

                    Case "mappingInfo"
                        Dim storeTypeDifferent = relationalTypeMapping.StoreType <> defaultInstance.StoreType

                        Dim sizeDifferent = relationalTypeMapping.Size.HasValue AndAlso
                                            (Not defaultInstance.Size.HasValue OrElse relationalTypeMapping.Size.Value <> defaultInstance.Size.Value)

                        Dim precisionDifferent = relationalTypeMapping.Precision.HasValue AndAlso
                                                 (Not defaultInstance.Precision.HasValue OrElse relationalTypeMapping.Precision.Value <> defaultInstance.Precision.Value)

                        Dim scaleDifferent = relationalTypeMapping.Scale.HasValue AndAlso
                                             (Not defaultInstance.Scale.HasValue OrElse relationalTypeMapping.Scale.Value <> defaultInstance.Scale.Value)

                        Dim dbTypeDifferent = relationalTypeMapping.DbType.HasValue AndAlso
                                              (Not defaultInstance.DbType.HasValue OrElse relationalTypeMapping.DbType.Value <> defaultInstance.DbType.Value)

                        Dim isUnicodeDifferent = relationalTypeMapping.IsUnicode <> defaultInstance.IsUnicode

                        Dim isFixedLengthDifferent = relationalTypeMapping.IsFixedLength <> defaultInstance.IsFixedLength

                        If storeTypeDifferent OrElse
                           sizeDifferent OrElse
                           precisionDifferent OrElse
                           scaleDifferent OrElse
                           dbTypeDifferent OrElse
                           isUnicodeDifferent OrElse
                           isFixedLengthDifferent Then

                            AddNamespace(GetType(RelationalTypeMappingInfo), parameters.Namespaces)
                            mainBuilder.
                                AppendLine("New RelationalTypeMappingInfo(").
                                IncrementIndent()

                            Dim firstParameter = True
                            If storeTypeDifferent Then
                                GenerateArgument(
                                    "storeTypeName", code.Literal(relationalTypeMapping.StoreType), mainBuilder, firstParameter)
                            End If

                            If sizeDifferent Then
                                GenerateArgument(
                                    "size", code.Literal(relationalTypeMapping.Size), mainBuilder, firstParameter)
                            End If

                            If isUnicodeDifferent Then
                                GenerateArgument(
                                    "unicode", code.Literal(relationalTypeMapping.IsUnicode), mainBuilder, firstParameter)
                            End If

                            If isFixedLengthDifferent Then
                                GenerateArgument(
                                    "fixedLength", code.Literal(relationalTypeMapping.IsFixedLength), mainBuilder, firstParameter)
                            End If

                            If precisionDifferent Then
                                GenerateArgument(
                                    "precision", code.Literal(relationalTypeMapping.Precision), mainBuilder, firstParameter)
                            End If

                            If scaleDifferent Then
                                GenerateArgument(
                                    "scale", code.Literal(relationalTypeMapping.Scale), mainBuilder, firstParameter)
                            End If

                            If dbTypeDifferent Then
                                GenerateArgument(
                                    "dbType", code.Literal(relationalTypeMapping.DbType.Value, fullName:=True), mainBuilder, firstParameter)
                            End If

                            mainBuilder.
                                Append(")"c).
                                DecrementIndent()
                        Else
                            mainBuilder.Append("Type.Missing")
                        End If

                    Case "converter"
                        If relationalTypeMapping.Converter IsNot Nothing AndAlso
                           relationalTypeMapping.Converter IsNot defaultInstance.Converter Then

                            Create(relationalTypeMapping.Converter, parameters, code)
                        Else
                            mainBuilder.Append("Type.Missing")
                        End If

                    Case "clrType"
                        Dim typeDifferent = relationalTypeMapping.Converter Is Nothing AndAlso
                                relationalTypeMapping.ClrType <> defaultInstance.ClrType

                        If typeDifferent Then
                            mainBuilder.
                                Append(code.Literal(relationalTypeMapping.ClrType))
                        Else
                            mainBuilder.Append("Type.Missing")
                        End If

                    Case "storeTypePostfix"
                        Dim storeTypePostfixDifferent = relationalTypeMapping.StoreTypePostfix <> defaultInstance.StoreTypePostfix

                        If storeTypePostfixDifferent Then
                            mainBuilder.
                                Append(code.Literal(DirectCast(relationalTypeMapping.StoreTypePostfix, [Enum])))
                        Else
                            mainBuilder.Append("Type.Missing")
                        End If

                    Case "jsonValueReaderWriter"
                        If relationalTypeMapping.JsonValueReaderWriter IsNot Nothing AndAlso
                           relationalTypeMapping.JsonValueReaderWriter IsNot defaultInstance.JsonValueReaderWriter Then

                            CreateJsonValueReaderWriter(relationalTypeMapping.JsonValueReaderWriter, parameters, code)
                        Else
                            mainBuilder.Append("Type.Missing")
                        End If

                    Case "elementMapping"
                        If relationalTypeMapping.ElementTypeMapping IsNot Nothing AndAlso
                           relationalTypeMapping.ElementTypeMapping IsNot defaultInstance.ElementTypeMapping Then

                            Create(relationalTypeMapping.ElementTypeMapping, parameters)
                        Else
                            mainBuilder.Append("Type.Missing")
                        End If

                    Case Else
                        mainBuilder.Append("Type.Missing")
                End Select
            Next

            mainBuilder.
                Append("}), CoreTypeMapping)").
                DecrementIndent().
                DecrementIndent()

            Return True
        End Function

        Private Shared Sub GenerateArgument(name As String, value As String, builder As IndentedStringBuilder, ByRef firstArgument As Boolean)
            If Not firstArgument Then
                builder.AppendLine(","c)
            End If

            firstArgument = False
            builder.Append($"{name}:={value}")
        End Sub

        Private Shared Function IsSpatial(relationalTypeMapping As RelationalTypeMapping) As Boolean
            Dim elementTypeMapping = TryCast(relationalTypeMapping.ElementTypeMapping, RelationalTypeMapping)

            Return IsSpatialType(relationalTypeMapping.GetType()) OrElse
                   (elementTypeMapping IsNot Nothing AndAlso IsSpatialType(elementTypeMapping.GetType()))
        End Function

        Private Shared Function IsSpatialType(relationalTypeMappingType As Type) As Boolean
            Return (relationalTypeMappingType.IsGenericType AndAlso
                    relationalTypeMappingType.GetGenericTypeDefinition() = GetType(RelationalGeometryTypeMapping(Of ,))) OrElse
                   (relationalTypeMappingType.BaseType <> GetType(Object) AndAlso IsSpatialType(relationalTypeMappingType.BaseType))
        End Function

        Private Shared Sub CreateAnnotations(Of TAnnotatable As IAnnotatable)(
            Annotatable As TAnnotatable,
            process As Action(Of TAnnotatable, VisualBasicRuntimeAnnotationCodeGeneratorParameters),
            parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            process(
                Annotatable,
                parameters.
                    Cloner.
                    WithAnnotations(Annotatable.
                                        GetAnnotations().
                                        ToDictionary(Function(a) a.Name, Function(a) a.Value)).
                    WithIsRuntime(False).
                    Clone())

            process(
                Annotatable,
                parameters.
                    Cloner.
                    WithAnnotations(Annotatable.
                                        GetRuntimeAnnotations().
                                        ToDictionary(Function(a) a.Name, Function(a) a.Value)).
                    WithIsRuntime(True).
                    Clone())
        End Sub

        Private Shared Function Capitalize(str As String) As String
            Select Case str.Length
                Case 0 : Return str
                Case 1 : Return Char.ToUpperInvariant(str(0)).ToString()
                Case Else
                    If Char.IsUpper(str(0)) Then
                        Return str
                    End If

                    Return Char.ToUpperInvariant(str(0)) & str.Substring(1)
            End Select
        End Function

        Private Shared Sub AppendLiteral(storeObject As StoreObjectIdentifier, builder As IndentedStringBuilder, code As IVisualBasicHelper)

            builder.Append("StoreObjectIdentifier.")

            Select Case storeObject.StoreObjectType
                Case StoreObjectType.Table
                    builder.
                        Append("Table(").
                        Append(code.Literal(storeObject.Name)).
                        Append(", ").
                        Append(code.Literal(storeObject.Schema)).
                        Append(")"c)

                Case StoreObjectType.View
                    builder.
                        Append("View(").
                        Append(code.Literal(storeObject.Name)).
                        Append(", ").
                        Append(code.Literal(storeObject.Schema)).
                        Append(")"c)

                Case StoreObjectType.SqlQuery
                    builder.
                        Append("SqlQuery(").
                        Append(code.Literal(storeObject.Name)).
                        Append(")"c)

                Case StoreObjectType.Function
                    builder.
                        Append("DbFunction(").
                        Append(code.Literal(storeObject.Name)).
                        Append(")"c)

                Case StoreObjectType.InsertStoredProcedure
                    builder.
                        Append("InsertStoredProcedure(").
                        Append(code.Literal(storeObject.Name)).
                        Append(", ").
                        Append(code.Literal(storeObject.Schema)).
                        Append(")"c)

                Case StoreObjectType.DeleteStoredProcedure
                    builder.
                        Append("DeleteStoredProcedure(").
                        Append(code.Literal(storeObject.Name)).
                        Append(", ").
                        Append(code.Literal(storeObject.Schema)).
                        Append(")"c)

                Case StoreObjectType.UpdateStoredProcedure
                    builder.
                        Append("UpdateStoredProcedure(").
                        Append(code.Literal(storeObject.Name)).
                        Append(", ").
                        Append(code.Literal(storeObject.Schema)).
                        Append(")"c)

                Case Else
                    DebugAssert(True, "Unexpected StoreObjectType: " & storeObject.StoreObjectType)
            End Select
        End Sub
    End Class
End Namespace
