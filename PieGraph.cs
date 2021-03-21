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

    public class PieGraph : MonoBehaviour
    {
        public bool         background = true;
        [ShowIf("background")]
        public Color        backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.85f);
        public bool         displayLegend = false;
        [ShowIf("displayLegend")]
        public Vector2      legendOffset = Vector2.zero;
        public Color        textColor = Color.yellow;
        public float        titleTextSize = 14;
        public bool         displayPercentange = false;
        [ShowIf("displayPercentange")]
        public bool         displayAbsoluteValue = false;
        [ShowIf("displayPercentange")]
        public Color        percentageColor = Color.yellow;
        [ShowIf("displayPercentange")]
        public float        percentageTextSize = 14;
        [ShowIf("displayPercentange")]
        public string       percentageFormat = "{0:0.00}";
        [ShowIf("displayPercentange")]
        public float        minPercentageWrite = 0;

        [BoxGroup("Graph Area")]
        public Vector2      minGraphArea = new Vector2(0.15f, 0.15f);
        [BoxGroup("Graph Area")]
        public Vector2      maxGraphArea = new Vector2(0.95f, 0.85f);
        [BoxGroup("Graph Area")]
        public Color        graphBackgroundColor = new Color(0, 0, 0, 0);
        [BoxGroup("Graph Area")]

        string          _title;
        bool            dirty;
        bool            layoutDirty;
        TextMeshProUGUI titleElement;
        RectTransform   rectTransform;
        Rect            prevRect;
        RectTransform   graphingArea;
        Legend          legend;
        bool            legendDirty;

        class Category
        {
            public string           name;
            public float            data;
            public Color            color;
            public UICircle         circle;
            public TextMeshProUGUI  percentageText;
            public RectTransform    percentageTextRT;
        }

        List<Category> categories;

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
                if (img == null) img = gameObject.AddComponent<Image>();
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
            if (legendDirty)
            {
                UpdateLegend();
            }
            if (dirty)
            {
                RefreshPlot();
            }
        }

        public int AddCategory(Color color, string name)
        {
            var category = new Category();
            category.name = name;
            category.color = color;

            if (categories == null) categories = new List<Category>();
            categories.Add(category);

            CreateElement(category);

            UpdateLegend();

            return categories.Count - 1;
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
                foreach (var sg in categories)
                {
                    legend.Add(sg.name, sg.color);
                }
            }
        }

        public void SetData(int categoryId, float data)
        {
            categories[categoryId].data = data;
            dirty = true;
        }

        public void SetName(int categoryId, string name)
        {
            categories[categoryId].name = name;
            legendDirty = true;
            dirty = true;
        }
        public void SetColor(int categoryId, Color color)
        {
            categories[categoryId].color = color;
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

            prevRect = rectTransform.rect;
            layoutDirty = false;
        }

        void RefreshPlot()
        {
            int width = (int)graphingArea.rect.width;
            int height = (int)graphingArea.rect.height;

            float radius = (width < height) ? (width * 0.5f) : (height * 0.5f);

            float dataRange = 0.0f;
            foreach (var category in categories)
            {
                dataRange += category.data;
            }

            float angle = 0;
            foreach (var category in categories)
            {
                float normValue = (dataRange > 0)?(category.data / dataRange):(0);

                category.circle.Arc = Mathf.CeilToInt(normValue * 360) / 360.0f;
                category.circle.ArcRotation = (int)angle;
                category.circle.color = category.color;

                category.circle.SetAllDirty();

                angle += category.circle.Arc * 360;

                if (displayPercentange)
                {
                    float minAngle = category.circle.ArcRotation;
                    float maxAngle = minAngle + category.circle.Arc * 360.0f;
                    float midAngle = (180 - ((maxAngle + minAngle) * 0.5f)) * Mathf.Deg2Rad;
                    float r = 0.75f * radius;

                    Vector2 pos = new Vector2(r * Mathf.Cos(midAngle), r * Mathf.Sin(midAngle));

                    if (minPercentageWrite <= normValue)
                    {
                        category.percentageText.gameObject.SetActive(true);
                        category.percentageTextRT.anchoredPosition = pos;

                        string txt = string.Format(percentageFormat, normValue * 100.0f);
                        if (displayAbsoluteValue) txt += "\n(" + category.data + ")";
                        category.percentageText.text = txt;

                    }
                    else
                    {
                        category.percentageText.gameObject.SetActive(false);
                    }
                }
                else
                {
                    category.percentageText.gameObject.SetActive(false);
                }
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

        void CreateElement(Category category)
        {
            var rt = GraphUtils.CreateUIObject("Circle " + category.name, graphingArea);
            category.circle = rt.gameObject.AddComponent<UICircle>();
            category.circle.color = category.color;
            category.circle.SetArcRotation(0);
            category.circle.Arc = 0.25f;

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            category.percentageText = GraphUtils.CreateTextRenderer("Text " + category.name, percentageColor, percentageTextSize, rt);
            category.percentageText.alignment = TextAlignmentOptions.Center;
            category.percentageTextRT = category.percentageText.GetComponent<RectTransform>();
            category.percentageTextRT.anchorMin = category.percentageTextRT.anchorMax = category.percentageTextRT.pivot = new Vector2(0.5f, 0.5f);
            category.percentageTextRT.sizeDelta = Vector2.zero;
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Handles.Label(transform.position, "PieGraph: " + name);
            }
#endif
        }

    }
}
