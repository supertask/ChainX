# ChainX
3Dのデータを使った共有アプリケーションです（工事中！！ 年内に完成予定）。

このアプリケーションは、ChainVoxelを使った3D共有のアプリケーションです。ChainVoxelは、3Dデータの合意アルゴリズムにおいて、2相コミットやRaftによる合意アルゴリズムよりも、速い速度で合意を取ることのできることが証明されています（[ChainVoxel-Simulator](http://github.com/kengo92i/ChainVoxel-Simulator)）。そして、このChainXは、ChainVoxelのデータ構造を使った初めてのアプリケーションです。

現在の段階では、ChainVoxelのデータ構造を可視化しています。
![screenshot](./images/ChainX_screenshot.gif)

## ChainVoxelの性能

以下の図は、操作のために必要なメッセージ数を示しています。このようにChainVoxelは従来の3D共有のためのアルゴリズムよりも早く操作を終えることができます。共同研究者のkengo92iによって、このデータ構造が従来のものよりも優位であることが証明されています。
![UnitX logo image](./Assets/img/graph_message_operation.png)


## 環境

- Game engine: Unity 5.5.2
- OS: MacOSX 10.10.5

## 実行方法
`./ChainVoxel-SimulatorX/Assets/Sceen/`ディレクトリの`Main.unity`を実行して、アプリケーションをスタートさせると起動します。

    $ cd ./ChainVoxel-SimulatorX/Assets/Sceen/
    $ open Main.unity








