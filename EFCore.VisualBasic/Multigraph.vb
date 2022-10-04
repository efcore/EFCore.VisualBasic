Imports Microsoft.EntityFrameworkCore.Diagnostics

Namespace Utilities
    Friend Class Multigraph(Of TVertex, TEdge)
        Inherits Graph(Of TVertex)

        Private ReadOnly _secondarySortComparer As IComparer(Of TVertex)
        Private ReadOnly _vertices As New HashSet(Of TVertex)()
        Private ReadOnly _successorMap As New Dictionary(Of TVertex, Dictionary(Of TVertex, Object))()
        Private ReadOnly _predecessorMap As New Dictionary(Of TVertex, Dictionary(Of TVertex, Object))()

        Sub New()
        End Sub

        Public Sub New(secondarySortComparer As IComparer(Of TVertex))
            _secondarySortComparer = secondarySortComparer
        End Sub

        Public Sub New(secondarySortComparer As Comparison(Of TVertex))
            Me.New(Comparer(Of TVertex).Create(secondarySortComparer))
        End Sub

        Public Function GetEdges(from As TVertex, [to] As TVertex) As IEnumerable(Of TEdge)
            Dim successorSet As Dictionary(Of TVertex, Object) = Nothing
            If _successorMap.TryGetValue(from, successorSet) Then
                Dim edges As Object = Nothing
                If successorSet.TryGetValue([to], edges) Then

                    If TypeOf edges Is IEnumerable(Of Edge) Then
                        Dim edgeList = DirectCast(edges, IEnumerable(Of Edge))
                        Return edgeList.Select(Function(e) e.Payload)
                    Else
                        Return {DirectCast(edges, Edge).Payload}
                    End If
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

        Public Sub AddEdge(from As TVertex,
                           [to] As TVertex,
                           payload As TEdge,
                           Optional requiresBatchingBoundary As Boolean = False)


            Dim edge As New Edge(payload, requiresBatchingBoundary)
            Dim successorEdges As Dictionary(Of TVertex, Object) = Nothing
            If Not _successorMap.TryGetValue(from, successorEdges) Then
                successorEdges = New Dictionary(Of TVertex, Object)
                _successorMap.Add(from, successorEdges)
            End If

            Dim edges As Object = Nothing
            If successorEdges.TryGetValue([to], edges) Then

                Dim edgeList As List(Of Edge)
                If TypeOf edges Is List(Of Edge) Then
                    edgeList = DirectCast(edges, List(Of Edge))
                Else
                    edgeList = New List(Of Edge) From {DirectCast(edges, Edge)}
                    successorEdges([to]) = edgeList
                End If

                edgeList.Add(Edge)
            Else
                successorEdges.Add([to], Edge)
            End If

            Dim predecessorEdges As Dictionary(Of TVertex, Object) = Nothing
            If Not _predecessorMap.TryGetValue([to], predecessorEdges) Then
                predecessorEdges = New Dictionary(Of TVertex, Object)
                _predecessorMap.Add([to], predecessorEdges)
            End If

            edges = Nothing
            If predecessorEdges.TryGetValue(from, edges) Then

                Dim edgeList As List(Of Edge)
                If TypeOf edges Is List(Of Edge) Then
                    edgeList = DirectCast(edges, List(Of Edge))
                Else
                    edgeList = New List(Of Edge) From {DirectCast(edges, Edge)}
                    predecessorEdges(from) = edgeList
                End If

                edgeList.Add(edge)

            Else
                predecessorEdges.Add(from, edge)
            End If
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

            Dim batches = TopologicalSortCore(withBatching:=False,
                                              tryBreakEdge,
                                              formatCycle,
                                              formatException)

            Debug.Assert(batches.Count < 2, "TopologicalSortCore did batching but withBatching was false")

            Return If(batches.Count = 1,
                        batches(0),
                        DirectCast(Array.Empty(Of TVertex), IReadOnlyList(Of TVertex)))
        End Function

        Protected Overridable Overloads Function ToString(vertex As TVertex) As String
            Return vertex.ToString()
        End Function

        Public Function BatchingTopologicalSort() As IReadOnlyList(Of List(Of TVertex))
            Return BatchingTopologicalSort(Nothing, Nothing)
        End Function

        Public Function BatchingTopologicalSort(
            tryBreakEdge As Func(Of TVertex, TVertex, IEnumerable(Of TEdge), Boolean),
            formatCycle As Func(Of IReadOnlyList(Of Tuple(Of TVertex, TVertex, IEnumerable(Of TEdge))), String),
            Optional formatException As Func(Of String, String) = Nothing) As IReadOnlyList(Of List(Of TVertex))

            Return TopologicalSortCore(withBatching:=True, tryBreakEdge, formatCycle, formatException)
        End Function

        Private Function TopologicalSortCore(
            withBatching As Boolean,
            tryBreakEdge As Func(Of TVertex, TVertex, IEnumerable(Of TEdge), Boolean),
            formatCycle As Func(Of IReadOnlyList(Of Tuple(Of TVertex, TVertex, IEnumerable(Of TEdge))), String),
            Optional formatException As Func(Of String, String) = Nothing) As IReadOnlyList(Of List(Of TVertex))

            ' Performs a breadth-first topological sort (Kahn's algorithm)
            Dim result As New List(Of List(Of TVertex))()
            Dim currentRootsQueue As New List(Of TVertex)()
            Dim nextRootsQueue As New List(Of TVertex)()
            Dim vertexesProcessed = 0
            Dim batchBoundaryRequired = False
            Dim currentBatch As New List(Of TVertex)()
            Dim currentBatchSet As New HashSet(Of TVertex)()

            Dim predecessorCounts As New Dictionary(Of TVertex, Integer)(_predecessorMap.Count)

            For Each predecessor In _predecessorMap
                predecessorCounts(predecessor.Key) = predecessor.Value.Count
            Next

            ' Bootstrap the topological sort by finding all vertexes which have no predecessors
            For Each vertex In _vertices
                If Not predecessorCounts.ContainsKey(vertex) Then
                    currentRootsQueue.Add(vertex)
                End If
            Next

            result.Add(currentBatch)

            While vertexesProcessed < _vertices.Count
                While currentRootsQueue.Count > 0

                    ' Secondary sorting: after the first topological sorting (according to dependencies between the commands as expressed in
                    ' the graph), we apply an optional secondary sort.
                    ' When sorting modification commands, this ensures a deterministic ordering And prevents deadlocks between concurrent
                    ' transactions locking the same rows in different orders.
                    If _secondarySortComparer IsNot Nothing Then
                        currentRootsQueue.Sort(_secondarySortComparer)
                    End If

                    ' If we detected in the last roots pass that a batch boundary Is required, close the current batch And start a New one.
                    If batchBoundaryRequired Then
                        currentBatch = New List(Of TVertex)()
                        result.Add(currentBatch)
                        currentBatchSet.Clear()

                        batchBoundaryRequired = False
                    End If

                    For Each currentRoot In currentRootsQueue

                        currentBatch.Add(currentRoot)
                        currentBatchSet.Add(currentRoot)
                        vertexesProcessed += 1

                        For Each successor In GetOutgoingNeighbors(currentRoot)

                            predecessorCounts(successor) -= 1

                            ' If the successor has no other predecessors, add it for processing in the next roots pass.
                            If predecessorCounts(successor) = 0 Then

                                nextRootsQueue.Add(successor)

                                ' Detect batch boundary (if batching Is enabled).
                                ' If the successor has any predecessor where the edge requires a batching boundary, And that predecessor Is
                                ' already in the current batch, then the next batch will have to be executed in a separate batch.
                                ' TODO: Optimization : Instead of currentBatchSet, store a batch counter On Each vertex, And check if later
                                ' vertexes have a boundary-requiring dependency on a vertex with the same batch counter.
                                If withBatching AndAlso
                                   _predecessorMap(successor).Any(
                                    Function(kv)

                                        If TypeOf kv.Value Is Edge Then
                                            If DirectCast(kv.Value, Edge).RequiresBatchingBoundary Then
                                                Return True
                                            End If
                                        End If

                                        If TypeOf kv.Value Is IEnumerable(Of Edge) Then
                                            Dim edges = DirectCast(kv.Value, IEnumerable(Of Edge))

                                            If edges.Any(Function(e) e.RequiresBatchingBoundary) AndAlso
                                               currentBatchSet.Contains(kv.Key) Then

                                                Return True
                                            End If
                                        End If

                                        Return False
                                    End Function) Then

                                    batchBoundaryRequired = True
                                End If
                            End If
                        Next
                    Next

                    ' Finished passing over the current roots, move on to the next set.
                    Dim temp = currentRootsQueue
                    currentRootsQueue = nextRootsQueue
                    nextRootsQueue = temp
                    nextRootsQueue.Clear()
            End While

                ' We have no more roots to process. That either means we're done, or that there's a cycle which we need to break
                If vertexesProcessed < _vertices.Count Then
                    Dim broken = False

                    Dim candidateVertices = predecessorCounts.Keys.ToList()
                    Dim candidateIndex = 0

                    While candidateIndex < candidateVertices.Count AndAlso
                                              Not broken AndAlso
                                              tryBreakEdge IsNot Nothing

                        Dim candidateVertex = candidateVertices(candidateIndex)
                        If predecessorCounts(candidateVertex) = 0 Then
                            candidateIndex += 1
                            Continue While
                        End If

                        ' Find a vertex in the unsorted portion of the graph that has edges to the candidate
                        Dim incomingNeighbor = GetIncomingNeighbors(candidateVertex).
                                                First(Function(neighbor)
                                                          Dim neighborPredecessors = 0
                                                          Return predecessorCounts.TryGetValue(neighbor, neighborPredecessors) AndAlso
                                                                    neighborPredecessors > 0
                                                      End Function)

                        If tryBreakEdge(incomingNeighbor, candidateVertex, GetEdges(incomingNeighbor, candidateVertex)) Then

                            Dim removed = _successorMap(incomingNeighbor).Remove(candidateVertex)
                            Debug.Assert(removed, "Candidate vertex not found in successor map")
                            removed = _predecessorMap(candidateVertex).Remove(incomingNeighbor)
                            Debug.Assert(removed, "Incoming neighbor not found in predecessor map")

                            predecessorCounts(candidateVertex) -= 1
                            If predecessorCounts(candidateVertex) = 0 Then
                                currentRootsQueue.Add(candidateVertex)
                                broken = True
                            End If
                            Continue While
                        End If

                        candidateIndex += 1
                    End While

                    If broken Then
                        Continue While
                    End If

                    Dim currentCycleVertex = _vertices.First(Function(v)
                                                                 Dim predecessorCount = 0
                                                                 Return predecessorCounts.TryGetValue(v, predecessorCount) AndAlso
                                                                        predecessorCount <> 0
                                                             End Function)
                    Dim cycle As New List(Of TVertex) From {currentCycleVertex}
                    Dim finished = False

                    While Not finished
                        For Each predecessor In GetIncomingNeighbors(currentCycleVertex)
                            Dim predecessorCount = 0
                            If Not predecessorCounts.TryGetValue(predecessor, predecessorCount) OrElse
                               predecessorCount = 0 Then

                                Continue For
                            End If

                            predecessorCounts(currentCycleVertex) = -1

                            currentCycleVertex = predecessor
                            cycle.Add(currentCycleVertex)
                            finished = predecessorCounts(predecessor) = -1
                            Exit For
                        Next
                    End While

                    cycle.Reverse()

                    ' Remove any tail that's not part of the cycle
                    Dim startingVertex = cycle(0)
                    For i = cycle.Count - 1 To 0 Step -1
                        If cycle(i).Equals(startingVertex) Then
                            Exit For
                        End If

                        cycle.RemoveAt(i)
                    Next

                    ThrowCycle(cycle, formatCycle, formatException)
                End If
            End While

            Return result
        End Function

        Private Sub ThrowCycle(cycle As List(Of TVertex),
                               formatCycle As Func(Of IReadOnlyList(Of Tuple(Of TVertex, TVertex, IEnumerable(Of TEdge))), String),
                               Optional formatException As Func(Of String, String) = Nothing)

            Dim cycleString As String
            If formatCycle Is Nothing Then
                cycleString = String.Join(" ->" & Environment.NewLine, cycle.Select(Function(e) ToString(e)))
            Else
                Dim currentCycleVertex = cycle.First()
                Dim cycleData As New List(Of Tuple(Of TVertex, TVertex, IEnumerable(Of TEdge)))

                For Each vertex As TVertex In cycle.Skip(1)
                    cycleData.Add(Tuple.Create(currentCycleVertex, vertex, GetEdges(currentCycleVertex, vertex)))
                    currentCycleVertex = vertex
                Next

                cycleString = formatCycle(cycleData)
            End If

            Dim message = If(formatException Is Nothing, CoreStrings.CircularDependency(cycleString), formatException(cycleString))
            Throw New InvalidOperationException(message)
        End Sub

        Public Overrides ReadOnly Property Vertices As IEnumerable(Of TVertex)
            Get
                Return _vertices
            End Get
        End Property

        Public Overrides Function GetOutgoingNeighbors(from As TVertex) As IEnumerable(Of TVertex)
            Dim successorSet As Dictionary(Of TVertex, Object) = Nothing
            Return If(_successorMap.TryGetValue(from, successorSet),
                        successorSet.Keys,
                        Enumerable.Empty(Of TVertex)())
        End Function

        Public Overrides Function GetIncomingNeighbors([to] As TVertex) As IEnumerable(Of TVertex)
            Dim predecessors As Dictionary(Of TVertex, Object) = Nothing
            Return If(_predecessorMap.TryGetValue([to], predecessors),
                        predecessors.keys,
                        Enumerable.Empty(Of TVertex)())
        End Function

        Private Structure Edge
            ReadOnly Property Payload As TEdge
            ReadOnly Property RequiresBatchingBoundary As Boolean
            Sub New(payload As TEdge, requiresBatchingBoundary As Boolean)
                Me.Payload = payload
                Me.RequiresBatchingBoundary = requiresBatchingBoundary
            End Sub
        End Structure
    End Class
End Namespace
