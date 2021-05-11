Imports System.Numerics
Imports Microsoft.EntityFrameworkCore.Design

Namespace Design

    ''' <summary>
    '''     Helper for generating Visual Basic code.
    ''' </summary>
    Public Interface IVisualBasicHelper
        Inherits ICSharpHelper

        ''' <summary>
        '''     Generates a BigInteger
        ''' </summary>
        ''' <param name="value">The BigInteger</param>
        ''' <returns> The literal.</returns>
        Overloads Function Literal(value As BigInteger) As String

    End Interface

End Namespace