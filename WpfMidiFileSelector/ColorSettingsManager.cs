using System.Windows.Media;
using System.Globalization; // ColorConverter に必要
using System.Diagnostics; // Debug.WriteLine に必要
using System.Collections.Generic; // もし必要であれば

namespace WpfMidiFileSelector
{
    /// <summary>
    /// アプリケーションの色設定と、ユーザーによる色選択の管理を担当するクラスです。
    /// ユーザーが選択したオプションに基づいて、実際に使用される Brush オブジェクトを決定し保持します。
    /// </summary>
    public class ColorSettingsManager
    {
        // ★ 内部で保持する Brush フィールド ★
        // これらのフィールドが、各色設定の現在の Brush オブジェクトを保持します。
        private SolidColorBrush _backgroundColorBrush;
        private SolidColorBrush _normalNoteColorBrush;
        private SolidColorBrush _playingNoteColorBrush;

        // 外部から現在の色を取得するためのプロパティ (読み取り専用)
        public SolidColorBrush BackgroundColorBrush => _backgroundColorBrush;
        public SolidColorBrush NormalNoteColorBrush => _normalNoteColorBrush;
        public SolidColorBrush PlayingColorBrush => _playingNoteColorBrush; // XAML と合わせるために PlayingColorBrush にしておきます。


        /// <summary>
        /// ColorSettingsManager の新しいインスタンスを初期化し、デフォルト色を設定します。
        /// </summary>
        public ColorSettingsManager()
        {
            _backgroundColorBrush = new SolidColorBrush(Colors.Green);

            // 定数を使用して色を取得
            Color normalNoteColor = (Color)ColorConverter.ConvertFromString(ColorConstants.NormalNoteColor);
            _normalNoteColorBrush = new SolidColorBrush(normalNoteColor);

            Color playingColor = (Color)ColorConverter.ConvertFromString(ColorConstants.PlayingNoteColor);
            _playingNoteColorBrush = new SolidColorBrush(playingColor);
        }

        /// <summary>
        /// 背景色の設定をユーザーの選択に基づいて適用し、内部の Brush を更新します。
        /// </summary>
        /// <param name="option">選択されたオプション名（例: "緑", "青", "色コードで指定"）。</param>
        /// <param name="hexValue">オプションが「色コードで指定」の場合の Hex 文字列。</param>
        /// <returns>適用された最終的な Color。</returns>
        public Color ApplyBackgroundColorSetting(string option, string hexValue)
        {
            Color finalColor;

            if (option == ColorOptionNames.CustomHex)
            {
                // "色コードで指定" が選択されている場合
                if (!TryConvertHexToColor(hexValue, out finalColor))
                {
                    // 解析失敗時は定数のデフォルト色を使用
                    finalColor = (Color)ColorConverter.ConvertFromString(ColorConstants.BackgroundGreenColor);
                    Debug.WriteLine($"ColorSettingsManager: Invalid Hex for background: {hexValue}. Using default {ColorConstants.BackgroundGreenColor}.");
                }
            }
            else if (option == ColorOptionNames.Green)
            {
                finalColor = (Color)ColorConverter.ConvertFromString(ColorConstants.BackgroundGreenColor);
            }
            else if (option == ColorOptionNames.Blue)
            {
                finalColor = (Color)ColorConverter.ConvertFromString(ColorConstants.BackgroundBlueColor);
            }
            else // 想定外のオプションの場合のフォールバック
            {
                finalColor = (Color)ColorConverter.ConvertFromString(ColorConstants.BackgroundGreenColor);
                Debug.WriteLine($"ColorSettingsManager: Unexpected option for background: {option}. Using default {ColorConstants.BackgroundGreenColor}.");
            }

            // ★ 内部の Brush フィールドを更新 ★
            _backgroundColorBrush = new SolidColorBrush(finalColor);
            return finalColor;
        }

