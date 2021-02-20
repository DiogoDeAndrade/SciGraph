using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SciGraph.Demo
{

    public class DemoDynamicHistGraph : MonoBehaviour
    {
        public Color    color1 = Color.red;
        public Color    color2 = Color.yellow;

        HistGraph histGraph;
        int       graph1;
        int       graph2;

        HistGraph.Bin[] data1;
        HistGraph.Bin[] data2;

        // Start is called before the first frame update
        void Start()
        {
            histGraph = GetComponent<HistGraph>();

            graph1 = histGraph.AddSubgraph(color1, "First type");
            graph2 = histGraph.AddSubgraph(color2, "Another type");

            data1 = new HistGraph.Bin[histGraph.nBins];
            data2 = new HistGraph.Bin[histGraph.nBins];

            for (int i = 0; i < histGraph.nBins; i++)
            {
                data1[i].range = new Vector2(i, i + 1);
                data1[i].value = Random.Range(0, 100);
                data2[i].range = new Vector2(i, i + 1);
                data2[i].value = Random.Range(0, 100);
            }
        }

        void Update()
        {
            HistGraph.Bin[] dataToChange;
            
            int d = Random.Range(0, 100);
            if (d < 50)
                dataToChange = data1;
            else
                dataToChange = data2;

            int bin = Random.Range(0, histGraph.nBins);
            int inc = Random.Range(-2, 2);

            dataToChange[bin].value = Mathf.Clamp(dataToChange[bin].value + inc, 0, 100);

            histGraph.SetData(graph1, data1);
            histGraph.SetData(graph2, data2);
        }
    }
}