Imports System.Reflection
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Migrations

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
                    Create(relationalModel,
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

        Private Sub Create(model As IRelationalModel,
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.AppendLine("Private Function CreateRelationalModel() As IRelationalModel")

            Using mainBuilder.Indent()

                parameters.Namespaces.Add(GetType(RelationalModel).Namespace)
                parameters.Namespaces.Add(GetType(Microsoft.EntityFrameworkCore.RelationalModelExtensions).Namespace)
                Dim relationalModelVariable = code.Identifier("relationalModel", parameters.ScopeVariables, capitalize:=False)

                mainBuilder.AppendLine($"Dim {relationalModelVariable} As New RelationalModel({parameters.TargetName})")

                Dim metadataVariables = New Dictionary(Of IAnnotatable, String)()

                Dim relationalModelParameters = parameters.Cloner.
                                                           WithTargetName(relationalModelVariable).
                                                           Clone

                AddNamespace(GetType(List(Of TableMapping)), parameters.Namespaces)

                ' All the mappings below are added in a way that preserves the order
                For Each entityType In model.Model.GetEntityTypes()
                    Dim entityTypeVariable = code.Identifier(entityType.ShortName(), parameters.ScopeVariables, capitalize:=False)
                    parameters.MainBuilder.
                                    AppendLine().
                                    AppendLine($"Dim {entityTypeVariable} = FindEntityType({code.Literal(entityType.Name)})")

                    metadataVariables.Add(entityType, entityTypeVariable)

                    For Each mapping In entityType.GetDefaultMappings()
                        Dim tableMappingsVariable = code.Identifier("defaultTableMappings", parameters.ScopeVariables, capitalize:=False)
                        mainBuilder.
                            AppendLine().
                            AppendLine($"Dim {tableMappingsVariable} As New List(Of TableMappingBase(Of ColumnMappingBase))()").
                            Append($"{entityTypeVariable}.SetRuntimeAnnotation(").
                            AppendLine($"{code.Literal(RelationalAnnotationNames.DefaultMappings)}, {tableMappingsVariable})")

                        Create(mapping, tableMappingsVariable, metadataVariables, relationalModelParameters)
                    Next

                    If entityType.GetTableMappings().Any() Then
                        Dim tableMappingsVariable = code.Identifier("tableMappings", parameters.ScopeVariables, capitalize:=False)
                        mainBuilder.
                            AppendLine().
                            AppendLine($"Dim {tableMappingsVariable} As New List(Of TableMapping)()").
                            Append($"{entityTypeVariable}.SetRuntimeAnnotation(").
                            AppendLine($"{code.Literal(RelationalAnnotationNames.TableMappings)}, {tableMappingsVariable})")

                        For Each mapping In entityType.GetTableMappings()
                            Create(mapping, tableMappingsVariable, metadataVariables, relationalModelParameters)
                        Next
                    End If

                    If entityType.GetViewMappings().Any() Then
                        Dim viewMappingsVariable = code.Identifier("viewMappings", parameters.ScopeVariables, capitalize:=False)

                        mainBuilder.
                            AppendLine().
                            AppendLine($"Dim {viewMappingsVariable} As New List(Of ViewMapping)()").
                            Append($"{entityTypeVariable}.SetRuntimeAnnotation(").
                            AppendLine($"{code.Literal(RelationalAnnotationNames.ViewMappings)}, {viewMappingsVariable})")

                        For Each mapping In entityType.GetViewMappings()
                            Create(mapping, viewMappingsVariable, metadataVariables, relationalModelParameters)
                        Next
                    End If

                    If entityType.GetSqlQueryMappings().Any() Then
                        Dim sqlQueryMappingsVariable = code.Identifier("sqlQueryMappings", parameters.ScopeVariables, capitalize:=False)

                        mainBuilder.
                            AppendLine().
                            AppendLine($"Dim {sqlQueryMappingsVariable} As New List(Of SqlQueryMapping)()").
                            Append($"{entityTypeVariable}.SetRuntimeAnnotation(").
                            AppendLine($"{code.Literal(RelationalAnnotationNames.SqlQueryMappings)}, {sqlQueryMappingsVariable})")

                        For Each mapping In entityType.GetSqlQueryMappings()
                            Create(mapping, sqlQueryMappingsVariable, metadataVariables, relationalModelParameters)
                        Next
                    End If

                    If entityType.GetFunctionMappings().Any() Then
                        Dim functionMappingsVariable = code.Identifier("functionMappings", parameters.ScopeVariables, capitalize:=False)

                        mainBuilder.
                            AppendLine().
                            AppendLine($"Dim {functionMappingsVariable} As New List(Of FunctionMapping)()").
                            Append($"{entityTypeVariable}.SetRuntimeAnnotation(").
                            AppendLine($"{code.Literal(RelationalAnnotationNames.FunctionMappings)}, {functionMappingsVariable})")

                        For Each mapping In entityType.GetFunctionMappings()
                            Create(mapping, functionMappingsVariable, metadataVariables, relationalModelParameters)
                        Next
                    End If

                    If entityType.GetDeleteStoredProcedureMappings().Any() Then
                        Dim deleteSprocMappingsVariable = code.Identifier("deleteSprocMappings", parameters.ScopeVariables, capitalize:=False)

                        mainBuilder.
                            AppendLine().
                            AppendLine($"Dim {deleteSprocMappingsVariable} As New List(Of StoredProcedureMapping)()").
                            Append($"{entityTypeVariable}.SetRuntimeAnnotation(").
                            AppendLine($"{code.Literal(RelationalAnnotationNames.DeleteStoredProcedureMappings)}, {deleteSprocMappingsVariable})")

                        For Each mapping In entityType.GetDeleteStoredProcedureMappings()
                            Create(mapping, deleteSprocMappingsVariable, StoreObjectType.DeleteStoredProcedure, metadataVariables, relationalModelParameters)
                        Next
                    End If

                    If entityType.GetInsertStoredProcedureMappings().Any() Then
                        Dim insertSprocMappingsVariable = code.Identifier("insertSprocMappings", parameters.ScopeVariables, capitalize:=False)

                        mainBuilder.
                            AppendLine().
                            AppendLine($"Dim {insertSprocMappingsVariable} As New List(Of StoredProcedureMapping)()").
                            Append($"{entityTypeVariable}.SetRuntimeAnnotation(").
                            AppendLine($"{code.Literal(RelationalAnnotationNames.InsertStoredProcedureMappings)}, {insertSprocMappingsVariable})")

                        For Each mapping In entityType.GetInsertStoredProcedureMappings()
                            Create(
                                mapping,
                                insertSprocMappingsVariable,
                                StoreObjectType.InsertStoredProcedure,
                                metadataVariables,
                                relationalModelParameters)
                        Next
                    End If

                    If entityType.GetUpdateStoredProcedureMappings().Any() Then
                        Dim updateSprocMappingsVariable = code.Identifier("updateSprocMappings", parameters.ScopeVariables, capitalize:=False)

                        mainBuilder.
                            AppendLine().
                            AppendLine($"Dim {updateSprocMappingsVariable} As New List(Of StoredProcedureMapping)()").
                            Append($"{entityTypeVariable}.SetRuntimeAnnotation(").
                            AppendLine($"{code.Literal(RelationalAnnotationNames.UpdateStoredProcedureMappings)}, {updateSprocMappingsVariable})")

                        For Each mapping In entityType.GetUpdateStoredProcedureMappings()
                            Create(
                                mapping,
                                updateSprocMappingsVariable,
                                StoreObjectType.UpdateStoredProcedure,
                                metadataVariables,
                                relationalModelParameters)
                        Next
                    End If
                Next

                For Each Table In model.Tables
                    For Each foreignKey In Table.ForeignKeyConstraints
                        Create(foreignKey, metadataVariables, parameters.Cloner.
                                                                         WithTargetName(metadataVariables(Table)).
                                                                         Clone)
                    Next
                Next

                For Each dbFunction In model.Model.GetDbFunctions()
                    If Not dbFunction.IsScalar Then
                        Continue For
                    End If

                    GetOrCreate(dbFunction.StoreFunction, metadataVariables, relationalModelParameters)
                Next

                CreateAnnotations(
                    model,
                    AddressOf Generate,
                    relationalModelParameters)

                mainBuilder.AppendLine($"Return {relationalModelVariable}.MakeReadOnly()")
            End Using

            mainBuilder.AppendLine("End Function")
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="model">The relational model to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(model As IRelationalModel, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Function GetOrCreate(Table As ITableBase,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) As String

            Dim tableVariable As String = Nothing
            If metadataVariables.TryGetValue(Table, tableVariable) Then
                Return tableVariable
            End If

            Dim code = VBCode
            tableVariable = code.Identifier(Table.Name & "TableBase", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(Table, tableVariable)
            Dim mainBuilder = parameters.MainBuilder

            mainBuilder.
                Append($"Dim {tableVariable} As New TableBase({code.Literal(Table.Name)}, {code.Literal(Table.Schema)}, ").
                AppendLine($"{parameters.TargetName})")

            Dim tableParameters = parameters.Cloner.
                                             WithTargetName(tableVariable).
                                             Clone

            For Each column In Table.Columns
                Create(column, metadataVariables, tableParameters)
            Next

            CreateAnnotations(Table,
                              AddressOf Generate,
                              tableParameters)

            mainBuilder.
                AppendLine($"{parameters.TargetName}.DefaultTables.Add({code.Literal(Table.Name)}, {tableVariable})")

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

        Private Function GetOrCreate(Table As ITable,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) As String

            Dim tableVariable As String = Nothing
            If metadataVariables.TryGetValue(Table, tableVariable) Then
                Return tableVariable
            End If

            Dim code = VBCode

            tableVariable = code.Identifier(Table.Name & "Table", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(Table, tableVariable)

            Dim mainBuilder = parameters.MainBuilder

            mainBuilder.
                Append($"Dim {tableVariable} As New Table({code.Literal(Table.Name)}, {code.Literal(Table.Schema)}, ").
                AppendLine($"{parameters.TargetName})")

            Dim tableParameters = parameters.Cloner.
                                             WithTargetName(tableVariable).
                                             Clone

            For Each column In Table.Columns
                Create(column, metadataVariables, tableParameters)
            Next

            For Each uniqueConstraint In Table.UniqueConstraints
                Create(uniqueConstraint, uniqueConstraint.Columns.Select(Function(c) metadataVariables(c)), tableParameters)
            Next

            For Each index In Table.Indexes
                Create(index, index.Columns.Select(Function(c) metadataVariables(c)), tableParameters)
            Next

            For Each trigger In Table.Triggers
                Dim entityTypeVariable = metadataVariables(trigger.EntityType)

                Dim triggerName = trigger.GetDatabaseName(StoreObjectIdentifier.Table(Table.Name, Table.Schema))

                mainBuilder.
                    Append($"{tableVariable}.Triggers.Add({code.Literal(triggerName)}, ").
                    AppendLine($"{entityTypeVariable}.FindDeclaredTrigger({code.Literal(trigger.ModelName)}))")
            Next

            CreateAnnotations(Table,
                              AddressOf Generate,
                              tableParameters)

            mainBuilder.
                Append($"{parameters.TargetName}.Tables.Add((").
                AppendLine($"{code.Literal(Table.Name)}, {code.Literal(Table.Schema)}), {tableVariable})")

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

        Private Function GetOrCreate(View As IView,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) As String

            Dim viewVariable As String = Nothing
            If metadataVariables.TryGetValue(View, viewVariable) Then
                Return viewVariable
            End If

            Dim code = VBCode

            viewVariable = code.Identifier(View.Name & "View", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(View, viewVariable)
            Dim mainBuilder = parameters.MainBuilder

            mainBuilder.
                Append($"Dim {viewVariable} As New View({code.Literal(View.Name)}, {code.Literal(View.Schema)}, ").
                AppendLine($"{parameters.TargetName})")

            Dim viewParameters = parameters.Cloner.
                                            WithTargetName(viewVariable).
                                            Clone

            For Each column In View.Columns
                Create(column, metadataVariables, viewParameters)
            Next

            CreateAnnotations(View,
                              AddressOf Generate,
                              viewParameters)

            mainBuilder.
                Append($"{parameters.TargetName}.Views.Add((").
                AppendLine($"{code.Literal(View.Name)}, {code.Literal(View.Schema)}), {viewVariable})")

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

        Private Function GetOrCreate(SqlQuery As ISqlQuery,
                                     metadataVariables As Dictionary(Of IAnnotatable, String),
                                     parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) As String

            Dim sqlQueryVariable As String = Nothing
            If metadataVariables.TryGetValue(SqlQuery, sqlQueryVariable) Then
                Return sqlQueryVariable
            End If

            Dim code = VBCode

            sqlQueryVariable = code.Identifier(SqlQuery.Name & "SqlQuery", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(SqlQuery, sqlQueryVariable)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.
                Append($"Dim {sqlQueryVariable} As New SqlQuery({code.Literal(SqlQuery.Name)}, {parameters.TargetName}, ").
                AppendLine($"{code.Literal(SqlQuery.Sql)})")

            Dim sqlQueryParameters = parameters.Cloner.
                                                WithTargetName(sqlQueryVariable).
                                                Clone

            For Each column In SqlQuery.Columns
                Create(column, metadataVariables, sqlQueryParameters)
            Next

            CreateAnnotations(SqlQuery,
                              AddressOf Generate,
                              sqlQueryParameters)

            mainBuilder.
                Append($"{parameters.TargetName}.Views.Add((").
                AppendLine($"{code.Literal(SqlQuery.Name)}, {code.Literal(SqlQuery.Schema)}), {sqlQueryVariable})")

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

            Dim code = VBCode

            Dim mainDbFunctionVariable = GetOrCreate([function].DbFunctions.First(), metadataVariables, parameters)
            functionVariable = code.Identifier([function].Name & "Function", parameters.ScopeVariables, capitalize:=False)
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
                    AppendLine($"{functionVariable}.DbFunctions.Add({code.Literal(dbFunction.ModelName)}, {dbFunctionVariable})")
            Next

            For Each parameter In [function].Parameters

                Dim parameterVariable = code.Identifier(parameter.Name & "FunctionParameter", parameters.ScopeVariables, capitalize:=False)
                metadataVariables.Add(parameter, parameterVariable)
                mainBuilder.AppendLine($"Dim {parameterVariable} = {functionVariable}.FindParameter({code.Literal(parameter.Name)})")

                CreateAnnotations(parameter,
                              AddressOf Generate,
                              parameters.Cloner.
                                         WithTargetName(parameterVariable).
                                         Clone)
            Next

            For Each column In [function].Columns
                Create(column, metadataVariables, functionParameters)
            Next

            CreateAnnotations([function],
                              AddressOf Generate,
                              functionParameters)

            mainBuilder.
                AppendLine($"{parameters.TargetName}.Functions.Add(").
                IncrementIndent().
                Append($"({code.Literal([function].Name)}, {code.Literal([function].Schema)}, ").
                AppendLine($"{code.Literal([function].DbFunctions.First().Parameters.Select(Function(p) p.StoreType).ToArray())}),").
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
                    AppendLine(")")
            Next

            CreateAnnotations(
                storeStoredProcedure,
                AddressOf Generate,
                sprocParameters)

            mainBuilder.
            Append($"{parameters.TargetName}.StoredProcedures.Add(").
            AppendLine($"({code.Literal(storeStoredProcedure.Name)}, {code.Literal(storeStoredProcedure.Schema)}), {storedProcedureVariable})")

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

        Private Sub Create(column As IColumnBase,
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

        Private Sub Create(Column As IColumn,
                           metadataVariables As Dictionary(Of IAnnotatable, String),
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode

            Dim columnVariable = code.Identifier(Column.Name & "Column", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(Column, columnVariable)

            Dim mainBuilder = parameters.MainBuilder
            Dim columnType = If(TypeOf Column Is JsonColumn, "JsonColumn", "Column")

            mainBuilder.
                Append($"Dim {columnVariable} As New {columnType}(").
                Append($"{code.Literal(Column.Name)}, {code.Literal(Column.StoreType)}, {parameters.TargetName})")

            GenerateIsNullableInitializer(Column.IsNullable, mainBuilder, code).
                 AppendLine().
                 AppendLine($"{parameters.TargetName}.Columns.Add({code.Literal(Column.Name)}, {columnVariable})")

            CreateAnnotations(
            Column,
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

        Private Sub Create(Column As IViewColumn,
                           metadataVariables As Dictionary(Of IAnnotatable, String),
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim columnVariable = code.Identifier(Column.Name & "ViewColumn", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(Column, columnVariable)
            Dim mainBuilder = parameters.MainBuilder

            Dim columnType = If(TypeOf Column Is JsonViewColumn, "JsonViewColumn", "ViewColumn")
            mainBuilder.
                Append($"Dim {columnVariable} As New {columnType}(").
                Append($"{code.Literal(Column.Name)}, {code.Literal(Column.StoreType)}, {parameters.TargetName})")

            GenerateIsNullableInitializer(Column.IsNullable, mainBuilder, code).
                AppendLine().
                AppendLine($"{parameters.TargetName}.Columns.Add({code.Literal(Column.Name)}, {columnVariable})")

            CreateAnnotations(
            Column,
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

        Private Sub Create(column As ISqlQueryColumn,
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

        Private Sub Create(column As IFunctionColumn,
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

        Private Sub Create(Column As IStoreStoredProcedureResultColumn,
                           metadataVariables As Dictionary(Of IAnnotatable, String),
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim columnVariable = code.Identifier(Column.Name & "FunctionColumn", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(Column, columnVariable)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.
                Append($"Dim {columnVariable} As New StoreStoredProcedureResultColumn({code.Literal(Column.Name)}, ").
                Append($"{code.Literal(Column.StoreType)}, {code.Literal(Column.Position)}, {parameters.TargetName})")

            GenerateIsNullableInitializer(Column.IsNullable, mainBuilder, code).
                AppendLine().
                AppendLine($"{parameters.TargetName}.AddResultColumn({columnVariable})")

            CreateAnnotations(
            Column,
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

        Private Sub Create(parameter As IStoreStoredProcedureParameter,
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
                    Append("}")
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

        Private Sub Create(uniqueConstraint As IUniqueConstraint,
                           columns As IEnumerable(Of String),
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim uniqueConstraintVariable = code.Identifier(uniqueConstraint.Name, parameters.ScopeVariables, capitalize:=False)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.
                Append("Dim ").Append(uniqueConstraintVariable).
                Append(" As New ").
                Append("UniqueConstraint").
                Append("(").
                Append(code.Literal(uniqueConstraint.Name)).
                Append(", ").
                Append(parameters.TargetName).
                Append(", ").
                Append("{").
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

        Private Sub Create(index As ITableIndex,
                           columns As IEnumerable(Of String),
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim indexVariable = code.Identifier(index.Name, parameters.ScopeVariables, capitalize:=False)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.
                Append("Dim ").Append(indexVariable).Append(" As New ").Append("TableIndex").AppendLine("(").
                Append(code.Literal(index.Name)).Append(", ").
                Append(parameters.TargetName).Append(", ").
                Append("{").AppendJoin(columns).Append("}, ").
                Append(code.Literal(index.IsUnique)).AppendLine(")")

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
                AppendLine($"{If(mappedIndex.Name Is Nothing,
                                code.Literal(mappedIndex.Properties.Select(Function(p) p.Name).ToArray()),
                                code.Literal(mappedIndex.Name))})").
                DecrementIndent()

                mainBuilder.AppendLine($"{indexVariable}.MappedIndexes.Add({tableIndexVariable})")
                mainBuilder.AppendLine($"RelationalModel.GetOrCreateTableIndexes({tableIndexVariable}).Add({indexVariable})")
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

        Private Sub Create(foreignKey As IForeignKeyConstraint,
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
                Append("{").AppendJoin(foreignKey.Columns.Select(Function(c) metadataVariables(c))).
                AppendLine("},").
                Append($"{principalTableVariable}.FindUniqueConstraint({code.Literal(foreignKey.PrincipalUniqueConstraint.Name)}), ").
                Append(code.Literal(DirectCast(foreignKey.OnDeleteAction, [Enum]))).
                AppendLine(")").
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

                mainBuilder.AppendLine($"{foreignKeyConstraintVariable}.MappedForeignKeys.Add({foreignKeyVariable})")
                mainBuilder.AppendLine($"RelationalModel.GetOrCreateForeignKeyConstraints({foreignKeyVariable}).Add({foreignKeyConstraintVariable})")
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

        Private Sub Create(tableMapping As ITableMappingBase,
                           tableMappingsVariable As String,
                           metadataVariables As Dictionary(Of IAnnotatable, String),
        parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            Dim entityType = tableMapping.EntityType
            Dim entityTypeVariable = metadataVariables(entityType)

            Dim table = tableMapping.Table
            Dim tableVariable = GetOrCreate(table, metadataVariables, parameters)
            Dim tableMappingVariable = code.Identifier(table.Name & "MappingBase", parameters.ScopeVariables, capitalize:=False)

            GenerateAddMapping(
                tableMapping,
                tableVariable,
                entityTypeVariable,
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
                    Append($"RelationalModel.CreateColumnMapping(").
                    Append($"DirectCast({tableVariable}.FindColumn({code.Literal(columnMapping.Column.Name)}), ColumnBase(Of ColumnMappingBase)), ").
                    Append($"{entityTypeVariable}.FindProperty({code.Literal(columnMapping.Property.Name)}), ").
                    Append(tableMappingVariable).AppendLine(")")
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

        Private Sub Create(tableMapping As Microsoft.EntityFrameworkCore.Metadata.ITableMapping,
                           tableMappingsVariable As String,
                           metadataVariables As Dictionary(Of IAnnotatable, String),
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            Dim entityType = tableMapping.EntityType
            Dim entityTypeVariable = metadataVariables(entityType)

            Dim table = tableMapping.Table
            Dim tableVariable = GetOrCreate(table, metadataVariables, parameters)
            Dim tableMappingVariable = code.Identifier(table.Name & "TableMapping", parameters.ScopeVariables, capitalize:=False)
            metadataVariables.Add(tableMapping, tableMappingVariable)

            GenerateAddMapping(
                tableMapping,
                tableVariable,
                entityTypeVariable,
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
                    Append($"RelationalModel.CreateColumnMapping({tableVariable}.FindColumn({code.Literal(columnMapping.Column.Name)}), ").
                    Append($"{entityTypeVariable}.FindProperty({code.Literal(columnMapping.Property.Name)}), ").
                    Append(tableMappingVariable).AppendLine(")")
            Next
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="tableMapping">The table mapping to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Public Overridable Overloads Sub Generate(tableMapping As Microsoft.EntityFrameworkCore.Metadata.ITableMapping, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            GenerateSimpleAnnotations(parameters)
        End Sub

        Private Sub Create(viewMapping As IViewMapping,
                           viewMappingsVariable As String,
                           metadataVariables As Dictionary(Of IAnnotatable, String),
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            Dim entityType = viewMapping.EntityType
            Dim entityTypeVariable = metadataVariables(entityType)

            Dim view = viewMapping.View
            Dim viewVariable = GetOrCreate(view, metadataVariables, parameters)
            Dim viewMappingVariable = code.Identifier(view.Name & "ViewMapping", parameters.ScopeVariables, capitalize:=False)

            GenerateAddMapping(
                viewMapping,
                viewVariable,
                entityTypeVariable,
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
                Append($"RelationalModel.CreateViewColumnMapping({viewVariable}.FindColumn({code.Literal(columnMapping.Column.Name)}), ").
                Append($"{entityTypeVariable}.FindProperty({code.Literal(columnMapping.Property.Name)}), ").
                Append(viewMappingVariable).AppendLine(")")
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

        Private Sub Create(sqlQueryMapping As ISqlQueryMapping,
                           sqlQueryMappingsVariable As String,
                           metadataVariables As Dictionary(Of IAnnotatable, String),
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            Dim entityType = sqlQueryMapping.EntityType
            Dim entityTypeVariable = metadataVariables(entityType)

            Dim sqlQuery = sqlQueryMapping.SqlQuery
            Dim sqlQueryVariable = GetOrCreate(sqlQuery, metadataVariables, parameters)
            Dim sqlQueryMappingVariable = code.Identifier(sqlQuery.Name & "SqlQueryMapping", parameters.ScopeVariables, capitalize:=False)

            GenerateAddMapping(
                sqlQueryMapping,
                sqlQueryVariable,
                entityTypeVariable,
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
                    Append($"RelationalModel.CreateSqlQueryColumnMapping({sqlQueryVariable}.FindColumn({code.Literal(columnMapping.Column.Name)}), ").
                    Append($"{entityTypeVariable}.FindProperty({code.Literal(columnMapping.Property.Name)}), ").
                    Append(sqlQueryMappingVariable).
                    AppendLine(")")
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

        Private Sub Create(functionMapping As IFunctionMapping,
                           functionMappingsVariable As String,
                           metadataVariables As Dictionary(Of IAnnotatable, String),
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            Dim entityType = functionMapping.EntityType
            Dim entityTypeVariable = metadataVariables(entityType)

            Dim storeFunction = functionMapping.StoreFunction
            Dim functionVariable = GetOrCreate(storeFunction, metadataVariables, parameters)
            Dim dbFunctionVariable = metadataVariables(functionMapping.DbFunction)
            Dim functionMappingVariable = code.Identifier(storeFunction.Name & "FunctionMapping", parameters.ScopeVariables, capitalize:=False)

            GenerateAddMapping(
                functionMapping,
                functionVariable,
                entityTypeVariable,
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
                    Append($"RelationalModel.CreateFunctionColumnMapping({functionVariable}.FindColumn({code.Literal(columnMapping.Column.Name)}), ").
                    Append($"{entityTypeVariable}.FindProperty({code.Literal(columnMapping.Property.Name)}), ").
                    Append(functionMappingVariable).AppendLine(")")
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

        Private Sub Create(sprocMapping As IStoredProcedureMapping,
                           sprocMappingsVariable As String,
                           storeObjectType As StoreObjectType,
                           metadataVariables As Dictionary(Of IAnnotatable, String),
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            Dim entityType = sprocMapping.EntityType
            Dim entityTypeVariable = metadataVariables(entityType)

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
                entityTypeVariable,
                sprocMappingsVariable,
                sprocMappingVariable,
                "StoredProcedureMapping",
                parameters,
                $"{sprocSnippet}, {If(tableMappingVariable, "null")}, ")

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
                    Append($"{entityTypeVariable}.FindProperty({code.Literal(parameterMapping.Property.Name)}), ").
                    Append(sprocMappingVariable).AppendLine(")")
            Next

            For Each columnMapping In sprocMapping.ResultColumnMappings
                mainBuilder.
                Append($"RelationalModel.CreateStoredProcedureResultColumnMapping({metadataVariables(columnMapping.StoreResultColumn)}, ").
                Append($"{sprocVariable}.FindResultColumn({code.Literal(columnMapping.ResultColumn.Name)}), ").
                Append($"{entityTypeVariable}.FindProperty({code.Literal(columnMapping.Property.Name)}), ").
                Append(sprocMappingVariable).AppendLine(")")
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
            Dim entityType = tableMapping.EntityType

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
                    AppendLine("}")
            Else
                mainBuilder.AppendLine(")")
            End If

            Dim table = tableMapping.Table
            Dim isOptional = table.IsOptional(entityType)

            mainBuilder.
                AppendLine($"{tableVariable}.AddEntityTypeMapping({tableMappingVariable}, {code.Literal(isOptional)})").
                AppendLine($"{tableMappingsVariable}.Add({tableMappingVariable})")

            For Each internalForeignKey In table.GetRowInternalForeignKeys(entityType)
                mainBuilder.
                    Append(tableVariable).Append($".AddRowInternalForeignKey({entityTypeVariable}, ").
                    AppendLine($"RelationalModel.GetForeignKey(Me,").
                    IncrementIndent().
                    AppendLine($"{code.Literal(internalForeignKey.DeclaringEntityType.Name)},").
                    AppendLine($"{code.Literal(internalForeignKey.Properties.Select(Function(p) p.Name).ToArray())},").
                    AppendLine($"{code.Literal(internalForeignKey.PrincipalEntityType.Name)},").
                    AppendLine($"{code.Literal(internalForeignKey.PrincipalKey.Properties.Select(Function(p) p.Name).ToArray())}))").
                    DecrementIndent()
            Next
        End Sub

        Private Sub Create([function] As IDbFunction,
                           functionsVariable As String,
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            If [function].Translation IsNot Nothing Then
                Throw New InvalidOperationException(RelationalStrings.CompiledModelFunctionTranslation([function].Name))
            End If

            If TypeOf [function] Is IConventionDbFunction Then
                Dim conventionFunction = DirectCast([function], IConventionDbFunction)

                If conventionFunction.GetTypeMappingConfigurationSource() IsNot Nothing Then
                    Throw New InvalidOperationException(RelationalStrings.CompiledModelFunctionTypeMapping(
                        [function].Name, "Customize()", parameters.ClassName))
                End If
            End If

            AddNamespace([function].ReturnType, parameters.Namespaces)

            Dim code = VBCode

            Dim functionVariable = code.Identifier(
            If([function].MethodInfo?.Name, [function].Name), parameters.ScopeVariables, capitalize:=False)

            Dim mainBuilder = parameters.MainBuilder
            With mainBuilder
                .Append("Dim ").Append(functionVariable).AppendLine(" As New RuntimeDbFunction(").IncrementIndent()
                .Append(code.Literal([function].ModelName)).AppendLine(","c)
                .Append(parameters.TargetName).AppendLine(","c)
                .Append(code.Literal([function].ReturnType)).AppendLine(","c)
                .Append(code.Literal([function].Name))
            End With

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
                With mainBuilder
                    .AppendLine(","c)
                    .Append("methodInfo:=").Append(code.Literal(method.DeclaringType)).AppendLine(".GetMethod(").IncrementIndent()
                    .Append(code.Literal(method.Name)).AppendLine(","c)
                    .Append(If(method.IsPublic, "BindingFlags.Public", "BindingFlags.NonPublic"))
                    .Append(If(method.IsStatic, " Or BindingFlags.Static", " Or BindingFlags.Instance"))
                    .AppendLine(" Or BindingFlags.DeclaredOnly,")
                    .AppendLine("Nothing,")
                    .Append("{"c).Append(String.Join(", ", method.GetParameters().Select(Function(p) code.Literal(p.ParameterType)))).AppendLine("},")
                    .Append("Nothing)").DecrementIndent()
                End With
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

            CreateAnnotations([function],
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

        Private Sub Create(parameter As IDbFunctionParameter, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            If TypeOf parameter Is IConventionDbFunctionParameter Then
                Dim conventionParameter = DirectCast(parameter, IConventionDbFunctionParameter)
                If conventionParameter.GetTypeMappingConfigurationSource() IsNot Nothing Then
                    Throw New InvalidOperationException(
                RelationalStrings.CompiledModelFunctionParameterTypeMapping(
                    parameter.Function.Name, parameter.Name, "Customize()", parameters.ClassName))
                End If
            End If

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

            CreateAnnotations(parameter,
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

        Private Sub Create(aSequence As ISequence, sequencesVariable As String, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
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

            CreateAnnotations(aSequence,
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

        Private Sub Create(fragment As IEntityTypeMappingFragment,
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

            CreateAnnotations(fragment,
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

        Private Sub Create(storedProcedure As IStoredProcedure, sprocVariable As String, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
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

            CreateAnnotations(storedProcedure,
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

        Private Sub Create(parameter As IStoredProcedureParameter, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

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
                AppendLine(")").DecrementIndent()

            CreateAnnotations(parameter,
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

        Private Sub Create(resultColumn As IStoredProcedureResultColumn, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim code = VBCode
            Dim mainBuilder = parameters.MainBuilder
            Dim resultColumnVariable = code.Identifier(resultColumn.Name, parameters.ScopeVariables, capitalize:=False)

            mainBuilder.
                Append("Dim ").Append(resultColumnVariable).Append(" = ").
                Append(parameters.TargetName).AppendLine(".AddResultColumn(").IncrementIndent().
                Append(code.Literal(resultColumn.Name)).Append(", ").
                Append(code.Literal(resultColumn.ForRowsAffected)).Append(", ").
                Append(code.Literal(resultColumn.PropertyName)).
                AppendLine(")").DecrementIndent()

            CreateAnnotations(resultColumn,
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

        Private Sub Create([overrides] As IRelationalPropertyOverrides,
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

            CreateAnnotations([overrides],
                              AddressOf Generate,
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

        Private Shared Sub CreateAnnotations(Of TAnnotatable As IAnnotatable)(
            Annotatable As TAnnotatable,
            process As Action(Of TAnnotatable, VisualBasicRuntimeAnnotationCodeGeneratorParameters),
            parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            process(
                Annotatable,
                parameters.
                    Cloner.
                    WithAnnotations(Annotatable.GetAnnotations().
                                        ToDictionary(Function(a) a.Name, Function(a) a.Value)).
                    WithIsRuntime(False).
                    Clone())

            process(
                Annotatable,
                parameters.
                    Cloner.
                    WithAnnotations(Annotatable.GetRuntimeAnnotations().
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
