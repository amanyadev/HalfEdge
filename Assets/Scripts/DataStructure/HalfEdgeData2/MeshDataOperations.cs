
namespace DataStructure.HalfEdge
{
    public static class MeshDataOperations
    {
        public static HalfEdgeData2 SplitEdge(this HalfEdgeData2 data2, HalfEdge2 edge, MyVector2 position)
        {
            return new SplitEdgeDataOperation(edge, position).Apply(data2);
            
        }
    }
}