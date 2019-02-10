# LAB2VMD

## 用途
CeVIO Creative Studio (CeVIO) で書き出した「連続WAV」を Miku Miku Dance (MMD) のリップシンクに使う。  

## 必要なもの
- Microsoft Windows 10 64bit
- Microsoft Visual Studio Community 2017
- CeVIO Creative Studio 6 製品版 [公式サイト](http://cevio.jp/others/CCS/)
- 任意のVMDファイル (.vmd)

## 使い方
- LAB2VMD.sln を Visual Studio 2017 でビルドする。
- CeVIO で適当にトークを作成し、エクスポートから「セリフの連続WAV書き出し」で .lab , .txt , .wav を作成する。(「セリフをテキスト出力」と「タイミング情報をテキスト出力」に必ずチェックを入れておくこと。)
)
- LAB2VMD でこのフォルダを指定し、.lab , .txt を読み込む。
- 読み込んだ音素情報が表示される。
- 適当な .vmd を用意する。
- VMD更新により .vmd を更新する。必要に応じて連結 .wav も作成する。
- Face And Lips などのソフトウェアでタイミングを修正したり、数値を調整する。
- MMD などで .vmd と .wav を読み込み、リップシンクが容易に実現できる。

## 補足
- リップシンクの挙動は超適当なので注意。Face And Lips などで編集しないと見栄えは悪い。あくまでも目安としての位置づけ。
- ランダム (8～12秒) でまばたきモーションも入る。(強制上書き)
- 本ソフトウェアは ＤＸライブラリ を使用している。ライセンスなどは DxLib.txt を参照のこと。