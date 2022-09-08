Imports System.Data
Imports System.Reflection
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal

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
                annotations.Remove(RelationalAnnotationNames.RelationalModel)
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
                        Append("Dim ").Append(sequencesVariable).AppendLine(" As New SortedDictionary(Of (String, String), ISequence)()")

                    For Each sequencePair In sequences
                        Create(sequencePair.Value, sequencesVariable, parameters)
                    Next

                    GenerateSimpleAnnotation(RelationalAnnotationNames.Sequences, sequencesVariable, parameters)
                End If
            End If

            MyBase.Generate(model, parameters)
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
            AddNamespace(GetType(ParameterDirection), parameters.Namespaces)

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
                Append(code.Literal(DirectCast(parameter.Direction, [Enum]))).Append(", ").
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
