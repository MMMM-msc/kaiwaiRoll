# kaiwaiRoll

MIDIファイルの内容をピアノロール形式で視覚的に表示し、再生できるシンプルなツールです。

## 機能

* 標準MIDIファイル（.mid, .midi）の読み込みと表示
* MIDIデータのピアノロール形式での表示（横：時間、縦：音の高さ）
* MIDI再生機能。たいていの場合はWindows標準のMIDI出力デバイスで再生されると思います。
* 再生中の音符がリアルタイムでハイライト表示されます。
* ピアノロールの表示色のカスタマイズ機能（背景色、再生していないノート色、再生中のノート色）
    * 定義済みのプリセット色からの選択
    * 16進数カラーコード（例: `#FF0000`）での任意の色指定が可能。透過情報アリの16進数（例: `#80FF0000`）でも指定できます。

## 目的

界隈曲でよく見る、色が変わるピアノロール動画を簡単に得るために作りました。
ですので、音が正確に鳴るかどうかはおまけ程度としてお使いください。

## 使い方

1.  ツールを起動します。
2.  画面上部にある「**ファイルを選択して表示**」ボタンをクリックし、表示したい標準MIDIファイル（.mid または .midi 形式）を選択して開きます。
3.  中央の領域にMIDIデータのピアノロールが表示されます。
4.  「**表示色の設定**」エリアで、背景色、再生していないノート色（通常ノート）、再生中のノート色（再生ノート）をそれぞれお好みに合わせて変更できます。プリセット選択またはHex（16進数）入力で指定できます。
5.  「**再生**」ボタンをクリックすると、MIDIの再生が開始されます。再生中は、ピアノロール上の音符の色が変化します。
6.  再生を停止したい場合は、「**停止**」ボタンをクリックしてください。

## 動作環境

* OS: Windows
* その他: MIDI音を鳴らすためには、Windows上でMIDI出力デバイスが利用可能な状態である必要があります。（例: Windows 標準の Microsoft GS Wavetable Synth など）

## ダウンロード

最新バージョンは以下の GitHub Releases ページからダウンロードできます。

[**kaiwaiRoll Releases ページ**](https://github.com/MMM/kaiwaiRoll/releases)

上記ページにアクセスし、最新リリースの項目から添付されている ZIP ファイル（例: `kaiwaiRoll_VerX.X.X.zip`）をダウンロードしてください。ファイルを展開すると、実行ファイル（.exe）と、ツールの実行に必要なファイルが含まれています。

## tips

* 細かく（短いフレーズで）出力することを前提として制作しました（例えば、2小節～最大でも8小節程度を想定しています）。そのため、長大なMIDIファイルの表示や、自動スクロール機能は実装していません。
* ピアノロールの表示サイズや位置の細かい調整は、このツールで表示・保存した画像データを、外部の画像編集ソフト側で加工することを想定しています。
* 色設定の16進数入力では、`#RRGGBB` 形式に加えて、透過情報を含む `#AARRGGBB` 形式も利用可能です。

## ライセンス

このツール本体は、[MIT License] で公開されています。

本ツールは以下のオープンソースライブラリを利用しています。これらのライブラリも MIT ライセンスに基づいています。

* **DryWetMidi**: MIDIファイルの読み込み・再生機能を提供します。
    * リポジトリ: [https://github.com/melanchall/drywetmidi](https://github.com/melanchall/drywetmidi)
    * ライセンス: [MIT License](https://github.com/melanchall/drywetmidi/blob/master/LICENSE)
* **MaterialDesignThemes**: アプリケーションのUIデザインに利用しています。
    * リポジトリ: [https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
    * ライセンス: [MIT License](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/blob/dev/LICENSE)

各ライブラリのライセンス全文は、ダウンロードしたツールの配布物に含まれているライセンスファイルをご確認ください。

## 貢献

バグの発見や、コードの改善提案など、このプロジェクトに貢献していただける場合は歓迎いたします。以下の方法でご協力いただけます。

* バグ報告や機能提案: [GitHub Issues](https://github.com/MMM/kaiwaiRoll/issues) に新しい Issue を作成してください。
* コードの修正・追加: リポジトリをフォークし、変更を加えて Pull Request を作成してください。

## お問い合わせ・フィードバック

ツールの使用に関するご質問、バグ報告、ご意見、ご要望などがございましたら、以下の方法でご連絡ください。

* GitHub Issues: [https://github.com/MMM/kaiwaiRoll/issues](https://github.com/MMM/kaiwaiRoll/issues)
* X (旧Twitter): [https://x.com/play_matitan](https://x.com/play_matitan)

---
