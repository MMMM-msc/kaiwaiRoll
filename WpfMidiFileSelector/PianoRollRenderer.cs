using Melanchall.DryWetMidi.Interaction;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls; // Canvas に必要
using System.Windows.Media; // SolidColorBrush, Color に必要
using System.Windows.Shapes; // Rectangle に必要
using System.Diagnostics; // Debug.WriteLine に必要


namespace WpfMidiFileSelector
{
    internal class PianoRollRenderer
    {
        // ★ 描画設定用の定数。必要に応じてコンストラクターで設定可能にするなども検討 ★
        private const double NoteHeight = 10.0;
        private const double TimeScale = 0.1;
        private const double LeftPadding = 50.0;
        private const double RightPadding = 100.0;
        private const double TotalNoteRangeHeight = 128 * NoteHeight; // NoteNumber 全ての範囲に必要な高さ

        private Dictionary<(int NoteNumber, long Time), Rectangle> _noteToRectangleMap;

        public PianoRollRenderer()
        {
            // コンストラクターで描画設定を受け取るなども考えられますが、シンプルにここでは定数を使用
        }

        /// <summary>
        /// 指定された Canvas にピアノロールを描画します。
        /// </summary>
        /// <param name="canvas">描画対象の Canvas コントロール。</param>
        /// <param name="notes">描画するノートのリスト。</param>
        /// <param name="backgroundColor">背景色。</param>
        /// <param name="normalNoteColor">再生していないノートの色。</param>
        public void Render(Canvas canvas, List<Note> notes, SolidColorBrush backgroundColor, SolidColorBrush normalNoteColor)
        {
            if (canvas == null)
            {
                Debug.WriteLine("PianoRollRenderer: 描画対象の Canvas が null です。");
                return;
            }


            // 描画前に Canvas の既存の要素を全てクリアする
            canvas.Children.Clear();
            // Note と Rectangle のマッピングもクリア
            _noteToRectangleMap = new Dictionary<(int NoteNumber, long Time), Rectangle>();

            // Canvas の背景色を設定
            canvas.Background = backgroundColor;

            // 描画するノートがない場合は終了
            if (notes == null || !notes.Any())
            {
                Debug.WriteLine("PianoRollRenderer: 描画するノートがありません。");
                canvas.Width = 0;
                canvas.Height = 0;
                return;
            }

            double yOffset = TotalNoteRangeHeight;
            double maxTime = 0;

            foreach (Note note in notes)
            {
                var noteRectangle = new Rectangle();

                //座標と大きさを決める
                double y = (127 - note.NoteNumber) * NoteHeight;
                double x = note.Time * TimeScale + LeftPadding;

                double width = note.Length * TimeScale;
                if (width < 1.0) width = 1.0;

                double height = NoteHeight;

                noteRectangle.Width = width;
                noteRectangle.Height = height;
                // 矩形の塗りつぶし色を「再生していないノートの色」に設定
                noteRectangle.Fill = normalNoteColor;

                noteRectangle.Stroke = null;

                // Tag に Note オブジェクト自体を格納
                noteRectangle.Tag = note;
                // Note と Rectangle のマッピングを辞書に登録
                try
                {
                    _noteToRectangleMap[(note.NoteNumber, note.Time)] = noteRectangle;
                }
                catch (ArgumentException ex)
                {
                    Debug.WriteLine($"PianoRollRenderer: Warning: Duplicate key when adding Note to map (Num={note.NoteNumber}, Time={note.Time}). A rectangle for this key already exists.");
                }


                // Canvas 上での位置を設定
                Canvas.SetLeft(noteRectangle, x);
                Canvas.SetTop(noteRectangle, y);

                // Canvas の子要素として矩形を追加
                canvas.Children.Add(noteRectangle);

                // 最大時間を更新
                double noteEndTime = (double)note.Time + note.Length;
                if (noteEndTime > maxTime) maxTime = noteEndTime;
            }


            // Canvas サイズ設定
            canvas.Height = TotalNoteRangeHeight;
            double requiredWidth = maxTime * TimeScale + LeftPadding + RightPadding;
            canvas.Width = requiredWidth;

            Debug.WriteLine("PianoRollRenderer: ピアノロールの描画を完了しました。");
            Debug.WriteLine($"PianoRollRenderer: 計算された Canvas Width: {canvas.Width}, Height: {canvas.Height}");
        }

        /// <summary>
        /// 指定されたノートに対応する描画済み Rectangle の色を変更します。
        /// </summary>
        /// <param name="note">色を変更するノート。</param>
        /// <param name="color">設定する色。</param>
        public void SetNoteColor(Note note, SolidColorBrush color)
        {
            // _noteToRectangleMap が null でないか、および note が null でないかを確認
            if (_noteToRectangleMap != null && note != null)
            {
                // ★ 辞書から Rectangle を検索。キーを (NoteNumber, Time) の ValueTuple にする ★
                if (_noteToRectangleMap.TryGetValue((note.NoteNumber, note.Time), out Rectangle targetRectangle))
                {
                    // Rectangle が見つかった場合
                    targetRectangle.Fill = color;
                    // Debug.WriteLine($"PianoRollRenderer: Note color changed for Num={note.NoteNumber}, Time={note.Time}. Rectangle found.");
                }
                else
                {
                    // Rectangle が見つからなかった場合
                    // このメッセージが表示されるのは、イベントで渡されたノートが、
                    // 描画時に登録されたノートのどれとも (NoteNumber, Time) の組み合わせで一致しない場合です。
                    Debug.WriteLine($"PianoRollRenderer: Could not find Rectangle in map for Note Num={note.NoteNumber}, Time={note.Time}, Length={note.Length}.");
                }
            }
            else
            {
                if (_noteToRectangleMap == null) Debug.WriteLine("PianoRollRenderer: _noteToRectangleMap is null in SetNoteColor.");
                if (note == null) Debug.WriteLine("PianoRollRenderer: Input Note is null in SetNoteColor.");
            }
        }

        /// <summary>
        /// 描画された全てのノートの色を元の色に戻します。
        /// </summary>
        /// <param name="normalNoteColor">通常ノートの色。</param>
        public void ResetAllNoteColors(SolidColorBrush normalNoteColor)
        {
            // マッピング辞書に登録されている全ての Rectangle の色をリセット
            if (_noteToRectangleMap != null)
            {
                foreach (var rectangle in _noteToRectangleMap.Values)
                {
                    rectangle.Fill = normalNoteColor;
                }
                Debug.WriteLine("PianoRollRenderer: 全てのノートの色をリセットしました。");
            }
        }

    }
}
