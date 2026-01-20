using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace LanguagePractice.Helpers
{
    /// <summary>
    /// TextBoxに薄い入力例(ウォーターマーク)を表示するためのAttachedProperty。
    /// Textが空で、かつフォーカスが無い時に表示します。
    /// </summary>
    public static class WatermarkService
    {
        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.RegisterAttached(
                "Watermark",
                typeof(string),
                typeof(WatermarkService),
                new FrameworkPropertyMetadata(string.Empty, OnWatermarkChanged));

        private static readonly DependencyProperty WatermarkAdornerProperty =
            DependencyProperty.RegisterAttached(
                "WatermarkAdorner",
                typeof(WatermarkAdorner),
                typeof(WatermarkService),
                new PropertyMetadata(null));

        // ★どのAdornerLayerにAddしたかを保持（Remove時に同じLayerから外すため）
        private static readonly DependencyProperty WatermarkLayerProperty =
            DependencyProperty.RegisterAttached(
                "WatermarkLayer",
                typeof(AdornerLayer),
                typeof(WatermarkService),
                new PropertyMetadata(null));

        public static void SetWatermark(DependencyObject element, string value)
            => element.SetValue(WatermarkProperty, value);

        public static string GetWatermark(DependencyObject element)
            => (string)element.GetValue(WatermarkProperty);

        private static void SetWatermarkAdorner(DependencyObject element, WatermarkAdorner? value)
            => element.SetValue(WatermarkAdornerProperty, value);

        private static WatermarkAdorner? GetWatermarkAdorner(DependencyObject element)
            => (WatermarkAdorner?)element.GetValue(WatermarkAdornerProperty);

        private static void SetWatermarkLayer(DependencyObject element, AdornerLayer? value)
            => element.SetValue(WatermarkLayerProperty, value);

        private static AdornerLayer? GetWatermarkLayer(DependencyObject element)
            => (AdornerLayer?)element.GetValue(WatermarkLayerProperty);

        private static void OnWatermarkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox tb) return;

            tb.Loaded -= Tb_Loaded;
            tb.Loaded += Tb_Loaded;

            tb.Unloaded -= Tb_Unloaded;
            tb.Unloaded += Tb_Unloaded;

            tb.TextChanged -= Tb_TextChanged;
            tb.TextChanged += Tb_TextChanged;

            tb.GotKeyboardFocus -= Tb_FocusChanged;
            tb.GotKeyboardFocus += Tb_FocusChanged;

            tb.LostKeyboardFocus -= Tb_FocusChanged;
            tb.LostKeyboardFocus += Tb_FocusChanged;

            tb.IsVisibleChanged -= Tb_IsVisibleChanged;
            tb.IsVisibleChanged += Tb_IsVisibleChanged;

            // 既に表示中なら文言更新
            var adorner = GetWatermarkAdorner(tb);
            if (adorner != null)
            {
                adorner.WatermarkText = GetWatermark(tb);
                Update(tb);
            }
        }

        private static void Tb_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb) Update(tb);
        }

        private static void Tb_Unloaded(object sender, RoutedEventArgs e)
        {
            // ★画面切替等でVisualTreeから外れるとき、確実に剥がす
            if (sender is TextBox tb) Remove(tb);
        }

        private static void Tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb) Update(tb);
        }

        private static void Tb_FocusChanged(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb) Update(tb);
        }

        private static void Tb_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                // 非表示になったら消す／表示になったら状態更新
                if (!tb.IsVisible) Remove(tb);
                else Update(tb);
            }
        }

        private static void Update(TextBox tb)
        {
            string watermark = GetWatermark(tb);
            if (string.IsNullOrWhiteSpace(watermark))
            {
                Remove(tb);
                return;
            }

            // 表示されていない/未ロードなら出さない（残留防止）
            if (!tb.IsLoaded || !tb.IsVisible)
            {
                Remove(tb);
                return;
            }

            // フォーカス中は消える（要望どおり）
            bool shouldShow = string.IsNullOrEmpty(tb.Text) && !tb.IsKeyboardFocusWithin;

            if (shouldShow) Show(tb, watermark);
            else Remove(tb);
        }

        private static void Show(TextBox tb, string watermark)
        {
            var layer = AdornerLayer.GetAdornerLayer(tb);
            if (layer == null) return;

            var adorner = GetWatermarkAdorner(tb);

            // ★Layerが変わっていたら、古いLayerから剥がして付け替える
            var storedLayer = GetWatermarkLayer(tb);
            if (adorner != null && storedLayer != null && !ReferenceEquals(storedLayer, layer))
            {
                try { storedLayer.Remove(adorner); } catch { /* ignore */ }
                SetWatermarkAdorner(tb, null);
                SetWatermarkLayer(tb, null);
                adorner = null;
            }

            if (adorner == null)
            {
                adorner = new WatermarkAdorner(tb) { WatermarkText = watermark };
                layer.Add(adorner);
                SetWatermarkAdorner(tb, adorner);
                SetWatermarkLayer(tb, layer);
            }
            else
            {
                adorner.WatermarkText = watermark;
                adorner.InvalidateVisual();
            }
        }

        private static void Remove(TextBox tb)
        {
            var adorner = GetWatermarkAdorner(tb);
            if (adorner == null)
            {
                SetWatermarkLayer(tb, null);
                return;
            }

            // ★必ず「追加したLayer」からRemoveする
            var layer = GetWatermarkLayer(tb) ?? AdornerLayer.GetAdornerLayer(tb);

            if (layer != null)
            {
                try { layer.Remove(adorner); } catch { /* ignore */ }
            }

            SetWatermarkAdorner(tb, null);
            SetWatermarkLayer(tb, null);
        }

        private sealed class WatermarkAdorner : Adorner
        {
            private VisualCollection? _visuals;
            private TextBlock? _textBlock;

            public string WatermarkText
            {
                get => _textBlock?.Text ?? "";
                set { if (_textBlock != null) _textBlock.Text = value ?? ""; }
            }

            public WatermarkAdorner(TextBox adornedElement) : base(adornedElement)
            {
                IsHitTestVisible = false;

                _textBlock = new TextBlock
                {
                    Foreground = Brushes.Gray,
                    Opacity = 0.45,
                    FontStyle = FontStyles.Italic,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(2, 0, 2, 0)
                };

                _visuals = new VisualCollection(this) { _textBlock };
            }

            protected override int VisualChildrenCount => _visuals?.Count ?? 0;

            protected override Visual GetVisualChild(int index)
            {
                if (_visuals == null) throw new ArgumentOutOfRangeException(nameof(index));
                return _visuals[index];
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                if (_textBlock == null || AdornedElement is not TextBox tb)
                    return finalSize;

                var p = tb.Padding;
                double x = p.Left + 2;
                double y = p.Top + 1;

                double w = Math.Max(0, finalSize.Width - p.Left - p.Right - 4);
                double h = Math.Max(0, finalSize.Height - p.Top - p.Bottom - 2);

                _textBlock.Arrange(new Rect(new Point(x, y), new Size(w, h)));
                return finalSize;
            }
        }
    }
}
