''' <summary>
'''     Helper for generating Visual Basic code.
''' </summary>
Public Interface IVisualBasicHelper

    ''' <summary>
    '''     Generates a valid Visual Basic identifier from the specified string unique to the scope.
    ''' </summary>
    ''' <param name="name"> The base identifier name. </param>
    ''' <param name="scope"> A list of in-scope identifiers. </param>
    ''' <returns> The identifier. </returns>
    Function Identifier(name As String, Optional scope As ICollection(Of String) = Nothing) As String

    ''' <summary>
    '''     Generates a property accessor lambda.
    ''' </summary>
    ''' <param name="properties"> The property names. </param>
    ''' <returns> The lambda. </returns>
    Function Lambda(properties As IReadOnlyList(Of String)) As String

    ''' <summary>
    '''     Generates a multidimensional array literal.
    ''' </summary>
    ''' <param name="values"> The multidimensional array. </param>
    ''' <returns> The literal. </returns>
    Function Literal(values As Object(,)) As String

    ''' <summary>
    '''     Generates a nullable literal.
    ''' </summary>
    ''' <typeparam name="T"> The underlying type of the nullable type. </typeparam>
    ''' <param name="value"> The nullable value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(Of T As Structure)(value As T?) As String

    ''' <summary>
    '''     Generates a byte array literal.
    ''' </summary>
    ''' <param name="values"> The byte array. </param>
    ''' <returns> The literal. </returns>
    Function Literal(values As Byte()) As String

    ''' <summary>
    '''     Generates a bool literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As Boolean) As String

    ''' <summary>
    '''     Generates a byte literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As Byte) As String

    ''' <summary>
    '''     Generates a char literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As Char) As String

    ''' <summary>
    '''     Generates a DateTime literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As DateTime) As String

    ''' <summary>
    '''     Generates a DateTimeOffset literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As DateTimeOffset) As String

    ''' <summary>
    '''     Generates a decimal literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As Decimal) As String

    ''' <summary>
    '''     Generates a double literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As Double) As String

    ''' <summary>
    '''     Generates an enum literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As [Enum]) As String

    ''' <summary>
    '''     Generates a float literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As Single) As String

    ''' <summary>
    '''     Generates a Guid literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As Guid) As String

    ''' <summary>
    '''     Generates an int literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As Integer) As String

    ''' <summary>
    '''     Generates an array literal.
    ''' </summary>
    ''' <typeparam name="T"> The element type of the array. </typeparam>
    ''' <param name="values"> The array. </param>
    ''' <returns> The literal. </returns>
    Function Literal(Of T)(values As IReadOnlyList(Of T)) As String

    ''' <summary>
    '''     Generates an object array literal.
    ''' </summary>
    ''' <param name="values"> The object array. </param>
    ''' <returns> The literal. </returns>
    Function Literal(values As IReadOnlyList(Of Object)) As String

    ''' <summary>
    '''     Generates a long literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As Long) As String

    ''' <summary>
    '''     Generates a sbyte literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As SByte) As String

    ''' <summary>
    '''     Generates a short literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As Short) As String

    ''' <summary>
    '''     Generates a string literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As String) As String

    ''' <summary>
    '''     Generates a TimeSpan literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As TimeSpan) As String

    ''' <summary>
    '''     Generates a uint literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As UInteger) As String

    ''' <summary>
    '''     Generates a ulong literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As ULong) As String

    ''' <summary>
    '''     Generates a ushort literal.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function Literal(value As UShort) As String

    ''' <summary>
    '''     Generates an object array literal.
    ''' </summary>
    ''' <param name="values"> The object array. </param>
    ''' <param name="vertical"> A value indicating whether to layout the literal vertically. </param>
    ''' <returns> The literal. </returns>
    Function Literal(values As IReadOnlyList(Of Object), vertical As Boolean) As String

    ''' <summary>
    '''     Generates a valid Visual Basic namespace from the specified parts.
    ''' </summary>
    ''' <param name="name"> The base parts of the namespace. </param>
    ''' <returns> The namespace. </returns>
    Function [Namespace](ParamArray name As String()) As String

    ''' <summary>
    '''     Generates a Visual Basic type reference.
    ''' </summary>
    ''' <param name="type"> The type to reference. </param>
    ''' <returns> The reference. </returns>
    Function Reference(type As Type) As String

    ''' <summary>
    '''     Generates a literal for a type Not known at compile time.
    ''' </summary>
    ''' <param name="value"> The value. </param>
    ''' <returns> The literal. </returns>
    Function UnknownLiteral(value As Object) As String

End Interface