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

        private ColorSettingsManager _colorSettingsManager;

        public MainWindow()
        {

            _midiDataManager = new MidiDataManager();
            _pianoRollRenderer = new PianoRollRenderer();
            _playbackManager = new PlaybackManager();
            _colorSettingsManager = new ColorSettingsManager();

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

            backgroundColorHexTextBox.Text = _colorSettingsManager.BackgroundColorBrush.Color.ToString();
            normalNoteColorHexTextBox.Text = _colorSettingsManager.NormalNoteColorBrush.Color.ToString();
            playingNoteColorHexTextBox.Text = _colorSettingsManager.PlayingColorBrush.Color.ToString();

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
            this.Height = 840; // ウィンドウの高さを変更
            this.Width = 1490; // ウィンドウの幅を変更

            fileSelectDividerTop.Visibility = Visibility.Visible;
            fileSelectDividerBottom.Visibility = Visibility.Visible;
            colorSelection.Visibility = Visibility.Visible;
            playButton.Visibility = Visibility.Visible;
            stopButton.Visibility = Visibility.Visible;
            _pianoRollRenderer.Render(pianoRollCanvas, _midiDataManager.Notes, _colorSettingsManager.BackgroundColorBrush, _colorSettingsManager.NormalNoteColorBrush);


        }


        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {

            MidiFile fileToPlay = _midiDataManager.MidiFile;

            if (fileToPlay == null || _playbackManager.OutputDevice == null)
            {
                MessageBox.Show("再生する MIDI ファイルが読み込まれていないか、出力デバイスが利用できません。", "再生エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _pianoRollRenderer.ResetAllNoteColors(_colorSettingsManager.NormalNoteColorBrush);

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

            _pianoRollRenderer.ResetAllNoteColors(_colorSettingsManager.NormalNoteColorBrush);
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
                    _pianoRollRenderer.SetNoteColor(note, _colorSettingsManager.PlayingColorBrush);
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
                    _pianoRollRenderer.SetNoteColor(note, _colorSettingsManager.NormalNoteColorBrush);
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

            // どの ComboBox がイベントを発生させたかを判定
            if (comboBox == backgroundColorComboBox)
            {
                hexTextBox = backgroundColorHexTextBox;
                _colorSettingsManager.ApplyBackgroundColorSetting(selectedOption, hexTextBox.Text);
                // UI 更新はメソッド呼び出し後に行います。
            }
            else if (comboBox == normalNoteColorComboBox)
            {
                hexTextBox = normalNoteColorHexTextBox;
                // ★ ColorSettingsManager のメソッドを呼び出して通常ノート色の設定を適用 ★
                _colorSettingsManager.ApplyNormalNoteColorSetting(selectedOption, hexTextBox.Text);
                // UI 更新はメソッド呼び出し後に行います。
            }
            else if (comboBox == playingNoteColorComboBox) // XAML の Name と合わせる
            {
                hexTextBox = playingNoteColorHexTextBox;
                // ★ ColorSettingsManager のメソッドを呼び出して再生ノート色の設定を適用 ★
                _colorSettingsManager.ApplyPlayingColorSetting(selectedOption, hexTextBox.Text); // PlayingColorSetting に合わせる
                // UI 更新はメソッド呼び出し後に行います。
            }
            else
            {
                Debug.WriteLine($"ColorComboBox_SelectionChanged: Unknown ComboBox {comboBox.Name}");
                return;
            }
            // Show/Hide the Hex TextBox based on the selected option (変更なし)
            bool isHexSelected = selectedOption == ColorOptionNames.CustomHex;
            hexTextBox.Visibility = isHexSelected ? Visibility.Visible : Visibility.Collapsed;

            // ★ ColorSettingsManager から更新された Brush を取得し、UI に反映 ★
            // Apply...Setting メソッドの中で Brush は更新されています。
            if (comboBox == backgroundColorComboBox)
            {
                if (pianoRollCanvas != null)
                {
                    pianoRollCanvas.Background = _colorSettingsManager.BackgroundColorBrush;
                }
                Debug.WriteLine($"ColorComboBox_SelectionChanged: Background color updated via manager.");
            }
            else if (comboBox == normalNoteColorComboBox)
            {
                // 通常ノート色が変わったので、画面を再描画して反映
                // null チェックを追加 (_midiDataManager や pianoRollCanvas が初期化されているか)
                if (pianoRollCanvas != null && _midiDataManager != null)
                {
                    // Render メソッドには背景色と通常ノート色の両方を渡す必要があります。
                    _pianoRollRenderer.Render(pianoRollCanvas, _midiDataManager.Notes, _colorSettingsManager.BackgroundColorBrush, _colorSettingsManager.NormalNoteColorBrush);
                }
                Debug.WriteLine($"ColorComboBox_SelectionChanged: Normal Note color updated via manager, rendering.");
            }
            else if (comboBox == playingNoteColorComboBox)
            {
                // 再生ノート色はイベントハンドラーで適用されるため、ここでは再描画不要
                // ColorSettingsManager の PlayingColorBrush プロパティは PlaybackManager_NoteStarted/Finished ハンドラーで参照されます。
                Debug.WriteLine($"ColorComboBox_SelectionChanged: Playing Note color updated via manager.");
            }

            Debug.WriteLine($"Color changed for {comboBox.Name} to option '{selectedOption}'.");

            // Note: TextChanged ハンドラーも同じ解析を行うため、二重処理になりますが、
            // ComboBox 変更時の即時反映のためにこのロジックを含めています。
        }

        // ★ HexTextBox_TextChanged() メソッドの修正 ★
        // 色の決定ロジックを ColorSettingsManager に委譲し、更新された Brush を使用して UI に反映します。
        private void HexTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // どの TextBox がイベントを発生させたか判定し、対応する ComboBox を特定
            ComboBox correspondingComboBox = null;


            if (textBox == backgroundColorHexTextBox)
            {
                correspondingComboBox = backgroundColorComboBox;
            }
            else if (textBox == normalNoteColorHexTextBox)
            {
                correspondingComboBox = normalNoteColorComboBox;
            }
            else if (textBox == playingNoteColorHexTextBox)
            {
                correspondingComboBox = playingNoteColorComboBox;
            }
            else
            {
                Debug.WriteLine($"HexTextBox_TextChanged: Unknown TextBox {textBox.Name}");
                return;
            }

            // 対応する ComboBox の選択が "色コードで指定" でない場合は処理しない (変更なし)
            if (correspondingComboBox.SelectedItem as string != ColorOptionNames.CustomHex)
            {
                Debug.WriteLine($"HexTextBox_TextChanged: ComboBox selection is not '{ColorOptionNames.CustomHex}'. Skipping update for {textBox.Name}.");
                return; // ComboBox で Hex 指定が選択されていない場合は、TextBox の変更は無視
            }

            // ★ ColorSettingsManager のメソッドを呼び出して設定を適用 ★
            // Apply...Setting メソッドの中で Hex 値の解析と Brush の更新が行われます。
            if (textBox == backgroundColorHexTextBox)
            {
                _colorSettingsManager.ApplyBackgroundColorSetting(ColorOptionNames.CustomHex, textBox.Text);
                // UI 更新はメソッド呼び出し後に行います。
            }
            else if (textBox == normalNoteColorHexTextBox)
            {
                _colorSettingsManager.ApplyNormalNoteColorSetting(ColorOptionNames.CustomHex, textBox.Text);
                // UI 更新はメソッド呼び出し後に行います。
            }
            else if (textBox == playingNoteColorHexTextBox)
            {
                _colorSettingsManager.ApplyPlayingColorSetting(ColorOptionNames.CustomHex, textBox.Text); // PlayingColorSetting に合わせる
                // UI 更新はメソッド呼び出し後に行います。
            }

            // ★ ColorSettingsManager から更新された Brush を取得し、UI に反映 ★
            // Apply...Setting メソッドの中で Brush は更新されています。
            if (textBox == backgroundColorHexTextBox)
            {
                if (pianoRollCanvas != null)
                {
                    pianoRollCanvas.Background = _colorSettingsManager.BackgroundColorBrush;
                }
                Debug.WriteLine($"HexTextBox_TextChanged: Background color updated via manager.");
            }
            else if (textBox == normalNoteColorHexTextBox)
            {
                // 通常ノート色なのでピアノロールを再描画して反映
                if (pianoRollCanvas != null && _midiDataManager != null)
                {
                    // Render メソッドには背景色と通常ノート色の両方を渡す必要があります。
                    _pianoRollRenderer.Render(pianoRollCanvas, _midiDataManager.Notes, _colorSettingsManager.BackgroundColorBrush, _colorSettingsManager.NormalNoteColorBrush);
                }
                Debug.WriteLine($"HexTextBox_TextChanged: Normal Note color updated via manager, rendering.");
            }
            else if (textBox == playingNoteColorHexTextBox)
            {
                // 再生ノート色はイベントハンドラーで適用されるため、ここでは再描画不要
                // ColorSettingsManager の PlayingColorBrush プロパティは PlaybackManager_NoteStarted/Finished ハンドラーで参照されます。
                Debug.WriteLine($"HexTextBox_TextChanged: Playing Note color updated via manager.");
            }

            // Debug.WriteLine($"Hex value changed for {textBox.Name}. Color updated."); // より詳細なログは Apply メソッド内で出力されています。
        }

        // ★ TryConvertHexToColor() ヘルパーメソッドは ColorSettingsManager に移動します ★
        // private bool TryConvertHexToColor(string hexString, out Color color) { ... } // このメソッド定義は削除します


    }
}