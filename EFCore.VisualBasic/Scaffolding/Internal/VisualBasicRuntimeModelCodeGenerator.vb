Imports System.Reflection
Imports System.Text
Imports EntityFrameworkCore.VisualBasic.Design
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Internal
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Scaffolding

Namespace Scaffolding.Internal

    ''' <summary>
    '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
    '''     the same compatibility standards As public APIs. It may be changed Or removed without notice in
    '''     any release. You should only use it directly in your code with extreme caution And knowing that
    '''     doing so can result in application failures when updating to a New Entity Framework Core release.
    ''' </summary>
    Public Class VisualBasicRuntimeModelCodeGenerator
        Implements ICompiledModelCodeGenerator

        Private ReadOnly _code As IVisualBasicHelper
        Private ReadOnly _annotationCodeGenerator As IVisualBasicRuntimeAnnotationCodeGenerator

        Private Const FileExtension = ".vb"
        Private Const ModelSuffix = "Model"
        Private Const ModelBuilderSuffix = "ModelBuilder"
        Private Const EntityTypeSuffix = "EntityType"

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards As public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Public Sub New(annotationCodeGenerator As IVisualBasicRuntimeAnnotationCodeGenerator,
                       vbHelper As IVisualBasicHelper)

            _annotationCodeGenerator = NotNull(annotationCodeGenerator, NameOf(annotationCodeGenerator))
            _code = NotNull(vbHelper, NameOf(vbHelper))
        End Sub

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards As public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Public Overridable ReadOnly Property Language As String Implements ILanguageBasedService.Language
            Get
                Return "VB"
            End Get
        End Property

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards As public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Public Overridable Function GenerateModel(model As IModel,
                                                  options As CompiledModelCodeGenerationOptions) As IReadOnlyCollection(Of ScaffoldedFile) _
            Implements ICompiledModelCodeGenerator.GenerateModel

            NotNull(model, NameOf(model))
            NotNull(options, NameOf(options))

            Dim scaffoldedFiles As New List(Of ScaffoldedFile)
            Dim modelCode = CreateModel(options.ModelNamespace, options.ContextType)
            Dim modelFileName = options.ContextType.ShortDisplayName() & ModelSuffix & FileExtension

            scaffoldedFiles.Add(New ScaffoldedFile With {
                                .Path = modelFileName,
                                .Code = modelCode})

            Dim entityTypeIds As New Dictionary(Of IEntityType, (Variable As String, [Class] As String))
            Dim modelBuilderCode = CreateModelBuilder(model, options.ModelNamespace, options.ContextType, entityTypeIds)
            Dim modelBuilderFileName = options.ContextType.ShortDisplayName() & ModelBuilderSuffix & FileExtension

            scaffoldedFiles.Add(New ScaffoldedFile With {
                                .Path = modelBuilderFileName,
                                .Code = modelBuilderCode})

            For Each entityTypeId In entityTypeIds
                Dim entityType = entityTypeId.Key
                Dim namePair = entityTypeId.Value

                Dim generatedCode = GenerateEntityType(entityType, options.ModelNamespace, namePair.Class)

                Dim entityTypeFileName = namePair.Class & FileExtension
                scaffoldedFiles.Add(
                    New ScaffoldedFile With {
                        .Path = entityTypeFileName,
                        .Code = generatedCode})
            Next

            Return scaffoldedFiles
        End Function

        Private Shared Function GenerateHeader(namespaces As SortedSet(Of String), currentNamespace As String) As String

            For i = 0 To currentNamespace.Length - 1
                If currentNamespace(i) <> "."c Then
                    Continue For
                End If
                namespaces.Remove(currentNamespace.Substring(0, i))
            Next

            namespaces.Remove(currentNamespace)

            Dim builder = New StringBuilder()
            builder.AppendLine("' <auto-generated />")

            For Each [Namespace] In namespaces
                builder.
                    Append("Imports ").
                    AppendLine([Namespace])
            Next

            builder.AppendLine()

            Return builder.ToString()
        End Function

        Private Function CreateModel([namespace] As String,
                                     contextType As Type) As String

            Dim mainBuilder = New IndentedStringBuilder()
            Dim namespaces = New SortedSet(Of String)(New NamespaceComparer()) From {
                GetType(RuntimeModel).Namespace,
                GetType(DbContextAttribute).Namespace,
                "Microsoft.VisualBasic"
            }

            AddNamespace(contextType, namespaces)

            If Not String.IsNullOrEmpty([namespace]) Then
                mainBuilder.
                    Append("Namespace ").
                    AppendLine(_code.Namespace([namespace]))

                mainBuilder.Indent()
            End If

            Dim className = _code.Identifier(contextType.ShortDisplayName()) & ModelSuffix
            mainBuilder.Append("<DbContext(GetType(").
                        Append(_code.Reference(contextType)).AppendLine("))>").
                        Append("Public Partial Class ").
                        AppendLine(className)

            Using mainBuilder.Indent()
                mainBuilder.
                    Append("Inherits ").AppendLine(NameOf(RuntimeModel)).
                    AppendLine().
                    AppendLine("Private Shared ReadOnly _useOldBehavior31751 As Boolean").
                    AppendLine().
                    Append("Private Shared ").Append("_Instance As ").AppendLine(className).
                    AppendLine("Public Shared ReadOnly Property Instance As IModel").
                    IncrementIndent.
                    AppendLine("Get").
                    IncrementIndent.
                    AppendLine("Return _Instance").
                    DecrementIndent.
                    AppendLine("End Get").
                    DecrementIndent.
                    AppendLine("End Property").
                    AppendLine().
                    AppendLine("Shared Sub New()").
                    IncrementIndent().
                    AppendLine("Dim enabled31751 As Boolean").
                    AppendLine("_useOldBehavior31751 = System.AppContext.TryGetSwitch(""Microsoft.EntityFrameworkCore.Issue31751"", enabled31751) AndAlso enabled31751").
                    AppendLine().
                    AppendLines(
$"Dim model As New {className}()
If _useOldBehavior31751 Then
    model.Initialize()
Else
    Dim thread = New System.Threading.Thread(Sub() model.Initialize(), 10 * 1024 * 1024)
    thread.Start()
    thread.Join()
End If

model.Customize()
_Instance = model").
                    DecrementIndent().
                    AppendLine("End Sub").
                    AppendLine().
                    AppendLine("Partial Private Sub Initialize()").
                    AppendLine("End Sub").
                    AppendLine().
                    AppendLine("Partial Private Sub Customize()").
                    AppendLine("End Sub")
            End Using

            mainBuilder.
                    AppendLine("End Class")

            If Not String.IsNullOrEmpty([namespace]) Then
                mainBuilder.DecrementIndent()
                mainBuilder.AppendLine("End Namespace")
            End If

            Return GenerateHeader(namespaces, [namespace]) & mainBuilder.ToString()
        End Function

        Private Function CreateModelBuilder(model As IModel,
                                            [namespace] As String,
                                            contextType As Type,
                                            entityTypeIds As Dictionary(Of IEntityType, (Variable As String, [Class] As String))) As String

            Dim mainBuilder = New IndentedStringBuilder()
            Dim methodBuilder = New IndentedStringBuilder()
            Dim namespaces = New SortedSet(Of String)(New NamespaceComparer()) From {
                GetType(RuntimeModel).Namespace,
                GetType(DbContextAttribute).Namespace,
                "Microsoft.VisualBasic"
            }

            If Not String.IsNullOrEmpty([namespace]) Then
                mainBuilder.
                    Append("Namespace ").
                    AppendLine(_code.Namespace([namespace]))

                mainBuilder.Indent()
            End If

            Dim className = _code.Identifier(contextType.ShortDisplayName()) & ModelSuffix
            mainBuilder.
                Append("Public Partial Class ").
                AppendLine(className).
                AppendLine()

            Using mainBuilder.Indent()
                mainBuilder.
                    AppendLine("Private Sub Initialize()")

                Using mainBuilder.Indent()
                    Dim entityTypes = model.GetEntityTypesInHierarchicalOrder()
                    Dim variables As New HashSet(Of String)

                    Dim anyEntityTypes = False
                    For Each entityType As IEntityType In entityTypes
                        anyEntityTypes = True
                        Dim variableName = _code.Identifier(entityType.ShortName(), variables, capitalize:=False)

                        Dim firstChar = If(variableName(0) = "["c, variableName(1), variableName(0))
                        Dim entityClassName = ""

                        If firstChar = "_"c Then
                            entityClassName = EntityTypeSuffix & variableName.Substring(1)
                        Else
                            Dim NewName = variableName
                            If NewName(0) = "["c Then
                                NewName = NewName.Substring(1, NewName.Length - 2)
                            End If
                            entityClassName = Char.ToUpperInvariant(firstChar) & NewName.Substring(1) & EntityTypeSuffix
                        End If

                        entityTypeIds(entityType) = (variableName, entityClassName)

                        mainBuilder.
                            Append("Dim ").
                            Append(variableName).
                            Append(" = ").
                            Append(entityClassName).
                            Append(".Create(Me")

                        If entityType.BaseType IsNot Nothing Then
                            mainBuilder.
                                Append(", ").
                                Append(entityTypeIds(entityType.BaseType).Variable)
                        End If

                        mainBuilder.
                            AppendLine(")"c)
                    Next

                    If anyEntityTypes Then
                        mainBuilder.AppendLine()
                    End If

                    Dim anyForeignKeys = False
                    For Each entityTypeId In entityTypeIds

                        Dim entityType = entityTypeId.Key
                        Dim namePair = entityTypeId.Value

                        Dim variableName = namePair.Variable
                        Dim entityClassName = namePair.Class

                        Dim foreignKeyNumber = 1

                        For Each foreignKey As IForeignKey In entityType.GetDeclaredForeignKeys()
                            anyForeignKeys = True
                            Dim principalVariable = entityTypeIds(foreignKey.PrincipalEntityType).Variable

                            mainBuilder.
                                Append(entityClassName).
                                Append(".CreateForeignKey").
                                Append(foreignKeyNumber.ToString()).
                                Append("("c).
                                Append(variableName).
                                Append(", ").
                                Append(principalVariable).
                                AppendLine(")"c)

                            foreignKeyNumber += 1
                        Next
                    Next

                    If anyForeignKeys Then
                        mainBuilder.
                            AppendLine()
                    End If

                    Dim anySkipNavigations = False
                    For Each entityTypeId In entityTypeIds

                        Dim entityType = entityTypeId.Key
                        Dim namePair = entityTypeId.Value

                        Dim variableName = namePair.Variable
                        Dim entityClassName = namePair.Class

                        Dim navigationNumber = 1

                        For Each navigation As ISkipNavigation In entityType.GetDeclaredSkipNavigations()
                            anySkipNavigations = True
                            Dim targetVariable = entityTypeIds(navigation.TargetEntityType).Variable
                            Dim joinVariable = entityTypeIds(navigation.JoinEntityType).Variable

                            mainBuilder.
                                Append(entityClassName).
                                Append(".CreateSkipNavigation").
                                Append(navigationNumber.ToString()).
                                Append("("c).
                                Append(variableName).
                                Append(", ").
                                Append(targetVariable).
                                Append(", ").
                                Append(joinVariable).
                                AppendLine(")"c)

                            navigationNumber += 1
                        Next
                    Next

                    If anySkipNavigations Then
                        mainBuilder.AppendLine()
                    End If

                    For Each entityTypeId In entityTypeIds

                        Dim entityType = entityTypeId.Key
                        Dim namePair = entityTypeId.Value

                        Dim variableName = namePair.Variable
                        Dim entityClassName = namePair.Class

                        mainBuilder.
                            Append(entityClassName).
                            Append(".CreateAnnotations").
                            Append("("c).
                            Append(variableName).
                            AppendLine(")"c)
                    Next

                    If anyEntityTypes Then
                        mainBuilder.AppendLine()
                    End If

                    Dim parameters As New VisualBasicRuntimeAnnotationCodeGeneratorParameters(
                            "Me",
                            className,
                            mainBuilder,
                            methodBuilder,
                            namespaces,
                            variables)

                    For Each typeConfiguration In model.GetTypeMappingConfigurations()
                        Create(typeConfiguration, parameters)
                    Next

                    CreateAnnotations(model, AddressOf _annotationCodeGenerator.Generate, parameters)

                End Using

                mainBuilder.
                    AppendLine("End Sub")

                Dim methods = methodBuilder.ToString()
                If Not String.IsNullOrEmpty(methods) Then
                    mainBuilder.
                        AppendLine().
                        AppendLines(methods)
                End If
            End Using

            mainBuilder.
                AppendLine("End Class")

            If Not String.IsNullOrEmpty([namespace]) Then
                mainBuilder.DecrementIndent()
                mainBuilder.AppendLine("End Namespace")
            End If

            Return GenerateHeader(namespaces, [namespace]) & mainBuilder.ToString
        End Function

        Private Sub Create(typeConfiguration As ITypeMappingConfiguration,
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim variableName = _code.Identifier("typeConfig", parameters.ScopeVariables, capitalize:=False)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.Append("Dim ").Append(variableName).Append(" = ").Append(parameters.TargetName).AppendLine(".AddTypeMappingConfiguration(").
            IncrementIndent().
            Append(_code.Literal(typeConfiguration.ClrType))

            AddNamespace(typeConfiguration.ClrType, parameters.Namespaces)

            If typeConfiguration.GetMaxLength().HasValue Then
                mainBuilder.AppendLine(","c).
                Append("maxLength:=").
                Append(_code.Literal(typeConfiguration.GetMaxLength()))
            End If

            If typeConfiguration.IsUnicode().HasValue Then
                mainBuilder.AppendLine(","c).
                Append("unicode:=").
                Append(_code.Literal(typeConfiguration.IsUnicode()))
            End If

            If typeConfiguration.GetPrecision().HasValue Then
                mainBuilder.AppendLine(","c).
                Append("precision:=").
                Append(_code.Literal(typeConfiguration.GetPrecision()))
            End If

            If typeConfiguration.GetScale().HasValue Then
                mainBuilder.AppendLine(","c).
                Append("scale:=").
                Append(_code.Literal(typeConfiguration.GetScale()))
            End If

            Dim providerClrType = typeConfiguration.GetProviderClrType()
            If providerClrType IsNot Nothing Then
                AddNamespace(providerClrType, parameters.Namespaces)

                mainBuilder.AppendLine(","c).
                Append("providerPropertyType:=").
                Append(_code.Literal(providerClrType))
            End If

            Dim valueConverterType = CType(typeConfiguration(CoreAnnotationNames.ValueConverterType), Type)
            If valueConverterType IsNot Nothing Then
                AddNamespace(valueConverterType, parameters.Namespaces)

                mainBuilder.AppendLine(","c).
                Append("valueConverter:=New ").
                Append(_code.Reference(valueConverterType)).
                Append("()")
            End If

            mainBuilder.
                AppendLine(")"c).
                DecrementIndent()

            CreateAnnotations(
                typeConfiguration,
                AddressOf _annotationCodeGenerator.Generate,
                parameters.Cloner.
                           WithTargetName(variableName).
                           Clone())

            mainBuilder.AppendLine()
        End Sub

        Private Function GenerateEntityType(entityType As IEntityType, [namespace] As String, className As String) As String
            Dim mainBuilder As New IndentedStringBuilder()
            Dim methodBuilder As New IndentedStringBuilder()
            Dim namespaces As New SortedSet(Of String)(New NamespaceComparer()) From {
                GetType(BindingFlags).Namespace,
                GetType(MethodInfo).Namespace,
                GetType(RuntimeEntityType).Namespace,
                "Microsoft.VisualBasic"
            }

            If Not String.IsNullOrEmpty([namespace]) Then
                mainBuilder.
                    Append("Namespace ").
                    AppendLine(_code.Namespace([namespace]))

                mainBuilder.Indent()
            End If

            mainBuilder.
                Append("Friend Partial Class ").
                AppendLine(className).
                AppendLine()

            Using mainBuilder.Indent()
                CreateEntityType(entityType, mainBuilder, methodBuilder, namespaces, className)

                For Each complexProperty In entityType.GetDeclaredComplexProperties()
                    CreateComplexProperty(complexProperty, mainBuilder, methodBuilder, namespaces, className)
                Next

                Dim foreignKeyNumber = 1
                For Each foreignKey As IForeignKey In entityType.GetDeclaredForeignKeys()
                    CreateForeignKey(foreignKey, foreignKeyNumber, mainBuilder, methodBuilder, namespaces, className)
                    foreignKeyNumber += 1
                Next

                Dim navigationNumber = 1
                For Each navigation As ISkipNavigation In entityType.GetDeclaredSkipNavigations()
                    CreateSkipNavigation(navigation, navigationNumber, mainBuilder, methodBuilder, namespaces, className)
                    navigationNumber += 1
                Next

                CreateAnnotations(entityType,
                                  mainBuilder,
                                  methodBuilder,
                                  namespaces,
                                  className)
            End Using

            mainBuilder.AppendLine("End Class")

            If Not String.IsNullOrEmpty([namespace]) Then
                mainBuilder.DecrementIndent()
                mainBuilder.AppendLine("End Namespace")
            End If

            Return GenerateHeader(namespaces, [namespace]) & mainBuilder.ToString() & methodBuilder.ToString()
        End Function

        Private Sub CreateEntityType(entityType As IEntityType,
                                     mainBuilder As IndentedStringBuilder,
                                     methodBuilder As IndentedStringBuilder,
                                     namespaces As SortedSet(Of String),
                                     className As String)

            mainBuilder.
                Append("Public Shared Function Create").
                Append("(model As RuntimeModel, ").
                AppendLine("Optional baseEntityType As RuntimeEntityType = Nothing) As RuntimeEntityType")

            Using mainBuilder.Indent()

                Const entityTypeVariable = "entityType"
                Dim variables = New HashSet(Of String) From {
                    "model",
                    "baseEntityType",
                    entityTypeVariable
                }

                Dim parameters = New VisualBasicRuntimeAnnotationCodeGeneratorParameters(
                    entityTypeVariable,
                    className,
                    mainBuilder,
                    methodBuilder,
                    namespaces,
                    variables)

                Create(entityType, parameters)

                Dim propertyVariables As New Dictionary(Of IProperty, String)
                For Each prop In entityType.GetDeclaredProperties()
                    Create(prop, propertyVariables, parameters)
                Next

                For Each prop In entityType.GetDeclaredServiceProperties()
                    Create(prop, parameters)
                Next

                For Each complexProperty In entityType.GetDeclaredComplexProperties()
                    mainBuilder.
                        Append(_code.Identifier(complexProperty.Name, capitalize:=True)).
                        Append("ComplexProperty").
                        Append(".Create").
                        Append("("c).
                        Append(entityTypeVariable).
                        AppendLine(")"c)
                Next

                For Each aKey In entityType.GetDeclaredKeys()
                    Create(aKey, propertyVariables, parameters)
                Next

                For Each index In entityType.GetDeclaredIndexes()
                    Create(index, propertyVariables, parameters)
                Next

                For Each trigger In entityType.GetDeclaredTriggers()
                    Create(trigger, parameters)
                Next

                mainBuilder.
                    Append("Return ").
                    AppendLine(entityTypeVariable)
            End Using

            mainBuilder.
                AppendLine("End Function")
        End Sub

        Private Sub Create(entityType As IEntityType,
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim runTimeEntityType = TryCast(entityType, IRuntimeEntityType)

            If (runTimeEntityType.ConstructorBinding IsNot Nothing AndAlso
               ((runTimeEntityType?.GetConstructorBindingConfigurationSource()).OverridesStrictly(ConfigurationSource.Convention) OrElse
                   TypeOf runTimeEntityType.ConstructorBinding Is FactoryMethodBinding)) OrElse
                 (runTimeEntityType?.ServiceOnlyConstructorBinding IsNot Nothing AndAlso
                     (runTimeEntityType.GetServiceOnlyConstructorBindingConfigurationSource().OverridesStrictly(ConfigurationSource.Convention) OrElse
                        TypeOf runTimeEntityType.ServiceOnlyConstructorBinding Is FactoryMethodBinding)) Then

                Throw New InvalidOperationException(DesignStrings.CompiledModelConstructorBinding(
                    runTimeEntityType.ShortName(), "Customize()", parameters.ClassName))
            End If

            If runTimeEntityType.GetQueryFilter() IsNot Nothing Then
                Throw New InvalidOperationException(DesignStrings.CompiledModelQueryFilter(runTimeEntityType.ShortName()))
            End If

#Disable Warning BC40000 ' Type or member is obsolete
            If runTimeEntityType.GetDefiningQuery() IsNot Nothing Then
                Throw New InvalidOperationException(DesignStrings.CompiledModelDefiningQuery(runTimeEntityType.ShortName()))
            End If
#Enable Warning BC40000 ' Type or member is obsolete

            AddNamespace(runTimeEntityType.ClrType, parameters.Namespaces)

            Dim mainBuilder = parameters.MainBuilder

            mainBuilder.
                Append("Dim ").
                Append(parameters.TargetName).
                AppendLine(" = model.AddEntityType(").
                IncrementIndent().
                Append(_code.Literal(runTimeEntityType.Name)).
                AppendLine(","c).
                Append(_code.Literal(runTimeEntityType.ClrType)).
                AppendLine(","c).
                Append("baseEntityType")

            If runTimeEntityType.HasSharedClrType Then
                mainBuilder.
                    AppendLine(","c).
                    Append("sharedClrType:=").
                    Append(_code.Literal(runTimeEntityType.HasSharedClrType))
            End If

            Dim discriminatorProperty = runTimeEntityType.GetDiscriminatorPropertyName()

            If discriminatorProperty IsNot Nothing Then
                mainBuilder.
                    AppendLine(","c).
                    Append("discriminatorProperty:=").
                    Append(_code.Literal(discriminatorProperty))
            End If

            Dim changeTrackingStrat As ChangeTrackingStrategy = runTimeEntityType.GetChangeTrackingStrategy()

            If changeTrackingStrat <> ChangeTrackingStrategy.Snapshot Then
                parameters.Namespaces.Add(GetType(ChangeTrackingStrategy).Namespace)

                mainBuilder.
                    AppendLine(","c).
                    Append("changeTrackingStrategy:=").
                    Append(_code.Literal(CType(changeTrackingStrat, [Enum])))
            End If

            Dim indexerPropertyInfo = runTimeEntityType.FindIndexerPropertyInfo()

            If indexerPropertyInfo IsNot Nothing Then
                mainBuilder.
                    AppendLine(","c).
                    Append("indexerPropertyInfo:=RuntimeEntityType.FindIndexerProperty(").
                    Append(_code.Literal(runTimeEntityType.ClrType)).
                    Append(")"c)
            End If

            If runTimeEntityType.IsPropertyBag Then
                mainBuilder.
                    AppendLine(","c).
                    Append("propertyBag:=").
                    Append(_code.Literal(True))
            End If

            Dim discriminatorValue = entityType.GetDiscriminatorValue()
            If discriminatorValue IsNot Nothing Then
                AddNamespace(discriminatorValue.GetType(), parameters.Namespaces)

                mainBuilder.
                    AppendLine(","c).
                    Append("discriminatorValue:=").
                    Append(_code.UnknownLiteral(discriminatorValue))
            End If

            mainBuilder.
                AppendLine(")"c).
                AppendLine().
                DecrementIndent()
        End Sub

        Private Sub Create([property] As IProperty,
                           propertyVariables As Dictionary(Of IProperty, String),
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim variableName = _code.Identifier([property].Name, parameters.ScopeVariables, capitalize:=False)
            propertyVariables([property]) = variableName

            Create([property], variableName, propertyVariables, parameters)

            CreateAnnotations([property],
                              AddressOf _annotationCodeGenerator.Generate,
                              parameters.Cloner.
                                         WithTargetName(variableName).
                                         Clone)

            parameters.MainBuilder.AppendLine()
        End Sub

        Private Sub Create([property] As IProperty,
                           variableName As String,
                           propertyVariables As Dictionary(Of IProperty, String),
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim valueGeneratorFactoryType = TryCast([property](CoreAnnotationNames.ValueGeneratorFactoryType), Type)

            If valueGeneratorFactoryType Is Nothing AndAlso
               [property].GetValueGeneratorFactory() IsNot Nothing Then
                Throw New InvalidOperationException(
                    DesignStrings.CompiledModelValueGenerator(
                        [property].DeclaringType.ShortName(), [property].Name, NameOf(PropertyBuilder.HasValueGeneratorFactory)))
            End If

            Dim mainBuilder = parameters.MainBuilder

            mainBuilder.
                Append("Dim ").
                Append(variableName).
                Append(" = ").
                Append(parameters.TargetName).
                AppendLine(".AddProperty(").
                IncrementIndent().
                Append(_code.Literal([property].Name))

            PropertyBaseParameters([property], parameters)

            If [property].IsNullable Then
                mainBuilder.
                    AppendLine(","c).
                    Append("nullable:=").
                    Append(_code.Literal(True))
            End If

            If [property].IsConcurrencyToken Then
                mainBuilder.
                    AppendLine(","c).
                    Append("concurrencyToken:=").
                    Append(_code.Literal(True))
            End If

            If [property].ValueGenerated <> ValueGenerated.Never Then
                mainBuilder.
                    AppendLine(","c).
                    Append("valueGenerated:=").
                    Append(_code.Literal(CType([property].ValueGenerated, [Enum])))
            End If

            If [property].GetBeforeSaveBehavior() <> PropertySaveBehavior.Save Then
                mainBuilder.
                    AppendLine(","c).
                    Append("beforeSaveBehavior:=").
                    Append(_code.Literal(CType([property].GetBeforeSaveBehavior(), [Enum])))
            End If

            If [property].GetAfterSaveBehavior() <> PropertySaveBehavior.Save Then
                mainBuilder.
                    AppendLine(","c).
                    Append("afterSaveBehavior:=").
                    Append(_code.Literal(CType([property].GetAfterSaveBehavior(), [Enum])))
            End If

            If [property].GetMaxLength().HasValue Then
                mainBuilder.
                    AppendLine(","c).
                    Append("maxLength:=").
                    Append(_code.Literal([property].GetMaxLength()))
            End If

            If [property].IsUnicode().HasValue Then
                mainBuilder.
                    AppendLine(","c).
                    Append("unicode:=").
                    Append(_code.Literal([property].IsUnicode()))
            End If

            If [property].GetPrecision().HasValue Then
                mainBuilder.
                    AppendLine(","c).
                    Append("precision:=").
                    Append(_code.Literal([property].GetPrecision()))
            End If

            If [property].GetScale().HasValue Then
                mainBuilder.
                    AppendLine(","c).
                    Append("scale:=").
                    Append(_code.Literal([property].GetScale()))
            End If

            Dim providerClrType = [property].GetProviderClrType()

            If providerClrType IsNot Nothing Then
                AddNamespace(providerClrType, parameters.Namespaces)

                mainBuilder.
                    AppendLine(","c).
                    Append("providerPropertyType:=").
                    Append(_code.Literal(providerClrType))
            End If

            If valueGeneratorFactoryType IsNot Nothing Then
                AddNamespace(valueGeneratorFactoryType, parameters.Namespaces)

                mainBuilder.
                    AppendLine(","c).
                    Append("valueGeneratorFactory:=AddressOf New ").
                    Append(_code.Reference(valueGeneratorFactoryType)).
                    Append("().Create")
            End If

            Dim valueConverterType = GetValueConverterType([property])
            If valueConverterType IsNot Nothing Then
                AddNamespace(valueConverterType, parameters.Namespaces)

                mainBuilder.
                            AppendLine(","c).
                            Append("valueConverter:=New ").
                            Append(_code.Reference(valueConverterType)).
                            Append("()")
            End If

            Dim valueComparerType = DirectCast([property](CoreAnnotationNames.ValueComparerType), Type)
            If valueComparerType IsNot Nothing Then
                AddNamespace(valueComparerType, parameters.Namespaces)

                mainBuilder.
                    AppendLine(","c).
                    Append("valueComparer:=New ").
                    Append(_code.Reference(valueComparerType)).
                    Append("()")
            End If

            Dim providerValueComparerType = DirectCast([property](CoreAnnotationNames.ProviderValueComparerType), Type)
            If providerValueComparerType IsNot Nothing Then
                AddNamespace(providerValueComparerType, parameters.Namespaces)

                mainBuilder.
                    AppendLine(","c).
                    Append("providerValueComparer:=New ").
                    Append(_code.Reference(providerValueComparerType)).
                    Append("()")
            End If

            Dim sentinel = [property].Sentinel
            Dim converter = [property].FindTypeMapping()?.Converter
            If sentinel IsNot Nothing AndAlso
               converter Is Nothing Then
                mainBuilder.
                    AppendLine(","c).
                    Append("sentinel:=").
                    Append(_code.UnknownLiteral(sentinel))
            End If

            Dim jsonValueReaderWriterType = CType([property](CoreAnnotationNames.JsonValueReaderWriterType), Type)
            If jsonValueReaderWriterType IsNot Nothing Then
                mainBuilder.
                    AppendLine(","c).
                    Append("jsonValueReaderWriter:=")

                VisualBasicRuntimeAnnotationCodeGenerator.CreateJsonValueReaderWriter(jsonValueReaderWriterType, parameters, _code)
            End If

            mainBuilder.
                AppendLine(")"c).
                DecrementIndent()

            mainBuilder.
                Append(variableName).
                Append(".TypeMapping = ")

            _annotationCodeGenerator.Create(
                [property].GetTypeMapping(),
                [property],
                parameters.
                    Cloner.
                    WithTargetName(variableName).
                    Clone())

            mainBuilder.AppendLine()

            If sentinel IsNot Nothing AndAlso
               converter IsNot Nothing Then
                mainBuilder.
                    Append(variableName).Append(".SetSentinelFromProviderValue(").
                    Append(_code.UnknownLiteral(If(converter?.ConvertToProvider(sentinel), sentinel))).
                    AppendLine(")")
            End If
        End Sub

        Private Shared Function GetValueConverterType([property] As IProperty) As Type
            Dim annotation = [property].FindAnnotation(CoreAnnotationNames.ValueConverterType)

            If annotation IsNot Nothing Then
                Return DirectCast(annotation.Value, Type)
            End If

            If Not Metadata.Internal.[Property].UseOldBehavior32422 Then
                Return DirectCast([property], [Property]).
                           GetConversion(throwOnProviderClrTypeConflict:=False, throwOnValueConverterConflict:=False).
                           ValueConverterType
            End If

            Dim principalProperty = [property]

            Dim i = 0
            While i < ForeignKey.LongestFkChainAllowedLength
                Dim nextProperty As IProperty = Nothing
                For Each foreignKey In principalProperty.GetContainingForeignKeys()
                    For propertyIndex = 0 To foreignKey.Properties.Count - 1
                        If principalProperty Is foreignKey.Properties(propertyIndex) Then
                            Dim newPrincipalProperty = foreignKey.PrincipalKey.Properties(propertyIndex)
                            If newPrincipalProperty Is [property] OrElse
                               newPrincipalProperty Is principalProperty Then

                                Exit For
                            End If

                            annotation = newPrincipalProperty.FindAnnotation(CoreAnnotationNames.ValueConverterType)
                            If annotation IsNot Nothing Then
                                Return DirectCast(annotation.Value, Type)
                            End If

                            nextProperty = newPrincipalProperty
                        End If
                    Next
                Next

                If nextProperty Is Nothing Then
                    Exit While
                End If

                principalProperty = nextProperty
                i += 1
            End While

            If i = ForeignKey.LongestFkChainAllowedLength Then
                Throw New InvalidOperationException(
                    CoreStrings.RelationshipCycle(
                        [property].DeclaringType.DisplayName(), [property].Name, "ValueConverterType"))
            Else
                Return Nothing
            End If
        End Function

        Private Sub PropertyBaseParameters(prop As IPropertyBase,
                                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters,
                                           Optional skipType As Boolean = False)

            Dim mainBuilder = parameters.MainBuilder

            If Not skipType Then
                AddNamespace(prop.ClrType, parameters.Namespaces)
                mainBuilder.
                    AppendLine(","c).
                    Append(_code.Literal(prop.ClrType))
            End If

            Dim propertyInfo = prop.PropertyInfo

            If propertyInfo IsNot Nothing Then

                AddNamespace(propertyInfo.DeclaringType, parameters.Namespaces)

                mainBuilder.
                    AppendLine(","c).
                    Append("propertyInfo:=")

                If prop.IsIndexerProperty() Then
                    mainBuilder.
                        Append(parameters.TargetName).
                        Append(".FindIndexerPropertyInfo()")
                Else
                    mainBuilder.
                        Append(_code.Literal(propertyInfo.DeclaringType)).
                        Append(".GetProperty(").
                        Append(_code.Literal(propertyInfo.Name)).
                        Append(", ").
                        Append(If(propertyInfo.GetAccessors().Length <> 0, "BindingFlags.Public", "BindingFlags.NonPublic")).
                        Append(If(propertyInfo.IsStatic(), " Or BindingFlags.Static", " Or BindingFlags.Instance")).
                        Append(" Or BindingFlags.DeclaredOnly)")
                End If
            End If

            Dim fieldInfo = prop.FieldInfo

            If fieldInfo IsNot Nothing Then

                AddNamespace(fieldInfo.DeclaringType, parameters.Namespaces)

                mainBuilder.
                    AppendLine(","c).
                    Append("fieldInfo:=").
                    Append(_code.Literal(fieldInfo.DeclaringType)).
                    Append(".GetField(").
                    Append(_code.Literal(fieldInfo.Name)).
                    Append(", ").
                    Append(If(fieldInfo.IsPublic, "BindingFlags.Public", "BindingFlags.NonPublic")).
                    Append(If(fieldInfo.IsStatic, " Or BindingFlags.Static", " Or BindingFlags.Instance")).
                    Append(" Or BindingFlags.DeclaredOnly)")
            End If

            Dim propertyAccessMode = prop.GetPropertyAccessMode()

            If propertyAccessMode <> Model.DefaultPropertyAccessMode Then
                parameters.Namespaces.Add(GetType(PropertyAccessMode).Namespace)

                mainBuilder.
                    AppendLine(","c).
                    Append("propertyAccessMode:=").
                    Append(_code.Literal(CType(propertyAccessMode, [Enum])))
            End If
        End Sub

        Private Sub FindProperties(entityTypeVariable As String,
                                   properties As IEnumerable(Of IProperty),
                                   mainBuilder As IndentedStringBuilder,
                                   Optional propertyVariables As Dictionary(Of IProperty, String) = Nothing)

            mainBuilder.Append("{"c)

            Dim first = True

            For Each prop In properties
                If first Then
                    first = False
                Else
                    mainBuilder.
                        Append(", ")
                End If

                Dim propertyVariable As String = Nothing

                If propertyVariables IsNot Nothing AndAlso
                   propertyVariables.TryGetValue(prop, propertyVariable) Then

                    mainBuilder.
                        Append(propertyVariable)
                Else
                    mainBuilder.
                        Append(entityTypeVariable).
                        Append(".FindProperty(").
                        Append(_code.Literal(prop.Name)).
                        Append(")"c)
                End If
            Next

            mainBuilder.Append("}"c)
        End Sub

        Private Sub Create([property] As IServiceProperty,
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim variableName = _code.Identifier([property].Name, parameters.ScopeVariables, capitalize:=False)

            Dim mainBuilder = parameters.MainBuilder

            mainBuilder.
                Append("Dim ").
                Append(variableName).
                Append(" = ").
                Append(parameters.TargetName).
                AppendLine(".AddServiceProperty(").
                IncrementIndent().
                Append(_code.Literal([property].Name))

            PropertyBaseParameters([property], parameters, skipType:=True)

            AddNamespace([property].ClrType, parameters.Namespaces)
            mainBuilder.
                AppendLine(","c).
                Append("serviceType:=GetType(" & _code.Reference([property].ClrType) & ")")

            mainBuilder.
                AppendLine(")"c).
                DecrementIndent()

            CreateAnnotations([property],
                              AddressOf _annotationCodeGenerator.Generate,
                              parameters.Cloner.
                                         WithTargetName(variableName).
                                         Clone())

            mainBuilder.
                AppendLine()
        End Sub

        Private Sub Create(key As IKey,
                           propertyVariables As Dictionary(Of IProperty, String),
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim variableName = _code.Identifier("key", parameters.ScopeVariables)
            Dim mainBuilder = parameters.MainBuilder

            mainBuilder.
                Append("Dim ").
                Append(variableName).
                Append(" = ").
                Append(parameters.TargetName).
                AppendLine(".AddKey(").
                IncrementIndent()

            FindProperties(parameters.TargetName, key.Properties, mainBuilder, propertyVariables)

            mainBuilder.
                AppendLine(")"c).
                DecrementIndent()

            If key.IsPrimaryKey() Then
                mainBuilder.
                    Append(parameters.TargetName).
                    Append(".SetPrimaryKey(").
                    Append(variableName).
                    AppendLine(")"c)
            End If

            CreateAnnotations(key,
                              AddressOf _annotationCodeGenerator.Generate,
                              parameters.Cloner.
                                         WithTargetName(variableName).
                                         Clone())

            mainBuilder.
                AppendLine()
        End Sub

        Private Sub Create(index As IIndex,
                           propertyVariables As Dictionary(Of IProperty, String),
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim variableName = _code.Identifier(If(index.Name, "index"), parameters.ScopeVariables, capitalize:=False)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.
                Append("Dim ").
                Append(variableName).
                Append(" = ").
                Append(parameters.TargetName).
                AppendLine(".AddIndex(").
                IncrementIndent()

            FindProperties(parameters.TargetName, index.Properties, mainBuilder, propertyVariables)

            If index.Name IsNot Nothing Then
                mainBuilder.
                    AppendLine(","c).
                    Append("name:=").
                    Append(_code.Literal(index.Name))
            End If

            If index.IsUnique Then
                mainBuilder.
                    AppendLine(","c).
                    Append("unique:=").
                    Append(_code.Literal(True))
            End If

            mainBuilder.
                AppendLine(")"c).
                DecrementIndent()

            CreateAnnotations(index,
                              AddressOf _annotationCodeGenerator.Generate,
                              parameters.Cloner.
                                         WithTargetName(variableName).
                                         Clone())

            mainBuilder.
                        AppendLine()
        End Sub

        Private Sub CreateComplexProperty(complexProperty As IComplexProperty,
                                          mainBuilder As IndentedStringBuilder,
                                          methodBuilder As IndentedStringBuilder,
                                          namespaces As SortedSet(Of String),
                                          topClassName As String)

            mainBuilder.
                AppendLine().
                Append("Private Class ").
                Append(_code.Identifier(complexProperty.Name, capitalize:=True)).
                AppendLine("ComplexProperty")

            Dim complexType = complexProperty.ComplexType
            Using mainBuilder.Indent()
                Dim declaringTypeVariable = "declaringType"
                mainBuilder.
                    Append("Public Shared Function Create(").
                    Append(declaringTypeVariable).
                    Append(" As ").
                    Append(If(TypeOf complexProperty.DeclaringType Is IEntityType, "RuntimeEntityType", "RuntimeComplexType")).
                    AppendLine(") As RuntimeComplexProperty")

                Using mainBuilder.Indent()
                    Const complexPropertyVariable = "complexProperty"
                    Const complexTypeVariable = "complexType"

                    Dim variables As New HashSet(Of String) From {
                        declaringTypeVariable,
                        complexPropertyVariable,
                        complexTypeVariable
                    }

                    mainBuilder.
                        Append("Dim ").
                        Append(complexPropertyVariable).
                        Append(" = ").
                        Append(declaringTypeVariable).
                        Append(".AddComplexProperty(").
                        IncrementIndent().
                        Append(_code.Literal(complexProperty.Name)).
                        AppendLine(","c).
                        Append(_code.Literal(complexProperty.ClrType)).
                        AppendLine(","c).
                        Append(_code.Literal(complexType.Name)).
                        AppendLine(","c).
                        Append(_code.Literal(complexType.ClrType))

                    AddNamespace(complexProperty.ClrType, namespaces)
                    AddNamespace(complexType.ClrType, namespaces)

                    Dim parameters As New VisualBasicRuntimeAnnotationCodeGeneratorParameters(
                            declaringTypeVariable,
                            topClassName,
                            mainBuilder,
                            methodBuilder,
                            namespaces,
                            variables)

                    PropertyBaseParameters(complexProperty, parameters, skipType:=True)

                    If complexProperty.IsNullable Then
                        mainBuilder.
                            AppendLine(","c).
                            Append("nullable:=").
                            Append(_code.Literal(True))
                    End If

                    If complexProperty.IsCollection Then
                        mainBuilder.
                            AppendLine(","c).
                            Append("collection:=").
                            Append(_code.Literal(True))
                    End If

                    Dim changeTrackingStrategy = complexType.GetChangeTrackingStrategy()
                    If changeTrackingStrategy <> ChangeTrackingStrategy.Snapshot Then
                        namespaces.Add(GetType(ChangeTrackingStrategy).Namespace)

                        mainBuilder.AppendLine(","c).
                            Append("changeTrackingStrategy:=").
                            Append(_code.Literal(DirectCast(changeTrackingStrategy, [Enum])))
                    End If

                    Dim indexerPropertyInfo = complexType.FindIndexerPropertyInfo()
                    If indexerPropertyInfo IsNot Nothing Then
                        mainBuilder.
                            AppendLine(","c).
                            Append("indexerPropertyInfo:=RuntimeEntityType.FindIndexerProperty(").
                            Append(_code.Literal(complexType.ClrType)).
                            Append(")"c)
                    End If

                    If complexType.IsPropertyBag Then
                        mainBuilder.AppendLine(","c).
                            Append("propertyBag:=").
                            Append(_code.Literal(True))
                    End If

                    mainBuilder.
                        AppendLine(")"c).
                        AppendLine().
                        DecrementIndent()

                    mainBuilder.
                        Append("Dim ").Append(complexTypeVariable).Append(" = ").
                        Append(complexPropertyVariable).AppendLine(".ComplexType")

                    Dim complexTypeParameters = parameters.Cloner.WithTargetName(complexTypeVariable).Clone
                    Dim propertyVariables As New Dictionary(Of IProperty, String)()

                    For Each [property] In complexType.GetProperties()
                        Create([property], propertyVariables, complexTypeParameters)
                    Next

                    For Each nestedComplexProperty In complexType.GetComplexProperties()
                        mainBuilder.
                            Append(_code.Identifier(nestedComplexProperty.Name, capitalize:=True)).
                            Append("ComplexProperty").
                            Append(".Create").
                            Append("("c).
                            Append(complexTypeVariable).
                            AppendLine(")"c)
                    Next

                    CreateAnnotations(
                        complexType,
                        AddressOf _annotationCodeGenerator.Generate,
                        complexTypeParameters)

                    CreateAnnotations(
                        complexProperty,
                        AddressOf _annotationCodeGenerator.Generate,
                        parameters.Cloner.WithTargetName(complexPropertyVariable).Clone())

                    mainBuilder.
                        Append("Return ").
                        AppendLine(complexPropertyVariable)
                End Using

                mainBuilder.AppendLine("End Function")
            End Using

            Using mainBuilder.Indent()
                For Each nestedComplexProperty In complexType.GetComplexProperties()
                    CreateComplexProperty(nestedComplexProperty, mainBuilder, methodBuilder, namespaces, topClassName)
                Next
            End Using

            mainBuilder.AppendLine("End Class")
        End Sub

        Private Sub CreateForeignKey(aforeignKey As IForeignKey,
                                     foreignKeyNumber As Integer,
                                     mainBuilder As IndentedStringBuilder,
                                     methodBuilder As IndentedStringBuilder,
                                     namespaces As SortedSet(Of String),
                                     className As String)

            Const declaringEntityType = "declaringEntityType"
            Const principalEntityType = "principalEntityType"

            mainBuilder.
                AppendLine().
                Append("Public Shared Function CreateForeignKey").
                Append(foreignKeyNumber.ToString()).
                Append("("c).
                Append(declaringEntityType).
                Append(" As RuntimeEntityType").
                Append(", ").
                Append(principalEntityType).
                Append(" As RuntimeEntityType").
                AppendLine(") As RuntimeForeignKey")

            Using mainBuilder.Indent()
                Const foreignKeyVariable = "runtimeForeignKey"

                Dim variables = New HashSet(Of String) From {
                    declaringEntityType,
                    principalEntityType,
                    foreignKeyVariable
                }

                mainBuilder.
                    Append("Dim ").
                    Append(foreignKeyVariable).Append(" = ").
                    Append(declaringEntityType).
                    Append(".AddForeignKey(").
                    IncrementIndent()

                FindProperties(declaringEntityType, aforeignKey.Properties, mainBuilder)

                mainBuilder.
                    AppendLine(","c).
                    Append(principalEntityType).
                    Append(".FindKey(")

                FindProperties(principalEntityType, aforeignKey.PrincipalKey.Properties, mainBuilder)
                mainBuilder.Append(")"c)

                mainBuilder.
                    AppendLine(","c).
                    Append(principalEntityType)

                If aforeignKey.DeleteBehavior <> ForeignKey.DefaultDeleteBehavior Then
                    namespaces.Add(GetType(DeleteBehavior).Namespace)

                    mainBuilder.
                        AppendLine(","c).
                        Append("deleteBehavior:=").
                        Append(_code.Literal(CType(aforeignKey.DeleteBehavior, [Enum])))
                End If

                If aforeignKey.IsUnique Then
                    mainBuilder.
                        AppendLine(","c).
                        Append("unique:=").
                        Append(_code.Literal(True))
                End If

                If aforeignKey.IsRequired Then
                    mainBuilder.
                        AppendLine(","c).
                        Append("required:=").
                        Append(_code.Literal(True))
                End If

                If aforeignKey.IsRequiredDependent Then
                    mainBuilder.
                        AppendLine(","c).
                        Append("requiredDependent:=").
                        Append(_code.Literal(True))
                End If

                If aforeignKey.IsOwnership Then
                    mainBuilder.
                        AppendLine(","c).
                        Append("ownership:=").
                        Append(_code.Literal(True))
                End If

                mainBuilder.
                    AppendLine(")"c).
                    AppendLine().
                    DecrementIndent()

                Dim parameters = New VisualBasicRuntimeAnnotationCodeGeneratorParameters(
                        foreignKeyVariable,
                        className,
                        mainBuilder,
                        methodBuilder,
                        namespaces,
                        variables)

                Dim navigation = aforeignKey.DependentToPrincipal

                If navigation IsNot Nothing Then
                    Create(navigation,
                           foreignKeyVariable,
                           parameters.Cloner.
                                      WithTargetName(declaringEntityType).
                                      Clone())
                End If

                navigation = aforeignKey.PrincipalToDependent
                If navigation IsNot Nothing Then
                    Create(navigation,
                           foreignKeyVariable,
                           parameters.Cloner.
                                      WithTargetName(principalEntityType).
                                      Clone())
                End If

                CreateAnnotations(aforeignKey,
                                  AddressOf _annotationCodeGenerator.Generate,
                                  parameters)

                mainBuilder.
                    Append("Return ").
                    AppendLine(foreignKeyVariable)
            End Using

            mainBuilder.
                AppendLine("End Function")
        End Sub

        Private Sub Create(navigation As INavigation,
                           foreignKeyVariable As String,
                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim mainBuilder = parameters.MainBuilder
            Dim navigationVariable = _code.Identifier(navigation.Name, parameters.ScopeVariables, capitalize:=False)

            mainBuilder.
                Append("Dim ").
                Append(navigationVariable).
                Append(" = ").
                Append(parameters.TargetName).
                Append(".AddNavigation(").
                IncrementIndent().
                Append(_code.Literal(navigation.Name)).
                AppendLine(","c).
                Append(foreignKeyVariable).
                AppendLine(","c).
                Append("onDependent:=").
                Append(_code.Literal(navigation.IsOnDependent))

            PropertyBaseParameters(navigation, parameters)

            If navigation.IsEagerLoaded Then
                mainBuilder.
                    AppendLine(","c).
                    Append("eagerLoaded:=").
                    Append(_code.Literal(True))
            End If

            If Not navigation.LazyLoadingEnabled Then
                mainBuilder.
                    AppendLine(","c).
                    Append("lazyLoadingEnabled:=").
                    Append(_code.Literal(False))
            End If

            mainBuilder.
                AppendLine(")"c).
                AppendLine().
                DecrementIndent()

            CreateAnnotations(navigation,
                              AddressOf _annotationCodeGenerator.Generate,
                              parameters.Cloner.
                                         WithTargetName(navigationVariable).
                                         Clone())
        End Sub

        Private Sub CreateSkipNavigation(navigation As ISkipNavigation,
                                         navigationNumber As Integer,
                                         mainBuilder As IndentedStringBuilder,
                                         methodBuilder As IndentedStringBuilder,
                                         namespaces As SortedSet(Of String),
                                         className As String)

            Const declaringEntityType = "declaringEntityType"
            Const targetEntityType = "targetEntityType"
            Const joinEntityType = "joinEntityType"

            mainBuilder.
                AppendLine().
                Append("Public Shared Function CreateSkipNavigation").
                Append(navigationNumber.ToString()).
                Append("("c).Append(declaringEntityType).Append(" As RuntimeEntityType").
                Append(", ").Append(targetEntityType).Append(" As RuntimeEntityType").
                Append(", ").Append(joinEntityType).Append(" As RuntimeEntityType").
                Append(")"c).
                AppendLine(" As RuntimeSkipNavigation")

            Using mainBuilder.Indent()

                Const navigationVariable = "skipNavigation"
                Dim variables = New HashSet(Of String) From {
                    declaringEntityType,
                    targetEntityType,
                    joinEntityType,
                    navigationVariable
                }

                Dim parameters = New VisualBasicRuntimeAnnotationCodeGeneratorParameters(
                        targetName:=navigationVariable,
                        className,
                        mainBuilder,
                        methodBuilder,
                        namespaces,
                        scopeVariables:=variables)

                mainBuilder.
                    Append("Dim ").Append(navigationVariable).
                    Append(" = ").
                    Append(declaringEntityType).
                    AppendLine(".AddSkipNavigation(").
                    IncrementIndent().
                    Append(_code.Literal(navigation.Name)).
                    AppendLine(","c).
                    Append(targetEntityType).
                    AppendLine(","c).
                    Append(joinEntityType).
                    AppendLine(".FindForeignKey(")

                Using mainBuilder.Indent()
                    FindProperties(joinEntityType, navigation.ForeignKey.Properties, mainBuilder)

                    mainBuilder.
                        AppendLine(","c).
                        Append(declaringEntityType).
                        Append(".FindKey(")

                    FindProperties(declaringEntityType, navigation.ForeignKey.PrincipalKey.Properties, mainBuilder)
                    mainBuilder.Append(")"c)

                    mainBuilder.
                        AppendLine(","c).
                        Append(declaringEntityType).
                        Append(")"c)
                End Using

                mainBuilder.
                    AppendLine(","c).
                    Append(_code.Literal(navigation.IsCollection)).AppendLine(","c).
                    Append(_code.Literal(navigation.IsOnDependent))

                PropertyBaseParameters(navigation, parameters.Cloner.
                                                              WithTargetName(declaringEntityType).
                                                              Clone())

                If navigation.IsEagerLoaded Then
                    mainBuilder.
                        AppendLine(","c).
                        Append("eagerLoaded:=").
                        Append(_code.Literal(True))
                End If

                If Not navigation.LazyLoadingEnabled Then
                    mainBuilder.
                        AppendLine(","c).
                        Append("lazyLoadingEnabled:=").
                        Append(_code.Literal(False))
                End If

                mainBuilder.
                    AppendLine(")"c).
                    DecrementIndent()

                mainBuilder.AppendLine()

                variables.Add("inverse")

                mainBuilder.
                    Append("Dim inverse = ").Append(targetEntityType).Append(".FindSkipNavigation(").
                    Append(_code.Literal(navigation.Inverse.Name)).AppendLine(")"c).
                    AppendLine("If inverse IsNot Nothing Then")

                Using mainBuilder.Indent()
                    mainBuilder.
                        Append(navigationVariable).
                        AppendLine(".Inverse = inverse").
                        Append("inverse.Inverse = ").AppendLine(navigationVariable)
                End Using

                mainBuilder.
                    AppendLine("End If").
                    AppendLine()

                CreateAnnotations(navigation,
                                  AddressOf _annotationCodeGenerator.Generate,
                                  parameters)

                mainBuilder.
                    Append("Return ").
                    AppendLine(navigationVariable)
            End Using

            mainBuilder.
                AppendLine("End Function")
        End Sub

        Private Sub Create(trigger As ITrigger, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            Dim triggerVariable = _code.Identifier(trigger.ModelName, parameters.ScopeVariables, capitalize:=False)

            Dim mainBuilder = parameters.MainBuilder
            mainBuilder.
                Append("Dim ").Append(triggerVariable).Append(" = ").Append(parameters.TargetName).AppendLine(".AddTrigger(").
                IncrementIndent().
                Append(_code.Literal(trigger.ModelName)).
                AppendLine(")"c).
                DecrementIndent()

            CreateAnnotations(
                trigger,
                AddressOf _annotationCodeGenerator.Generate,
                parameters.Cloner.
                           WithTargetName(triggerVariable).
                           Clone)

            mainBuilder.AppendLine()
        End Sub

        Private Sub CreateAnnotations(entityType As IEntityType,
                                      mainBuilder As IndentedStringBuilder,
                                      methodBuilder As IndentedStringBuilder,
                                      namespaces As SortedSet(Of String),
                                      className As String)

            mainBuilder.
                AppendLine().
                Append("Public Shared Sub CreateAnnotations").
                AppendLine("(entityType As RuntimeEntityType)")

            Using mainBuilder.Indent()

                Const entityTypeVariable = "entityType"
                Dim variables = New HashSet(Of String) From {
                    entityTypeVariable
                }

                CreateAnnotations(
                    entityType,
                    AddressOf _annotationCodeGenerator.Generate,
                    New VisualBasicRuntimeAnnotationCodeGeneratorParameters(
                        entityTypeVariable,
                        className,
                        mainBuilder,
                        methodBuilder,
                        namespaces,
                        variables))

                mainBuilder.
                    AppendLine().
                    AppendLine("Customize(entityType)")
            End Using

            mainBuilder.
                    AppendLine("End Sub").
                    AppendLine().
                    AppendLine("Shared Partial Private Sub Customize(entityType As RuntimeEntityType)").
                    AppendLine("End Sub")
        End Sub

        Private Shared Sub CreateAnnotations(Of TAnnotatable As IAnnotatable)(
            annotatable As TAnnotatable,
            process As Action(Of TAnnotatable, VisualBasicRuntimeAnnotationCodeGeneratorParameters),
            parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            process(
                annotatable,
                parameters.
                    Cloner.
                    WithAnnotations(annotatable.GetAnnotations().ToDictionary(Function(a) a.Name, Function(a) a.Value)).
                    WithIsRuntime(False).
                    Clone())

            process(
                annotatable,
                parameters.
                    Cloner.
                    WithAnnotations(annotatable.GetRuntimeAnnotations().ToDictionary(Function(a) a.Name, Function(a) a.Value)).
                    WithIsRuntime(True).
                    Clone())
        End Sub

        Private Shared Sub AddNamespace(type As Type, namespaces As ISet(Of String))
            VisualBasicRuntimeAnnotationCodeGenerator.AddNamespace(type, namespaces)
        End Sub
    End Class
End Namespace
