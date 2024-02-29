namespace DataStructure.HalfEdge
{
    public class SplitEdgeDataOperation : IDataOperation
    {
        private readonly HalfEdge2 _edge;
        private readonly MyVector2 _newVertexPosition;

        public HalfEdgeData2 Apply(HalfEdgeData2 data)
        {
            return data;
        }

        /// <summary>
        /// Performs split operation on Half-Edge. 
        /// </summary>
        /// <param name="data"> HalfEdgeData</param>
        /// <param name="origin_to_position">First HalfEdge from a to splitting position</param>
        /// <param name="position_to_destination">Second HalfEdge from splitting position to destination</param>
        /// <param name="insertedVertex">the new vertex which was inserted </param>
        /// <returns></returns>
        public HalfEdgeData2 Apply(HalfEdgeData2 data, out HalfEdge2 origin_to_position, out HalfEdge2 position_to_destination, out HalfEdgeVertex2 insertedVertex)
        {
            //Create the new vertex
            insertedVertex = data.CreateNewVertex(_newVertexPosition);

            data.vertices.Add(insertedVertex);
            HalfEdgeVertex2 origin = _edge.origin;
            HalfEdgeVertex2 midPoint = insertedVertex;
            HalfEdgeVertex2 destination = _edge.Twin.origin;

            //Create the new edges
            HalfEdge2 origin_to_midPoint = data.CreateNewEdge(origin);
            HalfEdge2 origin_to_midPoint_twin = data.CreateNewEdge(midPoint);

            HalfEdge2 midPoint_to_destination =data.CreateNewEdge(midPoint);
            HalfEdge2 midPoint_to_destination_twin = data.CreateNewEdge(destination);

            // set twins
            origin_to_midPoint.SetTwin(origin_to_midPoint_twin);
            origin_to_midPoint_twin.SetTwin(origin_to_midPoint); 
            
            midPoint_to_destination.SetTwin(midPoint_to_destination_twin); 
            midPoint_to_destination_twin.SetTwin(midPoint_to_destination); 

            //origin to midpoint
            if (_edge.PrevEdge.Equals(_edge.Twin))
            {
                origin_to_midPoint.SetPrevEdge(origin_to_midPoint_twin);
            }
            else
            {
                origin_to_midPoint.SetPrevEdge(_edge.PrevEdge);    
            }
     
            origin_to_midPoint.SetNextEdge(midPoint_to_destination);

            origin_to_midPoint_twin.SetPrevEdge(midPoint_to_destination_twin);

            if (_edge.Twin.NextEdge.Equals(_edge))
            {
                origin_to_midPoint_twin.SetNextEdge(origin_to_midPoint);
            }
            else
            {
                origin_to_midPoint_twin.SetNextEdge(_edge.Twin.NextEdge); 
            }

            // midpoint to destination:
            midPoint_to_destination.SetPrevEdge(origin_to_midPoint);

            if (_edge.NextEdge == _edge.Twin)
            {
                midPoint_to_destination.SetNextEdge(midPoint_to_destination_twin);
            }
            else
            {
                midPoint_to_destination.SetNextEdge(_edge.NextEdge);    
            }


            if (_edge.Twin.PrevEdge == _edge.Twin)
            {
                midPoint_to_destination_twin.SetPrevEdge(midPoint_to_destination);
            }
            else
            {
                midPoint_to_destination_twin.SetPrevEdge(_edge.Twin.PrevEdge);    
            }
            midPoint_to_destination_twin.SetNextEdge(origin_to_midPoint_twin); 

            //existing edge connections:
            if (_edge.PrevEdge != null && _edge.PrevEdge != _edge.Twin)
                _edge.PrevEdge.SetNextEdge(origin_to_midPoint);
            if (_edge.NextEdge != null && _edge.NextEdge != _edge.Twin)
                _edge.NextEdge.SetPrevEdge(midPoint_to_destination); 
            if (_edge.Twin.PrevEdge != null && _edge.Twin.PrevEdge != _edge)
                _edge.Twin.PrevEdge.SetNextEdge(midPoint_to_destination.Twin); 
            if (_edge.Twin.NextEdge != null && _edge.Twin.NextEdge != _edge)
            {
                _edge.Twin.NextEdge.SetPrevEdge(origin_to_midPoint.Twin);
            }

            // assign faces: 
            origin_to_midPoint.SetFace(_edge.Face); 
            midPoint_to_destination.SetFace(_edge.Face);
            
            origin_to_midPoint_twin.SetFace( _edge.Twin.Face);  
            midPoint_to_destination_twin.SetFace( _edge.Twin.Face);
            
            data.edges.Add(origin_to_midPoint);
            data.edges.Add(origin_to_midPoint_twin);

            data.edges.Add(midPoint_to_destination);
            data.edges.Add(midPoint_to_destination_twin);
            
            data.edges.Remove(_edge);
            data.edges.Remove(_edge.Twin);
            
            if (_edge.Face != null)
            {
                foreach (var edge in data.edges)
                {
                    if (edge.Face == null) continue;
                   
                    
                    if (edge.Face.Equals(_edge.Face))
                        edge.Face.edge = origin_to_midPoint;
                }
            }

        
            if (_edge.Twin.Face != null)
            {
                // origin_to_midPoint_twin.face.edge = origin_to_midPoint_twin;

                foreach (var edge in data.edges)
                {
                    if (edge.Face == null) continue;
                    
                    if (edge.Face.Equals(_edge.Twin.Face))
                        edge.Face.edge = origin_to_midPoint_twin;
                }
            }
            
            origin_to_position = origin_to_midPoint;
            position_to_destination = midPoint_to_destination;

            return data;
        }

        /// <summary>
        ///  This is the constructor for the SplitEdgeDataOperation
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="newVertexPosition"></param>
        public SplitEdgeDataOperation(HalfEdge2 edge, MyVector2 newVertexPosition)
        {
            _edge = edge;
            _newVertexPosition = newVertexPosition;
        }
    }
}