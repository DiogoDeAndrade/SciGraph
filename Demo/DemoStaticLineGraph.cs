using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SciGraph.Demo
{

    public class DemoStaticLineGraph : MonoBehaviour
    {
        public Color    color1 = Color.red;
        public Color    color2 = Color.yellow;

        LineGraph lineGraph;

        // Start is called before the first frame update
        void Start()
        {
            lineGraph = GetComponent<LineGraph>();

            int graph1 = lineGraph.AddSubgraph(color1, "Sine");
            int graph2 = lineGraph.AddSubgraph(color2, "Perlin");

            List<float> data1 = new List<float>();
            for (int i = 0; i < 500; i++)
            {
                float value = 40.0f * Mathf.Sin(i * 0.01f);
                data1.Add(value);
            }
            lineGraph.SetData(graph1, data1);

            List<float> data2 = new List<float>();
            for (int i = 0; i < 500; i++)
            {
                float value = 20.0f * Mathf.PerlinNoise(i * 0.01f, 0.0f) +
                              10.0f * Mathf.PerlinNoise(i * 0.02f, 0.0f) +
                               5.0f * Mathf.PerlinNoise(i * 0.04f, 0.0f) +
                               2.0f * Mathf.PerlinNoise(i * 0.08f, 0.0f);
                data2.Add(value);
            }
            lineGraph.SetData(graph2, data2);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}