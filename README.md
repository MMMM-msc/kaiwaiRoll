# [kaiwaiRoll]

MIDIファイルの内容をピアノロール形式で視覚的に表示し、再生できるシンプルなツールです。

## 機能

* 標準MIDIファイル（.mid, .midi）の読み込みと表示
* MIDIデータのピアノロール形式での表示（横：時間、縦：音の高さ）
* MIDI再生機能。たいていの場合はwindows標準のMIDI出力デバイスで再生されると思います。
* 再生中の音符がリアルタイムでハイライト表示される。
* ピアノロールの表示色のカスタマイズ機能（背景色、再生していないノート色、再生中のノート色）
    * 定義済みのプリセット色からの選択
    * 16進数カラーコード（例: `#FF0000`）での任意の色指定が可能。
    * 透過情報アリの16進数でも指定できます
 

## 目的
界隈曲でよく見る、色が変わるピアノロールを簡単に得るために作りました。
ですので音が鳴るのはおまけ程度として使ってください。

## 使い方

1.  ツールを起動します。
2.  画面上部にある「**ファイルを選択して表示**」ボタンをクリックし、表示したい標準MIDIファイル（.mid または .midi 形式）を選択して開きます。
3.  中央の領域にMIDIデータのピアノロールが表示されます。
4.  「**表示色の設定**」エリアで、背景色、再生していないノート色（通常ノート）、再生中のノート色（再生ノート）をそれぞれお好みに合わせて変更できます。
5.  「**再生**」ボタンをクリックすると、MIDIの再生が開始されます。再生中は、ピアノロール上の音符の色が変化します。
6.  再生を停止したい場合は、「**停止**」ボタンをクリックしてください。

## 動作環境

* OS: Windows
* その他: MIDIの音を鳴らすためには、Windows上でMIDI出力デバイスが利用可能な状態である必要があります。（例: Microsoft GS Wavetable Synth など）

## ダウンロード

最新バージョンは以下の GitHub Releases ページからダウンロードできます。

[**[kaiwaiRoll] Releases ページ**](https://github.com/MMM/kaiwaiRoll/releases)

上記ページにアクセスし、最新リリースの項目から添付されている ZIP ファイル（`kaiwaiRoll_VerX.X.zip`）をダウンロードしてください。ファイルを展開すると、実行ファイル（.exe）と、ツールの実行に必要なファイルが含まれています。

## tips
* 細かく出力することを前提として制作しました(2小節～最大でも8小節ぐらいを想定)。ですので自動スクロール機能は実装してません
* 細かいサイズの調整は編集ソフト側ですることを想定しています。

## ライセンス

このツール自体は [MIT License] で公開されています。

本ツールは以下のオープンソースライブラリを利用しています。これらのライブラリは MIT ライセンスに基づいています。

* **DryWetMidi**: MIDIファイルの読み込み・再生機能を提供します。
    * リポジトリ: [https://github.com/melanchall/drywetmidi](https://github.com/melanchall/drywetmidi)
    * ライセンス: [MIT License](https://github.com/melanchall/drywetmidi/blob/master/LICENSE)
* **MaterialDesignThemes**: アプリケーションのUIデザインに利用しています。
    * リポジトリ: [https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
    * ライセンス: [MIT License](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/blob/dev/LICENSE)

各ライブラリのライセンス全文は、ダウンロードしたツールの配布物に含まれているライセンスファイル（`LICENSE_DryWetMidi.txt`, `LICENSE_MaterialDesignThemes.txt`）をご確認ください。

## お問い合わせ・フィードバック

ツールの使用に関するご質問、バグ報告、ご意見、ご要望などがございましたら、以下の方法でご連絡ください。

* GitHub Issues: [https://github.com/MMM/kaiwaiRoll/issues](https://github.com/MMM/kaiwaiRoll/issues)
* X https://x.com/play_matitan

---
