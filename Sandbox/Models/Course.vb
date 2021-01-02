Imports System.ComponentModel.DataAnnotations.Schema

Namespace Models
    Public Class Course

        'This attribute allows entering the PK for the course rather than having the database generate it.
        <DatabaseGenerated(DatabaseGeneratedOption.None)>
        Public Property CourseID As Integer
        Public Property Title As String
        Public Property Credits As Integer

        Public Property Enrollments As ICollection(Of Enrollment)

        'EF Core 5.0 supports many-to-many relationships without explicitly mapping the join table.
        Public Property Instructors As ICollection(Of Instructor)
    End Class

End Namespace