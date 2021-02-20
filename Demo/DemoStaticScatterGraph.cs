using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SciGraph.Demo
{

    public class DemoStaticScatterGraph : MonoBehaviour
    {
        [System.Serializable]
        public struct RandomPoints
        {
            public string   name;
            public int      nPoints;
            public Color    color;
        }

        public RandomPoints[]   randomPoints;

        ScatterGraph scatterGraph;

        // Start is called before the first frame update
        void Start()
        {
            scatterGraph = GetComponent<ScatterGraph>();

            foreach (var rp in randomPoints)
            {
                int graph_id = scatterGraph.AddSubgraph(rp.color, rp.name);

                List<Vector2> points = new List<Vector2>();
                for (int i = 0; i < rp.nPoints; i++)
                {
                    points.Add(new Vector2(Random.Range(-180, 180.0f), Random.Range(0.0f, 90.0f)));
                }

                scatterGraph.SetData(graph_id, points);
            }
        }

        private void Update()
        {
            scatterGraph.Refresh();
        }
    }
}
