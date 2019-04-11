HoloFastDepth
=====

[dwofk/fast-depth](https://github.com/dwofk/fast-depth)の学習済みモデルを利用し、HoloLens上でのデプスの推定と表示を行います。

## ビルド方法

1. UWPのビルド
2. 作成された sln を開く
3. HoloFastDepth プロジェクトの "プロパティ" を開き、"アプリケーション" タブの "ターゲットバージョン" を "1809" に設定する。
4. `Assets/MLModel` を HoloFastDepth プロジェクトの `Assets` 内へコピー
5. `fastdepth_7.onnx` の "ビルドアクション" を "コンテンツ"に、"出力ディレクトリにコピー" を "常にコピーする" に変更
6. ターゲットを指定してビルド

## 参考情報

* [HoloLens photo capture - Unity マニュアル](https://docs.unity3d.com/ja/current/Manual/windowsholographic-photocapture.html)
* [Locatable camera - Mixed Reality | Microsoft Docs](https://docs.microsoft.com/ja-jp/windows/mixed-reality/locatable-camera)

## Authors

* @matsunoh
* @miso3

## License

[MIT](https://github.com/miso3/HoloFastDepth/blob/master/LICENSE)