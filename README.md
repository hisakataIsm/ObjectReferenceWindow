# ObjectReferenceWindow
UnityのObjectがもつ参照ファイルを表示するウインドウ
It is a window that displays the reference of Unity Object.

    # 起動方法
    Ism > ObjectReferenceWindow

    # 使用方法
    1.Objectに参照を調べたいObjectを入れる。
    2.入れると「Search」と「Clipboard」と「UseGit」が表示される
        Search:     参照を検索します。
        Clipboard:  検索結果をクリップボードにコピーします。
                    Eexelやspreadsheetで読みやすいフォーマットにしています。
        UseGit:     Gitログを取得して表示します。

    # Unity
    Unity2018.3.8f1で動作確認しました。

    # C#
    C#6以上で動作します。
    Edit > PlayerSettings... > OtherSettings > Scripting Runtime Version
    を「.NET 4.x Equivalent」にする。
    C#4で使用したい場合は、System.Actionのnullチェックを入れる。

        C#6
        onComplete?.Invoke(json);
        C#4
        if(onComplete != null) onComplete.Invoke(json);

    # EditorCoroutines
    エディタ用のコルーチンを使用するためにEditorCoroutinesをInstallしています。
        1.Window > PackageManager
        2.Advanced > Show preview packages > EditorCoroutines
        3.Install *バージョンは0.0.2で確認しました。
