**[English version here](README.md)**

# STYLY NetSync Utility

STYLY NetSync (`com.styly.styly-netsync`) のコア機能をラップし、**型安全**かつ**リアクティブ**なインターフェースを提供するユーティリティパッケージです。

## 利用メリット

- **Enum ベースの型安全な定義** — RPC や同期変数を Enum で宣言するだけで、文字列の打ち間違いによるバグを防止できます。
- **R3 Observable 対応** — `AsObservable()` で変数やRPCをリアクティブストリームとして購読でき、LINQ 演算子によるフィルタ・変換・合成が可能です。
- **UnityEvent との併用** — `AddListener()` による従来型コールバックもサポート。既存コードとの統合が容易です。
- **ボイラープレートの削減** — NetSyncManager の Ready 待ち、文字列⇔型変換、クライアント番号のフィルタリングなど、定型処理を内部で処理します。
- **Self API** — ユーザー変数の操作で自分のクライアント番号を自動解決する `SetSelf` / `GetSelf` / `AsObservableSelf` を提供し、コードを簡潔に保てます。

## パッケージ情報

| 項目 | 値 |
|------|-----|
| パッケージ名 | `com.styly.styly-netsync.utility` |
| バージョン | 0.4.2 |
| 対応 Unity | 6000.0 以上 |
| 名前空間 | `Styly.NetSync.Utility` |

### 依存パッケージ

| パッケージ | バージョン | 用途 |
|-----------|-----------|------|
| `com.styly.styly-netsync` | 0.10.2 | コアのネットワーク同期機能 |
| `com.cysharp.r3` | 1.3.0 | Reactive Extensions (Observable) |
| `com.unity.xr.management` | 4.5.2 | XR ローダー管理（`XRLoaderAutoConfigurator` で使用） |
| `com.cysharp.unitask` | 2.5.10 | async/await 非同期処理 |

## 基本的な使い方

### EventManager — Ready 状態の待機

`EventManager` は NetSyncManager の準備完了を待機するシングルトンです。

```csharp
// async/await パターン
await EventManager.Instance.WaitForReadyAsync(destroyCancellationToken);

// Observable パターン
EventManager.Instance.OnReadyAsObservable()
    .Subscribe(_ => Debug.Log("Ready!"))
    .AddTo(this);
```

### RpcManagerBase — RPC の送受信

Enum で RPC を定義し、サブクラスを作るだけで型安全な RPC 管理が使えます。

```csharp
// 1. RPC を Enum で定義
public enum GameRpc { Ready, StartRound, Attack }

// 2. サブクラスを作成して GameObject にアタッチ
public class GameRpcManager : RpcManagerBase<GameRpc> { }
```

**送信:**

```csharp
// 全クライアントに送信
rpcManager.Send(GameRpc.Attack, new[] { "fireball", "50" });

// 特定のクライアントに送信
rpcManager.Send(GameRpc.Attack, new[] { "heal", "30" }, targetClientNo: 2);

// 複数の特定クライアントに送信
rpcManager.Send(GameRpc.Attack, new[] { "aoe" }, new[] { 1, 3, 5 });
```

**受信 (Observable):**

```csharp
rpcManager.AsObservable(GameRpc.Attack)
    .Subscribe(data =>
    {
        int sender = data.ClientNo;
        string skill = data.Parameters[0];
        string damage = data.Parameters[1];
        Debug.Log($"Client {sender}: {skill} ({damage} dmg)");
    })
    .AddTo(this);
```

**受信 (UnityEvent):**

```csharp
rpcManager.AddListener(GameRpc.Attack, (clientNo, parameters) =>
{
    Debug.Log($"Client {clientNo} attacked!");
});
```

### VariableManagerBase — 同期変数の管理

グローバル変数 (全クライアント共有) とユーザー変数 (クライアント別) を 2 つの Enum で定義します。

```csharp
// 1. 変数を Enum で定義
public enum GlobalVar { GamePhase, RoundNumber }
public enum UserVar   { Health, Score, IsAlive }

// 2. サブクラスを作成して GameObject にアタッチ
public class GameVarManager : VariableManagerBase<GlobalVar, UserVar> { }
```

**グローバル変数の読み書き:**

```csharp
// 設定（型変換は自動）
vars.Set(GlobalVar.RoundNumber, 3);

// 取得（デフォルト値付き）
int round = vars.Get<int>(GlobalVar.RoundNumber, 1);

// 購読（初期値 + 変更通知）
vars.AsObservable<int>(GlobalVar.RoundNumber)
    .Subscribe(n => roundText.text = $"Round {n}")
    .AddTo(this);

// 変更のみ購読（初期値なし）
vars.AsObservableOnChanged(GlobalVar.GamePhase)
    .Subscribe(phase => Debug.Log($"Phase changed: {phase}"))
    .AddTo(this);
```

**ユーザー変数の読み書き (Self — 自分自身):**

```csharp
// 自分の値を設定・取得
vars.SetSelf(UserVar.Health, 100);
int myHp = vars.GetSelf<int>(UserVar.Health);

// 自分の値を購読（初期値 + 変更通知）
vars.AsObservableSelf<int>(UserVar.Health)
    .Subscribe(hp => healthBar.value = hp)
    .AddTo(this);
```

**ユーザー変数の監視 (全クライアント / 特定クライアント):**

```csharp
// 全クライアントの変更を監視
vars.AsObservableOnChanged<int>(UserVar.Score)
    .Subscribe(data => UpdateScoreboard(data.ClientNo, data.Value))
    .AddTo(this);

// 特定クライアントを購読（初期値 + 変更通知）
vars.AsObservable<int>(UserVar.Health, clientNo: 2)
    .Subscribe(hp => player2HealthBar.value = hp)
    .AddTo(this);
```

## 同梱シェーダー / マテリアル

`Runtime/Materials` に LBE (Location-Based Experience) 向けの汎用シェーダーが含まれています。

| シェーダー | 用途 |
|-----------|------|
| `NetSyncUtility/Occlusion` | 深度バッファにのみ書き込むオクルージョンシェーダー。現実空間の壁や床に配置し、仮想オブジェクトを遮蔽するために使用します。 |
| `NetSyncUtility/WarningWall` | 距離に応じてフェードする赤い格子パターンのシェーダー。プレイエリアの境界を視覚的に警告するために使用します。トライプラナー投影により、メッシュの向きに依存しないパターンを描画します。 |

## Editor ツール

### Project Validation Window の抑制

XR Hands / XR Interaction Toolkit のサンプルスクリプトには、Unity エディタ起動時に Project Settings ウィンドウを自動で開く処理が含まれています。Multiplayer Playmode のように複数インスタンスを起動する環境では特に邪魔になるため、メニューから抑制・復元できるようにしています。

| メニュー | 説明 |
|---------|------|
| **Tools > Suppress Project Validation Window** | 検証ウィンドウの自動表示を抑制する |
| **Tools > Restore Project Validation Window** | 元の動作に戻す |

### OpenXR ローダーの自動切り替え

`XRLoaderAutoConfigurator` は Scripting Define Symbols の `USE_OPENXR` の有無に応じて、OpenXR ローダーを自動的に有効化/無効化します。エディタ読み込み時およびビルド前処理で自動実行されるため、手動操作は不要です。

- `USE_OPENXR` 定義あり → OpenXR ローダーを有効化
- `USE_OPENXR` 定義なし → OpenXR ローダーを無効化

