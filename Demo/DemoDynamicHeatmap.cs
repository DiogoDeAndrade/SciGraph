using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SciGraph.Demo
{

    public class DemoDynamicHeatmap : MonoBehaviour
    {
        [System.Serializable]
        public struct RandomPoints
        {
            public string   name;
            public int      nPoints;
            public Color    color;
            public Gradient gradient;
            public float    weight;
        }

        public RandomPoints[]   randomPoints;

        public class Particle
        {
            public Vector2  pos;
            public Vector2  dir;
            public float    speed;
        }

        public struct Particles
        {
            public int              subgraph_id;
            public List<Particle>   particles;
        }

        public List<Particles>  allPS;

        Heatmap heatmap;

        // Start is called before the first frame update
        void Start()
        {
            heatmap = GetComponent<Heatmap>();

            allPS = new List<Particles>();
            foreach (var rp in randomPoints)
            {
                Particles ps = new Particles();

                ps.subgraph_id = heatmap.AddSubgraph(rp.gradient, rp.color, new Rect(-100.0f, -100.0f, 200.0f, 200.0f), 0.25f, rp.name);
                ps.particles = new List<Particle>();

                for (int i = 0; i < rp.nPoints; i++)
                {
                    ps.particles.Add(new Particle()
                    {
                        pos = new Vector2(Random.Range(-100.0f, 100.0f), Random.Range(-100.0f, 100.0f)),
                        speed = Random.Range(10.0f, 40.0f),
                        dir = Random.insideUnitCircle.normalized
                });
                }


                allPS.Add(ps);
            }
        }

        private void Update()
        {
            foreach (var ps in allPS)
            {
                List<Vector2> positions = new List<Vector2>();

                foreach (var p in ps.particles)
                {
                    Vector2 newPos = p.pos + p.dir * p.speed * Time.deltaTime;

                    if ((newPos.x <= -100.0f) || (newPos.y <= -100.0f) ||
                        (newPos.x >= 100.0f) || (newPos.y >= 100.0f))
                    {
                        p.dir = Random.insideUnitCircle.normalized;
                    }
                    else
                    {
                        p.pos = newPos;
                    }

                    positions.Add(p.pos);
                }

                heatmap.SetData(ps.subgraph_id, positions);
            }
        }
    }
}
