using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfMidiFileSelector
{
    internal class PlaybackManager : IDisposable
    {
        private Playback _playback;
        private OutputDevice _outputDevice;

        /// <summary>
        /// ノートの再生が開始されたときに発生します。
        /// イベント引数には、再生が開始されたノートのリストが含まれます。
        /// </summary>
        public event EventHandler<NotesEventArgs> NoteStarted;

        /// <summary>
        /// ノートの再生が終了したときに発生します。
        /// イベント引数には、再生が終了したノートのリストが含まれます。
        /// </summary>
        public event EventHandler<NotesEventArgs> NoteFinished;

        /// <summary>
        /// 再生が完了したときに発生します。
        /// </summary>
        public event EventHandler PlaybackCompleted;

        /// <summary>
        /// 使用する MIDI 出力デバイスを取得します。デバイスが見つからない場合は null です。
        /// </summary>
        public OutputDevice OutputDevice => _outputDevice;

        /// <summary>
        /// 再生が現在アクティブであるかどうかを示す値を取得します。
        /// </summary>
        public bool IsPlaying => _playback?.IsRunning ?? false;


        public PlaybackManager()
        {
            // コンストラクターでデバイスの初期化を行う
            InitializeOutputDevice();
        }

        /// <summary>
        /// システムの MIDI 出力デバイスを初期化します。
        /// </summary>
        private void InitializeOutputDevice()
        {
            try
            {
                // 利用可能な出力デバイスをすべて取得
                var outputDevices = OutputDevice.GetAll().ToList();
                Debug.WriteLine("PlaybackManager: 利用可能な MIDI 出力デバイス:");
                if (!outputDevices.Any()) // デバイスが見つからない場合
                {
                    Debug.WriteLine("PlaybackManager: 利用可能な MIDI 出力デバイスが見つかりません。");
                    _outputDevice = null; // デバイスがない場合は null のまま
                }
                else
                {
                    // リストの最初のデバイスを _outputDevice として使用
                    _outputDevice = outputDevices.First();
                    Debug.WriteLine($"PlaybackManager: 使用する MIDI 出力デバイス: {_outputDevice.Name}");

                    // 利用可能なデバイスをすべてデバッグ出力 (確認用)
                    foreach (var device in outputDevices)
                    {
                        Debug.WriteLine($"  - {device.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PlaybackManager: MIDI デバイスの初期化中にエラーが発生しました: {ex}");
                _outputDevice = null;

            }
        }

        /// <summary>
        /// 指定された MIDI ファイルの再生を開始します。
        /// </summary>
        /// <param name="midiFile">再生する MIDI ファイル。</param>
        public void StartPlayback(MidiFile midiFile)
        {
            // 既存の Playback オブジェクトがあれば一度破棄する
            _playback?.Dispose();
            _playback = null; // Dispose したら null にする

            // MIDI ファイルまたは出力デバイスがない場合は再生できない
            if (midiFile == null || _outputDevice == null)
            {
                Debug.WriteLine("PlaybackManager: 再生する MIDI ファイルまたは出力デバイスが利用できません。");
                return;
            }
            try
            {
                // 新しい Playback オブジェクトを作成
                _playback = midiFile.GetPlayback(_outputDevice);

                // ★ DryWetMidi のイベントハンドラーを登録 ★
                _playback.NotesPlaybackStarted += OnNotesPlaybackStarted;
                _playback.NotesPlaybackFinished += OnNotesPlaybackFinished;
                _playback.Finished += OnFinished;

                // 再生を開始
                _playback.Start();

                Debug.WriteLine("PlaybackManager: MIDI 再生を開始しました。");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PlaybackManager: MIDI 再生開始中にエラーが発生しました: {ex}");
                // エラー発生時は Playback オブジェクトを破棄
                _playback?.Dispose();
                _playback = null;
                // ここで再生失敗を外部に通知するイベントを定義するのも良いかもしれません。
            }

        }

        /// <summary>
        /// 現在再生中の MIDI を停止します。
        /// </summary>
        public void StopPlayback()
        {
            if (_playback != null && _playback.IsRunning)
            {
                _playback.Stop();
                Debug.WriteLine("PlaybackManager: MIDI 再生を停止しました。");
                // 停止した時も Finished イベントが発生する可能性がありますが、念のため明示的にイベントを発生させることも検討
                // OnFinished(_playback, EventArgs.Empty); // 必要であれば
            }
        }

        /// <summary>
        /// Playback オブジェクトおよび出力デバイスを破棄し、リソースを解放します。
        /// </summary>
        public void DisposePlayback()
        {
            Debug.WriteLine("PlaybackManager: リソースを破棄します。");
            // DryWetMidi のイベントハンドラーの登録を解除 (推奨されるクリーンアップ)
            if (_playback != null)
            {
                _playback.NotesPlaybackStarted -= OnNotesPlaybackStarted;
                _playback.NotesPlaybackFinished -= OnNotesPlaybackFinished;
                _playback.Finished -= OnFinished;
            }
            _playback?.Dispose();
            _playback = null;

            _outputDevice?.Dispose();
            _outputDevice = null;
        }


        public void Dispose()
        {
            DisposePlayback();
        }

        private void OnNotesPlaybackStarted(object sender , NotesEventArgs e)
        {
            Debug.WriteLine("PlaybackManager: DryWetMidi NotesPlaybackStarted Event Occurred.");
            // カスタムイベント NoteStarted を発生させ、イベント引数を渡す
            NoteStarted?.Invoke(this, e);
        }

        private void OnNotesPlaybackFinished(object sender, NotesEventArgs e)
        {
            Debug.WriteLine("PlaybackManager: DryWetMidi NotesPlaybackFinished Event Occurred.");
            // カスタムイベント NoteFinished を発生させ、イベント引数を渡す
            NoteFinished?.Invoke(this, e);
        }

        private void OnFinished(object sender, EventArgs e)
        {
            Debug.WriteLine("PlaybackManager: DryWetMidi Finished Event Occurred.");
            // Playback が Finished になったとき、UI スレッドでボタンの状態を更新するために
            // PlaybackCompleted イベントを MainWindow に通知します。
            PlaybackCompleted?.Invoke(this, EventArgs.Empty);

            // 再生が終了したら Playback オブジェクトを破棄することもここで担当できます。
            // _playback?.Dispose(); // StartPlayback で新しいものが作られる前に破棄するのでここでは不要かもしれません。
            // _playback = null;
        }
    }
}
