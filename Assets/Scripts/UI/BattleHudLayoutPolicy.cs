using UnityEngine;

namespace PhalanxChronicle.UI
{
    internal enum BattleAspectClass
    {
        Standard,
        Compact,
        Narrow,
    }

    internal readonly struct BattleLayoutMetrics
    {
        public BattleLayoutMetrics(
            BattleAspectClass aspectClass,
            float canvasWidth,
            float canvasHeight,
            float selectedPanelWidth,
            float rosterPanelWidth,
            float sidePanelHeight,
            float actionDockWidth,
            float actionDockHeight,
            float actionDockHorizontalReserve,
            float panelOuterMargin,
            int panelPadding,
            float panelSectionSpacing,
            float textScale,
            float contextRibbonWidth,
            float contextRibbonHeight,
            float sidePanelAnchoredY,
            float cameraOrthographicSize,
            float cameraVerticalOffset,
            float leftReservePixels,
            float rightReservePixels,
            float topReservePixels,
            float bottomReservePixels)
        {
            AspectClass = aspectClass;
            CanvasWidth = canvasWidth;
            CanvasHeight = canvasHeight;
            SelectedPanelWidth = selectedPanelWidth;
            RosterPanelWidth = rosterPanelWidth;
            SidePanelHeight = sidePanelHeight;
            ActionDockWidth = actionDockWidth;
            ActionDockHeight = actionDockHeight;
            ActionDockHorizontalReserve = actionDockHorizontalReserve;
            PanelOuterMargin = panelOuterMargin;
            PanelPadding = panelPadding;
            PanelSectionSpacing = panelSectionSpacing;
            TextScale = textScale;
            ContextRibbonWidth = contextRibbonWidth;
            ContextRibbonHeight = contextRibbonHeight;
            SidePanelAnchoredY = sidePanelAnchoredY;
            CameraOrthographicSize = cameraOrthographicSize;
            CameraVerticalOffset = cameraVerticalOffset;
            LeftReservePixels = leftReservePixels;
            RightReservePixels = rightReservePixels;
            TopReservePixels = topReservePixels;
            BottomReservePixels = bottomReservePixels;
        }

        public BattleAspectClass AspectClass { get; }

        public float CanvasWidth { get; }

        public float CanvasHeight { get; }

        public float SelectedPanelWidth { get; }

        public float RosterPanelWidth { get; }

        public float SidePanelHeight { get; }

        public float ActionDockWidth { get; }

        public float ActionDockHeight { get; }

        public float ActionDockHorizontalReserve { get; }

        public float PanelOuterMargin { get; }

        public int PanelPadding { get; }

        public float PanelSectionSpacing { get; }

        public float TextScale { get; }

        public float ContextRibbonWidth { get; }

        public float ContextRibbonHeight { get; }

        public float SidePanelAnchoredY { get; }

        public float CameraOrthographicSize { get; }

        public float CameraVerticalOffset { get; }

        public float LeftReservePixels { get; }

        public float RightReservePixels { get; }

        public float TopReservePixels { get; }

        public float BottomReservePixels { get; }
    }

    internal static class BattleHudLayoutPolicy
    {
        private const float BaselineCanvasWidth = 1920f;
        private const float BaselineCanvasHeight = 1080f;
        private const float BaselineAspect = BaselineCanvasWidth / BaselineCanvasHeight;
        private const float BasePanelHeight = 836f;
        private const float SidePanelMinHeight = 260f;
        private const float SideLaneBottomReserve = 18f;
        private const float MaxActionDockWidth = 1120f;
        private const float MinActionDockWidth = 540f;

