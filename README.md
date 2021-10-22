# SimpleVoiceroid2Proxy

VOICEROID2 を HTTP API で操作して読み上げさせるコンソールアプリケーションです。(要管理者権限)

[Voiceroid2Proxy](https://github.com/kanosaki/Voiceroid2Proxy) を基に機能拡張を行いました。

![main](https://github.com/SlashNephy/SimpleVoiceroid2Proxy/blob/master/Images/main.png?raw=true)

## ACT.Hojoring との連携

[ACT.Hojoring](https://github.com/anoyetta/ACT.Hojoring) で棒読みちゃんの代わりに使用することで **低遅延** **UI 妨害のない** 読み上げが可能です。  
FFXIV プレイ中に読み上げが行われてもウィンドウが前面に出現しないため, 操作がブロックされることはありません。

![act](https://github.com/SlashNephy/SimpleVoiceroid2Proxy/blob/master/Images/act.png?raw=true)

## HTTP API

現在実装されている HTTP API は以下の通りです。  
LAN に `4532/tcp` を開放するので同一ネットワーク内から操作が可能です。

- `GET /talk`  
    パラメータ `text` を渡すことで読み上げを行えます。ただし `GET` リクエストは URL の長さの制約を受けるので `POST` を推奨します。(ACT.Hojoring との互換性のため `GET` 対応しています)

    「てすと」を読み上げる例:
    ```
    http://localhost:4532/talk?text=%E3%81%A6%E3%81%99%E3%81%A8
    ```

- `POST /talk`  
    ペイロードとして JSON を送信することで読み上げを行えます。

    「てすと」を読み上げる例:
    ```json
    {
        "text": "てすと"
    }
    ```

## コマンド

特定の文字列を `text` に含めることで特殊な操作を行えます。

- `結月ゆかり＞`  
    話者名を指定することで特定の話者に読み上げさせることが可能です。(VOICEROID2 の機能で、VOICEROID2 側の設定から記号を変更できます)  
    他の話者も指定可能です。  
    この指定は `text` の先頭で行う必要があります。

- `<clear>`  
    読み上げのキューをクリアします。

- `<pause>`, `<resume>`  
    現在の読み上げを 一時停止 / 再開 します。

- `<interrupt_enable>`, `<interrupt_disable>`  
    読み上げの割り込みモードを 有効化 / 無効化 します。  
    デフォルトでは割り込み (読み上げ中に別のテキストを受け取ると中断し, 新しいテキストを読み上げます) が有効になっています。
