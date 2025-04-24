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
        private PianoRollRenderer _pianoRollRenderer;
        private PlaybackManager _playbackManager;

        private SolidColorBrush _backgroundColor = new SolidColorBrush(Colors.Green);
        private SolidColorBrush _normalNoteColor = new SolidColorBrush(Colors.LightGray);
        private SolidColorBrush _playingNoteColor = new SolidColorBrush(Colors.White);

        public MainWindow()
        {

            _midiDataManager = new MidiDataManager();
            _pianoRollRenderer = new PianoRollRenderer();
            _playbackManager = new PlaybackManager();

            InitializeComponent();

            InitializeColorComboBoxes();
            this.Closing += MainWindow_Closing;

            // ★ PlaybackManager のカスタムイベントを購読 ★
            _playbackManager.NoteStarted += PlaybackManager_NoteStarted;
            _playbackManager.NoteFinished += PlaybackManager_NoteFinished;
            _playbackManager.PlaybackCompleted += PlaybackManager_PlaybackCompleted;
        }

        private void InitializeColorComboBoxes()
        {
            var colors = typeof(Colors).GetProperties()
                .Where(p => p.PropertyType == typeof(Color))
                .Select(p => p.Name)
                .ToList();
            backgroundColorComboBox.ItemsSource = new List<string> { ColorOptionNames.Green, ColorOptionNames.Blue, ColorOptionNames.CustomHex };
            normalNoteColorComboBox.ItemsSource = new List<string> { ColorOptionNames.Default, ColorOptionNames.CustomHex };
            playingNoteColorComboBox.ItemsSource = new List<string> { ColorOptionNames.Default, ColorOptionNames.CustomHex };
            
            backgroundColorComboBox.SelectedItem = ColorOptionNames.Green;
            normalNoteColorComboBox.SelectedItem = ColorOptionNames.Default;
            playingNoteColorComboBox.SelectedItem = ColorOptionNames.Default;

            backgroundColorHexTextBox.Visibility = Visibility.Collapsed;
            normalNoteColorHexTextBox.Visibility = Visibility.Collapsed;
            playingNoteColorHexTextBox.Visibility = Visibility.Collapsed;

            backgroundColorHexTextBox.Text = _backgroundColor.Color.ToString();
            normalNoteColorHexTextBox.Text = _normalNoteColor.Color.ToString();
            playingNoteColorHexTextBox.Text = _playingNoteColor.Color.ToString();

        }

        // ★ ウィンドウが閉じられるときに呼び出されるイベントハンドラー ★
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _playbackManager?.Dispose();
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

            if (_midiDataManager.MidiFile != null && _playbackManager.OutputDevice != null)
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

            _pianoRollRenderer.Render(pianoRollCanvas, _midiDataManager.Notes, _backgroundColor, _normalNoteColor);


        }


        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {

            MidiFile fileToPlay = _midiDataManager.MidiFile;

            if (fileToPlay == null || _playbackManager.OutputDevice == null)
            {
                MessageBox.Show("再生する MIDI ファイルが読み込まれていないか、出力デバイスが利用できません。", "再生エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _pianoRollRenderer.ResetAllNoteColors(_normalNoteColor);

            _playbackManager.StartPlayback(fileToPlay);

            playButton.IsEnabled = false;
            stopButton.IsEnabled = true;

        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {

            if (!_playbackManager.IsPlaying) return;


            // 再生を停止
            _playbackManager.StopPlayback();
            Debug.WriteLine("MIDI 再生を停止しました。");

            playButton.IsEnabled = true;
            stopButton.IsEnabled = false;

            _pianoRollRenderer.ResetAllNoteColors(_normalNoteColor);

        }

        // これらのメソッドの中で、UI の更新や PianoRollRenderer への指示を行います。
        private void PlaybackManager_NoteStarted(object sender, NotesEventArgs e)
        {
            // PlaybackManager からノート開始イベントを受け取った
            // UI スレッドで PianoRollRenderer の色変更メソッドを呼び出す
            Dispatcher.Invoke(() =>
            {
                if (e.Notes == null || !e.Notes.Any()) return;

                foreach (var note in e.Notes)
                {
                    _pianoRollRenderer.SetNoteColor(note, _playingNoteColor);
                }
            });
        }
        private void PlaybackManager_NoteFinished(object sender, NotesEventArgs e)
        {
            // PlaybackManager からノート終了イベントを受け取った
            // UI スレッドで PianoRollRenderer の色変更メソッドを呼び出す
            Dispatcher.Invoke(() =>
            {
                if (e.Notes == null || !e.Notes.Any()) return;

                foreach (var note in e.Notes)
                {
                    _pianoRollRenderer.SetNoteColor(note, _normalNoteColor);
                }
            });
        }

        private void PlaybackManager_PlaybackCompleted(object sender, EventArgs e)
        {
            // PlaybackManager から再生完了イベントを受け取った
            // UI スレッドでボタンの状態を更新
            Debug.WriteLine("MainWindow: 再生完了イベントを受信しました。");
            Dispatcher.Invoke(() =>
            {
                playButton.IsEnabled = true;
                stopButton.IsEnabled = false;
            });
            // 再生完了時の色リセットは StopButton_Click や Finished イベントで行われます。
        }




        private void ColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            string selectedOption = comboBox.SelectedItem as string;
            TextBox hexTextBox = null;
            SolidColorBrush brushToUpdate = null;
            Color defaultColorValue = Colors.Black;
            bool isNormalNoteColor = false;
            bool isPlayingNoteColor = false;

            // どの ComboBox がイベントを発生させたかを判定
            if (comboBox == backgroundColorComboBox)
            {
                hexTextBox = backgroundColorHexTextBox;
                // _backgroundColor フィールド自体を更新するため、ここでは参照を保持するだけ
            }
            else if (comboBox == normalNoteColorComboBox)
            {
                hexTextBox = normalNoteColorHexTextBox;
                defaultColorValue = Colors.LightGray; // 通常ノートのデフォルト色
                isNormalNoteColor = true;
            }
            else if (comboBox == playingNoteColorComboBox) // XAML の Name と合わせる
            {
                hexTextBox = playingNoteColorHexTextBox;
                defaultColorValue = Colors.White; // 再生ノートのデフォルト色
                isPlayingNoteColor = true;
            }

            bool isHexSelected = selectedOption == ColorOptionNames.CustomHex;
            hexTextBox.Visibility = isHexSelected ? Visibility.Visible : Visibility.Collapsed;

            Color finalColor = Colors.Black; // 最終的に決定される色。初期値は適当な色。

            if (isHexSelected)
            {
                if (TryConvertHexToColor(hexTextBox.Text, out finalColor))
                {
                    // Hex 値の解析に成功
                    Debug.WriteLine($"Parsed Hex {hexTextBox.Text} to Color {finalColor}");
                }
                else
                {
                    Debug.WriteLine($"Invalid Hex string: {hexTextBox.Text}. Using a fallback color.");
                    // エラー処理として、どの色の設定かによって適切なフォールバック色を使用
                    if (comboBox == normalNoteColorComboBox) finalColor = Colors.LightGray;
                    else if (comboBox == playingNoteColorComboBox) finalColor = Colors.White;
                    else if (comboBox == backgroundColorComboBox) finalColor = Colors.Green;
                }
            }
            else
            {
                if (comboBox == backgroundColorComboBox)
                {
                    // 背景色の場合は Green または Blue を直接指定
                    if (selectedOption == ColorOptionNames.Green) finalColor = Colors.Green;
                    else if (selectedOption == ColorOptionNames.Blue) finalColor = Colors.Blue;
                    // ここに他のプリセット背景色を追加することも可能
                    else finalColor = Colors.Black; // 想定外のオプションの場合のフォールバック
                }
                else // 通常ノート色または再生ノート色の場合は "Default"
                {
                    finalColor = defaultColorValue; // 定義しておいたデフォルト色を使用
                }
                Debug.WriteLine($"Selected preset option: {selectedOption} -> {finalColor}");
            }

            SolidColorBrush newBrush = new SolidColorBrush(finalColor);

            if (comboBox == backgroundColorComboBox)
            {
                _backgroundColor = newBrush;
                // 背景色が変わったので Canvas の背景を即座に更新
                if (pianoRollCanvas != null)
                {
                    pianoRollCanvas.Background = _backgroundColor;
                }
            }
            else if (comboBox == normalNoteColorComboBox)
            {
                _normalNoteColor = newBrush;
                // 再生していないノートの色が変わったので、画面を再描画して反映
                // null チェックを追加 (_midiDataManager や pianoRollCanvas が初期化されているか)
                if (pianoRollCanvas != null && _midiDataManager != null)
                {
                    _pianoRollRenderer.Render(pianoRollCanvas, _midiDataManager.Notes, _backgroundColor, _normalNoteColor);
                }
            }
            else if (comboBox == playingNoteColorComboBox)
            {
                _playingNoteColor = newBrush;
                // 再生中のノートの色は PlaybackManager のイベントハンドラーで適用されるため、
                // ここで即座に再描画する必要はありません。
            }
        }

        private void HexTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // どの TextBox がイベントを発生させたか判定し、対応する ComboBox と色フィールドを特定
            ComboBox correspondingComboBox = null;
            SolidColorBrush brushToUpdate = null;


            if (textBox == backgroundColorHexTextBox)
            {
                correspondingComboBox = backgroundColorComboBox;
                brushToUpdate = _backgroundColor;
            }
            else if (textBox == normalNoteColorHexTextBox)
            {
                correspondingComboBox = normalNoteColorComboBox;
                brushToUpdate = _normalNoteColor;
            }
            else if (textBox == playingNoteColorHexTextBox) // XAML の名前に合わせる
            {
                correspondingComboBox = playingNoteColorComboBox; // XAML の名前に合わせる
                brushToUpdate = _playingNoteColor;
            }
            else
            {
                // 想定外の TextBox がイベントを発生させた場合
                Debug.WriteLine($"HexTextBox_TextChanged: Unknown TextBox {textBox.Name}");
                return;
            }

            // 対応する ComboBox の選択が "Custom Hex" でない場合は処理しない
            // ComboBox で "Custom Hex" 以外が選択されている場合、TextBox の内容は無視されます。
            if (correspondingComboBox.SelectedItem as string != ColorOptionNames.CustomHex)
            {
                return;
            }

            // Hex 値の解析を試みる
            Color newColor;
            if (TryConvertHexToColor(textBox.Text, out newColor))
            {
                // 解析に成功した場合
                SolidColorBrush newBrush = new SolidColorBrush(newColor);

                // 対応する色フィールドを更新
                // フィールドの参照を直接更新する必要があります。
                if (textBox == backgroundColorHexTextBox)
                {
                    _backgroundColor = newBrush;
                    // 背景色なので Canvas の背景を即座に更新
                    if (pianoRollCanvas != null)
                    {
                        pianoRollCanvas.Background = _backgroundColor;
                    }
                    Debug.WriteLine($"HexTextBox_TextChanged: Background color updated to {newColor}");
                }
                else if (textBox == normalNoteColorHexTextBox)
                {
                    _normalNoteColor = newBrush;
                    // 通常ノート色なのでピアノロールを再描画して反映
                    // null チェックを追加 (_midiDataManager や pianoRollCanvas が初期化されているか)
                    if (pianoRollCanvas != null && _midiDataManager != null)
                    {
                        _pianoRollRenderer.Render(pianoRollCanvas, _midiDataManager.Notes, _backgroundColor, _normalNoteColor);
                    }
                    Debug.WriteLine($"HexTextBox_TextChanged: Normal Note color updated to {newColor}");
                }
                else if (textBox == playingNoteColorHexTextBox) // XAML の名前に合わせる
                {
                    _playingNoteColor = newBrush;
                    // 再生ノート色はイベントハンドラーで適用されるため、ここでは再描画不要
                    Debug.WriteLine($"HexTextBox_TextChanged: Playing Note color updated to {newColor}");
                }
            }
            else
            {
                // 解析に失敗した場合 (無効な Hex 文字列など)
                // 色は更新しない (前の有効な色のままにする)
                // 必要に応じて、TextBox の背景色を赤くするなど、無効な入力を示す UI を追加することもできます。
                Debug.WriteLine($"HexTextBox_TextChanged: Invalid Hex format for {textBox.Name}: {textBox.Text}");
            }
        }
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

    }
}