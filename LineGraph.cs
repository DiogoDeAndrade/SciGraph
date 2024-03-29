﻿using System.Collections;
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

    [RequireComponent(typeof(Image))]
    public class LineGraph : MonoBehaviour
    {
        public enum CurrentValueAlign { Left, Center, Right };

        public bool         background = true;
        [ShowIf("background")]
        public Color        backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.85f);
        public bool         displayCurrentValue = false;
        [ShowIf("displayCurrentValue")]
        public CurrentValueAlign    displayCurrentValueAlign = CurrentValueAlign.Left;
        public bool         displayLegend = false;
        [ShowIf("displayLegend")]
        public Vector2      legendOffset = Vector2.zero;
        public Color        textColor = Color.white;
        public float        labelTextSize = 8;
        public float        titleTextSize = 14;

        [BoxGroup("Graph Area")]
        public Vector2      minGraphArea = new Vector2(0.15f, 0.15f);
        [BoxGroup("Graph Area")]
        public Vector2      maxGraphArea = new Vector2(0.95f, 0.85f);
        [BoxGroup("Graph Area")]
        public bool         fixedLimits;
        [ShowIf("fixedLimits"), BoxGroup("Graph Area")]
        public Vector2      limitMin = new Vector2(0, 0);
        [ShowIf("fixedLimits"), BoxGroup("Graph Area")]
        public Vector2      limitMax = new Vector2(100, 100);
        [ShowIf("fixedLimits"), BoxGroup("Graph Area")]
        public bool         allowExpandX = false;
        [ShowIf("fixedLimitsAndNotAllowExpandX"), BoxGroup("Graph Area")]
        public bool         followX = false;
        [ShowIf("fixedLimits"), BoxGroup("Graph Area")]
        public bool         allowExpandY = false;

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

        [BoxGroup("Perfomance")]
        public float        distanceBetweenPointsX = 0;

        string          _title = "";
        bool            dirty;
        bool            layoutDirty;
        Vector2         min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2         max = new Vector2(-float.MaxValue, -float.MaxValue);
        Vector2         dataRange;
        Axis            axisX;
        Axis            axisY;
        RectTransform   rectTransform;
        RectTransform   graphingArea;
        TextMeshProUGUI titleElement;
        Legend          legend;

        struct SubLineGraph
        {
            public string           name;
            public UILineRenderer   lineRenderer;
            public List<Vector2>    dataPoints;
            public UILineRenderer   currentValueLine;
            public TextMeshProUGUI  currentValueLabel;
            public RectTransform    currentValueLabelRT;
        }

        List<SubLineGraph>  subGraphs;
        Rect                prevRect;

        bool fixedLimitsAndNotAllowExpandX => (fixedLimits) && (!allowExpandX);

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
            titleElement.text = (_title != "") ? (_title) : (name);
            RectTransform rt = titleElement.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1.0f);

            graphingArea = GraphUtils.CreateUIObject("LineContainer", rectTransform);
            graphingArea.anchorMin = new Vector2(0, 0);
            graphingArea.anchorMax = new Vector2(1, 1);
            graphingArea.pivot = new Vector2(0.5f, 0.5f);
            graphingArea.offsetMin = graphingArea.offsetMax = Vector2.zero;

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
                RefreshLines();
            }
        }

        public int AddSubgraph(Color color, string name)
        {
            var subgraph = new SubLineGraph();
            subgraph.name = name;
            subgraph.dataPoints = new List<Vector2>();

            subgraph.lineRenderer = GraphUtils.CreateLineRenderer(name, color, graphingArea);
            subgraph.currentValueLine = GraphUtils.CreateLineRenderer("Current " + name, new Color(color.r, color.g, color.b, 0.25f), graphingArea);
            subgraph.currentValueLabel = GraphUtils.CreateTextRenderer("Value" + name, color, labelTextSize, graphingArea);
            switch (displayCurrentValueAlign)
            {
                case CurrentValueAlign.Left:
                    subgraph.currentValueLabel.alignment = TextAlignmentOptions.Center;
                    subgraph.currentValueLabel.alignment = TextAlignmentOptions.Right;
                    break;
                case CurrentValueAlign.Center:
                    subgraph.currentValueLabel.alignment = TextAlignmentOptions.Center;
                    break;
                case CurrentValueAlign.Right:
                    subgraph.currentValueLabel.alignment = TextAlignmentOptions.Center;
                    subgraph.currentValueLabel.alignment = TextAlignmentOptions.Left;
                    break;
                default:
                    break;
            }
            subgraph.currentValueLabelRT = subgraph.currentValueLabel.GetComponent<RectTransform>();

            if (subGraphs == null) subGraphs = new List<SubLineGraph>();
            subGraphs.Add(subgraph);

            UpdateLegend();

            return subGraphs.Count - 1;
        }

        public void AddDataPoint(int subgraph_id, float x, float y)
        {
            subGraphs[subgraph_id].dataPoints.Add(new Vector2(x, y));
            min.x = Mathf.Min(min.x, x);
            min.y = Mathf.Min(min.y, y);
            max.x = Mathf.Max(max.x, x);
            max.y = Mathf.Max(max.y, y);
            dataRange = max - min;
            if (dataRange.x == 0) dataRange.x = 1;
            if (dataRange.y == 0) dataRange.y = 1;
            dirty = true;
        }

        public void SetData(int subgraph_id, List<float> value)
        {
            subGraphs[subgraph_id].dataPoints.Clear();
            for (int i = 0; i < value.Count; i++)
            {
                subGraphs[subgraph_id].dataPoints.Add(new Vector2(i, value[i]));
                min.y = Mathf.Min(min.y, value[i]);
                max.y = Mathf.Max(max.y, value[i]);
            }
            min.x = Mathf.Min(min.x, 0);
            max.x = Mathf.Max(max.x, value.Count);
            dataRange = max - min;
            if (dataRange.x == 0) dataRange.x = 1;
            if (dataRange.y == 0) dataRange.y = 1;
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
                    legend.Add(sg.name, sg.lineRenderer.color);
                }
            }
        }

        void RefreshLines()
        {
            // Get limits
            Vector2 displayMin = min;
            Vector2 displayMax = max;

            if (fixedLimits)
            {
                displayMin = limitMin;
                displayMax = limitMax;

                if (allowExpandX)
                {
                    limitMax.x = Mathf.Max(limitMax.x, max.x);
                }
                else
                {
                    if (followX)
                    {
                        float deltaX = limitMax.x - limitMin.x;
                        if (limitMax.x < max.x) limitMax.x = max.x;
                        limitMin.x = limitMax.x - deltaX;
                    }
                }
                if (allowExpandY)
                {
                    limitMax.y = Mathf.Max(limitMax.y, max.y);
                }
            }

            foreach (var sg in subGraphs)
            {
                sg.dataPoints.Sort((v1, v2) => v1.x.CompareTo(v2.x));

                float width = sg.lineRenderer.rectTransform.rect.width;
                float height = sg.lineRenderer.rectTransform.rect.height;

                Vector2 offset = new Vector2(-width * 0.5f, -height * 0.5f);

                float lastCoordX = -float.MaxValue;

                List<Vector2> points = new List<Vector2>();
                for (int i = 0; i < sg.dataPoints.Count; i++)
                {
                    var dataPoint = sg.dataPoints[i];

                    if (dataPoint.x < displayMin.x) continue;
                    if (dataPoint.x > displayMax.x) break;

                    float x = offset.x + width * (dataPoint.x - displayMin.x) / (displayMax.x - displayMin.x);
                    float y = offset.y + height * (dataPoint.y - displayMin.y) / (displayMax.y - displayMin.y);

                    if ((x - lastCoordX) < distanceBetweenPointsX) continue;

                    points.Add(new Vector2(x, y));

                    lastCoordX = x;
                }

                sg.lineRenderer.Points = points.ToArray();

                if (displayCurrentValue)
                {
                    sg.currentValueLine.enabled = true;
                    sg.currentValueLabel.enabled = true;

                    if (sg.dataPoints.Count > 0)
                    {
                        float valueY = sg.dataPoints[sg.dataPoints.Count - 1].y;
                        float y = offset.y + height * (valueY - displayMin.y) / (displayMax.y - displayMin.y);

                        sg.currentValueLine.Points = new Vector2[]
                        {
                        new Vector2(offset.x, y),
                        new Vector2(offset.x + width, y),
                        };

                        sg.currentValueLabel.text = string.Format("{0:" + labelFormatY + "}", valueY);
                        switch (displayCurrentValueAlign)
                        {
                            case CurrentValueAlign.Left:
                                sg.currentValueLabelRT.anchoredPosition = new Vector2(-5, y);
                                break;
                            case CurrentValueAlign.Center:
                                sg.currentValueLabelRT.anchoredPosition = new Vector2(width * 0.5f, y);
                                break;
                            case CurrentValueAlign.Right:
                                sg.currentValueLabelRT.anchoredPosition = new Vector2(width, y);
                                break;
                            default:
                                break;
                        }   
                    }
                }
                else
                {
                    sg.currentValueLine.enabled = false;
                    sg.currentValueLabel.enabled = false;
                }
            }

            if (axisX != null)
            {
                axisX.range = new Vector2(displayMin.x, displayMax.x);
                axisX.gameObject.SetActive(displayAxisX);
            }

            if (axisY != null)
            {
                axisY.range = new Vector2(displayMin.y, displayMax.y);
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

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Handles.Label(transform.position, "LineGraph: " + name);
            }
#endif
        }
    }
}