# LAB2CSVMD

## 用途
CeVIO Creative Studio (CeVIO)で書き出したLABファイルからMiku Miku Dance (MMD)用リップシンクデータを作成。

## 必要なもの
- Microsoft Windows 10 64bit
- Microsoft Visual Studio Community 2017
- CeVIO Creative Studio 6 製品版 [公式サイト](http://cevio.jp/others/CCS/)
- 任意のVMDファイル

## 使い方
- LAB2CSVMD.slnをVisual Studioでビルドする。
- CeVIOでトークやソングを作成し、LABファイルとWAVファイル(トークの場合はTXTファイルも)をエクスポートする。
- LAB2VMD でLAB、TXTを読み込む。
- 用意したVMDファイルを更新する。トークの場合は必要に応じて連結WAVも作成する。
- Face And Lips などのソフトウェアで調整するとよい。

## 補足
- リップシンクの挙動は超適当。あくまでも目安としての位置づけ。
- ランダム (8～12秒) でまばたきモーションも入る。(強制上書き)
- 本ソフトウェアは ＤＸライブラリ を使用している。ライセンスなどは DxLib.txt を参照のこと。
