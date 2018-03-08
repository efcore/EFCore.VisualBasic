Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore.Migrations

Namespace Sandbox.Migrations
    Public Partial Class InitialCreate
        Inherits Migration

        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)
            migrationBuilder.CreateTable(
                name:= "Blogs",
                columns:= Function(table) New With
                {
                    .Id = table.Column(Of Integer)(nullable:= False) _
                        .Annotation("Sqlite:Autoincrement", True)
                },
                constraints:= Sub(table)
                    table.PrimaryKey("PK_Blogs", Function(x) x.Id)
                End Sub)
        End Sub

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)
            migrationBuilder.DropTable(
                name:= "Blogs")
        End Sub
    End Class
End Namespace