        public static BattleLayoutMetrics Evaluate(RectTransform canvasRect, float boardWidth, float boardHeight)
        {
            Vector2 canvasSize = ResolveCanvasSize(canvasRect);
            float safeBoardWidth = Mathf.Max(8f, boardWidth);
            float safeBoardHeight = Mathf.Max(8f, boardHeight);
            BattleAspectClass aspectClass = ClassifyAspect(canvasSize.x / Mathf.Max(1f, canvasSize.y));
            LayoutProfile currentProfile = GetProfile(aspectClass);
            LayoutProfile baselineProfile = GetProfile(BattleAspectClass.Standard);

            float currentPanelHeight = CalculatePanelHeight(canvasSize.y, currentProfile);
            float currentDockWidth = Mathf.Clamp(
                canvasSize.x - currentProfile.SelectedPanelWidth - currentProfile.RosterPanelWidth - currentProfile.ActionDockHorizontalReserve,
                MinActionDockWidth,
                MaxActionDockWidth);
            float currentRibbonWidth = Mathf.Clamp(currentDockWidth * 0.82f, 620f, 792f);
            float currentAnchoredY = CalculateSafeAnchoredY(canvasSize.y, currentPanelHeight);

            float baselineRequirement = CalculateCameraRequirement(
                BaselineCanvasWidth,
                BaselineCanvasHeight,
                safeBoardWidth,
                safeBoardHeight,
                baselineProfile);
            float currentRequirement = CalculateCameraRequirement(
                canvasSize.x,
                canvasSize.y,
                safeBoardWidth,
                safeBoardHeight,
                currentProfile);

            float orthographicSize = Mathf.Max(baselineRequirement, currentRequirement);
            orthographicSize = Mathf.Ceil(orthographicSize * 16f) / 16f;

            float topReserveRatio = currentProfile.TopReservePixels / Mathf.Max(1f, canvasSize.y);
            float bottomReserveRatio = currentProfile.BottomReservePixels / Mathf.Max(1f, canvasSize.y);
            float verticalOffset = (topReserveRatio - bottomReserveRatio) * orthographicSize * 0.24f;

            return new BattleLayoutMetrics(
                aspectClass,
                canvasSize.x,
                canvasSize.y,
                currentProfile.SelectedPanelWidth,
                currentProfile.RosterPanelWidth,
                currentPanelHeight,
                currentDockWidth,
                currentProfile.ActionDockHeight,
                currentProfile.ActionDockHorizontalReserve,
                currentProfile.PanelOuterMargin,
                currentProfile.PanelPadding,
                currentProfile.PanelSectionSpacing,
                currentProfile.TextScale,
                currentRibbonWidth,
                currentProfile.ContextRibbonHeight,
                currentAnchoredY,
                orthographicSize,
                verticalOffset,
                currentProfile.LeftReservePixels,
                currentProfile.RightReservePixels,
                currentProfile.TopReservePixels,
                currentProfile.BottomReservePixels);
        }

        private static Vector2 ResolveCanvasSize(RectTransform canvasRect)
        {
            if (canvasRect != null && canvasRect.rect.width > 0f && canvasRect.rect.height > 0f)
            {
                return canvasRect.rect.size;
            }

            return new Vector2(BaselineCanvasWidth, BaselineCanvasHeight);
        }

        private static BattleAspectClass ClassifyAspect(float aspect)
        {
            if (aspect >= 1.70f)
            {
                return BattleAspectClass.Standard;
            }

            return aspect >= 1.50f ? BattleAspectClass.Compact : BattleAspectClass.Narrow;
        }

        private static LayoutProfile GetProfile(BattleAspectClass aspectClass)
        {
            switch (aspectClass)
            {
                case BattleAspectClass.Compact:
                    return new LayoutProfile(
                        aspectClass,
                        selectedPanelWidth: 268f,
                        rosterPanelWidth: 292f,
                        actionDockHeight: 296f,
                        actionDockHorizontalReserve: 40f,
                        panelOuterMargin: 18f,
                        panelPadding: 14,
                        panelSectionSpacing: 5f,
                        textScale: 0.94f,
                        topReservePixels: 94f,
                        bottomReservePixels: 72f,
                        leftReservePixels: 320f,
                        rightReservePixels: 342f,
                        contextRibbonHeight: 112f,
                        sidePanelTopMargin: 18f);
                case BattleAspectClass.Narrow:
                    return new LayoutProfile(
                        aspectClass,
                        selectedPanelWidth: 248f,
                        rosterPanelWidth: 272f,
                        actionDockHeight: 280f,
                        actionDockHorizontalReserve: 32f,
                        panelOuterMargin: 18f,
                        panelPadding: 12,
                        panelSectionSpacing: 4f,
                        textScale: 0.88f,
                        topReservePixels: 86f,
                        bottomReservePixels: 68f,
                        leftReservePixels: 294f,
                        rightReservePixels: 316f,
                        contextRibbonHeight: 106f,
                        sidePanelTopMargin: 16f);
                default:
                    return new LayoutProfile(
                        aspectClass,
                        selectedPanelWidth: 292f,
                        rosterPanelWidth: 318f,
                        actionDockHeight: 316f,
                        actionDockHorizontalReserve: 56f,
                        panelOuterMargin: 18f,
                        panelPadding: 16,
                        panelSectionSpacing: 6f,
                        textScale: 1f,
                        topReservePixels: 104f,
                        bottomReservePixels: 78f,
                        leftReservePixels: 354f,
                        rightReservePixels: 376f,
                        contextRibbonHeight: 118f,
                        sidePanelTopMargin: 22f);
            }
        }

