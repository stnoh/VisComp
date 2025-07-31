# VisComp

- ビジュアルコンピューティング特論の雛形 (2022-2024, @東京工科大学)  
- 基本的に，Unity 2019.3.15f1 で開発を持続しているものの，上位バージョンでも動作は可能と思われる．  
- コード自体は使用環境に無関係に動くように作っているものの，C#版おのOpenCVの問題により実際は Windows でしか動かない．  
  Windows以外の環境で使用する場合は，既に入っている OpenCVSharp を削除した上，[OpenCV plus Unity](https://assetstore.unity.com/packages/tools/integration/opencv-plus-unity-85928) などを導入すること．  

## 使用ライブラリ

以下のようなものが既にライブラリとして含まれている．  

- [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity/releases/tag/v3.1.3)
- OpenCVSharp (commit b1d0ac, get from NuGet) 
  + System.Drawing.Common.5.0.0
- [PDFSharp](http://www.pdfsharp.net/)
