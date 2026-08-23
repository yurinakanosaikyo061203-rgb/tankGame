using UnityEngine;
using TMPro;

public class TankUIManager : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private move2 tankScript;               // プレイヤーのmove2スクリプトをアタッチする

    [Header("左下常時UI")]
    [SerializeField] private TextMeshProUGUI speedAndGearText; // 速度とギアを表示するText

    [Header("MキーメニューUI")]
    [SerializeField] private GameObject statusMenuPanel;     // Mキーで開閉する親パネル
    [SerializeField] private TextMeshProUGUI specText;       // スペック表記用のText

    private void Start()
    {
        // プレイヤーの参照がなければ自動取得
        if (tankScript == null)
        {
            tankScript = GetComponent<move2>();
        }

        // メニューパネルを初期状態で非表示に
        if (statusMenuPanel != null)
        {
            statusMenuPanel.SetActive(false);
        }

        // スペックテキストの初期設定（War Thunderの車両カードを意識した固定情報）
        if (specText != null)
        {
            specText.text = "<b>【車両スペック】</b>\n" +
                            "■ 全備重量: 32.0 t\n" +
                            "■ エンジン出力: 500 hp\n" +
                            "■ 最高速度 (前進): 50 km/h\n" +
                            "■ 最高速度 (後進): 11 km/h";
        }
    }

    private void Update()
    {
        // 1. 左下の常時メーターUIの更新
        if (tankScript != null && speedAndGearText != null)
        {
            float kmh = Mathf.Abs(tankScript.CurrentForwardSpeed) * 3.6f;
            int displaySpeed = Mathf.RoundToInt(kmh);
            speedAndGearText.text = $"速度: {displaySpeed} km/h\nギア: {tankScript.CurrentGear}";
        }

        // 2. Mキーの開閉入力を監視
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleStatusMenu();
        }
    }

    private void ToggleStatusMenu()
    {
        if (statusMenuPanel != null)
        {
            bool isActive = !statusMenuPanel.activeSelf;
            statusMenuPanel.SetActive(isActive);

            // メニューが開いている間は、戦車側のスクリプトをフリーズ（移動停止）させる
            if (tankScript != null)
            {
                tankScript.SetFreeze(isActive);
            }
        }
    }
}
