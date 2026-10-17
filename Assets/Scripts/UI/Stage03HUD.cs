using TMPro;
using UnityEngine;

public sealed class Stage03HUD : MonoBehaviour
{
    [SerializeField] private TMP_Text goalLabel;
    [SerializeField] private PressurePlate westernPlate;
    [SerializeField] private PressurePlate easternPlate;
    [SerializeField] private StageGate westernGate;
    [SerializeField] private StageGate easternGate;
    [SerializeField] private RotationModule rotor;
    [SerializeField] private ShadowController shadow;
    [SerializeField] private StageExit exit;

    public void Configure(TMP_Text label, PressurePlate plateA, PressurePlate plateB,
        StageGate gateA, StageGate gateB, RotationModule rotation,
        ShadowController shadowController, StageExit stageExit)
    {
        goalLabel = label;
        westernPlate = plateA;
        easternPlate = plateB;
        westernGate = gateA;
        easternGate = gateB;
        rotor = rotation;
        shadow = shadowController;
        exit = stageExit;
    }

    private void Update()
    {
        if (goalLabel == null) return;
        if (exit != null && exit.IsComplete)
            goalLabel.text = "第三关完成 · 你和影子走过了同一片庭院";
        else if (westernGate != null && !westernGate.IsOpen)
            goalLabel.text = shadow != null && shadow.HasShadow
                ? "沿共用格子移动，让影子踏上西庭的青色踏板 ①"
                : "按住 F 查看可投放点，松开 F 将影子放到西庭的青色格子";
        else if (easternGate != null && !easternGate.IsOpen)
            goalLabel.text = shadow != null && shadow.HasShadow
                ? "第二庭院：引导影子踏上北侧青色踏板 ②"
                : "第二庭院：在青色格子重新投放影子，向北引导它";
        else if (rotor != null && rotor.CurrentIndex == 0)
            goalLabel.text = "南侧门已开 · 沿东庭外缘进入右下角南廊\n"
                + "红色陷阱 [E] 可清巡逻者 · 旋桥 [E] 通向出口";
        else
            goalLabel.text = "旋桥已转向 · 沿接通的格子走向北侧出口";
    }
}
