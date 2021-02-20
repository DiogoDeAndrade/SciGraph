using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace SciGraph
{
    public class Legend : MonoBehaviour
    {
        public Color textColor = Color.white;
        public float textSize = 10;

        struct LegendElem
        {
            public string           name;
            public Color            color;
            public RectTransform    layoutGroup;
        }

        List<LegendElem>    legend;
        RectTransform       rectTransform;
        bool                dirty;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = Vector2.one;
            var vlg = rectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = vlg.childForceExpandWidth = false;
            vlg.childControlWidth = true; vlg.childControlHeight = false;
            vlg.spacing = 5;
            vlg.childAlignment = TextAnchor.UpperRight;
        }

        public void Clear()
        {
            if (legend != null)
            {
                foreach (var l in legend)
                {
                    Destroy(l.layoutGroup.gameObject);
                }
            }
            legend = new List<LegendElem>();
            dirty = true;
        }

        public void Add(string name, Color c)
        {
            if (legend == null) legend = new List<LegendElem>();

            var newLegend = new LegendElem()
            {
                name = name,
                color = c
            };
            newLegend.layoutGroup = GraphUtils.CreateUIObject("Element " + name, rectTransform);
            newLegend.layoutGroup.sizeDelta = new Vector2(0, textSize + 2);
            newLegend.layoutGroup.pivot = new Vector2(1, 1);
            var hlg = newLegend.layoutGroup.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlHeight = hlg.childControlWidth = false;
            hlg.childForceExpandHeight = hlg.childForceExpandWidth = false;
            hlg.spacing = 5;
            var text = GraphUtils.CreateTextRenderer("Label", textColor, textSize, newLegend.layoutGroup);
            text.text = name;
            text.alignment = TextAlignmentOptions.TopRight;
            var boxBorder = GraphUtils.CreateImageRenderer("BoxBorder", Color.black, textSize, textSize, newLegend.layoutGroup);
            var boxInside = GraphUtils.CreateImageRenderer("BoxInside", c, textSize * 0.9f, textSize * 0.9f, boxBorder.GetComponent<RectTransform>());
            var boxInsideRT = boxInside.GetComponent<RectTransform>();
            boxInsideRT.anchorMin = boxInsideRT.anchorMax = boxInsideRT.pivot = new Vector2(0.5f, 0.5f);
            boxInsideRT.sizeDelta = new Vector2(textSize * 0.9f, textSize * 0.9f);

            legend.Add(newLegend);
            dirty = true;
        }

        private void Update()
        {
            if (dirty)
            {
                UpdateLayout();
            }
        }

        void UpdateLayout()
        {
            dirty = false;
        }

        public void SetOffset(Vector2 offset)
        {
            rectTransform.anchoredPosition = offset;
        }
    }
}