HoloFastDepth
=====

[dwofk/fast-depth](https://github.com/dwofk/fast-depth)[1] の学習済みモデルを利用し、HoloLens上でのデプスの推定と表示を行います。

## Demo

[![IMAGE ALT TEXT HERE](https://img.youtube.com/vi/JJo-ZF0Oc-Y/0.jpg)](https://www.youtube.com/watch?v=JJo-ZF0Oc-Y)

## Usage

* AirTapするとカメラから取得した画像を元に奥行きを推定、メッシュ化して表示します

## Requirement

* HoloLens (Windows 10 version 1809)
* Unity 2017.4
* Visual Studio 2017

## Installation

1. UWPのビルド
2. 作成された sln を開く
3. HoloFastDepth プロジェクトの "プロパティ" を開き、"アプリケーション" タブの "ターゲットバージョン" を "1809" に設定する。
4. `Assets/MLModel` を HoloFastDepth プロジェクトの `Assets` 内へコピー
5. `fastdepth_7.onnx` の "ビルドアクション" を "コンテンツ"に、"出力ディレクトリにコピー" を "常にコピーする" に変更
6. ターゲットを指定してビルド & デプロイ

## Authors

* @matsunoh
* @miso3

## License

[MIT](https://github.com/miso3/HoloFastDepth/blob/master/LICENSE)

## Reference

[1] Diana Wofk, Fangchang Ma, Tien-Ju Yang, Sertac Karaman, Vivienne Sze, "FastDepth: Fast Monocular Depth Estimation on Embedded Systems," IEEE International Conference on Robotics and Automation (ICRA), 2019.
