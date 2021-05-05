Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.EntityFrameworkCore.Diagnostics

Namespace Utilities
    Friend Class Multigraph(Of TVertex, TEdge)
        Inherits Graph(Of TVertex)

        Private ReadOnly _vertices As New HashSet(Of TVertex)()
        Private ReadOnly _successorMap As New Dictionary(Of TVertex, Dictionary(Of TVertex, List(Of TEdge)))()
        Private ReadOnly _predecessorMap As New Dictionary(Of TVertex, HashSet(Of TVertex))()
        Public ReadOnly Property Edges As IEnumerable(Of TEdge)
            Get
                Return _successorMap.Values.SelectMany(Function(s) s.Values).SelectMany(Function(e) e).Distinct()
            End Get
        End Property
        Public Function GetEdges(from As TVertex, [to] As TVertex) As IEnumerable(Of TEdge)
            Dim successorSet As Dictionary(Of TVertex, List(Of TEdge)) = Nothing
            If _successorMap.TryGetValue(from, successorSet) Then
                Dim edgeList As List(Of TEdge) = Nothing
                If successorSet.TryGetValue([to], edgeList) Then
                    Return edgeList
                End If
            End If

            Return Enumerable.Empty(Of TEdge)()
        End Function
        Public Sub AddVertex(vertex As TVertex)
            Call _vertices.Add(vertex)
        End Sub
        Public Sub AddVertices(vertices As IEnumerable(Of TVertex))
            Call _vertices.UnionWith(vertices)
        End Sub
        Public Sub AddEdge(from As TVertex, [to] As TVertex, edge As TEdge)
            Dim successorEdges As Dictionary(Of TVertex, List(Of TEdge)) = Nothing
            If Not _successorMap.TryGetValue(from, successorEdges) Then
                successorEdges = New Dictionary(Of TVertex, List(Of TEdge))
                _successorMap.Add(from, successorEdges)
            End If

            Dim edgeList As List(Of TEdge) = Nothing
            If Not successorEdges.TryGetValue([to], edgeList) Then
                edgeList = New List(Of TEdge)
                successorEdges.Add([to], edgeList)
            End If

            edgeList.Add(edge)
            Dim predecessors As HashSet(Of TVertex) = Nothing
            If Not _predecessorMap.TryGetValue([to], predecessors) Then
                predecessors = New HashSet(Of TVertex)
                _predecessorMap.Add([to], predecessors)
            End If

            predecessors.Add(from)
        End Sub
        Public Sub AddEdges(from As TVertex, [to] As TVertex, edges1 As IEnumerable(Of TEdge))
            Dim successorEdges As Dictionary(Of TVertex, List(Of TEdge)) = Nothing
            If Not _successorMap.TryGetValue(from, successorEdges) Then
                successorEdges = New Dictionary(Of TVertex, List(Of TEdge))
                _successorMap.Add(from, successorEdges)
            End If

            Dim edgeList As List(Of TEdge) = Nothing
            If Not successorEdges.TryGetValue([to], edgeList) Then
                edgeList = New List(Of TEdge)
                successorEdges.Add([to], edgeList)
            End If

            edgeList.AddRange(edges1)
            Dim predecessors As HashSet(Of TVertex) = Nothing
            If Not _predecessorMap.TryGetValue([to], predecessors) Then
                predecessors = New HashSet(Of TVertex)
                _predecessorMap.Add([to], predecessors)
            End If

            predecessors.Add(from)
        End Sub
        Public Overrides Sub Clear()
            _vertices.Clear()
            _successorMap.Clear()
            _predecessorMap.Clear()
        End Sub
        Public Function TopologicalSort() As IReadOnlyList(Of TVertex)
            Return TopologicalSort(Nothing, Nothing)
        End Function
        Public Function TopologicalSort(
            tryBreakEdge As Func(Of TVertex, TVertex, IEnumerable(Of TEdge), Boolean)) As IReadOnlyList(Of TVertex)
            Return TopologicalSort(tryBreakEdge, Nothing)
        End Function
        Public Function TopologicalSort(
            formatCycle As Func(Of IEnumerable(Of Tuple(Of TVertex, TVertex, IEnumerable(Of TEdge))), String)) As IReadOnlyList(Of TVertex)
            Return TopologicalSort(Nothing, formatCycle)
        End Function
        Public Function TopologicalSort(
            tryBreakEdge As Func(Of TVertex, TVertex, IEnumerable(Of TEdge), Boolean),
            formatCycle As Func(Of IReadOnlyList(Of Tuple(Of TVertex, TVertex, IEnumerable(Of TEdge))), String),
            Optional formatException As Func(Of String, String) = Nothing) As IReadOnlyList(Of TVertex)
            Dim sortedQueue As List(Of TVertex) = New List(Of TVertex)
            Dim predecessorCounts As Dictionary(Of TVertex, Integer) = New Dictionary(Of TVertex, Integer)

            For Each vertex As TVertex In _vertices
                For Each outgoingNeighbor As TVertex In GetOutgoingNeighbors(vertex)
                    If predecessorCounts.ContainsKey(outgoingNeighbor) Then
                        predecessorCounts(outgoingNeighbor) += 1
                    Else
                        predecessorCounts(outgoingNeighbor) = 1
                    End If
                Next
            Next

            For Each vertex As TVertex In _vertices
                If Not predecessorCounts.ContainsKey(vertex) Then
                    sortedQueue.Add(vertex)
                End If
            Next

            Dim index As Integer = 0
            While sortedQueue.Count < _vertices.Count
                While index < sortedQueue.Count
                    Dim currentRoot As TVertex = sortedQueue(index)

                    For Each successor As TVertex In GetOutgoingNeighbors(currentRoot).Where(Function(neighbor) predecessorCounts.ContainsKey(neighbor))
                        ' Decrement counts for edges from sorted vertices and append any vertices that no longer have predecessors
                        predecessorCounts(successor) -= 1
                        If predecessorCounts(successor) = 0 Then
                            sortedQueue.Add(successor)
                            predecessorCounts.Remove(successor)
                        End If
                    Next

                    index += 1
                End While

                ' Cycle breaking
                If sortedQueue.Count < _vertices.Count Then
                    Dim broken As Boolean = False

                    Dim candidateVertices As List(Of TVertex) = predecessorCounts.Keys.ToList()
                    Dim candidateIndex As Integer = 0

                    ' Iterate over the unsorted vertices
                    While (candidateIndex < candidateVertices.Count) AndAlso Not broken _
                        AndAlso tryBreakEdge IsNot Nothing
                        Dim candidateVertex As TVertex = candidateVertices(candidateIndex)

                        ' Find vertices in the unsorted portion of the graph that have edges to the candidate
                        Dim incomingNeighbors As List(Of TVertex) = GetIncomingNeighbors(candidateVertex) _
                            .Where(Function(neighbor) predecessorCounts.ContainsKey(neighbor)).ToList()

                        For Each incomingNeighbor As TVertex In incomingNeighbors
                            ' Check to see if the edge can be broken
                            If tryBreakEdge(incomingNeighbor, candidateVertex, _successorMap(incomingNeighbor)(candidateVertex)) Then
                                predecessorCounts(candidateVertex) -= 1
                                If predecessorCounts(candidateVertex) = 0 Then
                                    sortedQueue.Add(candidateVertex)
                                    predecessorCounts.Remove(candidateVertex)
                                    broken = True
                                    Exit For
                                End If
                            End If
                        Next

                        candidateIndex += 1
                    End While

                    If Not broken Then
                        ' Failed to break the cycle
                        Dim currentCycleVertex As TVertex = _vertices.First(Function(v) predecessorCounts.ContainsKey(v))
                        Dim cycle As List(Of TVertex) = New List(Of TVertex) From {
                            currentCycleVertex}
                        Dim finished As Boolean = False
                        While Not finished
                            ' Find a cycle
                            For Each predecessor As TVertex In GetIncomingNeighbors(currentCycleVertex) _
                                .Where(Function(neighbor) predecessorCounts.ContainsKey(neighbor))
                                If predecessorCounts(predecessor) <> 0 Then
                                    predecessorCounts(currentCycleVertex) = -1

                                    currentCycleVertex = predecessor
                                    cycle.Add(currentCycleVertex)
                                    finished = predecessorCounts(predecessor) = -1
                                    Exit For
                                End If
                            Next
                        End While

                        cycle.Reverse()

                        ThrowCycle(cycle, formatCycle, formatException)
                    End If
                End If
            End While

            Return sortedQueue
        End Function
        Private Sub ThrowCycle(
            cycle As List(Of TVertex),
            formatCycle As Func(Of IReadOnlyList(Of Tuple(Of TVertex, TVertex, IEnumerable(Of TEdge))), String),
            Optional formatException As Func(Of String, String) = Nothing)
            Dim cycleString As String
            If formatCycle Is Nothing Then
                cycleString = cycle.Select(New Func(Of TVertex, String)(AddressOf ToString)).Join(" ->" & Environment.NewLine)
            Else
                Dim currentCycleVertex As TVertex = cycle.First()
                Dim cycleData As List(Of Tuple(Of TVertex, TVertex, IEnumerable(Of TEdge))) = New List(Of Tuple(Of TVertex, TVertex, IEnumerable(Of TEdge)))

                For Each vertex As TVertex In cycle.Skip(1)
                    cycleData.Add(Tuple.Create(currentCycleVertex, vertex, GetEdges(currentCycleVertex, vertex)))
                    currentCycleVertex = vertex
                Next

                cycleString = formatCycle(cycleData)
            End If

            Dim message = If(formatException Is Nothing, CoreStrings.CircularDependency(cycleString), formatException(cycleString))
            Throw New InvalidOperationException(message)
        End Sub
        Protected Overridable Overloads Function ToString(vertex As TVertex) As String
            Return vertex.ToString()
        End Function
        Public Function BatchingTopologicalSort() As IReadOnlyList(Of List(Of TVertex))
            Return BatchingTopologicalSort(Nothing)
        End Function
        Public Function BatchingTopologicalSort(
            formatCycle As Func(Of IReadOnlyList(Of Tuple(Of TVertex, TVertex, IEnumerable(Of TEdge))), String)) As IReadOnlyList(Of List(Of TVertex))
            Dim currentRootsQueue As List(Of TVertex) = New List(Of TVertex)
            Dim predecessorCounts As Dictionary(Of TVertex, Integer) = New Dictionary(Of TVertex, Integer)

            For Each vertex As TVertex In _vertices
                For Each outgoingNeighbor As TVertex In GetOutgoingNeighbors(vertex)
                    If predecessorCounts.ContainsKey(outgoingNeighbor) Then
                        predecessorCounts(outgoingNeighbor) += 1
                    Else
                        predecessorCounts(outgoingNeighbor) = 1
                    End If
                Next
            Next

            For Each vertex As TVertex In _vertices
                If Not predecessorCounts.ContainsKey(vertex) Then
                    currentRootsQueue.Add(vertex)
                End If
            Next

            Dim result As List(Of List(Of TVertex)) = New List(Of List(Of TVertex))
            Dim nextRootsQueue As List(Of TVertex) = New List(Of TVertex)
            Dim currentRootIndex As Integer = 0

            While currentRootIndex < currentRootsQueue.Count
                Dim currentRoot As TVertex = currentRootsQueue(currentRootIndex)
                currentRootIndex += 1

                ' Remove edges from current root and add any exposed vertices to the next batch
                For Each successor As TVertex In GetOutgoingNeighbors(currentRoot)
                    predecessorCounts(successor) -= 1
                    If predecessorCounts(successor) = 0 Then
                        nextRootsQueue.Add(successor)
                    End If
                Next

                ' Roll lists over for next batch
                If currentRootIndex = currentRootsQueue.Count Then
                    result.Add(currentRootsQueue)

                    currentRootsQueue = nextRootsQueue
                    currentRootIndex = 0

                    If currentRootsQueue.Count <> 0 Then
                        nextRootsQueue = New List(Of TVertex)
                    End If
                End If
            End While

            If result.Sum(Function(b) b.Count) <> _vertices.Count Then
                Dim predecessorNumber As Integer = Nothing
                Dim currentCycleVertex As TVertex = _vertices.First(
                                    Function(v) If(predecessorCounts.TryGetValue(v, predecessorNumber), predecessorNumber <> 0, False))
                Dim cyclicWalk As List(Of TVertex) = New List(Of TVertex) From {
                    currentCycleVertex}
                Dim finished As Boolean = False
                While Not finished
                    For Each predecessor As TVertex In GetIncomingNeighbors(currentCycleVertex)
                        Dim predecessorCount As Integer = Nothing
                        If Not predecessorCounts.TryGetValue(predecessor, predecessorCount) Then
                            Continue While
                        End If

                        If predecessorCount <> 0 Then
                            predecessorCounts(currentCycleVertex) = -1

                            currentCycleVertex = predecessor
                            cyclicWalk.Add(currentCycleVertex)
                            finished = predecessorCounts(predecessor) = -1
                            Exit For
                        End If
                    Next
                End While

                cyclicWalk.Reverse()

                Dim cycle As List(Of TVertex) = New List(Of TVertex)
                Dim startingVertex As TVertex = cyclicWalk.First()
                cycle.Add(startingVertex)
                For Each vertex As TVertex In cyclicWalk.Skip(1)
                    If Not vertex.Equals(startingVertex) Then
                        cycle.Add(vertex)
                    Else
                        Exit For
                    End If
                Next

                cycle.Add(startingVertex)

                ThrowCycle(cycle, formatCycle)
            End If

            Return result
        End Function
        Public Overrides ReadOnly Property Vertices As IEnumerable(Of TVertex)
            Get
                Return _vertices
            End Get
        End Property
        Public Overrides Function GetOutgoingNeighbors(from As TVertex) As IEnumerable(Of TVertex)
            Dim successorSet As Dictionary(Of TVertex, List(Of TEdge)) = Nothing
            Return If(_successorMap.TryGetValue(from, successorSet), successorSet.Keys _
                            , Enumerable.Empty(Of TVertex)())
        End Function
        Public Overrides Function GetIncomingNeighbors([to] As TVertex) As IEnumerable(Of TVertex)
            Dim predecessors As HashSet(Of TVertex) = Nothing
            Return If(_predecessorMap.TryGetValue([to], predecessors), predecessors _
                            , Enumerable.Empty(Of TVertex)())
        End Function
    End Class
End Namespace
