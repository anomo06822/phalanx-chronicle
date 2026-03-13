using UnityEngine;

namespace PhalanxChronicle.UI
{
    internal static class BattleHudLayoutPolicy
    {
        public const float BasePanelHeight = 836f;
        public const float ActionDockHeight = 292f;
        public const float ActionDockBottomMargin = 14f;
        public const float ActionDockHorizontalReserve = 56f;
        public const float SidePanelBottomMargin = 16f;
        public const float SidePanelTopMargin = 22f;
        public const float SidePanelMinHeight = 260f;
        public const float SelectedPanelWidth = 292f;
        public const float RosterSidebarWidth = 318f;

        public static float CalculatePanelHeight(RectTransform canvasRect)
        {
            if (canvasRect == null || canvasRect.rect.height <= 0f)
            {
                return BasePanelHeight;
            }

            float reservedBottom = ActionDockBottomMargin + ActionDockHeight + SidePanelBottomMargin;
            float usableHeight = canvasRect.rect.height - reservedBottom - SidePanelTopMargin;
            if (usableHeight < SidePanelMinHeight)
            {
                usableHeight = SidePanelMinHeight;
            }

            return Mathf.Min(BasePanelHeight, usableHeight);
        }

        public static float CalculateSafeAnchoredY(RectTransform canvasRect, float panelHeight)
        {
            if (canvasRect == null || canvasRect.rect.height <= 0f)
            {
                return 0f;
            }

            float reservedBottom = ActionDockBottomMargin + ActionDockHeight + SidePanelBottomMargin;
            return reservedBottom + panelHeight * 0.5f - canvasRect.rect.height * 0.5f;
        }
    }
}
