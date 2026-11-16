// Auto Terrain Designations
// Copyright (c) 2026 Kayser
// Licensed under the MIT License.
using Mafi;
using Mafi.Unity.UiToolkit.Library;
using UnityEngine;
using UnityEngine.UIElements;

namespace AutoTerrainDesignations
{
    internal static class MineTowerInspectorScrollArea
    {
        internal static void Install(Window window)
        {
            var body = window.Body;
            var frame = body.Parent.ValueOrNull;
            if (frame == null || frame is ScrollColumn)
                return;

            int bodyIndex = 0;
            while (bodyIndex < frame.ChildrenCount && frame[bodyIndex] != body)
                bodyIndex++;
            if (bodyIndex == frame.ChildrenCount)
                return;

            var scroll = new ScrollColumn();
            scroll.ScrollerAuto();
            var view = (ScrollView)scroll.RootElement;
            view.name = "ATD-MineTowerBody";
            view.style.alignSelf = Align.Stretch;
            view.style.flexShrink = 1f;
            view.style.minHeight = 0f;
            view.style.minWidth = 0f;
            view.contentContainer.style.width = Length.Percent(100f);
            view.contentContainer.style.alignItems = Align.Stretch;
            body.RootElement.style.alignSelf = Align.Stretch;
            body.RootElement.style.flexGrow = 0f;
            body.RootElement.style.flexShrink = 0f;
            body.RootElement.style.minWidth = 0f;
            frame.RootElement.style.flexShrink = 1f;
            frame.RootElement.style.minHeight = 0f;

            // 原版标题栏留在窗口框架内，原版正文和 ATD 面板共用一个滚动区。
            body.RemoveFromHierarchy();
            scroll.Add(body);
            frame.InsertAt(bodyIndex, scroll);
            window.WindowMaxHeight(96.Percent());

            if (frame.RootElement.parent is VisualElement windowShell)
            {
                // 展开面板或调整 UI 缩放后，移动窗口使窗口边框保持可见。
                view.schedule.Execute(() => KeepWindowVisible(windowShell)).Every(80);
            }
        }

        private static void KeepWindowVisible(VisualElement windowShell)
        {
            var viewport = windowShell.parent;
            if (viewport == null)
                return;

            Rect available = viewport.contentRect;
            Rect bounds = viewport.WorldToLocal(windowShell.worldBound);
            if (!(available.width > 0f && available.height > 0f
                && bounds.width > 0f && bounds.height > 0f))
                return;

            float x = Mathf.Clamp(bounds.xMin, available.xMin,
                Mathf.Max(available.xMin, available.xMax - bounds.width)) - bounds.xMin;
            float y = Mathf.Clamp(bounds.yMin, available.yMin,
                Mathf.Max(available.yMin, available.yMax - bounds.height)) - bounds.yMin;
            if (Mathf.Abs(x) < 0.5f && Mathf.Abs(y) < 0.5f)
                return;

            Translate offset = windowShell.resolvedStyle.translate;
            windowShell.style.translate = new Translate(offset.x.value + x, offset.y.value + y, offset.z);
        }
    }
}
