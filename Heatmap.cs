using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.UI.Extensions;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SciGraph
{

    public class Heatmap : MonoBehaviour
    {
        public bool         background = true;
        [ShowIf("background")]
        public Color        backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.85f);
        public bool         displayLegend = false;
        [ShowIf("displayLegend")]
        public Vector2      legendOffset = Vector2.zero;
        public Color        textColor = Color.yellow;
        public float        titleTextSize = 14;
        public Vector2Int   resolution = new Vector2Int(10, 10);
        public float        normalizationFactor = 1.0f;
        public bool         decay;
        [ShowIf("decay")]
        public float        decayRate = 0.1f;

        [BoxGroup("Graph Area")]
        public Vector2      minGraphArea = new Vector2(0.15f, 0.15f);
        [BoxGroup("Graph Area")]
        public Vector2      maxGraphArea = new Vector2(0.95f, 0.85f);
        [BoxGroup("Graph Area")]
        public Vector2      rangeX;
        [BoxGroup("Graph Area")]
        public Vector2      rangeY;
        [BoxGroup("Axis")]
        public bool         displayAxisX = true;
        [BoxGroup("Axis")]
        public bool         displayAxisY = true;
        [BoxGroup("Axis")]
        public float        axisTextSize = 10;
        [BoxGroup("Axis")]
        public string       labelAxisX = "AxisX";
        [BoxGroup("Axis")]
        public string       labelAxisY = "AxisY";
        [BoxGroup("Axis")]
        public string       labelFormatX = "0.00";
        [BoxGroup("Axis")]
        public string       labelFormatY = "0.00";

        string          _title;
        bool            dirty;
        bool            layoutDirty;
        TextMeshProUGUI titleElement;
        RectTransform   rectTransform;
        Rect            prevRect;
        RectTransform   graphingArea;
        Axis            axisX;
        Axis            axisY;
        Legend          legend;
        Texture2D       texture;
        Color[]         bitmap;
        RawImage        rawImage;

        class Subgraph
        {
            public string               name;
            public List<Vector2>        data;
            public float                elementWeight;
            public Color                legendColor;
            public UnityEngine.Gradient colorGradient;
            public float[]              heatmap;

        }

        List<Subgraph> subGraphs;

        public string title
        {
            get { return _title; }
            set { if (titleElement != null) titleElement.text = value; _title = value; }
        }

        void Awake()
        {
            Image img = gameObject.GetComponent<Image>();
            if (background)
            {
                if (img == null) img = gameObject.GetComponent<Image>();
                img.color = backgroundColor;
                img.enabled = true;
            }
            else
            {
                if (img != null) img.enabled = false;
            }

            rectTransform = GetComponent<RectTransform>();

            titleElement = GraphUtils.CreateTextRenderer("Title", textColor, titleTextSize, rectTransform);
            titleElement.alignment = TextAlignmentOptions.Center;
            titleElement.alignment = TextAlignmentOptions.Top;
            titleElement.text = _title;
            RectTransform rt = titleElement.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1.0f);

            graphingArea = GraphUtils.CreateUIObject("Graph", rectTransform);
            graphingArea.anchorMin = new Vector2(0, 0);
            graphingArea.anchorMax = new Vector2(1, 1);
            graphingArea.pivot = new Vector2(0.5f, 0.5f);
            graphingArea.offsetMin = graphingArea.offsetMax = Vector2.zero;

            var rawImageRT = GraphUtils.CreateUIObject("Plot", graphingArea);
            rawImageRT.anchorMin = new Vector2(0, 0);
            rawImageRT.anchorMax = new Vector2(1, 1);
            rawImageRT.pivot = new Vector2(0.5f, 0.5f);
            rawImageRT.offsetMin = rawImageRT.offsetMax = Vector2.zero;
            rawImage = rawImageRT.gameObject.AddComponent<RawImage>();

            prevRect = rectTransform.rect;
            layoutDirty = true;
        }

        // Update is called once per frame
        void Update()
        {
            if ((layoutDirty) || (prevRect != rectTransform.rect))
            {
                UpdateLayout();
            }
            if (dirty)
            {
                RefreshPlot();
            }
        }

        public int AddSubgraph(UnityEngine.Gradient colorGradient, Color legendColor, Rect limits, float elementWeight, string name)
        {
            var subgraph = new Subgraph();
            subgraph.name = name;
            subgraph.legendColor = legendColor;
            subgraph.colorGradient = colorGradient;
            subgraph.elementWeight = elementWeight;
            subgraph.heatmap = new float[resolution.x * resolution.y];

            if (subGraphs == null) subGraphs = new List<Subgraph>();
            subGraphs.Add(subgraph);

            UpdateLegend();

            return subGraphs.Count - 1;
        }

        public void SetData(int subgraph_id, List<Vector2> data)
        {
            var sg = subGraphs[subgraph_id];
            sg.data = data;

            // Compute heat map
            if (!decay)
            {
                for (int i = 0; i < resolution.x * resolution.y; i++) sg.heatmap[i] = 0;
            }
            else
            {
                float multiplier = 1 - decayRate;
                for (int i = 0; i < resolution.x * resolution.y; i++)
                {
                    sg.heatmap[i] *= multiplier;
                }
            }
            foreach (var dataPoint in data)
            {
                int u = (int)(resolution.x * ((dataPoint.x - rangeX.x) / (rangeX.y - rangeX.x)));
                if ((u < 0) || (u >= resolution.x)) continue;
                int v = (int)(resolution.y * ((dataPoint.y - rangeY.x) / (rangeY.y - rangeY.x)));
                if ((v < 0) || (v >= resolution.y)) continue;

                int idx = u + v * resolution.x;
                sg.heatmap[idx] += sg.elementWeight * normalizationFactor;
            }

            dirty = true;
        }

        void UpdateLayout()
        {
            graphingArea.sizeDelta = new Vector2(rectTransform.rect.width * (maxGraphArea.x - minGraphArea.x),
                                                 rectTransform.rect.height * (maxGraphArea.y - minGraphArea.y));
            graphingArea.anchoredPosition = new Vector2(rectTransform.rect.width * minGraphArea.x,
                                                        rectTransform.rect.height * minGraphArea.y);
            graphingArea.offsetMin = new Vector2(rectTransform.rect.width * minGraphArea.x,
                                                 rectTransform.rect.height * minGraphArea.y);
            graphingArea.offsetMax = new Vector2(-rectTransform.rect.width * (1 - maxGraphArea.x),
                                                 -rectTransform.rect.height * (1 - maxGraphArea.y));

            if (displayAxisX)
            {
                if (axisX == null)
                {
                    RectTransform rt = GraphUtils.CreateUIObject("AxisX", graphingArea);

                    axisX = rt.gameObject.AddComponent<Axis>();
                    axisX.orientation = Axis.Orientation.Horizontal;
                    axisX.textColor = textColor;
                    axisX.textSize = axisTextSize;
                    axisX.labelFormat = labelFormatX;
                }
                axisX.UpdateLayout();
                axisX.labelAxis = labelAxisX;
            }
            else
            {
                if (axisX != null) axisX.gameObject.SetActive(false);
            }

            if (displayAxisY)
            {
                if (axisY == null)
                {
                    RectTransform rt = GraphUtils.CreateUIObject("AxisY", graphingArea);

                    axisY = rt.gameObject.AddComponent<Axis>();
                    axisY.orientation = Axis.Orientation.Vertical;
                    axisY.textColor = textColor;
                    axisY.textSize = axisTextSize;
                    axisY.labelFormat = labelFormatY;
                }
                axisY.UpdateLayout();
                axisY.labelAxis = labelAxisY;
            }
            else
            {
                if (axisY != null) axisY.gameObject.SetActive(false);
            }

            UpdateLegend();

            prevRect = rectTransform.rect;
            layoutDirty = false;
        }

        void UpdateLegend()
        {
            if (displayLegend)
            {
                if (legend == null)
                {
                    var rt = GraphUtils.CreateUIObject("Legend", graphingArea);
                    legend = rt.gameObject.AddComponent<Legend>();
                    rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(1, 1);
                }
                legend.gameObject.SetActive(true);

                legend.Clear();
                foreach (var sg in subGraphs)
                {
                    legend.Add(sg.name, sg.legendColor);
                }
            }
        }

        void RefreshPlot()
        {
            if ((texture == null) || (texture.width != resolution.x) || (texture.height != resolution.y))
            {
                texture = new Texture2D(resolution.x, resolution.y, TextureFormat.RGBA32, true);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Point;
                bitmap = new Color[texture.width * texture.height];

                rawImage.texture = texture;
            }

            if (subGraphs == null) return;

            for (int i = 0; i < resolution.x * resolution.y; i++)
            {
                bitmap[i] = Color.black;
            }

            for (int graph_id = 0; graph_id < subGraphs.Count; graph_id++)
            {
                var graph = subGraphs[graph_id];
                if (graph.data == null) continue;

                for (int i = 0; i < resolution.x * resolution.y; i++)
                {
                    bitmap[i] += graph.colorGradient.Evaluate(Mathf.Clamp01(graph.heatmap[i]));
                }
            }

            for (int i = 0; i < resolution.x * resolution.y; i++)
            {
                bitmap[i].r = Mathf.Clamp01(bitmap[i].r);
                bitmap[i].g = Mathf.Clamp01(bitmap[i].g);
                bitmap[i].b = Mathf.Clamp01(bitmap[i].b);
                bitmap[i].a = 1;
            }

            texture.SetPixels(bitmap);
            texture.Apply(true, false);

            if (axisX)
            {
                if (displayAxisX)
                {
                    axisX.range = new Vector2(rangeX.x, rangeX.y);
                }
                axisY.gameObject.SetActive(displayAxisY);
            }
            if (axisY)
            {
                if (displayAxisY)
                {
                    axisY.range = new Vector2(rangeY.x, rangeY.y);
                }
                axisY.gameObject.SetActive(displayAxisY);
            }

            if (legend)
            {
                legend.gameObject.SetActive(displayLegend);
                legend.SetOffset(legendOffset);
            }
            else
            {
                if (displayLegend) layoutDirty = true;
            }

            dirty = false;
        }

        public void Refresh()
        {
            dirty = true;
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Handles.Label(transform.position, "Heatmap: " + name);
            }
#endif
        }

    }
}
