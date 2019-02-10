# LAB2CSVMD

## Overview
CeVIO Creative Studio (CeVIO)で書き出したLABファイルからMiku Miku Dance (MMD)用リップシンクデータを作成。  
![Image](https://elftune.github.io/app/lab2csvmd/image.png)

## Requirements
- Microsoft Windows 10 64bit
- Microsoft Visual Studio Community 2017
- CeVIO Creative Studio 6 製品版 [公式サイト](http://cevio.jp/others/CCS/)
- 任意のVMDファイル

## Usage
- LAB2CSVMD.slnをVisual Studioでビルドする。
- CeVIOでトークやソングを作成し、LABファイルとWAVファイル(トークの場合はTXTファイルも)をエクスポートする。
- LAB2CSVMD でLAB、TXTを読み込む。
- 用意したVMDファイルを読み込み別名で保存する。トークの場合は必要に応じて連結WAVも作成する。

## Notes
- リップシンクの挙動は超適当。あくまでも目安としての位置づけ。Face And Lips 等での調整推奨。
- ランダム (8～12秒) でまばたきモーションも入る。(強制上書き)
- 本ソフトウェアは ＤＸライブラリ を使用している。ライセンスなどは DxLib.txt を参照のこと。
- [Wiki](https://github.com/elftune/LAB2CSVMD/wiki)

## Special Thanks to
- https://github.com/hangingman/wxMMDViewer/tree/master/libvmdconv
- https://blog.goo.ne.jp/torisu_tetosuki/e/bc9f1c4d597341b394bd02b64597499d
- https://harigane.at.webry.info/201103/article_1.html
