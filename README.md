# LAB2CSVMD

## Overview
CeVIO Creative Studio (CeVIO)で書き出したLABファイルからMiku Miku Dance (MMD)用リップシンクデータを作成。

## Requirements
- Microsoft Windows 10 64bit
- Microsoft Visual Studio Community 2017
- CeVIO Creative Studio 6 製品版 [公式サイト](http://cevio.jp/others/CCS/)
- 任意のVMDファイル

## Usage
- LAB2CSVMD.slnをVisual Studioでビルドする。
- CeVIOでトークやソングを作成し、LABファイルとWAVファイル(トークの場合はTXTファイルも)をエクスポートする。
- LAB2VMD でLAB、TXTを読み込む。
- 用意したVMDファイルを更新する。トークの場合は必要に応じて連結WAVも作成する。
- Face And Lips などのソフトウェアで調整するとよい。

## Notes
- リップシンクの挙動は超適当。あくまでも目安としての位置づけ。
- ランダム (8～12秒) でまばたきモーションも入る。(強制上書き)
- 本ソフトウェアは ＤＸライブラリ を使用している。ライセンスなどは DxLib.txt を参照のこと。

## Special Thanks to
- https://github.com/hangingman/wxMMDViewer/tree/master/libvmdconv
- https://blog.goo.ne.jp/torisu_tetosuki/e/bc9f1c4d597341b394bd02b64597499d
- https://harigane.at.webry.info/201103/article_1.html
