Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

Namespace Models
    Public Class Instructor
        Public Property ID As Integer

        <Required>
        <Display(Name:="Last Name")>
        <StringLength(50)>
        Public Property LastName As String

        <Required>
        <Column("FirstName")>
        <Display(Name:="First Name")>
        <StringLength(50)>
        Public Property FirstMidName As String

        <DataType(DataType.[Date])>
        <DisplayFormat(DataFormatString:="{0:yyyy-MM-dd}", ApplyFormatInEditMode:=True)>
        <Display(Name:="Hire Date")>
        Public Property HireDate As DateTime
        <Display(Name:="Full Name")>
        Public ReadOnly Property FullName As String
            Get
                Return LastName & ", " & FirstMidName
            End Get
        End Property

        'EF Core 5.0 supports many-to-many relationships without explicitly mapping the join table.
        Public Property CourseAssignments As ICollection(Of Course)
    End Class


End Namespace