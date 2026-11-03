# RuneManager

League of Legends の LCU API で、現在のカスタムルーンページを上書きする軽量 Windows アプリです。

Electron や WebView は使わず、.NET WinForms のネイティブ UI だけにしています。接続は 2.5 秒間隔の lockfile 確認のみで、常駐 WebSocket は持ちません。

## 必要環境

- Windows x64
- .NET 10 SDK
- 起動中の League クライアント（ログイン済み）

## ビルド

```powershell
dotnet build .\src\RuneManager\RuneManager.csproj -c Release
dotnet run --project .\src\RuneManager\RuneManager.csproj -c Release
```

単一 exe にする場合:

```powershell
dotnet publish .\src\RuneManager\RuneManager.csproj -c Release -r win-x64 --self-contained false
```

framework-dependent のままにしておくと、ランタイムを共有できてプロセスのメモリが小さくなります。

## 使い方

1. League クライアントを起動してログインする
2. RuneManager を起動する（自動で `LeagueClientUx` の lockfile を探す）
3. 必要ならプリセットを選ぶか、右側でルーンを組む
4. **現在のページを上書き** を押す

上書き対象は「現在のページ」が編集可能なときそのページ、そうでなければ最初の編集可能ページです。公式の既定ページは書き換えられないので、クライアント側でカスタムページを 1 つ作っておいてください。

プリセットは `%AppData%\RuneManager\presets\` に JSON で保存されます。`examples/` のファイルをそこにコピーしても使えます。

## 構成

```
RuneManager/
  RuneManager.sln
  examples/
  src/RuneManager/
    Program.cs
    Lcu/          lockfile 検出と HTTPS クライアント
    Models/       LCU / プリセットの DTO
    Runes/        カタログ組み立てとプリセット保存
    Ui/           WinForms
```

LCU は自己署名証明書なので、localhost 向けに証明書検証だけ無効化しています。認証は lockfile の `riot:<password>` です。
