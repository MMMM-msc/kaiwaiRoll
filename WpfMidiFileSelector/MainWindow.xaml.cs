using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;


namespace WpfMidiFileSelector
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private MidiDataManager _midiDataManager;


        private Playback _playback;
        private OutputDevice _outputDevice;

        private const double NoteHeight = 10.0;
        private const double TimeScale = 0.1;
        private const double PianoRollHeight = 128;
        private const double TotalNoteRangeHeight = 128 * NoteHeight; // NoteNumber 全ての範囲に必要な高さ
        private const double LeftPadding = 50.0;
        private const double RightPadding = 100.0;

        private SolidColorBrush _backgroundColor = new SolidColorBrush(Colors.Black); // デフォルトは黒
        private SolidColorBrush _normalNoteColor = new SolidColorBrush(Colors.DodgerBlue); // デフォルトは青
        private SolidColorBrush _playingNoteColor = new SolidColorBrush(Colors.Yellow); // デフォルトは黄色

        public MainWindow()
        {

            _midiDataManager = new MidiDataManager();
            InitializeComponent();

            try
            {
                var outputDevices = OutputDevice.GetAll();
                Debug.WriteLine("利用可能な MIDI 出力デバイス:");

                if (!outputDevices.Any())
                {
                    Debug.WriteLine("利用可能な MIDI 出力デバイスが見つかりません。");
                    MessageBox.Show("MIDI 出力デバイスが見つかりません。MIDI 再生機能は利用できません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    _outputDevice = null; // デバイスがない場合は null のまま
                }
                else
                {
                    _outputDevice = outputDevices.First();
                    Debug.WriteLine($"使用する MIDI 出力デバイス: {_outputDevice.Name}");
                    foreach (var outputDevice in outputDevices)
                    {
                        Debug.WriteLine($"- {outputDevice.Name}");
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MIDI デバイスの取得中にエラーが発生しました: {ex}");
                MessageBox.Show($"MIDI デバイスの初期化中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                _outputDevice = null; // エラー発生時は null に
            }

            InitializeColorComboBoxes();
            this.Closing += MainWindow_Closing;
        }

        private void InitializeColorComboBoxes()
        {
            var colors = typeof(Colors).GetProperties()
                .Where(p => p.PropertyType == typeof(Color))
                .Select(p => p.Name)
                .ToList();

            backgroundColorComboBox.ItemsSource = colors;
            normalNoteColorComboBox.ItemsSource = colors;
            playingNoteColorComboBox.ItemsSource = colors;

            backgroundColorComboBox.SelectedItem = _backgroundColor.Color.ToString(); // Color オブジェクトから名前を取得
            normalNoteColorComboBox.SelectedItem = _normalNoteColor.Color.ToString();
            playingNoteColorComboBox.SelectedItem = _playingNoteColor.Color.ToString();

        }

        // ★ ウィンドウが閉じられるときに呼び出されるイベントハンドラー ★
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Playback オブジェクトと出力デバイスを適切に破棄する
            _playback?.Dispose(); // Playback が null でなければ Dispose() を呼び出す
            _outputDevice?.Dispose(); // 出力デバイスが null でなければ Dispose() を呼び出す
        }


        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "MIDIファイルの選択";
            openFileDialog.Filter = "MIDIファイル (*.mid;*.midi)|*.mid;*.midi|すべてのファイル (*.*)|*.*";

            bool? result = openFileDialog.ShowDialog();

            if (result != true) { return; }

            string selectedFilePath = openFileDialog.FileName;
            filePathTextBox.Text = selectedFilePath;

            bool loadSuccess = _midiDataManager.LoadFile(selectedFilePath);

            if (!loadSuccess)
            {
                // MidiDataManager の LoadFile 内でエラーメッセージはログに出力されているので、
                // 必要であればユーザー向けのメッセージボックスを表示することも検討
                MessageBox.Show("MIDIファイルの読み込みに失敗しました。詳細はログを確認してください。", "読み込みエラー", MessageBoxButton.OK, MessageBoxImage.Error);

                // 読み込み失敗したので、データをクリアし、ボタンを無効にする
                _midiDataManager.ClearData(); // LoadFile 内でもクリアされますが、念のため
                // 描画領域もクリア
                pianoRollCanvas.Children.Clear();
                pianoRollCanvas.Width = 0;
                pianoRollCanvas.Height = 0;

                playButton.IsEnabled = false;
                stopButton.IsEnabled = false;
                return;
            }

            MessageBox.Show("MIDIファイルを正常に読み込みました。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

            if (_midiDataManager.MidiFile != null && _outputDevice != null)
            {
                playButton.IsEnabled = true;
                stopButton.IsEnabled = false;
            }
            else
            {
                // デバイスがない場合は再生関連ボタンは無効のまま
                playButton.IsEnabled = false;
                stopButton.IsEnabled = false;
            }


            // ★ MidiDataManager からノートリストを取得して描画メソッドに渡す (または描画メソッド内で MidiDataManager を参照する) ★
            // ここでは DrawPianoRoll メソッド内で MidiDataManager を参照するようにします。
            DrawPianoRoll();

        }
        /// <summary>
        /// 取得したノート情報 (_currentNotes) を元にピアノロールを描画します。
        /// </summary>
        private void DrawPianoRoll()
        {
            // 描画前に Canvas の既存の要素を全てクリアする
            // これにより、新しいファイルを読み込むたびに前の描画が消去されます。
            pianoRollCanvas.Children.Clear();

            pianoRollCanvas.Background = _backgroundColor;

            List<Note> notesToDraw = _midiDataManager.Notes;

            if (notesToDraw == null || !notesToDraw.Any())
            {
                Debug.WriteLine("描画するノートがありません。");
                pianoRollCanvas.Width = 0;
                pianoRollCanvas.Height = 0;
                return;
            }

            double yOffset = TotalNoteRangeHeight;
            double maxTime = 0;

            foreach (Note note in notesToDraw)
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
                noteRectangle.Fill = _normalNoteColor;

                noteRectangle.Tag = note;

                Canvas.SetLeft(noteRectangle, x);
                Canvas.SetTop(noteRectangle, y);

                pianoRollCanvas.Children.Add(noteRectangle);
                Debug.WriteLine($" Note (Drawn): Num={note.NoteNumber}, Time={note.Time}, Length={note.Length}, Vel={note.Velocity}");

                double noteEndTime = (double)note.Time + note.Length;
                if (noteEndTime > maxTime) maxTime = noteEndTime;

            }

            pianoRollCanvas.Height = TotalNoteRangeHeight;

            double requiredWidth = maxTime * TimeScale + LeftPadding + RightPadding;
            pianoRollCanvas.Width = requiredWidth;


            Debug.WriteLine("ピアノロールの描画を完了しました。");
            Debug.WriteLine($"計算された Canvas Width: {pianoRollCanvas.Width}, Height: {pianoRollCanvas.Height}");

        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {

            MidiFile fileToPlay = _midiDataManager.MidiFile;

            if (fileToPlay == null || _outputDevice == null)
            {
                MessageBox.Show("再生する MIDI ファイルが読み込まれていないか、出力デバイスが利用できません。", "再生エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // 既存の Playback オブジェクトがあれば一度破棄する
            _playback?.Dispose();

            try
            {
                _playback = fileToPlay.GetPlayback(_outputDevice);
                _playback.NotesPlaybackStarted += Playback_NotePlaybackStarted;
                _playback.NotesPlaybackFinished += Playback_NotePlaybackFinished;
                _playback.Finished += Playback_Finished;
                _playback.Start();
                Debug.WriteLine("MIDI 再生を開始しました。");
                playButton.IsEnabled = false; // 再生中は再生ボタンを無効に
                stopButton.IsEnabled = true;  // 停止ボタンを有効に
                // ★ オプション: 再生終了時のイベントハンドラーを設定 ★
                _playback.Finished += Playback_Finished;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MIDI 再生開始中にエラーが発生しました: {ex}");
                MessageBox.Show($"MIDI 再生開始中にエラーが発生しました: {ex.Message}", "再生エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                // エラー発生時はボタンを無効にする
                playButton.IsEnabled = true;
                stopButton.IsEnabled = false;
                _playback?.Dispose(); // エラー発生時は破棄しておく
                _playback = null;
            }


        }

        private void Playback_Finished(object sender, EventArgs e)
        {
            Debug.WriteLine("MIDI 再生が終了しました。");

            Dispatcher.Invoke(() =>
            {
                playButton.IsEnabled = true;
                stopButton.IsEnabled = false;
            }

                );

            _playback?.Dispose();
            _playback = null;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // Playback オブジェクトが存在し、再生中であるか確認
            if (_playback != null && _playback.IsRunning)
            {
                // 再生を停止
                _playback.Stop();
                Debug.WriteLine("MIDI 再生を停止しました。");

                // ボタンの状態を更新
                playButton.IsEnabled = true;  // 停止したので再生ボタンを有効に
                stopButton.IsEnabled = false; // 停止ボタンを無効に

                // 停止したら Playback オブジェクトを破棄することも検討
                // _playback?.Dispose();
                // _playback = null;
            }
        }
        private void Playback_NotePlaybackStarted(object sender, NotesEventArgs e) // ★ シグネチャを NotesEventArgs に変更 ★
        {
            // このイベントはUIスレッドとは別のスレッドで発生する可能性があるため、
            // UI要素の操作は Dispatcher.Invoke を使ってUIスレッドで行う必要があります。
            Dispatcher.Invoke(() =>
            {

                if (e.Notes == null || !e.Notes.Any())
                {
                    Debug.WriteLine("NotePlaybackStarted: No notes in event args.");
                    return; // ノートがない場合はここでメソッドを終了
                }


                // ★ イベント引数 e.Notes から再生開始されたノートのコレクションを取得 ★
                // NotesEventArgs に Notes プロパティがあることを期待します。
                IEnumerable<Note> playedNotes = e.Notes;

                Debug.WriteLine($"NotePlaybackStarted: {playedNotes?.Count() ?? 0} note(s) started."); // Null チェックとカウント

                // 同時に開始された各ノートについて処理
                foreach (var playedNote in playedNotes)
                {
                    Debug.WriteLine($"  Played Note: Num={playedNote.NoteNumber}, Time={playedNote.Time}, Length={playedNote.Length}, Vel={playedNote.Velocity}");

                    // 再生開始されたノートに対応する描画された Rectangle を探し出す
                    // Canvas の子要素 (Rectangle) を調べて、Tag に設定した Note オブジェクトと比較します。
                    var targetRectangle = pianoRollCanvas.Children.OfType<Rectangle>()
                        .FirstOrDefault(rect =>
                        {
                            // Tag に Note オブジェクトが設定されているか確認
                            if (rect.Tag is Note note)
                            {
                                bool numMatch = note.NoteNumber == playedNote.NoteNumber;
                                bool timeMatch = note.Time == playedNote.Time;
                                Debug.WriteLine($"    Comparing Tag Note (Num={note.NoteNumber}, Time={note.Time}) with Played Note (Num={playedNote.NoteNumber}, Time={playedNote.Time}). NumMatch={numMatch}, TimeMatch={timeMatch}");
                                // ★ Tag の Note オブジェクトが、再生開始された playedNote と同じであるか比較 ★
                                // DryWetMidi の Note クラスは、デフォルトで値の等価性 (音高, 時間, 長さなど) を比較します。
                                return numMatch && timeMatch; ;
                            }
                            return false; // Tag が Note でない場合はスキップ
                        });

                    // 対応する Rectangle が見つかった場合
                    if (targetRectangle != null)
                    {
                        // ★ ノートの色をハイライト色に変更 ★
                        targetRectangle.Fill = _playingNoteColor; // ハードコードされた Colors.Yellow から変更
                    }
                    else
                    {
                        // 対応する Rectangle が見つからなかった場合に警告を出力
                        Debug.WriteLine($"  Warning: Could not find Rectangle for Played Note Num={playedNote.NoteNumber}, Time={playedNote.Time}");
                        // デバッグのため、Canvas の子要素の Tag 情報を全て出力してみる (必要な場合のみコメント解除)
                        // Debug.WriteLine("  Canvas children Tag info:");
                        // foreach (var child in pianoRollCanvas.Children.OfType<Rectangle>())
                        // {
                        //     if (child.Tag is Note tagNote)
                        //     {
                        //         Debug.WriteLine($"    Tag Note: Num={tagNote.NoteNumber}, Time={tagNote.Time}, Length={tagNote.Length}");
                        //     }
                        // }
                    }
                }

            });
        }
        private void Playback_NotePlaybackFinished(object sender, NotesEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Notes == null || !e.Notes.Any())
                {
                    Debug.WriteLine("NotePlaybackFinished: No notes in event args.");
                    return; // ノートがない場合はここでメソッドを終了
                }

                IEnumerable<Note> finishedNotes = e.Notes;
                Debug.WriteLine($"NotePlaybackFinished: {finishedNotes?.Count() ?? 0} note(s) finished."); // Null チェックとカウント
                // 同時に終了した各ノートについて処理

                foreach (var finishedNote in finishedNotes)
                {
                    Debug.WriteLine($"  Num={finishedNote.NoteNumber}, Time={finishedNote.Time}, Length={finishedNote.Length}");

                    var targetRectangle = pianoRollCanvas.Children.OfType<Rectangle>()
                    .FirstOrDefault(rect =>
                    {
                        if (rect.Tag is Note note)
                        {
                            bool numMatch = note.NoteNumber == finishedNote.NoteNumber;
                            bool timeMatch = note.Time == finishedNote.Time;
                            Debug.WriteLine($"    Comparing Tag Note (Num={note.NoteNumber}, Time={note.Time}) with Finished Note (Num={finishedNote.NoteNumber}, Time={finishedNote.Time}). NumMatch={numMatch}, TimeMatch={timeMatch}");

                            return numMatch && timeMatch;
                        }
                        return false;
                    });

                    if (targetRectangle != null)
                    {
                        targetRectangle.Fill = _normalNoteColor;
                        Debug.WriteLine($"  Found and reset color for Rectangle for Note Num={finishedNote.NoteNumber}, Time={finishedNote.Time}");
                    }
                    else
                    {
                        // 対応する Rectangle が見つからなかった場合に警告を出力
                        Debug.WriteLine($"  Warning: Could not find Rectangle for Finished Note Num={finishedNote.NoteNumber}, Time={finishedNote.Time}");
                    }
                }

            });
        }


        // ★ 描画された全てのノートの色を元の色に戻すメソッド (変更なし) ★
        private void ResetNoteColors()
        {
            // Canvas の全ての子要素 (Rectangle) をループ処理し、色を元の色に戻す
            foreach (var child in pianoRollCanvas.Children.OfType<Rectangle>())
            {
                // Tag に Note オブジェクトがあり、Fill が元の色でない場合にのみ変更
                // ★ 元の色を保持している _normalNoteColor に変更 ★
                if (child.Tag is Note && (child.Fill as SolidColorBrush)?.Color != _normalNoteColor.Color)
                {
                    child.Fill = _normalNoteColor; // 元の青色から保持している色に変更
                }
            }
        }

        private void ColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            string selectedColorName = comboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedColorName)) return;

            Color selectedColor = (Color)typeof(Colors)
                .GetProperty(selectedColorName).GetValue(null);
            SolidColorBrush selectedBrush = new SolidColorBrush(selectedColor);

            // どの ComboBox の選択が変更されたかに応じて、対応する色フィールドを更新
            if (comboBox == backgroundColorComboBox)
            {
                _backgroundColor = selectedBrush;
                // 背景色が変わったので Canvas の背景を即座に更新
                pianoRollCanvas.Background = _backgroundColor;
            }
            else if (comboBox == normalNoteColorComboBox)
            {
                _normalNoteColor = selectedBrush;
                // 再生していないノートの色が変わったので、画面を再描画して反映
                DrawPianoRoll(); // 再描画で全てのノートが新しい通常色になります
            }
            else if (comboBox == playingNoteColorComboBox)
            {
                _playingNoteColor = selectedBrush;
                // 再生中のノートの色はイベントハンドラーで適用されるため、再描画は不要です
            }

            Debug.WriteLine($"Color changed for {comboBox.Name} to {selectedColorName}");

        }

    }
}