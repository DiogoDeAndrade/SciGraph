using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SciGraph.Demo
{

    public class DemoStaticPieGraph : MonoBehaviour
    {
        PieGraph pieGraph;
        
        void Start()
        {
            pieGraph = GetComponent<PieGraph>();
            pieGraph.title = "Pie Chart";

            int categoryId = pieGraph.AddCategory(Color.red, "Red");
            pieGraph.SetData(categoryId, 150);

            categoryId = pieGraph.AddCategory(Color.green, "Green");
            pieGraph.SetData(categoryId, 230);

            categoryId = pieGraph.AddCategory(Color.blue, "Blue");
            pieGraph.SetData(categoryId, 120);

            categoryId = pieGraph.AddCategory(Color.yellow, "Yellow");
            pieGraph.SetData(categoryId, 196);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}