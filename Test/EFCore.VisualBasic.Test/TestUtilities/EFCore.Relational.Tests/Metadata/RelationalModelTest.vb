Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.TestUtilities

Namespace Metadata
    Public Module RelationalModelTest

        Public Sub AssertEqual(expectedModel As IRelationalModel, actualModel As IRelationalModel)
            RelationalModelAsserter.Instance.AssertEqual(expectedModel, actualModel)
        End Sub
    End Module
End Namespace
