using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Melanchall.DryWetMidi.Common; // Debug.WriteLine を使用するために追加

namespace WpfMidiFileSelector
{
    /// <summary>
    /// MIDI ファイルの読み込みとノート情報の管理を担当するクラスです。
    /// </summary>
    public class MidiDataManager
    {
        private MidiFile _midiFile;
        private List<Note> _notes;

        /// <summary>
        /// 読み込まれた MIDI ファイルを取得します。ファイルが読み込まれていない場合は null です。
        /// </summary>
        public MidiFile MidiFile => _midiFile;

        /// <summary>
        /// 読み込まれた MIDI ファイルから抽出されたノートのリストを取得します。
        /// ファイルが読み込まれていない場合やノートがない場合は null または空のリストです。
        /// </summary>
        public List<Note> Notes => _notes;

        /// <summary>
        /// 指定されたパスから MIDI ファイルを読み込み、ノートを抽出します。
        /// 読み込みが成功した場合、MidiFile および Notes プロパティが更新されます。
        /// </summary>
        /// <param name="filePath">MIDI ファイルのパス。</param>
        /// <returns>読み込みと抽出が成功した場合は true、それ以外の場合は false。</returns>
        public bool LoadFile(string filePath)
        {
            // 新しいファイルを読み込む前に、前のデータをクリアします。
            ClearData();

            if (string.IsNullOrEmpty(filePath))
            {
                Debug.WriteLine("MidiDataManager: ファイルパスが指定されていません。");
                return false; // ファイルパスが無効
            }

            try
            {
                // DryWetMidi でファイルを読み込み
                _midiFile = MidiFile.Read(filePath);
                // ノート情報を抽出
                _notes = _midiFile.GetNotes().ToList();

                Debug.WriteLine($"MidiDataManager: ファイル '{Path.GetFileName(filePath)}' を正常に読み込みました。");
                Debug.WriteLine($"MidiDataManager: タイムフォーマット: {_midiFile.TimeDivision}");
                Debug.WriteLine($"MidiDataManager: トラック数: {_midiFile.Chunks.OfType<TrackChunk>().Count()}");
                Debug.WriteLine($"MidiDataManager: 含まれるノート数: {_notes.Count()}");


                return true; // 成功
            }
            catch (MidiException ex)
            {
                // MIDI ファイル固有のエラーを捕捉
                Debug.WriteLine($"MidiDataManager: MIDIファイルの読み込みエラー: {ex.Message}");
                // エラー情報はログに出力し、false を返すことで呼び出し元に失敗を伝えます。
                return false;
            }
            catch (FileNotFoundException ex)
            {
                // ファイルが見つからないエラーを捕捉
                Debug.WriteLine($"MidiDataManager: ファイルが見つかりませんエラー: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // その他の予期しないエラーを捕捉
                Debug.WriteLine($"MidiDataManager: 予期しないエラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 現在読み込まれている MIDI データとノート情報をクリアします。
        /// </summary>
        public void ClearData()
        {
            _midiFile = null;
            _notes = null;
            Debug.WriteLine("MidiDataManager: データがクリアされました。");
        }

        // 必要に応じて、読み込んだ MIDI ファイルに関する他の情報を取得するメソッドを追加することも検討できます。
        // 例: public int GetTrackCount() => _midiFile?.Chunks.OfType<TrackChunk>().Count() ?? 0;
        // 例: public TimeDivision GetTimeDivision() => _midiFile?.TimeDivision;
    }
}