        private static float CalculatePanelHeight(float canvasHeight, LayoutProfile profile)
        {
            float usableHeight = canvasHeight - SideLaneBottomReserve - profile.SidePanelTopMargin;
            if (usableHeight < SidePanelMinHeight)
            {
                usableHeight = SidePanelMinHeight;
            }

            return Mathf.Min(BasePanelHeight, usableHeight);
        }

        private static float CalculateSafeAnchoredY(float canvasHeight, float panelHeight)
        {
            return SideLaneBottomReserve + panelHeight * 0.5f - canvasHeight * 0.5f;
        }

        private static float CalculateCameraRequirement(float canvasWidth, float canvasHeight, float boardWidth, float boardHeight, LayoutProfile profile)
        {
            float aspect = Mathf.Max(0.1f, canvasWidth / Mathf.Max(1f, canvasHeight));
            float leftReserveRatio = profile.LeftReservePixels / Mathf.Max(1f, canvasWidth);
            float rightReserveRatio = profile.RightReservePixels / Mathf.Max(1f, canvasWidth);
            float topReserveRatio = profile.TopReservePixels / Mathf.Max(1f, canvasHeight);
            float bottomReserveRatio = profile.BottomReservePixels / Mathf.Max(1f, canvasHeight);
            float usableWidth = Mathf.Max(0.2f, 1f - leftReserveRatio - rightReserveRatio);
            float usableHeight = Mathf.Max(0.2f, 1f - topReserveRatio - bottomReserveRatio);
            float orthographicSizeForHeight = (boardHeight * 0.5f) / usableHeight;
            float orthographicSizeForWidth = (boardWidth * 0.5f) / (aspect * usableWidth);
            return Mathf.Max(orthographicSizeForHeight, orthographicSizeForWidth);
        }

        private readonly struct LayoutProfile
        {
            public LayoutProfile(
                BattleAspectClass aspectClass,
                float selectedPanelWidth,
                float rosterPanelWidth,
                float actionDockHeight,
                float actionDockHorizontalReserve,
                float panelOuterMargin,
                int panelPadding,
                float panelSectionSpacing,
                float textScale,
                float topReservePixels,
                float bottomReservePixels,
                float leftReservePixels,
                float rightReservePixels,
                float contextRibbonHeight,
                float sidePanelTopMargin)
            {
                AspectClass = aspectClass;
                SelectedPanelWidth = selectedPanelWidth;
                RosterPanelWidth = rosterPanelWidth;
                ActionDockHeight = actionDockHeight;
                ActionDockHorizontalReserve = actionDockHorizontalReserve;
                PanelOuterMargin = panelOuterMargin;
                PanelPadding = panelPadding;
                PanelSectionSpacing = panelSectionSpacing;
                TextScale = textScale;
                TopReservePixels = topReservePixels;
                BottomReservePixels = bottomReservePixels;
                LeftReservePixels = leftReservePixels;
                RightReservePixels = rightReservePixels;
                ContextRibbonHeight = contextRibbonHeight;
                SidePanelTopMargin = sidePanelTopMargin;
            }

            public BattleAspectClass AspectClass { get; }

            public float SelectedPanelWidth { get; }

            public float RosterPanelWidth { get; }

            public float ActionDockHeight { get; }

            public float ActionDockHorizontalReserve { get; }

            public float PanelOuterMargin { get; }

            public int PanelPadding { get; }

            public float PanelSectionSpacing { get; }

            public float TextScale { get; }

            public float TopReservePixels { get; }

            public float BottomReservePixels { get; }

            public float LeftReservePixels { get; }

            public float RightReservePixels { get; }

            public float ContextRibbonHeight { get; }

            public float SidePanelTopMargin { get; }
        }
    }
}
