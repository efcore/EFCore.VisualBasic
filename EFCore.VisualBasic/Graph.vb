Namespace Utilities
    Friend MustInherit Class Graph(Of TVertex)

        Public MustOverride ReadOnly Property Vertices As IEnumerable(Of TVertex)
        Public MustOverride Sub Clear()
        Public MustOverride Function GetOutgoingNeighbors(from As TVertex) As IEnumerable(Of TVertex)
        Public MustOverride Function GetIncomingNeighbors([to] As TVertex) As IEnumerable(Of TVertex)

        Public Function GetUnreachableVertices(roots As IReadOnlyList(Of TVertex)) As ISet(Of TVertex)

            Dim unreachableVertices As New HashSet(Of TVertex)(Vertices)
            unreachableVertices.ExceptWith(roots)

            Dim visitingQueue As New List(Of TVertex)(roots)

            Dim currentVertexIndex As Integer = 0
            While currentVertexIndex < visitingQueue.Count
                Dim currentVertex = visitingQueue(currentVertexIndex)
                currentVertexIndex += 1
                For Each neighbor As TVertex In GetOutgoingNeighbors(currentVertex)
                    If unreachableVertices.Remove(neighbor) Then
                        visitingQueue.Add(neighbor)
                    End If
                Next
            End While

            Return unreachableVertices
        End Function
    End Class
End Namespace
