// Auto Terrain Designations
// Copyright (c) 2026 Kayser
// Licensed under the MIT License.
using Mafi.Unity.UiToolkit.Library;
using UnityEngine;
using UnityEngine.UIElements;

namespace AutoTerrainDesignations
{
    internal sealed class MineTowerInspectorScrollArea
    {
        // 以 2560×1440 为布局基准，给检查器底边留出 12 个 UI 布局单位。
        // 高度由游戏 UI 根节点实测。窗口模式和 UI 缩放会自动反映到测量结果。
        private const float BottomMargin = 12f;
        private const float InspectorFrameWidthAllowance = 64f;
        private const long LayoutCheckIntervalMs = 80;

        private readonly VisualElement m_mainBody;
        private readonly VisualElement m_host;
        private readonly VisualElement m_content;
        private readonly ScrollView m_scroll;

        internal static void Insert(Column mainBody, Column atdPanels)
        {
            var host = new Column();
            var scrollArea = new MineTowerInspectorScrollArea(mainBody, host, atdPanels);
            mainBody.InsertAt(0, host);

            // 调度任务随控件的挂载运行。原版折叠、窗口移动和 UI 缩放
            // 都可能改变可用高度，因此从游戏自己的布局读取最新数据。
            host.RootElement.schedule.Execute(scrollArea.UpdateHeight).Every(LayoutCheckIntervalMs);
        }

        private MineTowerInspectorScrollArea(Column mainBody, Column host, Column atdPanels)
        {
            m_mainBody = mainBody.RootElement;
            m_host = host.RootElement;
            m_content = atdPanels.RootElement;
            m_scroll = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "ATD-MineTowerPanels",
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Auto,
            };
            m_scroll.style.flexGrow = 0f;
            m_scroll.style.flexShrink = 0f;
            m_scroll.style.minHeight = 0f;
            m_scroll.contentContainer.style.flexShrink = 0f;
            m_content.style.flexShrink = 0f;
            m_host.style.flexShrink = 0f;

            // 先登记 Mafi 的组件父子关系，再让 ScrollView 接管视觉节点。
            // 面板内的按钮、下拉框和数据刷新仍由原有组件管理。
            host.Add(atdPanels);
            m_scroll.Add(m_content);
            m_host.Add(m_scroll);
        }

        private void UpdateHeight()
        {
            VisualElement? uiRoot = m_mainBody.panel?.visualTree;
            if (uiRoot == null || m_mainBody.resolvedStyle.display == DisplayStyle.None)
                return;

            Rect viewport = uiRoot.worldBound;
            Rect body = m_mainBody.worldBound;
            float contentHeight = m_content.layout.height;
            float bodyHeight = m_mainBody.layout.height;
            if (!(viewport.height > 0f && body.width > 0f && bodyHeight > 0f && contentHeight > 0f))
                return;

            float scale = body.height / bodyHeight;
            if (!(scale > 0f) || float.IsInfinity(scale))
                return;

            Rect frame = FindInspectorFrame(uiRoot, viewport, body, scale, out ScrollView? outerScroll);
            if (outerScroll != null)
            {
                // 游戏若已提供外层滚动，以它的可见范围为准。
                // 拖动原版滚动条时，内容移动不应被误认为窗口高度变了。
                Rect outerViewport = outerScroll.contentViewport.worldBound;
                if (!(outerViewport.height > 0f))
                    return;
                viewport.yMin = Mathf.Max(viewport.yMin, outerViewport.yMin);
                viewport.yMax = Mathf.Min(viewport.yMax, outerViewport.yMax);
            }

            float availableHeight = viewport.yMax - BottomMargin * scale
                - (outerScroll == null ? Mathf.Max(viewport.yMin, frame.yMin) : viewport.yMin);

            // 当前窗体总高度减去 ATD 视口高度，得到原版控件、标题和边框的占位。
            // ATD 内容列保留自身自然高度，收起一块面板后滚动条会自动消失。
            float vanillaHeight = Mathf.Max(0f, frame.height - m_scroll.worldBound.height);
            // MainBody 已被父容器限制时，原版子控件仍可能伸出正文底边。
            // 也为这部分留出空间，避免把它误算成 ATD 可以占用的高度。
            float nativeBottom = body.yMax;
            for (int index = 0; index < m_mainBody.childCount; index++)
            {
                VisualElement child = m_mainBody[index];
                if (child == m_host || child.resolvedStyle.display == DisplayStyle.None
                    || child.resolvedStyle.position == Position.Absolute)
                    continue;
                nativeBottom = Mathf.Max(nativeBottom,
                    child.worldBound.yMax + child.resolvedStyle.marginBottom * scale);
            }
            vanillaHeight += nativeBottom - body.yMax;
            float remainingHeight = Mathf.Max(0f, (availableHeight - vanillaHeight) / scale);
            if (outerScroll != null && remainingHeight <= 0f)
            {
                // 极小窗口下沿用原版外层滚动，保证两部分内容仍然能访问。
                remainingHeight = Mathf.Max(0f, availableHeight / scale);
            }
            float height = Mathf.Min(contentHeight, remainingHeight);
            if (float.IsNaN(height) || float.IsInfinity(height))
                return;

            if (m_scroll.style.height.keyword != StyleKeyword.Undefined
                || Mathf.Abs(m_scroll.style.height.value.value - height) >= 0.5f)
            {
                m_scroll.style.height = height;
            }
        }

        private Rect FindInspectorFrame(VisualElement uiRoot, Rect viewport, Rect body,
            float scale, out ScrollView? outerScroll)
        {
            Rect frame = body;
            outerScroll = null;
            bool insideInspector = true;
            for (VisualElement? parent = m_mainBody.parent;
                parent != null && parent != uiRoot;
                parent = parent.parent)
            {
                if (parent is ScrollView scroll && scroll.mode != ScrollViewMode.Horizontal)
                {
                    outerScroll = scroll;
                    break;
                }
                Rect bounds = parent.worldBound;
                // 同宽的祖先包含窗口标题和边框。全屏层及拉伸到屏幕边缘的
                // 停靠容器不属于检查器的内容高度，不能放进原版占位。
                if (bounds.width > body.width + InspectorFrameWidthAllowance * scale
                    || (bounds.yMin >= viewport.yMin - 1f
                        && bounds.yMin <= viewport.yMin + BottomMargin * scale
                        && bounds.yMax >= viewport.yMax - 1f))
                {
                    insideInspector = false;
                }
                if (insideInspector && bounds.height >= frame.height && bounds.width >= body.width)
                    frame = bounds;
            }
            return frame;
        }
    }
}
