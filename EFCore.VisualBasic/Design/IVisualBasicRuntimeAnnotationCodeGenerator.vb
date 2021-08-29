Imports Microsoft.EntityFrameworkCore.Metadata

Namespace Design
    ''' <summary>
    '''     Implemented by database providers to generate the code for annotations.
    ''' </summary>
    Public Interface IVisualBasicRuntimeAnnotationCodeGenerator
        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="model"> The model to which the annotations are applied. </param>
        ''' <param name="parameters"> Additional parameters used during code generation. </param>
        Sub Generate(model As IModel, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="entityType"> The entity type to which the annotations are applied. </param>
        ''' <param name="parameters"> Additional parameters used during code generation. </param>
        Sub Generate(entityType As IEntityType, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="property"> The property to which the annotations are applied. </param>
        ''' <param name="parameters"> Additional parameters used during code generation. </param>
        Sub Generate([property] As IProperty, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="property"> The property to which the annotations are applied. </param>
        ''' <param name="parameters"> Additional parameters used during code generation. </param>
        Sub Generate([property] As IServiceProperty, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="key"> The key to which the annotations are applied. </param>
        ''' <param name="parameters"> Additional parameters used during code generation. </param>
        Sub Generate(key As IKey, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="foreignKey"> The foreign key to which the annotations are applied. </param>
        ''' <param name="parameters"> Additional parameters used during code generation. </param>
        Sub Generate(foreignKey As IForeignKey, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="navigation"> The navigation to which the annotations are applied. </param>
        ''' <param name="parameters"> Additional parameters used during code generation. </param>
        Sub Generate(navigation As INavigation, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="navigation"> The skip navigation to which the annotations are applied. </param>
        ''' <param name="parameters"> Additional parameters used during code generation. </param>
        Sub Generate(navigation As ISkipNavigation, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="index"> The index to which the annotations are applied. </param>
        ''' <param name="parameters"> Additional parameters used during code generation. </param>
        Sub Generate(index As IIndex, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

        ''' <summary>
        '''     Generates code to create the given annotations.
        ''' </summary>
        ''' <param name="typeConfiguration"> The scalar type configuration to which the annotations are applied. </param>
        ''' <param name="parameters"> Additional parameters used during code generation. </param>
        Sub Generate(typeConfiguration As ITypeMappingConfiguration, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
    End Interface
End Namespace
