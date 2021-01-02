
Imports System.Collections.Generic
Imports System.Linq

Namespace Utilities
    Friend MustInherit Class Graph(Of TVertex)

        Public MustOverride ReadOnly Property Vertices As IEnumerable(Of TVertex)
        Public MustOverride Sub Clear()
        Public MustOverride Function GetOutgoingNeighbors(from As TVertex) As IEnumerable(Of TVertex)
        Public MustOverride Function GetIncomingNeighbors([to] As TVertex) As IEnumerable(Of TVertex)

        Public Function GetUnreachableVertices(roots As IReadOnlyList(Of TVertex)) As ISet(Of TVertex)

            Dim unreachableVertices As HashSet(Of TVertex) = New HashSet(Of TVertex)(Vertices)

            unreachableVertices.ExceptWith(roots)
            Dim visitingQueue As List(Of TVertex) = New List(Of TVertex)(roots)

            Dim currentVertexIndex As Integer = 0
            While currentVertexIndex < visitingQueue.Count
                Dim currentVertex As TVertex = visitingQueue(currentVertexIndex)
                currentVertexIndex += 1
                ' ReSharper disable once LoopCanBeConvertedToQuery
                For Each neighbor As TVertex In GetOutgoingNeighbors(currentVertex)
                    If unreachableVertices.Remove(neighbor) Then
                        visitingQueue.Add(neighbor)
                    End If
                Next
            End While

            Return unreachableVertices
        End Function
        Public Function GetWeaklyConnectedComponents() As IList(Of ISet(Of TVertex))

            Dim components As List(Of ISet(Of TVertex)) = New List(Of ISet(Of TVertex))
            Dim unvisitedVertices As HashSet(Of TVertex) = New HashSet(Of TVertex)(Vertices)
            Dim neighbors As Queue(Of TVertex) = New Queue(Of TVertex)
            While unvisitedVertices.Count > 0
                Dim unvisitedVertex As TVertex = unvisitedVertices.First()
                Dim currentComponent As HashSet(Of TVertex) = New HashSet(Of TVertex)

                neighbors.Enqueue(unvisitedVertex)

                While neighbors.Count > 0
                    Dim currentVertex As TVertex = neighbors.Dequeue()
                    If currentComponent.Contains(currentVertex) Then
                        Continue While
                    End If

                    currentComponent.Add(currentVertex)
                    unvisitedVertices.Remove(currentVertex)
                    For Each neighbor As TVertex In GetOutgoingNeighbors(currentVertex)
                        neighbors.Enqueue(neighbor)
                    Next

                    For Each neighbor As TVertex In GetIncomingNeighbors(currentVertex)
                        neighbors.Enqueue(neighbor)
                    Next
                End While

                components.Add(currentComponent)
            End While

            Return components
        End Function
    End Class
End Namespace
