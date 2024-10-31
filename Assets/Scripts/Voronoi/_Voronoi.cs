using System.Collections.Generic;

namespace Habrador_Computational_Geometry
{
    //Generates Voronoi diagrams with different algorithms
    public static class _Voronoi
    {
        //Algorithm 1. Delaunay to Voronoi (Will also generate the delaunay)
        public static HashSet<VoronoiCell2> DelaunayToVoronoi(HashSet<MyVector2> sites)
        {
            HashSet<VoronoiCell2> voronoiCells = DelaunayToVoronoiAlgorithm.GenerateVoronoiDiagram(sites);

            return voronoiCells;
        }
    }
}
