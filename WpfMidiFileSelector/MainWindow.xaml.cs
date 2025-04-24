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

        private SolidColorBrush _backgroundColor = new SolidColorBrush(Colors.Black); // デフォルトは黒
        private SolidColorBrush _normalNoteColor = new SolidColorBrush(Colors.DodgerBlue); // デフォルトは青
        private SolidColorBrush _playingNoteColor = new SolidColorBrush(Colors.Yellow); // デフォルトは黄色

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
                _pianoRollRenderer.Render(pianoRollCanvas, _midiDataManager.Notes, _backgroundColor, _normalNoteColor);
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