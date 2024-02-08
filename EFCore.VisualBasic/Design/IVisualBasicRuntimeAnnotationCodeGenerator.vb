Imports System.Runtime.CompilerServices
Imports Microsoft.EntityFrameworkCore.ChangeTracking
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Storage

Namespace Design
    ''' <summary>
    '''     Implemented by database providers to generate the code for annotations.
    ''' </summary>
    ''' <remarks>
    '''     The service lifetime Is <see cref="Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton" />. This means a single instance
    '''     Is used by many <see cref="Microsoft.EntityFrameworkCore.DbContext" /> instances. The implementation must be thread-safe.
    '''     This service cannot depend on services registered as <see cref="Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped" />.
    ''' </remarks>
    Public Interface IVisualBasicRuntimeAnnotationCodeGenerator
        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="model">The model to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Sub Generate(model As IModel, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="entityType">The entity type to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Sub Generate(entityType As IEntityType, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="complexProperty">The entity type to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Sub Generate(complexProperty As IComplexProperty, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="complexType">The entity type to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Sub Generate(complexType As IComplexType, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="property">The property to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Sub Generate([property] As IProperty, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="property">The property to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Sub Generate([property] As IServiceProperty, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="key">The key to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Sub Generate(key As IKey, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="foreignKey">The foreign key to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Sub Generate(foreignKey As IForeignKey, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="navigation">The navigation to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Sub Generate(navigation As INavigation, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="navigation">The skip navigation to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Sub Generate(navigation As ISkipNavigation, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="index">The index to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Sub Generate(index As IIndex, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="trigger">The trigger to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Sub Generate(trigger As ITrigger, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="typeConfiguration">The scalar type configuration to which the annotations are applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Sub Generate(typeConfiguration As ITypeMappingConfiguration, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given property type mapping.
        ''' </summary>
        ''' <param name="typeMapping">The type mapping to create.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        ''' <param name="valueComparer">The value comparer that should be used instead of the one in the type mapping.</param>
        ''' <param name="keyValueComparer">The key value comparer that should be used instead of the one in the type mapping.</param>
        ''' <param name="providerValueComparer">The provider value comparer that should be used instead of the one in the type mapping.</param>
        Function Create(
            typeMapping As CoreTypeMapping,
            parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters,
            Optional valueComparer As ValueComparer = Nothing,
            Optional keyValueComparer As ValueComparer = Nothing,
            Optional providerValueComparer As ValueComparer = Nothing) As Boolean
    End Interface

    Module IVisualBasicRuntimeAnnotationCodeGeneratorExtensions
        ''' <summary>
        '''     Generates code to create the given property type mapping.
        ''' </summary>
        ''' <param name="typeMapping">The type mapping to create.</param>
        ''' <param name="property">The property to which this type mapping will be applied.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        <Extension>
        Function Create(service As IVisualBasicRuntimeAnnotationCodeGenerator, typeMapping As CoreTypeMapping, [property] As IProperty, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) As Boolean
            Return service.Create(
                        typeMapping,
                        parameters,
                        [property].GetValueComparer(),
                        [property].GetKeyValueComparer(),
                        [property].GetProviderValueComparer())
        End Function
    End Module
End Namespace
