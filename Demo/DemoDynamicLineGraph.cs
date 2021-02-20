using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SciGraph.Demo
{

    public class DemoDynamicLineGraph : MonoBehaviour
    {
        public Color    color1 = Color.red;
        public Color    color2 = Color.yellow;
        public float    frequency = 0.01f;

        LineGraph lineGraph;
        int       graph1;
        int       graph2;
        float     elapsed = 0;

        // Start is called before the first frame update
        void Start()
        {
            lineGraph = GetComponent<LineGraph>();

            graph1 = lineGraph.AddSubgraph(color1, "Sine");
            graph2 = lineGraph.AddSubgraph(color2, "Perlin");
        }

        void Update()
        {
            float value = 40.0f * Mathf.Sin(elapsed * frequency);
            lineGraph.AddDataPoint(graph1, elapsed, value);

            value = 20.0f * Mathf.PerlinNoise(elapsed * 0.01f, 0.0f) +
                    10.0f * Mathf.PerlinNoise(elapsed * 0.02f, 0.0f) +
                     5.0f * Mathf.PerlinNoise(elapsed * 0.04f, 0.0f) +
                     2.0f * Mathf.PerlinNoise(elapsed * 0.08f, 0.0f);
            lineGraph.AddDataPoint(graph2, elapsed, value);

            elapsed++;
        }
    }
}