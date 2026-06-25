using System;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.JSInterop;

namespace BlazorPwaTemplate.Pages
{
    public partial class Index : IDisposable
    {
        public static string CurrentMode { get; private set; } = "Fit"; // Fit, Cut, Dye
        public static string SelectedStyleId { get; private set; } = "bob";
        public static string SelectedColorHex { get; private set; } = "#ff3b30";

        public static event Action? OnStyleUpdated;

        [JSImport("initCanvas", "ruler")]
        internal static partial void InitCanvasInterop();

        [JSImport("bindInterop", "ruler")]
        internal static partial void BindInterop();

        [JSImport("resetRuler", "ruler")]
        internal static partial void ResetRuler();

        [JSImport("setCalibrationType", "ruler")]
        internal static partial void SetCalibrationTypeJS(string type);

        [JSImport("setWigColor", "ruler")]
        internal static partial void SetWigColorJS(string colorHex);

        [JSImport("setInteractionMode", "ruler")]
        internal static partial void SetInteractionModeJS(string mode);

        [JSImport("triggerSnapshot", "ruler")]
        internal static partial void TriggerSnapshotJS();

        protected override void OnInitialized()
        {
            OnStyleUpdated += HandleStyleUpdated;
        }

        private void HandleStyleUpdated()
        {
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            OnStyleUpdated -= HandleStyleUpdated;
        }

        private void SetStyle(string styleId)
        {
            SelectedStyleId = styleId;
            try { SetCalibrationTypeJS(styleId); } catch {}
            StateHasChanged();
        }

        private void SetColor(string colorHex)
        {
            SelectedColorHex = colorHex;
            try { SetWigColorJS(colorHex); } catch {}
            StateHasChanged();
        }

        private void SetInteractionMode(string mode)
        {
            CurrentMode = mode;
            try { SetInteractionModeJS(mode); } catch {}
            StateHasChanged();
        }

        private void ResetWorkspace()
        {
            try { ResetRuler(); } catch {}
            StateHasChanged();
        }

        private void ExportCapture()
        {
            try { TriggerSnapshotJS(); } catch {}
        }
    }
}
