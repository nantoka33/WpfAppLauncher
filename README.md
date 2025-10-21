## 📦 WpfAppLauncher

.exe、.bat、.lnk をグループ登録して一括起動・管理できるアプリケーションです。  
よく使うツールやスクリプトをグループごとに整理し、クリックひとつでまとめて起動できます。  
タスクバーに常駐し、日々の作業を効率化します。

---

This application allows you to register and group `.exe`, `.bat`, and `.lnk` files  
and launch them all at once.  
Organize your favorite tools or scripts into groups and launch them with a single click.  
Resides in the system tray for quick access and productivity.


アプリケーション：release/WpfAppLauncher.exe

![image](https://github.com/user-attachments/assets/ef17a57c-c0fa-425a-a8a9-3ffa9320eea2)

## ⚙️ 設定

* 共通設定は `WpfAppLauncher/appsettings.json` に保存されます。アプリケーションデータの保存先、許可される拡張子、テーマボタンなどを変更できます。
* ビルド構成ごとの設定は `appsettings.Development.json`（Debug）と `appsettings.Production.json`（Release）で上書きできます。既定では開発環境のアプリデータ保存先のみ分離しています。
* 追加で安全な値を渡したい場合は、環境変数 `WPFAPPLAUNCHER_` プレフィックスを付けて設定できます（例：`WPFAPPLAUNCHER_AppData__ApplicationDirectoryName`）。`DOTNET_ENVIRONMENT` または `ASPNETCORE_ENVIRONMENT` を指定すると読み込まれる環境設定ファイルを切り替えられます。

## 🧩 拡張機能

* アプリ起動時に `Extensions` フォルダー（`appsettings.json` の `Extensions:DirectoryName` で変更可能）以下の DLL を自動検出し、`*.Extension.dll` という命名規約に合致するアセンブリから拡張機能をロードします。
* 拡張機能のエントリーポイントは `IAppExtension` を実装し、`[ExtensionMetadata("id", "表示名", "1.0.0")]` 属性でメタデータを宣言してください。属性の `Description` プロパティを利用すると UI に概要が表示されます。
* 拡張機能ごとのデータは `%AppData%/WpfAppLauncher/Extensions/<拡張機能ID>`（`Extensions:DataDirectoryName`）以下に保存されます。初期化時に `ExtensionInitializationContext` からロガーや構成情報へアクセスできます。
* 有効・無効の状態は `Extensions:StateFileName`（既定: `extensions.json`）に保持され、アプリの「拡張機能...」ボタンから UI で切り替えられます。構成ファイル側 (`Extensions:Disabled`) で明示的に無効化したプラグインは UI から変更できません。
* 拡張 DLL の更新や追加・削除を検出すると自動で再読み込みします。必要に応じて管理画面の「再読み込み」ボタンから手動で反映させることもできます。