        /// <summary>
        /// 通常ノート色の設定をユーザーの選択に基づいて適用し、内部の Brush を更新します。
        /// </summary>
        /// <param name="option">選択されたオプション名（例: "デフォルト色", "色コードで指定"）。</param>
        /// <param name="hexValue">オプションが「色コードで指定」の場合の Hex 文字列。</param>
        /// <returns>適用された最終的な Color。</returns>
        public Color ApplyNormalNoteColorSetting(string option, string hexValue)
        {
            Color finalColor;

            if (option == ColorOptionNames.CustomHex)
            {
                // "色コードで指定" が選択されている場合
                if (!TryConvertHexToColor(hexValue, out finalColor))
                {
                    // 解析失敗時は定数のデフォルト色を使用
                    finalColor = (Color)ColorConverter.ConvertFromString(ColorConstants.NormalNoteColor);
                    Debug.WriteLine($"ColorSettingsManager: Invalid Hex for normal note: {hexValue}. Using default {ColorConstants.NormalNoteColor}.");
                }
            }
            else if (option == ColorOptionNames.Default)
            {
                // デフォルト色に定数を使用
                finalColor = (Color)ColorConverter.ConvertFromString(ColorConstants.NormalNoteColor);
            }
            else // 想定外のオプションの場合
            {
                // デフォルト色に定数を使用
                finalColor = (Color)ColorConverter.ConvertFromString(ColorConstants.NormalNoteColor);
                Debug.WriteLine($"ColorSettingsManager: Unexpected option for normal note: {option}. Using default {ColorConstants.NormalNoteColor}.");
            }

            // ★ 内部の Brush フィールドを更新 ★
            _normalNoteColorBrush = new SolidColorBrush(finalColor);
            return finalColor;
        }

        /// <summary>
        /// 再生ノート色の設定をユーザーの選択に基づいて適用し、内部の Brush を更新します。
        /// </summary>
        /// <param name="option">選択されたオプション名（例: "デフォルト色", "色コードで指定"）。</param>
        /// <param name="hexValue">オプションが「色コードで指定」の場合の Hex 文字列。</param>
        /// <returns>適用された最終的な Color。</returns>
        public Color ApplyPlayingColorSetting(string option, string hexValue)
        {
            Color finalColor;

            if (option == ColorOptionNames.CustomHex)
            {
                // "色コードで指定" が選択されている場合
                if (!TryConvertHexToColor(hexValue, out finalColor))
                {
                    // 解析失敗時は定数のデフォルト色を使用
                    finalColor = (Color)ColorConverter.ConvertFromString(ColorConstants.PlayingNoteColor);
                    Debug.WriteLine($"ColorSettingsManager: Invalid Hex for playing note: {hexValue}. Using default {ColorConstants.PlayingNoteColor}.");
                }
            }
            else if (option == ColorOptionNames.Default)
            {
                // デフォルト色に定数を使用
                finalColor = (Color)ColorConverter.ConvertFromString(ColorConstants.PlayingNoteColor);
            }
            else // 想定外のオプションの場合
            {
                // デフォルト色に定数を使用
                finalColor = (Color)ColorConverter.ConvertFromString(ColorConstants.PlayingNoteColor);
                Debug.WriteLine($"ColorSettingsManager: Unexpected option for playing note: {option}. Using default {ColorConstants.PlayingNoteColor}.");
            }

            // ★ 内部の Brush フィールドを更新 ★
            _playingNoteColorBrush = new SolidColorBrush(finalColor);
            return finalColor;
        }


        /// <summary>
        /// Hex 文字列を Color オブジェクトに変換します。
        /// このメソッドは内部ヘルパーとして使用します。
        /// </summary>
        /// <param name="hexString">Hex 形式の文字列（例: "#RRGGBB" または "#AARRGGBB"）。</param>
        /// <param name="color">変換された Color オブジェクト（成功時）。</param>
        /// <returns>変換が成功した場合は true、それ以外の場合は false。</returns>
        private bool TryConvertHexToColor(string hexString, out Color color)
        {
            color = Colors.Black; // 変換失敗時のデフォルト値

            if (string.IsNullOrEmpty(hexString)) return false;

            // Hex 文字列の前に # がついていない場合は追加 (ColorConverter の要件)
            if (!hexString.StartsWith("#"))
            {
                hexString = "#" + hexString;
            }

            try
            {
                // ColorConverter は "#RRGGBB" や "#AARRGGBB" 形式を解析できます。
                var convertedColor = ColorConverter.ConvertFromString(hexString);
                if (convertedColor != null)
                {
                    color = (Color)convertedColor;
                    return true;
                }
                return false; // 変換結果が null の場合
            }
            catch
            {
                // 解析失敗 (無効なフォーマットなど)
                return false;
            }
        }

        // ユーザーが選択したオプション自体を文字列として取得するメソッドなども必要に応じて追加できます。
        // これは、アプリケーションの状態を保存・復元する際に役立ちます。
        // public string GetBackgroundColorOption() { ... }
    }
}